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
    [SerializeField]
    private GameRunner gameRunner;

    public HumanPlayerController GetPlayer() {
        var p = Instantiate(playerController);
        gameRunner.player = p;
        return p;
    }
}
