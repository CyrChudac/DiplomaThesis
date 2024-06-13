using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.GamePathing.NavGraphs;
using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using UnityEngine;
using System.Net.Http.Headers;
using GameCreatingCore.StaticSettings;

namespace GameCreatingCore.GamePathing
{
    public class GameSimulator{
		private readonly StaticGameRepresentation staticGameRepresentation;
		private readonly LevelRepresentation level;
		public GameSimulator(StaticGameRepresentation staticGameRepresentation, LevelRepresentation level) {
			this.staticGameRepresentation = staticGameRepresentation;
			this.level = level;
		}
		public GameSimulator Initialized(float viewconeLengthModifier, 
			LevelState? current = null, StaticNavGraph? navGraph = null) {
			current = current ?? LevelState.GetInitialLevelState(level, viewconeLengthModifier);
			navGraph = navGraph ?? new StaticNavGraph(level, true).Initialized();
			return Initialized(current, navGraph);

		}
		public GameSimulator Initialized(LevelState current, StaticNavGraph navGraph) {
			for(int i = 0; i < current.enemyStates.Count; i++) {
				if(current.enemyStates[i].PathIndex.HasValue && level.Enemies[i].Path != null) {
					level.Enemies[i].Path!.Commands[current.enemyStates[i].PathIndex!.Value].SetAction(
						enemyIndex: i, 
						currentPos: level.Enemies[i].Position, 
						staticGameRepresentation: staticGameRepresentation,
						navGraph: navGraph,
						level: level,
						backwards: false);
				}
			}
			return this;
		}

		private IGameAction voidAction = new StartAfterAction(enemyIndex: null, float.PositiveInfinity);

		public LevelState Simulate(LevelStateTimed current, ViewconeNavGraph navGraph,
			List<IGameAction> playerActions) {

			var lt = current.Time;
			var playerStates = new List<LevelStateTimed>() { current };

			IGameAction? beingPerformed = null;
			bool shouldBreak = false;
			IEnumerable<IGameAction> actions = Enumerable.Empty<IGameAction>();
			if(current.playerState.PerformingAction != null) {
				actions = actions.Append(current.playerState.PerformingAction);
			}
			//we need to add a void action in case the player does not do anything
			actions = actions.Concat(playerActions).Append(voidAction);
			foreach(IGameAction pa in actions) {
				var curr = pa!.CharacterActionPhase(playerStates.Last());
				var usedTime = lt - curr.Time;
				var enemChanged = EnemiesUpdate(curr, level, navGraph.staticNavGraph, usedTime);

				for(int i = 0; i < enemChanged.skillsInAction.Count; i++) {
					var a = enemChanged.skillsInAction[i];
					enemChanged = a.AutonomousActionPhase(new LevelStateTimed(enemChanged, usedTime));
				}
				var skillsInAction = enemChanged.skillsInAction.Where(p => !p.Done).ToList();
				curr = new LevelStateTimed(enemChanged.Change(skillsInAction: skillsInAction), curr.Time);
				lt = curr.Time;
				if(lt <= 0) {
					if(!pa.IsCancelable)
						beingPerformed = pa;
					shouldBreak = true;
				} else {
					if(pa.IsIndependentOfCharacter) {
						var skillsToPerform = current.skillsInAction.ToList();
						skillsToPerform.Add(pa);
						curr = new LevelStateTimed(curr.Change(skillsInAction: skillsToPerform), curr.Time);
					}
				}
				playerStates.Add(curr);
				if(shouldBreak || IsWin(curr))
					break;
			}
			return ChangeAlertingTimes(current, staticGameRepresentation, navGraph, level, playerStates);
		}

		bool IsWin(LevelState state)
				=> (state.playerState.Position - level.Goal.Position).sqrMagnitude < level.Goal.Radius * level.Goal.Radius;

