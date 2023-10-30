using UnityEngine;

namespace GameCreationCore; 

/// <param name="OuterObstacle">The Outer walls of the environment.</param>
public record LevelRepresentation(
	List<Obstacle> Obstacles,
	Obstacle OuterObstacle,
	List<Enemy> Enemies,
	Vector2 FriendlyStartPos,
	LevelGoal Goal
);