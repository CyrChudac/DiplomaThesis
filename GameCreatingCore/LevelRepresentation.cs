using UnityEngine;
using System.Collections.Generic;

namespace GameCreatingCore {
	
	[System.Serializable]
	public sealed class LevelRepresentation {
		[SerializeField]
		public List<Obstacle> Obstacles;
		/// <summary>
		/// The Outer walls of the environment.
		/// </summary>
		[SerializeField]
		public Obstacle OuterObstacle;
		[SerializeField]
		public List<Enemy> Enemies;
		[SerializeField]
		public Vector2 FriendlyStartPos;
		[SerializeField]
		public LevelGoal Goal;

		public LevelRepresentation(List<Obstacle> obstacles, Obstacle outerObstacle, List<Enemy> enemies, Vector2 friendlyStartPos, LevelGoal goal) {
			Obstacles = obstacles;
			OuterObstacle = outerObstacle;
			Enemies = enemies;
			FriendlyStartPos = friendlyStartPos;
			Goal = goal;
		}
	}
}