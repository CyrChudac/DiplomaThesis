using GameCreatingCore.LevelRepresentationData;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class UnityObstacle
{
	[SerializeField]
	public List<Vector2> Shape;
	[SerializeField]
	public WalkObstacleEffect FriendlyWalkEffect = WalkObstacleEffect.Unwalkable;
	[SerializeField]
	public WalkObstacleEffect EnemyWalkEffect = WalkObstacleEffect.Unwalkable;
	[SerializeField]
	public VisionObstacleEffect FriendlyVisionEffect = VisionObstacleEffect.NonSeeThrough;
	[SerializeField]
	public VisionObstacleEffect EnemyVisionEffect = VisionObstacleEffect.NonSeeThrough;

	
	public UnityObstacle(List<Vector2> shape, WalkObstacleEffect friendlyWalkEffect, WalkObstacleEffect enemyWalkEffect, VisionObstacleEffect friendlyVisionEffect, VisionObstacleEffect enemyVisionEffect) {
		Shape = shape;
		FriendlyWalkEffect = friendlyWalkEffect;
		EnemyWalkEffect = enemyWalkEffect;
		FriendlyVisionEffect = friendlyVisionEffect;
		EnemyVisionEffect = enemyVisionEffect;
	}

	public Obstacle ToObstacle()
		=> new Obstacle(Shape.AsReadOnly(), 
			new ObstacleEffect(FriendlyWalkEffect, EnemyWalkEffect, FriendlyVisionEffect, EnemyVisionEffect));

	public static UnityObstacle FromObstacle(Obstacle obstacle)
		=> new UnityObstacle(
			obstacle.Shape.ToList(), 
			obstacle.Effects.FriendlyWalkEffect, 
			obstacle.Effects.EnemyWalkEffect, 
			obstacle.Effects.FriendlyVisionEffect, 
			obstacle.Effects.EnemyVisionEffect);
}
