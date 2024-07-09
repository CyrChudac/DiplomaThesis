using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore.GamePathing;
using GameCreatingCore;
using System.Linq;
using UnityEditor;
using GameCreatingCore.GameActions;
using GameCreatingCore.LevelSolving.Viewcones;
using GameCreatingCore.LevelSolving;
using GameCreatingCore.LevelRepresentationData;
using System;
using Unity.VisualScripting;
using GameCreatingCore.StaticSettings;
using GameCreatingCore.LevelStateData;

[ExecuteAlways]
public class LevelTester : MonoBehaviour {
    IGamePathSolver pather;
    [Header("Scoring related")]
    [Tooltip("Game controller provides static game representation and whether the obstacles are inflated.")]
    public GameController controller;
    public float pathTimeStep = 2.0f;
    public int innerViewconeRayCount = 6;
    public int skillUsePointsCount = 4;
    [Range(500, 15_000)]
    public int pathMaxIterations = 500;
    [Tooltip("The level solution path from start to goal maximum in-game time.")]
    [Range(3, 60)]
    public float levelSolveTime = 20;
    [Min(1)]
    public float viewconesGraphDistances = 10;
    public float killingNotUsedMultiplier = 0.8f;
    public float killActionScoreAdd = 0.5f;
    public float throughViewconeActionScoreAdd = 0.3f;
    public float pickupActionScoreAdd = 0.2f;
    public float genericActionScoreAdd = 0.1f;
    public float defaultObstaclkesScoringCount = 20.0f;
    [Range(0, 10)]
    [Tooltip("How many second does the character stand still before it starts to find path.")]
    public float initialPathWait = 2f;

    [Header("Visualization only")]
    public bool visualize = true;
    public LevelProvider testProvider;
    public LevelInitializor initializor;
    public bool showObstacleNumbers = true;
    public bool showPathToGoal = false;
    public int pathMinGenerated = 0;
    public int pathGeneratedCount = 1000;
    public bool showEnemyPatrols = true;
    public bool showNavGraph = false;
    [Range(0, 0.15f)]
    public float graphEdgeEndMargin = 0;
    public VisualizationType navGraphVisualization = VisualizationType.IsPartOfViewcone;
    public bool showNavGraphNumbers = true;
    public ScoreVisualization scoreVisualization = ScoreVisualization.None;
    public int showSpecificViewcone = -1;
    public ViewconeVisualization viewconeVisualization = ViewconeVisualization.None;
    public bool removeNonTraversableEdgesFromGraph;
    public bool noEnemyPather = false;
    [Min(0)]
    public float navGraphTiming = 0f;
    [Range(0.05f, 1)]
    public float secondsBetweenPathSteps = 1;
    [Range(0, 1)]
    public float outerAlpha = 0.5f;
    [Range(0, 1)]
    public float obstaclesAlpha = 0.5f;

    public float Score(LevelRepresentation ind)
        => Score(GetPath(ind, controller.GetStaticGameRepr()), ind);

    private float Score(List<IGameAction> path, LevelRepresentation levelRepresentation) {
        if(path == null)
            return 0;
        float actionScore = 0;
        bool hasKill = false;
        foreach(var action in ExpandAllInner(path)) {
            if(action is KillGameAction) {
                actionScore += killActionScoreAdd;
                hasKill = true;
            } else if(action is WalkThroughViewConePlayerAction)
                actionScore += throughViewconeActionScoreAdd;
            else if(action is PickupAction)
                actionScore += pickupActionScoreAdd;
            else if(!action.IsCancelable) {
                Debug.Log($"new noncancelable action: {action.GetType()}");
                actionScore += genericActionScoreAdd;
            }
        }
        var pathScore = path!.Count / (defaultObstaclkesScoringCount 
            + levelRepresentation.Obstacles.Count 
            + levelRepresentation.Enemies.Count * 3);
        
        bool hasKillSkill = false;
        foreach(var item in levelRepresentation.SkillsStartingWith) {
            if(item is KillActionProvider) {
                hasKillSkill = true;
                break;
            }
        }
        if(!hasKillSkill) {
            foreach(var item in levelRepresentation.SkillsToPickup) {
                if(item.action is KillActionProvider) {
                    hasKillSkill = true;
                    break;
                }
            }
        }
        float multiplier = 1;
        if((!hasKill) && hasKillSkill)
            multiplier = killingNotUsedMultiplier;
        return actionScore + pathScore * multiplier;
    }

    private IEnumerable<IGameAction> ExpandAllInner(IEnumerable<IGameAction> actions) {
        IEnumerable<IGameAction> result = Enumerable.Empty<IGameAction>();
        foreach(var a in actions) {
            if(a is IWithInnerActions)
                result = result.Concat(ExpandAllInner(((IWithInnerActions)a).GetInnerActions()));
            else
                result = result.Append(a);
        }
        return result;
    }

