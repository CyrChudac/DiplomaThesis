using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;

public class ObstacleMaterial : ScriptableObject
{
	public WalkObstacleEffect FriendlyWalkEffect;
	public WalkObstacleEffect EnemyWalkEffect;

	public VisionObstacleEffect FriendlyVision;
	public VisionObstacleEffect EnemyVision;

	public Material Material;
}
