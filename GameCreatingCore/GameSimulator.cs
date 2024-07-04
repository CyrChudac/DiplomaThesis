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
		private StaticNavGraph StaticNavGraph { get; }
		public GameSimulator(StaticGameRepresentation staticGameRepresentation, LevelRepresentation level, StaticNavGraph staticNavGraph) {
			this.staticGameRepresentation = staticGameRepresentation;
			this.level = level;
			this.StaticNavGraph = staticNavGraph;
		}

		private IGameAction voidAction = new StartAfterAction(enemyIndex: null, float.PositiveInfinity);

		public LevelState Simulate(LevelStateTimed current, IViewconesBearer navGraph,
			List<IGameAction> playerActions) {

			//this way we don't change any actions in the instances of the previous LevelState
			current = new LevelStateTimed(current.DuplicateActions(), current.Time);  

			var lt = current.Time;

			bool shouldBreak = false;
			IEnumerable<IGameAction> actions = Enumerable.Empty<IGameAction>();
			if(current.playerState.PerformingAction != null) {
				actions = actions.Append(current.playerState.PerformingAction);
				current = new LevelStateTimed(current.ChangePlayer(performingActionToNull: true), current.Time);
			}
			var playerStates = new List<LevelStateTimed>() { current };
			//we need to add a void action in case the player does not do anything (to still activate the loop)
			actions = actions.Concat(playerActions).Append(voidAction);
			foreach(IGameAction pa in actions) {
				var curr = pa!.CharacterActionPhase(playerStates.Last());
				curr = UpdateAvailableSkills(curr);
				var usedTime = lt - curr.Time;
				var enemChanged = EnemiesUpdate(curr, level, StaticNavGraph, usedTime);

				for(int i = 0; i < enemChanged.skillsInAction.Count; i++) {
					var state = new LevelStateTimed(enemChanged, usedTime);
					enemChanged = enemChanged.skillsInAction[i].AutonomousActionPhase(state);
				}
				var skillsInAction = enemChanged.skillsInAction.Where(p => !p.Done).ToList();
				curr = new LevelStateTimed(enemChanged.Change(skillsInAction: skillsInAction), curr.Time);
				lt = curr.Time;
				if(FloatEquality.LessOrEqual(lt, 0)) {
					if(!pa.IsCancelable)
						curr = new LevelStateTimed(curr.ChangePlayer(performingAction: pa), 0);
					shouldBreak = true;
				} else {
					if(pa.IsIndependentOfCharacter) {
						var skillsToPerform = curr.skillsInAction.ToList();
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
				=> level.Goal.IsAchieved(state);

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
				var alertLengthMod = enems[i].ViewconeAlertLengthModifier;
				foreach(var s in viewcones) {
					var e = s.State.enemyStates[i];
					if(!e.Alive)
						break;
					string debugShapeString = string.Join(", ", s.Viewcones[i]);
					var isIn = Obstacle.ContainsPoint(s.State.playerState.Position, s.Viewcones[i], false);
					var time = previous.Time - s.State.Time; //this makes it highly dependent on the time step!
					if(isIn) {
						val = Math.Min(1, val + time / fullTimeAlerting);
					} else {
						val = Math.Max(0, val - time / fullTimeDisAlerting);
					}
					alerted = alerted || val == 1 ||
						FloatEquality.MoreOrEqual(settings.viewconeRepresentation.ComputeLength(alertLengthMod) * val, 
							Vector2.Distance(e.Position, s.State.playerState.Position));
					previous = s.State;
				}
				if(FloatEquality.AreEqual(val, 0))
					val = 0;
				if(alerted) {
					alertLengthMod += startingState.Time * settings.viewconeRepresentation.AlertLengthChangeSpeedIn;
				} else {
					alertLengthMod -= startingState.Time * settings.viewconeRepresentation.AlertLengthChangeSpeedOut;
				}
				alertLengthMod = Math.Clamp(alertLengthMod, 0, 1);
				enems[i] = enems[i].Change(alerted: alerted, 
					timeOfPlayerInView: val, 
					viewconeAlertLengthModifier: alertLengthMod);
			}

			return lastState.Change(enemyStates: enems);
		}

		LevelStateTimed UpdateAvailableSkills(LevelStateTimed state) {
			List<IActiveGameActionProvider> skills = new List<IActiveGameActionProvider>();
			bool changed = true;
			foreach(var s in state.playerState.AvailableSkills) {
				if(s.Uses == 0) {
					changed = true;
				} else {
					skills.Add(s);
				}
			}
			if(changed)
				return new LevelStateTimed(state.ChangePlayer(availableSkills: skills), state.Time);
			else
				return state;
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
				if(FloatEquality.AreEqual(time, 0)) {
					res = new LevelStateTimed(res.ChangeEnemy(enemyIndex, performingActionToNull: true), 0);
				}
				current = res;
				if(perf.IsIndependentOfCharacter) {
					var sia = current.skillsInAction.ToList();
					sia.Add(perf);
					current = current.Change(skillsInAction: sia);
				}
			}
			if(level.Enemies[enemyIndex].Path == null 
				|| (!current.enemyStates[enemyIndex].PathIndex.HasValue)
				|| (current.enemyStates[enemyIndex].CurrentPathAction == null))
				return current;
			var index = current.enemyStates[enemyIndex].PathIndex!.Value;
			var result = new LevelStateTimed(current, time);
			int iterations = 0;
			var currAction = current.enemyStates[enemyIndex].CurrentPathAction!;
			while(true) {
				result = currAction.CharacterActionPhase(result);
				if(result.Time > 0) {
					if(currAction.IsIndependentOfCharacter) {
						var sia = result.skillsInAction.ToList();
						sia.Add(currAction);
						result = new LevelStateTimed(result.Change(skillsInAction: sia), result.Time);
					}
					if(level.Enemies[enemyIndex].Path!.Commands.Count == 1) {
						currAction = null;
						break;
					}
					result = ChangePathIndex(ref index, result, enemyIndex, level);
					currAction = level.Enemies[enemyIndex].Path!.Commands[index].GetAction(
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
			result = new LevelStateTimed(result.ChangeEnemy(enemyIndex, pathIndex: index, removePathAction: currAction == null,
				currentPathAction: currAction), result.Time);
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
