using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerProvider : MonoBehaviour
{
    [SerializeField]
    private HumanPlayerController playerController;
    [SerializeField]
    private GameController gameController;

    public HumanPlayerController GetPlayer() {
        var p = Instantiate(playerController);
        var ag = p.GetComponentInChildren<NavMeshAgent>();
        ag.speed = gameController.GetStaticGameRepr().PlayerSettings.movementRepresentation.WalkSpeed;
        return p;
    }
}
