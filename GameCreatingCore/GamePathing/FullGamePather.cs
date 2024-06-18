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
	public class FullGamePather : IGamePathSolver {
		readonly int innerViewconeRayCount;
		readonly float viewconesGraphDistances;
		readonly float timestepSize;
		readonly float maximumLevelTime;
		readonly int skillUsePointsCount;
		public FullGamePather(int innerViewconeRayCount,
			float viewconesGraphDistances, float timestepSize, float maximumLevelTime, int skillUsePointsCount) {

			this.innerViewconeRayCount = innerViewconeRayCount;
			this.viewconesGraphDistances = viewconesGraphDistances;
			this.timestepSize = timestepSize;
			this.maximumLevelTime = maximumLevelTime;
			this.skillUsePointsCount = skillUsePointsCount;
		}

		public List<IGameAction>? GetPath(
			StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation) {

			var pathCheck = new NoEnemyGamePather(true).GetPath(staticGameRepresentation, levelRepresentation);
			//if there is no path, return null
			if(pathCheck == null 
				//if there are no enemies, we can just return the path
				|| levelRepresentation.Enemies.Count == 0) {
				return pathCheck;
			}

			var staticGraph = new StaticNavGraph(levelRepresentation.Obstacles,
				levelRepresentation.OuterObstacle,
				levelRepresentation.Goal,
				true)
				.Initialized();
			var viewGraph = new ViewconeNavGraph(levelRepresentation, staticGraph, staticGameRepresentation,
				innerViewconeRayCount, viewconesGraphDistances, skillUsePointsCount);
			var initLevel = LevelState.GetInitialLevelState(levelRepresentation);

			var playerMovementSettings = staticGameRepresentation.PlayerSettings.movementRepresentation;

			var simulator = new GameSimulator(staticGameRepresentation, levelRepresentation)
				.Initialized(initLevel, staticGraph);
			PriorityQueue<float, StateNode> que = new PriorityQueue<float, StateNode>();
			que.Enqueue(0, new StateNode(initLevel, null, 0, new List<IGameAction>()));
			int iterations = 0;
			while(que.Any()) {
				var currNode = que.DequeueMin();
				try {
					if(Vector2.SqrMagnitude(currNode.State.playerState.Position - levelRepresentation.Goal.Position)
						< levelRepresentation.Goal.Radius * levelRepresentation.Goal.Radius) {
						return GetActionsPath(currNode);
					}
					var newTime = currNode.Time + timestepSize;
					if(newTime > maximumLevelTime)
						break;
					var currGraph = viewGraph.GetScoredNavGraph(currNode.State);
					var actions = GetPossibleActions(currGraph, staticGraph,
						playerMovementSettings, currNode.State.playerState.Position);
					foreach(var a in actions) {
						var newState = simulator.Simulate(
							new LevelStateTimed(currNode.State, timestepSize),
							viewGraph,
							a);
						//TODO: is this the way to calculate score? not sure
						var score = newTime * playerMovementSettings.WalkSpeed +
							Vector2.Distance(newState.playerState.Position, levelRepresentation.Goal.Position);
						que.Enqueue(score, new StateNode(newState, currNode, newTime, a));
					}
				}catch(Exception e) {
					throw new Exception($"An exception was thrown during simulation on time {currNode.Time} and iteration {iterations}.", e);
				}
				iterations++;
			}
			//there is always an action (unless the player cannot move at all)...
			//the only way this happens is when maximum level time
			//is reached, so no path was found, hence we return null
			return null;
		}

		class StateNode {
			public LevelState State { get; }
			public StateNode? Previous { get; }
			public float Time { get; }
			public List<IGameAction> Actions { get; }

			public StateNode(LevelState state, StateNode? previous, float time, List<IGameAction> actions) {
				State = state;
				Previous = previous;
				Time = time;
				Actions = actions;
			}
		}

		List<IGameAction> GetActionsPath(StateNode? finalNode){
			List<StateNode> nodes = new List<StateNode>();
			int iterations = 0;
			while(finalNode != null) {
				nodes.Add(finalNode);
				finalNode = finalNode.Previous;
                if(iterations++ > 1000)
                    throw new Exception("Possible infinite loop, investigate.");
			}
			nodes.Reverse();
			List<IGameAction> result = new List<IGameAction>();
			for(int i = 1; i < nodes.Count; i++) {
				var timeStep = nodes[i].Time - nodes[i - 1].Time;
				//those are all player actions, so we can chain them.
				var chainedAction = new ChainedAction(nodes[i].Actions);
				result.Add(new TimeRestrictedAction(chainedAction, timeStep));
			}
			return result;
		}


		List<List<IGameAction>> GetPossibleActions(GraphWithViewcones graph, StaticNavGraph navGraph, 
			MovementSettingsProcessed playerMovement, Vector2 from) {

			var viewconesAsObsts = graph.Viewcones
				.Select(v => v.Nodes.Select(n => n.Position).ToList())
				.ToList();

			var straight = new List<(float, ScoredActionedNode)>();

			foreach(var v in graph.vertices) {
				if(navGraph.CanPlayerGetToStraight(from, v.Position)
					&& navGraph.CanGetToStraight(from, v.Position, viewconesAsObsts)){
					var dist = Vector2.Distance(from, v.Position);
					straight.Add((dist, v));
				}
			}
			List<List<IGameAction>> result = new List<List<IGameAction>>();
			foreach(var node in straight.OrderBy(p => p.Item1 + p.Item2.Score).Select(p => p.Item2)) {
				var list = new List<IGameAction>();
				ScoredActionedNode? n = node;
				int counter = 0;
				while(n != null) {
					list.Add(new OnlyWalkAction(playerMovement, n.Position, null, false));
					if(n.NodeAction != null) {
						list.Add(n.NodeAction);
					}
					n = (ScoredActionedNode?)node.Previous;
					counter++;
					if(counter > graph.vertices.Count) {
						Console.WriteLine($"Nonfunctional path finding with {nameof(FullGamePather)}.");
						break;
					}
				}
				result.Add(list);
			}
			return result;
		}
	}
}
