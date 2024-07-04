using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.GamePathing.NavGraphs;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCreatingCore.GamePathing
{
    public class NoEnemyGamePather : IGamePathSolver {

		private readonly bool inflateObstacles;
		private StaticNavGraph? staticNavGraph;

		public NoEnemyGamePather(bool inflateObstacles, StaticNavGraph? staticNavGraph = null) {
			this.inflateObstacles = inflateObstacles;
			this.staticNavGraph = staticNavGraph;
		}

		public List<IGameAction>? GetPath(
			StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation) {

			//only possible, because we do not count in the enemy viewcones
			//when we do count them in, we have to compute them based on the actual
			//obstacles, not the inflated ones.
			if(inflateObstacles) {
				levelRepresentation = ObstaclesInflator.InflateAllInLevel(
					levelRepresentation, staticGameRepresentation.StaticMovementSettings);
			}

			var graph = staticNavGraph ?? new StaticNavGraph(levelRepresentation, false).Initialized();

			var points = graph.GetEnemylessPlayerPath(levelRepresentation.FriendlyStartPos);

			var movS = staticGameRepresentation.PlayerSettings.movementRepresentation;
			return points?
				.Select(p => new OnlyWalkAction(movS, p, null))
				.Cast<IGameAction>()
				.ToList();
		}
		
		public List<List<IGameAction>>? GetFullPathsTree(
			StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation) {
			var p = GetPath(staticGameRepresentation, levelRepresentation);
			if(p == null)
				return null;
			return new List<List<IGameAction>>() { p };
		}
	}
} 
