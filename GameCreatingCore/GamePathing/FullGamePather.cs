using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.GamePathing.NavGraphs;
using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing {
	internal class FullGamePather : IGamePathSolver{
		readonly float viewconeLengthModifier;
		readonly int innerViewconeRayCount;
		readonly float viewconesGraphDistances;
		readonly float timestepSize;
		readonly float maximumLevelTime;
		readonly int skillUsePointsCount;
		public FullGamePather(float viewconeLengthModifier, int innerViewconeRayCount, 
			float viewconesGraphDistances, float timestepSize, float maximumLevelTime, int skillUsePointsCount) {

			this.viewconeLengthModifier = viewconeLengthModifier;
			this.innerViewconeRayCount = innerViewconeRayCount;
			this.viewconesGraphDistances = viewconesGraphDistances;
			this.timestepSize = timestepSize;
			this.maximumLevelTime = maximumLevelTime;
			this.skillUsePointsCount = skillUsePointsCount;
		}

		public List<IGameAction>? GetPath(
			StaticGameRepresentation staticGameRepresentation, 
			LevelRepresentation levelRepresentation) {

			
			var staticGraph = new StaticNavGraph(levelRepresentation.Obstacles,
				levelRepresentation.OuterObstacle,
				levelRepresentation.Goal,
				false);
			var viewGraph = new ViewconeNavGraph(levelRepresentation, staticGraph, staticGameRepresentation,
				innerViewconeRayCount, viewconesGraphDistances, skillUsePointsCount);
			var initLevel = GetInitialLevelState(levelRepresentation);

			var playerMovementSettings = staticGameRepresentation.PlayerSettings.movementRepresentation;

			var simulator = new GameSimulator(staticGameRepresentation)
				.Initialized(initLevel, staticGraph, levelRepresentation);
			PriorityQueue<float, (float Time, LevelState state)> que 
				= new PriorityQueue<float, (float Time, LevelState state)>();
			que.Enqueue(0, (0, initLevel));
			while(que.Any()) {
				var currPair = que.DequeueMin();
				if(Vector2.SqrMagnitude(currPair.state.playerState.Position - levelRepresentation.Goal.Position) 
					< levelRepresentation.Goal.Radius * levelRepresentation.Goal.Radius) {
					//TODO: we found out the winnning actions, how to trace them back though?
				}
				var newTime = currPair.Time + timestepSize;
				if(newTime > maximumLevelTime)
					continue;
				var currGraph = viewGraph.GetScoredNavGraph(currPair.state);
				var actions = GetActionPaths(currGraph, staticGraph, 
					playerMovementSettings, currPair.state.playerState.Position);
				foreach(var a in actions) {
					var newState = simulator.Simulate(
						new LevelStateTimed(currPair.state, timestepSize), 
						levelRepresentation, 
						viewGraph, 
						a);
					//TODO: is this the way to calculate score? not sure
					var score = newTime + Vector2.Distance(newState.playerState.Position, levelRepresentation.Goal.Position);
					que.Enqueue(score, (newTime, newState));
				}
			}
			//there is always an action (unless the player cannot move at all)...
			//the only way this happens is when maximum level time
			//is reached, so no path was found, hence we return null
			return null;
		}

		List<List<IGameAction>> GetActionPaths(GraphWithViewcones graph, StaticNavGraph navGraph, 
			MovementSettingsProcessed playerMovement, Vector2 from) {
			var straight = new List<(float, ScoredActionedNode)>();

			foreach(var v in graph.vertices) {
				if(navGraph.CanPlayerGetToStraight(from, v.Position)){
					var dist = Vector2.Distance(from, v.Position);
					straight.Add((dist, v));
				}
			}
			List<List<IGameAction>> result = new List<List<IGameAction>>();
			foreach(var node in straight.OrderBy(p => p.Item1 + p.Item2.Score).Select(p => p.Item2)) {
				var list = new List<IGameAction>();
				ScoredActionedNode? n = node;
				while(n != null) {
					list.Add(new OnlyWalkAction(playerMovement, n.Position, null, false));
					if(n.NodeAction != null) {
						list.Add(n.NodeAction);
					}
					n = (ScoredActionedNode?)node.Previous;
				}
				result.Add(list);
			}
			return result;
		}


		LevelState GetInitialLevelState(LevelRepresentation levelRepresentation) {

			List<EnemyState> enemies = levelRepresentation.Enemies
				.Select(e => new EnemyState(e.Position, e.Rotation, null, true, false, viewconeLengthModifier, 0))
				.ToList();

			return new LevelState(
				enemyStates: enemies,
				playerState: new PlayerState(levelRepresentation.FriendlyStartPos, 0, 
					levelRepresentation.SkillsStartingWith, null),
				skillsInAction: new List<IGameAction>(),
				Enumerable.Repeat<bool>(false, levelRepresentation.SkillsToPickup.Count).ToList()
				);
		}
	}
}
