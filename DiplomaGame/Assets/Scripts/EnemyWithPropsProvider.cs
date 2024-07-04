using GameCreatingCore;
using GameCreatingCore.StaticSettings;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyWithPropsProvider : EnemyProvider
{
    [SerializeField]
    private GameController gameController;
	[SerializeField]
	private EnemyController enemyPrefab;
	[SerializeField]
	private GameRunner gameRunner;
	[SerializeField]
	private Camera camera;
	[SerializeField]
	private ViewconesManager viewconesManager;
	[SerializeField]
	private List<int> enemyViewsOn;

	//TODO: add alert behaviour settings
	public override EnemyController GetEnemy(EnemyType type) {
		var settings = gameController.GetStaticGameRepr().GetEnemySettings(type);
		var e = Instantiate(enemyPrefab);
		SetViewconeSettings(e, settings.viewconeRepresentation);
		gameRunner.enemies.Add(e);
		e.killer.camera = camera;
		return e;
	}

	private void SetViewconeSettings(EnemyController e, ViewconeRepresentation viewRepr) {
		var vc = e.viewcone;
		viewconesManager.Enemies.Add(e);
		vc.SetViewAngle(viewRepr.Angle);
		vc.viewLength = viewRepr.Length;
		vc.displayViewcone = enemyViewsOn.Contains(gameController.levelsDone);
		vc.timeUntilFullView = viewRepr.AlertingTimeModifier * gameController.GameDifficulty;
	}
}
