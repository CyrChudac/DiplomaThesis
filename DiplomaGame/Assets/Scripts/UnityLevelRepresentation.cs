using GameCreatingCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnityLevelRepresentation : ScriptableObject
{
	[SerializeField]
	public List<UnityObstacle> Obstacles;
	/// <summary>
	/// The Outer walls of the environment.
	/// </summary>
	[SerializeField]
	public UnityObstacle OuterObstacle;
	[SerializeField]
	public List<Enemy> Enemies;
	[SerializeField]
	public Vector2 FriendlyStartPos;
	[SerializeField]
	public LevelGoal Goal;

	public UnityLevelRepresentation(List<UnityObstacle> obstacles, UnityObstacle outerObstacle, List<Enemy> enemies, Vector2 friendlyStartPos, LevelGoal goal) {
		Obstacles = obstacles;
		OuterObstacle = outerObstacle;
		Enemies = enemies;
		FriendlyStartPos = friendlyStartPos;
		Goal = goal;
	}

	public LevelRepresentation GetLevelRepresentation() {
		return new LevelRepresentation(
			Obstacles.Select(o => o.ToObstacle()).ToList(),
			OuterObstacle.ToObstacle(),
			Enemies,
			FriendlyStartPos,
			Goal);
	}
}
