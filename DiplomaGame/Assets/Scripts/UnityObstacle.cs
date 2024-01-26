using GameCreatingCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class UnityObstacle
{
	[SerializeField]
	public List<Vector2> Shape;
	[SerializeField]
	public WalkObstacleEffect FriendlyWalkEffect;
	[SerializeField]
	public WalkObstacleEffect EnemyWalkEffect;
	[SerializeField]
	public VisionObstacleEffect FriendlyVisionEffect;
	[SerializeField]
	public VisionObstacleEffect EnemyVisionEffect;

	
	public UnityObstacle(List<Vector2> shape, WalkObstacleEffect friendlyWalkEffect, WalkObstacleEffect enemyWalkEffect, VisionObstacleEffect friendlyVisionEffect, VisionObstacleEffect enemyVisionEffect) {
		Shape = shape;
		FriendlyWalkEffect = friendlyWalkEffect;
		EnemyWalkEffect = enemyWalkEffect;
		FriendlyVisionEffect = friendlyVisionEffect;
		EnemyVisionEffect = enemyVisionEffect;
	}

	public Obstacle ToObstacle()
		=> new Obstacle(Shape.AsReadOnly(), FriendlyWalkEffect, EnemyWalkEffect, FriendlyVisionEffect, EnemyVisionEffect);

	public static UnityObstacle FromObstacle(Obstacle obstacle)
		=> new UnityObstacle(
			obstacle.Shape.ToList(), 
			obstacle.FriendlyWalkEffect, 
			obstacle.EnemyWalkEffect, 
			obstacle.FriendlyVisionEffect, 
			obstacle.EnemyVisionEffect);
}
