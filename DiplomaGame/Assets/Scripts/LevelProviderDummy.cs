using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using GameCreationCore;

public class LevelProviderDummy : LevelCreatorProvider, ILevelCreator
{
    public override ILevelCreator GetLevelCreator() => this;

	public LevelRepresentation CreateLevel(int seed) {
		return new LevelRepresentation(
			new List<Obstacle>() {
				new Obstacle(
					PointsFromRect(new Rect(-5, -5, 10, 10)),
					WalkObstacleEffect.Unwalkable,
					WalkObstacleEffect.Unwalkable,
					VisionObstacleEffect.SeeThrough,
					VisionObstacleEffect.NonSeeThrough)
			},
			new Obstacle(
				PointsFromRect(new Rect(-20, -20, 40, 40)),
				WalkObstacleEffect.Unwalkable,
				WalkObstacleEffect.Unwalkable,
				VisionObstacleEffect.SeeThrough,
				VisionObstacleEffect.NonSeeThrough),
			new List<Enemy>() {
				new Enemy(
					new Vector2(17, -17),
					180,
					EnemyType.Basic,
					null),
				new Enemy(
					new Vector2(-17, 17),
					180,
					EnemyType.Basic,
					new Path(false, new List<PatrolCommand>() {
						new OnlyWalkCommand(new Vector2(17, 17)),
						new OnlyWalkCommand(new Vector2(-17, 17))
					})
				)
			},
			new Vector2(-17, -17),
			new LevelGoal(
				new Vector2(17, 17),
				2)
		);
	}

	private List<Vector2> PointsFromRect(Rect rect) {
		return new List<Vector2>() {
			new Vector2(rect.x, rect.y),
			new Vector2(rect.xMax, rect.y),
			new Vector2(rect.xMax, rect.yMax),
			new Vector2(rect.x, rect.yMax),
			new Vector2(rect.x, rect.y),
		};
	}
}
