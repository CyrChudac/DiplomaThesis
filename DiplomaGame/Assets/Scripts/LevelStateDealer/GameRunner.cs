using GameCreatingCore;
using GameCreatingCore.GamePathing;
using GameCreatingCore.GameActions;
using GameCreatingCore.LevelSolving.Viewcones;
using GameCreatingCore.LevelRepresentationData;
using GameCreatingCore.StaticSettings;
using GameCreatingCore.LevelStateData;
using System.Collections.Generic;
using UnityEngine;

public class GameRunner : MonoBehaviour
{
    public LevelInitializor levelInit;
    public GameController gameController;
    public int viewconeInnerRays = 100;
    public List<EnemyObject> enemies;
    public HumanPlayerController player;
    public LevelRepresentation currentLevel;
    public GameObject AttackAvailableIcon;
    public ViewconesManager viewconesManager;

    private GameSimulator gameSimulator;
    private ViewconeNavGraph viewconeNavGraph;
    private LevelState currentState;
    private PlayerSettingsProcessed playerSettings;
    private List<ViewconeRepresentation> enemySettings;
    private StaticNavGraph staticNavGraph;
    private StaticNavGraph inflatedNavGraph;
    private Obstacle inflatedOuter;
    private int currentAvailableSkillsCount;
    // Start is called before the first frame update
    void Start()
    {
        player.OnDestinationChanged.AddListener(ChangePlayerDestination);
        var statSett = gameController.GetStaticGameRepr();
        playerSettings = statSett.PlayerSettings;
        currentLevel = levelInit.currentLevel;
        inflatedNavGraph = new StaticNavGraph(ObstaclesInflator.InflateAllInLevel(currentLevel, statSett.StaticMovementSettings), false)
            .Initialized();
        staticNavGraph = new StaticNavGraph(currentLevel, true).Initialized();
        inflatedOuter = ObstaclesInflator.InflateOuterObst(currentLevel.OuterObstacle, statSett.StaticMovementSettings.CharacterMaxRadius);
        var rules = gameController.GetStaticGameRepr();
        currentState = GameSimulator.GetInitialLevelState(currentLevel, rules, staticNavGraph);
        gameSimulator = new GameSimulator(rules, currentLevel, staticNavGraph);
        viewconeNavGraph = new ViewconeNavGraph(currentLevel, staticNavGraph,
            gameController.GetStaticGameRepr(), viewconeInnerRays, 1, 1);
        enemySettings = new List<ViewconeRepresentation>();
        foreach(var e in currentLevel.Enemies) {
            enemySettings.Add(rules.GetEnemySettings(e.Type).viewconeRepresentation);
        }
        DisplayAvailableSkills();
    } 
    
    IGameAction currentAction = null;
    bool isGameEnd = false;
    void Update()
    {
        if(isGameEnd)
            return;
        IGameAction action = ProcessAvailableAbilities(currentState);
        if(action == null) {
            action = currentAction;
        }
        var actions = new List<IGameAction>();
        if(action != null)
            actions.Add(action);

        var timedState = new LevelStateTimed(currentState, Time.deltaTime);
        var state = gameSimulator.Simulate(timedState, viewconeNavGraph, actions);

        if(action != null && action.Done)
            currentAction = null;
        
        player.transform.position = state.playerState.Position;

        if(levelInit.currentLevel.Goal.IsAchieved(state)) {
            isGameEnd = true;
            gameController.GameWon();
            return;
        }
        foreach(var e in state.enemyStates) {
            if(e.Alerted) {
                gameController.GameOver();
                isGameEnd = true;
                return;
            }
        }

        for(int i = 0; i < state.enemyStates.Count; i++) {
            enemies[i].transform.position = state.enemyStates[i].Position;
            enemies[i].transform.rotation = Quaternion.Euler(0, 0, -state.enemyStates[i].Rotation);
            if(state.enemyStates[i].Alive) {
                var sus = state.enemyStates[i].TimeOfPlayerInView;
                if(sus > 0 && sus < 1) {
                    Debug.Log($"Sus: {sus}");
                }
                enemies[i].viewcone.suspitionRatio = sus;
                enemies[i].viewcone.viewLength = enemySettings[i]
                    .ComputeLength(state.enemyStates[i].ViewconeAlertLengthModifier);
            }
            if((!enemies[i].killer.Dead) && (!state.enemyStates[i].Alive)){
                enemies[i].killer.KillHim();
            }
            if(FloatEquality.AreEqual(currentState.enemyStates[i].TimeOfPlayerInView, 0)
                && !FloatEquality.AreEqual(state.enemyStates[i].TimeOfPlayerInView, 0))
                viewconesManager.ShowEnemyViewcone(enemies[i]);
        }
        if(currentAvailableSkillsCount != state.playerState.AvailableSkills.Count) {
            DisplayAvailableSkills();
        }
        currentState = state;
    }