    private List<IGameAction> GetPath(LevelRepresentation ind, StaticGameRepresentation gr)
        => pather.GetPath(gr, ind);


    [ContextMenu(nameof(TestScore))]
    public void TestScore() {
        Debug.Log("TEST: " + Score(testProvider.GetLevel(true)));
    }

    private void Start() {
        ResetPather();
    }

    private void ResetPather() {
        if(noEnemyPather) {
            pather = new NoEnemyGamePather(controller.InflatedObstacles);
        } else {
            pather = new FullGamePather(
                innerViewconeRayCount: innerViewconeRayCount,
                viewconesGraphDistances: viewconesGraphDistances,
                timestepSize: pathTimeStep,
                maximumLevelTime: levelSolveTime,
                maxIterations: pathMaxIterations,
                skillUsePointsCount: skillUsePointsCount,
                inflateObstacles: controller.InflatedObstacles,
                initialWait: 2); 
        }
    }

    public ViewconeNavGraph targetGraph;
    public LevelState state;
    public GraphWithViewcones graphWithViewcones;
    public LevelRepresentation level;
    private StaticNavGraph staticNavGraph;
    private List<(List<Vector2> Path, Vector2 PlayerPos)> paths;
    private List<Graph<Node>> viewcones;

    private bool firstValidate = true;
    private bool firstValidateEnd = true;

    private void OnValidate() {
        if(!visualize)
            return;
#if UNITY_EDITOR
        if(firstValidate) {
            Debug.Log("first validate initiated");
            firstValidate = false;
        }
#endif
        viewcones = new List<Graph<Node>>();
        ResetPather();
        var staticGameRepr = controller.GetStaticGameRepr();
        var startinglevel = testProvider.GetLevel(false);

        if(controller.InflatedObstacles)
            level = ObstaclesInflator.InflateAllInLevel(startinglevel, staticGameRepr.StaticMovementSettings);
        else
            level = startinglevel;
        staticNavGraph = new StaticNavGraph(level, true).Initialized();
        targetGraph = new ViewconeNavGraph(level, staticNavGraph, staticGameRepr,
            innerViewconeRayCount,
            viewconesGraphDistances,
            skillUsePointsCount);

        state = GameSimulator.GetInitialLevelState(level, staticGameRepr, staticNavGraph); 
        
        paths = new List<(List<Vector2>, Vector2)>();
        if(showPathToGoal && testProvider != null) {
            List<List<IGameAction>> preActions;
            if(pathMinGenerated == 0 && pathGeneratedCount == 1) {
                var p = pather.GetPath(staticGameRepr, startinglevel);
                preActions = new List<List<IGameAction>>();
                if(p != null) {
                    preActions.Add(p);
                }
            } else
                preActions = pather.GetFullPathsTree(staticGameRepr, startinglevel) ?? new List<List<IGameAction>>();
            foreach(var a in preActions.Skip(pathMinGenerated).Take(pathGeneratedCount)) {
                paths.Add(GetPath(a, staticGameRepr));
            }
        }

        if(navGraphTiming > 0) {
            var simu = new GameSimulator(staticGameRepr, level, staticNavGraph);
            var st = new LevelStateTimed(state, navGraphTiming);
            state = simu.Simulate(st, targetGraph, new List<IGameAction>());
        }
        
        if(!noEnemyPather) {
            graphWithViewcones = targetGraph.GetScoredNavGraph(state);
        }

        if(viewconeVisualization == ViewconeVisualization.Graph || viewconeVisualization == ViewconeVisualization.CutoffGraphs) {
            viewcones = targetGraph.GetViewConeGraphs(state, false, 
                viewconeVisualization == ViewconeVisualization.CutoffGraphs, removeNonTraversableEdgesFromGraph)
                .ToList();
        }
        
#if UNITY_EDITOR
        if(firstValidateEnd) {
            Debug.Log("first validate terminated");
            firstValidateEnd = false;
        }
#endif
    }

