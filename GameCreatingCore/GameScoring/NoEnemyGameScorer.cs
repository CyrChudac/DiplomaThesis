using GameCreatingCore.GameScoring.NavGraphs;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameCreatingCore.GameScoring {
	public class NoEnemyGameScorer : IGameScorer {
		public float Score(
			StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation) {

			var graph = new StaticNavGraph(levelRepresentation.Obstacles,
				levelRepresentation.OuterObstacle,
				levelRepresentation.Goal,
				false);

			var obsts = levelRepresentation.Obstacles
				.Where(o => o.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable)
				.ToList();

			var points = graph.PlayerStaticNavmesh.GetPath(
				levelRepresentation.FriendlyStartPos, 
				(f, s) => graph.CanGetToStraight(f, s, obsts));

			return Math.Min(1, points.Count / 100.0f);	
		}
	}
} 
