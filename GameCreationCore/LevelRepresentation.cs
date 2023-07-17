using UnityEngine;

namespace GameCreationCore; 
public record LevelRepresentation(
	List<Obstacle> Obstacles,
	List<Enemy> Enemies,
	Vector2 FriendlyStartPos,
	LevelGoal goal
);