    private (List<Vector2>, Vector2) GetPath(List<IGameAction> actions, StaticGameRepresentation staticGameRepr) {
        var innerState = state;
        var preActions = actions;
        var simulator = new GameSimulator(staticGameRepr, level, staticNavGraph);
        List<Vector2> playerPositions = new List<Vector2>();
        var iterations = 0;
        do {
            var timed = new LevelStateTimed(innerState, secondsBetweenPathSteps);
            var g = new ViewconeNavGraph(level, staticNavGraph, staticGameRepr, innerViewconeRayCount, 1, 1);
            innerState = simulator.Simulate(timed, g, actions);
            playerPositions.Add(innerState.playerState.Position);
            actions = actions.Where(a => !a.Done).ToList();
            if(iterations++ > (levelSolveTime / secondsBetweenPathSteps) + 5) {
                Debug.LogWarning("Level path too long to display, probably nonfuctional game action.");
                break;
            }
        } while(actions.Count > 0);
        for(int i = 0; i < preActions.Count; i++) {
            preActions[i].Reset();
            if(preActions[i].Done)
                throw new Exception("Reset did not reset the action!");
        }
        var playerOnPath = simulator.Simulate(new LevelStateTimed(state, navGraphTiming), targetGraph, preActions).playerState.Position;
        return (playerPositions, playerOnPath);
    }

    private void OnDrawGizmos() {
        if(!visualize)
            return;
        if(initializor == null) {
            Debug.LogWarning("Initializor not set!");
            return;
        }
        if(testProvider == null) {
            Debug.LogWarning("Test level not set!");
            return;
        }
#if UNITY_EDITOR
        if(!transform.GetComponentsInChildren<Transform>().Contains(Selection.activeTransform))
            return;
#endif

        DrawGoal(level);
        DrawOuter(level.OuterObstacle);
        DrawObstacles(level.Obstacles);
        DrawEnemies(level.Enemies, state.enemyStates);
        DrawNavGraph(targetGraph, staticNavGraph, state);
        DrawViewcones(targetGraph, state);
        DrawPlayerAndPath(state);
    }

    private void DrawPlayerAndPath(LevelState state) {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(state.playerState.Position, 2);
        if(paths == null)
            return;
        foreach(var path in paths) {
            var pre = state.playerState.Position;
            Gizmos.color = Color.Lerp(Color.yellow, Color.white, 0.5f);
            for(int i = 0; i < path.Path.Count; i++) {
                Gizmos.DrawLine(pre, path.Path[i]);
                pre = path.Path[i];
            }
            Gizmos.color = Color.Lerp(Color.yellow, Color.black, 0.1f);
            Gizmos.DrawSphere(path.PlayerPos, 0.2f);
        }
    }

    private void DrawGoal(LevelRepresentation lvl) {
        Gizmos.color = SetAlpha(Color.Lerp(Color.yellow, Color.white, 0.8f), 0.75f);
        Gizmos.DrawSphere(lvl.Goal.Position, lvl.Goal.Radius);
    }
    
    private void DrawOuter(Obstacle outer) {
        Gizmos.color = SetAlpha(Color.gray, outerAlpha);;
        var m = initializor.PolygonToMesh(initializor.GetOuterObstacleShape(outer.Shape.ToArray(), gameObject));
        m.RecalculateNormals();
        Gizmos.DrawMesh(m);
    }

    public static Color SetAlpha(Color color, float alpha)
        => new Color(color.r, color.g, color.b, alpha);

    private void DrawObstacles(IReadOnlyList<Obstacle> obsts) {
        for(int i = 0; i < obsts.Count; i++) {
            var obst = obsts[i];
            Gizmos.color = SetAlpha(Color.Lerp(Color.red, Color.blue, 0.55f), obstaclesAlpha);
            if(obst.Effects.FriendlyWalkEffect == WalkObstacleEffect.Walkable)
                Gizmos.color = SetAlpha(Color.cyan, obstaclesAlpha);
            var m = initializor.PolygonToMesh(initializor.GetObstacleShape(obst.Shape.ToArray(), gameObject));
            m.RecalculateNormals();
            Gizmos.DrawMesh(m);
        }
    }

    private void DrawEnemies(IReadOnlyList<Enemy> enemies, IReadOnlyList<EnemyState> states) {
        foreach(var state in states) {
            Gizmos.color = new Color(1, 0.1f, 0.1f, 0.5f);
            Gizmos.DrawSphere(state.Position, 2);
            var offset = Vector2Utils.VectorFromAngle(state.Rotation);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(state.Position + offset.normalized, 0.5f);
        }
        foreach(var enemy in enemies) {
            if(enemy.Path != null && enemy.Path.Commands != null && showEnemyPatrols) {
                if(enemy.Position != enemy.Path.Commands[0].Position) {
                    Gizmos.color = new Color(1, 0.2f, 0.2f, 0.25f);
                    Gizmos.DrawLine(enemy.Position, enemy.Path.Commands[0].Position);
                }
                Gizmos.color = new Color(1, 0, 0, 1);
                for(int i = 0; i < enemy.Path.Commands.Count - 1; i++) {
                    Gizmos.DrawLine(enemy.Path.Commands[i].Position, enemy.Path.Commands[i + 1].Position);
                }
                if(enemy.Path.Cyclic) {
                    Gizmos.color = new Color(1, 0.25f, 0, 1);
                    Gizmos.DrawLine(enemy.Path.Commands[enemy.Path.Commands.Count - 1].Position, enemy.Path.Commands[0].Position);
                }
            }
        }
    }

