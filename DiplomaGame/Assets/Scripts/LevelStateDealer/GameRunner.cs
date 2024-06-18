using GameCreatingCore;
using GameCreatingCore.GamePathing;
using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.GamePathing.NavGraphs;
using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using GameCreatingCore.StaticSettings;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameRunner : MonoBehaviour
{
    public LevelProvider levelProvider;
    public GameController gameController;
    public int viewconeInnerRays = 100;
    public List<EnemyController> enemies;
    public GameObject player;

    private LevelRepresentation levelRepresentation;
    private GameSimulator gameSimulator;
    private ViewconeNavGraph viewconeNavGraph;
    private LevelState currentState;
    private PlayerSettingsProcessed playerSettings;
    private List<EnemySettingsProcessed> enemySettings;
    // Start is called before the first frame update
    void Start()
    {
        playerSettings = gameController.GetStaticGameRepr().PlayerSettings;
        levelRepresentation = levelProvider.GetLevel();
        currentState = LevelState.GetInitialLevelState(levelRepresentation);
        var staticNavGraph = new StaticNavGraph(levelRepresentation, true).Initialized();
        var rules = gameController.GetStaticGameRepr();
        gameSimulator = new GameSimulator(rules, levelRepresentation)
            .Initialized(currentState, staticNavGraph);
        viewconeNavGraph = new ViewconeNavGraph(levelRepresentation, staticNavGraph,
            gameController.GetStaticGameRepr(), viewconeInnerRays, 1, 1);
        enemySettings = new List<EnemySettingsProcessed>();
        foreach(var e in levelRepresentation.Enemies) {
            enemySettings.Add(rules.GetEnemySettings(e.Type));
        }
    }

    void Update()
    {
        var playerWalkAction = new WalkAlongPath(
            new List<Vector2>() { player.transform.position},
            null,
            false,
            false,
            true,
            playerSettings.movementRepresentation,
            TurnSideEnum.ShortestPrefereClockwise
            );
        var timedState = new LevelStateTimed(currentState, Time.deltaTime);
        var state = gameSimulator.Simulate(timedState,
            viewconeNavGraph, new List<IGameAction>() {
                playerWalkAction
            });
        for(int i = 0; i < state.enemyStates.Count; i++) {
            enemies[i].transform.position = state.enemyStates[i].Position;
            enemies[i].transform.rotation = Quaternion.Euler(0, 0, state.enemyStates[i].Rotation);
            enemies[i].viewcone.viewLength = enemySettings[i].viewconeRepresentation
                .ComputeLength(state.enemyStates[i].ViewconeAlertLengthModifier);
        }
        var timeInView = state.enemyStates.Max(e => e.TimeOfPlayerInView);
        if(timeInView > 0) {
            Debug.Log(timeInView);
        }
        currentState = state;
    }
}
