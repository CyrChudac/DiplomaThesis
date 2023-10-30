using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAlarmController : MonoBehaviour
{
    private GameObject alertCause;

    void Update()
    {
        GameController gameController = GameObject.FindObjectOfType<GameController>();
        gameController.GameOver();
        Destroy(this);
    }

    public void SetAlertTarget(GameObject alertCause)
        => this.alertCause = alertCause;

    public void AlertTargetUnseen() {

    }
}
