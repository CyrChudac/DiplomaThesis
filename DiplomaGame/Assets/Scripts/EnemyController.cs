using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : PathedGameObject
{
    [SerializeField]
    private EnemyPathController pathController;
    
    [SerializeField]
    private EnemyAlarmController alarmController;

    [SerializeField]
    private NavmeshAgent2D agent;

    private MonoBehaviour currentBehaviour;

	private void Start() {
        OnResetPath();
        DisableAll();
        currentBehaviour = alarmController;
        SetCurrent(pathController);
	}

    private void DisableAll() {
        pathController.enabled = false;
        alarmController.enabled = false;
    }

    private void SetCurrent(MonoBehaviour obj) {
        currentBehaviour.enabled = false;
        currentBehaviour = obj;
        currentBehaviour.enabled = true;
    }

	protected override void OnResetPath()
        => pathController.SetPath(Path);
    
    public void OnSuspicionSeen(GameObject obj) {

    }

    public void OnSuspicionUnSeen() {

    }

    public void OnAlerted(GameObject obj) {
        SetCurrent(alarmController);
        alarmController.SetAlertTarget(obj);
    }

    public void OnAlertTargetOutOfSight() {
        alarmController.AlertTargetUnseen();
    }
}