    void DisplayAvailableSkills() {
        AttackAvailableIcon.SetActive(false);
        bool hasKilling = false;
        foreach(var skill in currentState.playerState.AvailableSkills) {
            if(skill is KillActionProvider) {
                AttackAvailableIcon.SetActive(true);
                hasKilling = true;
            } else {
                throw new System.NotImplementedException($"{nameof(IActiveGameActionProvider)} of type " +
                    $"{skill.GetType().Name} not implemented within Unity.");
            }
        }
        foreach(var e in enemies) {
            e.killer.CanBeKilled = hasKilling;
        }
        currentAvailableSkillsCount = currentState.playerState.AvailableSkills.Count;
    }

    private void ChangePlayerDestination(Vector2? potentialDestination) {
        if(!potentialDestination.HasValue) {
            currentAction = null;
            return;
        }
        Vector2 destination = potentialDestination.Value;
        if(staticNavGraph.IsInPlayerObstacle(destination, false)
            || !levelInit.currentLevel.OuterObstacle.ContainsPoint(destination, true)) {
            currentAction = null;
        } else {
            if(gameController.InflatedObstacles) {
                destination = ChangeDestinationOutsideOfInflated(destination);
            }
            List<Vector2> path;
            if(gameController.InflatedObstacles)
                path = inflatedNavGraph.GetEnemylessPlayerPath(currentState.playerState.Position, destination);
            else
                path = staticNavGraph.GetEnemylessPlayerPath(currentState.playerState.Position, destination);

            if(path == null)
                currentAction = null;
            else {
                currentAction = new WalkAlongPath(
                    path,
                    null,
                    false,
                    false,
                    true,
                    playerSettings.movementRepresentation,
                    TurnSideEnum.ShortestPrefereClockwise
                    );
            }
        }
    }

    Vector2 ChangeDestinationOutsideOfInflated(Vector2 destination) {
        var i = inflatedNavGraph.InWhichPlayerObstacle(destination, false);
        if(!i.HasValue) {
            if(!inflatedOuter.ContainsPoint(destination, true)) {
                return NearestPointOnObstacle(destination, inflatedOuter);
            }
            return destination;
        }
        return NearestPointOnObstacle(destination, inflatedNavGraph.PlayerObstacles[i.Value]);
    }

    Vector2 NearestPointOnObstacle(Vector2 to, Obstacle o) {
        Vector2 pre = o.Shape[^1];
        Vector2 result = to;
        float sqrMagn = float.MaxValue;
        foreach(var p in o.Shape) {
            var curr = LineIntersectionDecider.NearestPointOnLine(pre, p, to);
            var currMagn = (curr - to).sqrMagnitude;
            if(currMagn < sqrMagn) {
                sqrMagn = currMagn;
                result = curr;
            }
            pre = p;
        }
        return result + (result - to).normalized * 0.05f;
    }

    IGameAction ProcessAvailableAbilities(LevelState state) {
        foreach(var skill in state.playerState.AvailableSkills) {
            if(skill is KillActionProvider) {
                if(player.AttackingEnemy != null) {
                    int enemI = GetAttackedEnemyIndex();
                    if(!state.enemyStates[enemI].Alive)
                        continue;
                    var distSqr = (state.playerState.Position - state.enemyStates[enemI].Position).sqrMagnitude;
                    if(distSqr < skill.MaxUseDistance * skill.MaxUseDistance
                        && distSqr > skill.MinUseDistance * skill.MinUseDistance) {
                        var anim = player.GetComponent<Animator>();
                        var time = (skill as KillActionProvider).KillingTime;
                        anim.SetFloat("KillTime", 1 / time);
                        anim.Play("PlayerKill");
                        viewconesManager.ShowEnemyViewcone(enemies[enemI]);
                        return skill.Get(null, new GameActionTarget(null, enemI, false));
                    }
                }
            } else {
                throw new System.NotImplementedException($"{nameof(IActiveGameActionProvider)} of type " +
                    $"{skill.GetType().Name} not implemented within Unity.");
            }
        }
        return null;
    }

    int GetAttackedEnemyIndex() {
        for(int i = 0; i < enemies.Count; i++) {
            if(enemies[i].gameObject.Equals(player.AttackingEnemy.gameObject)) {
                return i;
            }
        }
        throw new System.Exception("Attacked enemy is not within the spawned enemies.");
    }
}
