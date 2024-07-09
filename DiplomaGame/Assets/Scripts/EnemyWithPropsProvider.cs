using GameCreatingCore;
using GameCreatingCore.StaticSettings;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using GameCreatingCore.LevelRepresentationData;

public class EnemyWithPropsProvider : EnemyProvider
{
    [SerializeField]
    private GameController gameController;
	[SerializeField]
	private EnemyObject enemyPrefab;
	[SerializeField]
	private GameRunner gameRunner;
	[SerializeField]
	private Camera cameraClicksAreFrom;
	[SerializeField]
	private ViewconesManager viewconesManager;
	[SerializeField]
	private List<int> enemyViewsOn;

	//TODO: add alert behaviour settings
	public override EnemyObject GetEnemy(EnemyType type) {
		var settings = gameController.GetStaticGameRepr().GetEnemySettings(type);
		var e = Instantiate(enemyPrefab);
		SetViewconeSettings(e, settings.viewconeRepresentation);
		gameRunner.enemies.Add(e);
		e.killer.camera = cameraClicksAreFrom;
		return e;
	}

	private void SetViewconeSettings(EnemyObject e, ViewconeRepresentation viewRepr) {
		var vc = e.viewcone;
		viewconesManager.Enemies.Add(e);
		vc.SetViewAngle(viewRepr.Angle);
		vc.viewLength = viewRepr.Length;
		vc.displayViewcone = enemyViewsOn.Contains(gameController.levelsDone);
		vc.timeUntilFullView = viewRepr.AlertingTimeModifier * gameController.GameDifficulty;
	}
}