    private void DrawNavGraph(ViewconeNavGraph navGraph, StaticNavGraph staticNavGraph, LevelState state) {
        if(!showNavGraph)
            return;

        Func<Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>, bool> predicate = (e) => {
            switch(navGraphVisualization) {
                case VisualizationType.IsPartOfViewcone:
                    return e.EdgeInfo.ViewconeIndex.HasValue;
                case VisualizationType.None:
                    return false;
                default:
                    return true;
            }
        };

        if(noEnemyPather) {
            DrawGraph(staticNavGraph.EnemyNavGraph, e => Color.white, e => Color.blue);
        } else {
            DrawGraph(graphWithViewcones, (e) => {
                    if(predicate(e)) {
                        return Color.cyan;
                    } else {
                        return Color.white;
                    }
                }, (e) => {
                    if(predicate(e)) {
                        return Color.Lerp(Color.blue, Color.red, 0.6f);
                    } else {
                        return Color.blue;
                    }
                });
        }
    }

    void DrawViewcones(ViewconeNavGraph viewconeNavGraph, LevelState state) {
        if(viewconeVisualization == ViewconeVisualization.None)
            return;
        if(viewconeVisualization == ViewconeVisualization.Outline) {
            var views = viewconeNavGraph.GetRawViewcones(state);

            Gizmos.color = Color.green;
            for(int i = 0; i < views.Count; i++) {
                if(showSpecificViewcone != -1 && showSpecificViewcone != i)
                    continue;
                var view = views[i];
                var previous = view[view.Count - 1];
                foreach(var pos in view) {
                    Gizmos.DrawLine(previous, pos);
                    previous = pos;
                }
            }
            Gizmos.color = Color.yellow;
            for(int i = 0; i < views.Count; i++) {
                if(showSpecificViewcone != -1 && showSpecificViewcone != i)
                    continue;
                var view = views[i];
                foreach(var pos in view) {
                    Gizmos.DrawSphere(pos, 0.3f);
                }
            }
        } else if(viewconeVisualization == ViewconeVisualization.Graph || viewconeVisualization == ViewconeVisualization.CutoffGraphs){
            var sc = Color.Lerp(Color.green, Color.yellow, 0.5f);
            var dc = Color.Lerp(Color.green, Color.black, 0.5f);
            for(int i = 0; i < viewcones.Count; i++) {
                if(showSpecificViewcone != -1 && showSpecificViewcone != i)
                    continue;
                DrawGraph(viewcones[i], e => sc, e => dc);
            }
        } else { 
            throw new System.NotImplementedException($"Viecone visualization with value {viewconeVisualization} not implemented.");
        }
    }

    private void DrawGraph<N, E>(Graph<N, E> graph, Func<Edge<N, E>, Color> singleEdgeColor, Func<Edge<N, E>, Color> doubleEdgeColor) 
        where N : Node where E : EdgeInfo{ 

        var edgesDict = new Dictionary<Vector2, List<Vector2>>();
        var doubleEdges = new List<Edge<N, E>>();
        foreach (var e in graph.edges)
        {
            if (edgesDict.TryGetValue(e.Second.Position, out var val) && val.Remove(e.First.Position))
            {
                doubleEdges.Add(e);
            }
            else
            {
                Gizmos.color = singleEdgeColor(e);
                var sec = e.First.Position + (e.Second.Position - e.First.Position) * (1 - graphEdgeEndMargin);
                Gizmos.DrawLine(e.First.Position, sec);
                List<Vector2> into;
                if (!edgesDict.TryGetValue(e.First.Position, out into))
                {
                    into = new List<Vector2>();
                    edgesDict.Add(e.First.Position, into);
                }
                into.Add(e.Second.Position);
            }
        }
        foreach (var e in doubleEdges)
        {
            Gizmos.color = doubleEdgeColor(e);
            Gizmos.DrawLine(e.First.Position, e.Second.Position);
        }
        Gizmos.color = Color.black;
        foreach (var v in graph.vertices)
        {
            Gizmos.DrawSphere(v.Position, 0.2f);
            Gizmos.color = Color.gray;
        }
    }


    public enum ViewconeVisualization {
        None,
        Outline,
        Graph,
        CutoffGraphs
    }

    public enum VisualizationType {
        IsPartOfViewcone,
        None
    }
}

public enum ScoreVisualization {
    None,
    GoalScore,
    UseSkillScore,
    PickUpSkillScore
}
