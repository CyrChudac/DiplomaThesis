using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.GamePathing.NavGraphs;
using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing {
	public class FullGamePather : IGamePathSolver {
		readonly int innerViewconeRayCount;
		readonly float viewconesGraphDistances;
		readonly float timestepSize;
		readonly float maximumLevelTime;
		readonly int skillUsePointsCount;
		readonly bool inflateObstacles;
		StaticNavGraph? staticGraph;
		
		readonly int maxIterations;
		public FullGamePather(int innerViewconeRayCount, float viewconesGraphDistances,
			float timestepSize, float maximumLevelTime, int skillUsePointsCount,
			int maxIterations, 
			StaticNavGraph? staticGraph = null, bool inflateObstacles = true) {

			this.innerViewconeRayCount = innerViewconeRayCount;
			this.viewconesGraphDistances = viewconesGraphDistances;
			this.timestepSize = timestepSize;
			this.maximumLevelTime = maximumLevelTime;
			this.skillUsePointsCount = skillUsePointsCount;
			this.maxIterations = maxIterations;
			this.staticGraph = staticGraph;
			this.inflateObstacles = inflateObstacles;
		}

		public List<IGameAction>? GetPath(
			StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation) {
			var tree = GetPathOrTree(staticGameRepresentation, levelRepresentation, true);
			if(tree == null)
				return null;
			return tree[0];
		}

		public List<List<IGameAction>>? GetFullPathsTree(
			StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation) {
			return GetPathOrTree(staticGameRepresentation, levelRepresentation, false);
		}
		
		private List<List<IGameAction>>? GetPathOrTree(
			StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation,
			bool path) {
			//we enlarge obstacles so that characters do not go too near to them
			if(inflateObstacles)
				levelRepresentation = ObstaclesInflator.InflateAllInLevel(levelRepresentation,
					staticGameRepresentation.StaticMovementSettings);
			staticGraph = staticGraph ?? new StaticNavGraph(levelRepresentation, true).Initialized();

			var pathCheck = new NoEnemyGamePather(false, staticGraph).GetPath(staticGameRepresentation, levelRepresentation);
			//if there is no path within the obstacles, return null
			if(pathCheck == null)
				return null;
			else if(levelRepresentation.Enemies.Count == 0) {
				//if there are no enemies, we can just return the path
				return new List<List<IGameAction>>() { pathCheck };
			}

			var viewGraph = new ViewconeNavGraph(levelRepresentation, staticGraph, staticGameRepresentation,
				innerViewconeRayCount, viewconesGraphDistances, skillUsePointsCount);
			var initLevel = LevelState.GetInitialLevelState(levelRepresentation, staticGameRepresentation, staticGraph);

			Func<ScoredActionedNode, ScoreHolder?> f = n => {
				if(n.GoalScore != null)
					return n.GoalScore;
				else if(n.SkillUseScore != null)
					return new ScoreHolder(n.SkillUseScore.Previous, n.SkillUseScore.Score * 2);
				else if(n.SkillPickupScore != null)
					return new ScoreHolder(n.SkillPickupScore.Previous, n.SkillPickupScore.Score * 4);
				return null;
			};
			var both = GetPathOrTreeRecurseInit(staticGameRepresentation, staticGraph, initLevel, viewGraph, levelRepresentation, f);
			List<List<IGameAction>> result;
			if(path) {
				if(both.Winning == null)
					return null;
				result = new List<List<IGameAction>>() {
					GetActionsPath(both.Winning)
				};
			} else {
				 result = both.All
					.Select(x => GetActionsPath(x))
					.ToList();
			}
			foreach(var r in result) {
				foreach(var a in r) {
					a.Reset();
				}
			}
			return result;
		}

		private (StateNode? Winning, List<StateNode> All) GetPathOrTreeRecurseInit(StaticGameRepresentation staticGameRepresentation,
			StaticNavGraph staticGraph,
			LevelState initLevel,
			ViewconeNavGraph viewGraph,
			LevelRepresentation levelRepresentation,
			Func<ScoredActionedNode, ScoreHolder?> scoreHolderFunction) { 

			var playerMovementSettings = staticGameRepresentation.PlayerSettings.movementRepresentation;

			var simulator = new GameSimulator(staticGameRepresentation, levelRepresentation, staticGraph);

			var startViewGraph = viewGraph.GetScoredNavGraph(initLevel);

			var firstNode = new StateNode(initLevel, null, 0, new List<IGameAction>(), 
				GetPossibleActions(startViewGraph, staticGraph,
					initLevel.playerState.Position, playerMovementSettings, 
					scoreHolderFunction));

			bool hasEnemyMovement = levelRepresentation.Enemies.Select(e => e.Path).Where(p => p != null).Any();
			int iterations = 0;
			var result = GetPartPathOrTree(playerMovementSettings, hasEnemyMovement, simulator, staticGraph, firstNode, viewGraph,
				levelRepresentation, scoreHolderFunction, ref iterations);
			if(result.Winning != null)
				result.AllButWin.Insert(0, result.Winning);
			result.AllButWin.RemoveAll(n => n.Previous?.Previous == null);
			return result;
		}

		private (StateNode? Winning, List<StateNode> AllButWin) GetPartPathOrTree(MovementSettingsProcessed playerMovementSettings,
			bool hasEnemyMovement,
			GameSimulator simulator,
			StaticNavGraph staticGraph,
			StateNode initNode,
			ViewconeNavGraph viewGraph,
			LevelRepresentation levelRepresentation,
			Func<ScoredActionedNode, ScoreHolder?> scoreHolderFunction,
			ref int iterations) {

			PriorityQueue<float, StateNode> que = new PriorityQueue<float, StateNode>();

			Func<ScoredActionedNode, float?> scoreFunction
				= (n) => scoreHolderFunction(n)?.Score;

			que.Enqueue(0, initNode);

			var timeDivisor = maximumLevelTime / timestepSize - 1;
			List<StateNode> deadEnds = new List<StateNode>();
			int enemDead = initNode.State.enemyStates.Where(e => !e.Alive).Count();
			int skillPicked = initNode.State.pickupableSkillsPicked.Where(p => p).Count();
			while(que.Any()) {
				var currNode = que.DequeueMin();
				try {
					if(enemDead != currNode.State.enemyStates.Where(e => !e.Alive).Count()
						|| skillPicked != currNode.State.pickupableSkillsPicked.Where(p => p).Count()) {

						var recurse = GetPartPathOrTree(playerMovementSettings, hasEnemyMovement, simulator,
							staticGraph, currNode, viewGraph, levelRepresentation, scoreHolderFunction, ref iterations);

						if(recurse.Winning != null) {
							return (recurse.Winning, que
								.Select(x => x.Item2)
								.Concat(recurse.AllButWin)
								.Concat(deadEnds)
								.ToList());
						} else {
							deadEnds.AddRange(recurse.AllButWin);
						}
					}
					if(levelRepresentation.Goal.IsAchieved(currNode.State)) {
						var res = new List<StateNode>();
						res.AddRange(que.Select(x => x.Item2));
						res.AddRange(deadEnds);
						return (currNode, res);
					}
					var newTime = currNode.Time + timestepSize;
					if(newTime > maximumLevelTime) {
						deadEnds.Add(currNode);
						continue;
					}
					if(iterations > maxIterations) {
						return (null, que
							.Append((-1, currNode))
							.Select(x => x.Item2)
							.ToList());
					}

					//this way we can wait for enemies to move
					if(hasEnemyMovement)
						currNode.PossibleActions.Add(new List<IGameAction>());
					int added = 0;
					foreach(var a in currNode.PossibleActions) {
						var newState = simulator.Simulate(
							new LevelStateTimed(currNode.State, timestepSize),
							viewGraph,
							a);
						if(IsLost(newState))
							continue;

						var thisTime = newTime;


						if(newState.playerState.PerformingAction != null) { 
							//scoring actions when player is forced to do this action anyway is quite useless,
							//so we perform it and then score with no downside
							var time = newState.playerState.PerformingAction.TimeUntilCancelable 
								//this is time unnoticable by the player and ensures the action finishing (floating point error)
								+ 0.0005f;
							newState = simulator.Simulate(
								new LevelStateTimed(newState, time), viewGraph, new List<IGameAction>());
							if(IsLost(newState))
								continue;

							thisTime += time;
						}
						var currGraph = viewGraph.GetScoredNavGraph(newState);
						var nodes = GetPossibleNodes(currGraph, staticGraph, newState.playerState.Position, scoreHolderFunction);
						if(nodes.Count == 0) {
							continue;
						}
						var nodes2 = nodes
							.Select(p => (p.Item2.Score + p.Item1, p.Item3)) //we compute the final score
							.OrderBy(x => x.Item1)
							.Take(2)	//we limit the branching factor, is this too much?
							.ToList(); 
						//TODO: is this the way to calculate score? not sure
						var score = thisTime * playerMovementSettings.WalkSpeed / timeDivisor +
							nodes2.Select(n => n.Item1).First(); //what if we are currently in a viewcone? fix!
						var actions = GetPossibleActions(nodes2.Select(x => x.Item2), 
							playerMovementSettings, n => scoreHolderFunction(n)?.GetPrevious(currGraph.vertices), currGraph);
						var node = new StateNode(newState, currNode, thisTime, a, actions);
						que.Enqueue(score, node); 
						added++;
					}
					if(added == 0) {
						deadEnds.Add(currNode);
					}
				}catch(Exception e) {
					throw new Exception($"An exception was thrown during simulation on time {currNode.Time} and iteration {iterations}.", e);
				}
				iterations++;
			}
			//the only way this happens is when maximum level time
			//is reached or no action available... so no path was found, hence we return null
			return (null, deadEnds);
		}



		private bool IsLost(LevelState state) => state.enemyStates.Any(e => e.Alerted);

		private 

		class StateNode {
			public LevelState State { get; }
			public StateNode? Previous { get; }
			public float Time { get; }
			public List<IGameAction> Actions { get; }
			public List<List<IGameAction>> PossibleActions { get; }

			public StateNode(LevelState state, StateNode? previous, float time, 
				List<IGameAction> actions, List<List<IGameAction>> possibleActions) {
				State = state;
				Previous = previous;
				Time = time;
				Actions = actions;
				PossibleActions = possibleActions;
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
				List<IGameAction> acts = new List<IGameAction>();
				foreach(var ga in nodes[i].Actions) {
					acts.Add(ga); //we also need the first nondone action, it was probably started as well
					if(!ga.Done)
						break;
				}
				var chainedAction = new ChainedAction(acts);
				result.Add(new TimeRestrictedAction(chainedAction, timeStep));
			}
			return result;
		}


		List<(float, ScoreHolder, ScoredActionedNode)> GetPossibleNodes(GraphWithViewcones graph, StaticNavGraph navGraph, 
			Vector2 from, Func<ScoredActionedNode, ScoreHolder?> holderFunc) {

			var viewconesAsObsts = graph.Viewcones
				.Select(v => v.Nodes.Select(x => x.Position).Append(v.InnerViewcone.StartPos).ToList())
				.ToList();

			var straight = new List<(float, ScoreHolder, ScoredActionedNode)>();

			foreach(var v in graph.vertices) {
				if(FloatEquality.AreEqual(from, v.Position)) {
					var h = holderFunc(v);
					if(h != null) {
						var dist = Vector2.Distance(from, v.Position);
						return new List<(float, ScoreHolder, ScoredActionedNode)>() { (dist, h, v) };
					}
				}
				if( navGraph.CanPlayerGetToStraight(from, v.Position))
					if(navGraph.CanGetToStraight(from, v.Position, viewconesAsObsts, true))
						if(!navGraph.IsLineInsideAnyObstacle(from, v.Position, viewconesAsObsts)) {
							//it is actually possible to get to this node
							var h = holderFunc(v);
							if(h != null) {
								var dist = Vector2.Distance(from, v.Position);
								straight.Add((dist, h, v));
							}
						}
			}
			return straight;
		}

		List<List<IGameAction>> GetPossibleActions(GraphWithViewcones graph, StaticNavGraph navGraph, Vector2 from, 
			MovementSettingsProcessed playerMovement, Func<ScoredActionedNode, ScoreHolder?> holderFunc)
			=>GetPossibleActions(
				GetPossibleNodes(graph, navGraph, from, holderFunc)
					.Select(x => x.Item3),
				playerMovement,
				n => holderFunc(n)!.GetPrevious(graph.vertices),
				graph);

		List<List<IGameAction>> GetPossibleActions(IEnumerable<ScoredActionedNode> possibleNodes, 
			MovementSettingsProcessed playerMovement,
			Func<ScoredActionedNode, ScoredActionedNode?> previousFunc,
			GraphWithViewcones graph) {

			List<List<IGameAction>> result = new List<List<IGameAction>>();
			foreach(var node in possibleNodes) {
				var list = new List<IGameAction>();
				ScoredActionedNode? n = node;
				list.Add(new OnlyWalkAction(playerMovement, n.Position, null, false));
				if(n.NodeAction != null) {
					list.Add(n.NodeAction);
				}
				ScoredActionedNode? previous = previousFunc(node);
				int counter = 0;
				while(previous != null) {
					var e = graph[n, previous];
					if(e == null)
						throw new Exception("There is no edge between node and its previous node."); 
					if((!e!.AlertingIncrease.HasValue) || FloatEquality.AreEqual(e!.AlertingIncrease!.Value, 0))
						list.Add(new OnlyWalkAction(playerMovement, previous.Position, null, false));
					else
						list.Add(new WalkThroughViewConePlayerAction(playerMovement, previous.Position, false));
					if(previous.NodeAction != null) {
						list.Add(previous.NodeAction);
					}
					n = previous;
					previous = previousFunc(previous);
					counter++;
					if(counter > 1000) {
						throw new Exception($"Nonfunctional path finding with {nameof(FullGamePather)}.");
					}
				}
				result.Add(list);
			}
			return result;
		}
	}
}