		LevelState ChangeAlertingTimes(LevelStateTimed startingState, 
			StaticGameRepresentation staticGameRepresentation,
			IViewconesBearer viewconesBearer,
			LevelRepresentation level, IEnumerable<LevelStateTimed> states) {

			//after the actions go, we reduce and increase the alerting times of enemies,
			//that no longer see the player
			//we assume, that if it was changed by an ability, we should not change it here as well

			var lastState = states.Last();
			var enems = lastState.enemyStates.ToList();

			var viewcones = states
				.Select(s => (State: s, Viewcones: viewconesBearer.GetRawViewcones(s)))
				.ToList();


			for(int i = 0; i < level.Enemies.Count; i++) {
				var val = startingState.enemyStates[i].TimeOfPlayerInView;
				if(val != lastState.enemyStates[i].TimeOfPlayerInView)
					continue;
				bool alerted = false;
				var settings = staticGameRepresentation.GetEnemySettings(level.Enemies[i].Type);
				var fullTimeAlerting = settings.viewconeRepresentation.AlertingTimeModifier 
					* staticGameRepresentation.GameDifficulty;
				var fullTimeDisAlerting = settings.viewconeRepresentation.DisalertingTimeModifier 
					* staticGameRepresentation.GameDifficulty;

				var previous = startingState;
				foreach(var s in viewcones) {
					var e = s.State.enemyStates[i];
					if(!e.Alive)
						break;
					var isIn = Obstacle.ContainsPoint(s.State.playerState.Position, s.Viewcones[i]);
					var time = previous.Time - s.State.Time; //this makes it highly dependent on the time step!
					if(isIn) {
						val = Math.Min(1, val + time / fullTimeAlerting);
					} else {
						val = Math.Max(0, val - time / fullTimeDisAlerting);
					}
					var dist = Vector2.Distance(e.Position, s.State.playerState.Position);
					alerted = alerted || settings.viewconeRepresentation.Length * val > dist;
					previous = s.State;
				}
				enems[i] = enems[i].Change(alerted: alerted, timeOfPlayerInView: alerted ? 1 : val);
			}

			return lastState.Change(enemyStates: enems);
		}

		private LevelState EnemiesUpdate(LevelState current, LevelRepresentation level, 
			StaticNavGraph staticNavGraph, float time) {
			for(int i = 0; i < level.Enemies.Count; i++) {
				current = EnemyUpdate(current, level, staticNavGraph, time, i);
			}
			return current;
		}

		private LevelState EnemyUpdate(LevelState current, LevelRepresentation level, 
			StaticNavGraph staticNavGraph, float time, int enemyIndex) {
			if(!current.enemyStates[enemyIndex].Alive)
				return current;
			IGameAction? perf;
			if((perf = current.enemyStates[enemyIndex].PerformingAction) != null){
				var res = perf.CharacterActionPhase(new LevelStateTimed(current, time));
				time = res.Time;
				current = res;
				if(perf.IsIndependentOfCharacter) {
					var sia = current.skillsInAction.ToList();
					sia.Add(perf);
					current = current.Change(skillsInAction: sia);
				}
			}
			if(level.Enemies[enemyIndex].Path == null || !current.enemyStates[enemyIndex].PathIndex.HasValue)
				return current;
			var index = current.enemyStates[enemyIndex].PathIndex!.Value;
			var result = new LevelStateTimed(current, time);
			int iterations = 0;
			while(true) {
				var currAction = level.Enemies[enemyIndex].Path!.Commands[index].Action;
				result = currAction.CharacterActionPhase(result);
				if(result.Time > 0) {
					if(currAction.IsIndependentOfCharacter) {
						var sia = result.skillsInAction.ToList();
						sia.Add(currAction);
						result = new LevelStateTimed(result.Change(skillsInAction: sia), result.Time);
					}
					result = ChangePathIndex(ref index, result, enemyIndex, level);
					level.Enemies[enemyIndex].Path!.Commands[index].SetAction(
						enemyIndex,
						result.enemyStates[enemyIndex].Position,
						staticGameRepresentation,
						staticNavGraph,
						level,
						result.enemyStates[enemyIndex].IsReturning
					);
				} else {
					break;
				}
				if(iterations++ >= Math.Max(10, time * 10)) {
					throw new Exception($"Too many iterations for enemy action in a given time window.");
				}
			}
			result = new LevelStateTimed(result.ChangeEnemy(enemyIndex, pathIndex: index), result.Time);
			return result;
		}

		private LevelStateTimed ChangePathIndex(ref int index, LevelStateTimed state, 
			int enemyIndex, LevelRepresentation level) {

			if(state.enemyStates[enemyIndex].IsReturning) {
				index--;
				if(index < 0) {
					index = level.Enemies[enemyIndex].Path!.Commands.Count > 1 ? 1 : 0;
					state = new LevelStateTimed(state.ChangeEnemy(enemyIndex, isReturning: false), state.Time);
				}
			} else {
				index++;
				if(index == level.Enemies[enemyIndex].Path!.Commands.Count) {
					if(level.Enemies[enemyIndex].Path!.Cyclic)
						index = 0;
					else {
						index = Math.Max(0, index - 2);
						state = new LevelStateTimed(state.ChangeEnemy(enemyIndex, isReturning: true), state.Time);
					}
				}
			}
			return state;
		}


		class ToSimulate {
			public readonly IReadOnlyList<int> pathedEnemies;
			public readonly IReadOnlyList<int> viewingEnemies;
			public ToSimulate(IReadOnlyList<int> pathedEnemies, IReadOnlyList<int> viewingEnemies) {
				this.pathedEnemies = pathedEnemies;
				this.viewingEnemies = viewingEnemies;
			}
		}
	}
}
