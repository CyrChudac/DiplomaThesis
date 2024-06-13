using GameCreatingCore;
using GameCreatingCore.GamePathing;
using GameCreatingCore.GamePathing.NavGraphs;
using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRunner : MonoBehaviour
{
    public LevelRepresentation levelRepresentation;
    public List<GameObject> enemies;
    public GameObject player;
    public GameController gameController;
    public int viewconeInnerRays;

    private LevelState currentState;
    private GameSimulator gameSimulator;
    private ViewconeNavGraph viewconeNavGraph;
    // Start is called before the first frame update
    void Start()
    {
        currentState = LevelState.GetInitialLevelState(levelRepresentation, 0.5f);
        var staticNavGraph = new StaticNavGraph(levelRepresentation, true).Initialized();
        gameSimulator = new GameSimulator(gameController.GetStaticGameRepr(), levelRepresentation)
            .Initialized(currentState, staticNavGraph);
        viewconeNavGraph = new ViewconeNavGraph(levelRepresentation, staticNavGraph,
            gameController.GetStaticGameRepr(), viewconeInnerRays, 1, 1);
    }

    void Update()
    {
        var state = gameSimulator.Simulate(new LevelStateTimed(currentState, Time.deltaTime),
            viewconeNavGraph, new List<GameCreatingCore.GamePathing.GameActions.IGameAction>());
        for(int i = 0; i < state.enemyStates.Count; i++) {
            enemies[i].transform.position = state.enemyStates[i].Position;
        }

        currentState = state;
    }
}
