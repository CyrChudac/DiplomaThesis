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


		public NoEnemyGamePather(bool inflateObstacles) {
			this.inflateObstacles = inflateObstacles;
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

			var graph = new StaticNavGraph(levelRepresentation, false).Initialized();

			var points = graph.GetEnemylessPlayerPath(levelRepresentation.FriendlyStartPos);

			var movS = staticGameRepresentation.PlayerSettings.movementRepresentation;
			return points?
				.Select(p => new OnlyWalkAction(movS, p, null))
				.Cast<IGameAction>()
				.ToList();
		}
	}
} 
