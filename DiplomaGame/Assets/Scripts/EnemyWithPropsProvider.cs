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

	//TODO: add alert behaviour settings
	public override PathedGameObject GetEnemy(EnemyType type, Path path) {
		var settings = gameController.GetStaticGameRepr().GetEnemySettings(type);
		var e = Instantiate(enemyPrefab);
		e.SetPath(path);
		SetMovementSettings(e, settings.movementRepresentation);
		SetViewconeSettings(e, settings.viewconeRepresentation);
		return e;
	}

	private void SetMovementSettings(EnemyController e, MovementSettingsProcessed movementRepr) {
		var na = e.GetComponentInChildren<NavMeshAgent>();
		na.speed = movementRepr.WalkSpeed;
	}

	private void SetViewconeSettings(EnemyController e, ViewconeRepresentation viewRepr) {
		var vc = e.GetComponentInChildren<ViewconeCreator>();
		vc.SetViewAngle(viewRepr.Angle);
		vc.viewLength = viewRepr.Length;
		var rm = e.GetComponentInChildren<RotationManager>();
		rm.angleDeviation = viewRepr.WiggleAngle;
		rm.deviationTime = viewRepr.WiggleTime;
	}
}
