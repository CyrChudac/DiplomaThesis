using GameCreatingCore;
using GameCreatingCore.StaticSettings;
using System.Collections;
using System.Collections.Generic;
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

	//TODO: add alert behaviour settings
	public override EnemyController GetEnemy(EnemyType type) {
		var settings = gameController.GetStaticGameRepr().GetEnemySettings(type);
		var e = Instantiate(enemyPrefab);
		SetViewconeSettings(e, settings.viewconeRepresentation);
		gameRunner.enemies.Add(e);
		return e;
	}

	private void SetViewconeSettings(EnemyController e, ViewconeRepresentation viewRepr) {
		var vc = e.viewcone;
		vc.SetViewAngle(viewRepr.Angle);
		vc.viewLength = viewRepr.Length;
		vc.timeUntilFullView = viewRepr.AlertingTimeModifier * gameController.GameDifficulty;
	}
}
