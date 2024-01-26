using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAlarmController : MonoBehaviour
{
    private GameObject alertCause;
    private bool used = false;
    void Update()
    {
        if(!used) {
            GameController gameController = GameObject.FindObjectOfType<GameController>();
            gameController.GameOver();
            used = true;
        }
    }

    public void SetAlertTarget(GameObject alertCause)
        => this.alertCause = alertCause;

    public void AlertTargetUnseen() {

    }
}
