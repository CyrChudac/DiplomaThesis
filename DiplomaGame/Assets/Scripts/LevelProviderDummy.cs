using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using GameCreatingCore;
using GameCreatingCore.StaticSettings;
using GameCreatingCore.GamePathing;
using GameCreatingCore.GamePathing.GameActions;

public class LevelProviderDummy : LevelProvider
{
	[SerializeField] private UnityStaticGameRepresentation staticGameRepresentation;
	[SerializeField] private bool includeEnemyKill;

	protected override LevelRepresentation GetLevelInner(bool vocal) {
		var availables = new List<IActiveGameActionProvider>();
		if(includeEnemyKill) {
			availables.Add(new KillActionProvider(0.5f, 3));
		}
		return new LevelRepresentation(
			new List<Obstacle>() {
				new Obstacle(
					PointsFromRect(new Rect(-5, -5, 10, 10)),
					new ObstacleEffect(
						WalkObstacleEffect.Unwalkable,
						WalkObstacleEffect.Unwalkable,
						VisionObstacleEffect.NonSeeThrough,
						VisionObstacleEffect.NonSeeThrough))
			},
			new Obstacle(
				PointsFromRect(new Rect(-20, -20, 40, 40)),
				new ObstacleEffect(
					WalkObstacleEffect.Unwalkable,
					WalkObstacleEffect.Unwalkable,
					VisionObstacleEffect.NonSeeThrough,
					VisionObstacleEffect.NonSeeThrough)),
			new List<Enemy>() {
				new Enemy(
					new Vector2(17, -17),
					90,
					EnemyType.Basic,
					null),
				new Enemy(
					new Vector2(-17, 17),
					180,
					EnemyType.Basic,
					new Path(false, new List<GameCreatingCore.GamePathing.PatrolCommand>() {
						new OnlyWalkCommand(new Vector2(17, 16), 
							false, GameCreatingCore.GamePathing.GameActions.TurnSideEnum.ShortestPrefereClockwise),
						new OnlyWalkCommand(new Vector2(-17, 17), 
							false, GameCreatingCore.GamePathing.GameActions.TurnSideEnum.ShortestPrefereClockwise)
					})
				)
			},
			new List<(GameCreatingCore.GamePathing.IActiveGameActionProvider, Vector2)>(),
			availables,
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
		};
	}
}
