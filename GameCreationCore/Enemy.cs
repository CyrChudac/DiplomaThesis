using UnityEngine;

namespace GameCreationCore;

public record Enemy (
	Vector2 Position,
	float Rotation,
	EnemyType Type,
	Path? Path
);