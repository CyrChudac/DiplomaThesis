using UnityEngine;

namespace GameCreationCore;

public record Obstacle (
	List<Vector2> Shape,
	WalkObstacleEffect FriendlyWalkEffect,
	WalkObstacleEffect EnemyWalkEffect,
	VisionObstacleEffect FriendlyVisionEffect,
	VisionObstacleEffect EnemyVisionEffect
);