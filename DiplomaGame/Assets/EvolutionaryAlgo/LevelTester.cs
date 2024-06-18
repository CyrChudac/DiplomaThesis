using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore.GamePathing;
using GameCreatingCore;
using System.Linq;
using UnityEditor;
using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.GamePathing.NavGraphs;
using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using System;

public class LevelTester : MonoBehaviour {
    IGamePathSolver pather;
    public GameController controller;
    public LevelProvider testProvider;
    public LevelInitializor initializor;
    public float pathTimeStep = 2.0f;
    public int innerViewconeRayCount = 6;
    public int skillUsePointsCount = 4;
    [Min(1)]
    public float viewconesGraphDistances = 10;
    public bool inflateObstacles = true;
    public bool showObstacleNumbers = true;
    public bool showPathToGoal = false;
    public bool showEnemyPatrols = true;
    public bool showNavGraph = false;
    public int showSpecificViewcone = -1;
    public ViewconeVisualization viewconeVisualization = ViewconeVisualization.None;
    public bool removeNonTraversableEdgesFromGraph;
    public bool noEnemyPather = false;
    [Min(0)]
    public float navGraphTiming = 0f;
    [Range(0.05f, 1)]
    public float secondsBetwwenPathSteps = 1;
    [Range(0, 1)]
    public float outerAlpha = 0.5f;
    [Range(0, 1)]
    public float obstaclesAlpha = 0.5f;

    [SerializeField]
    private List<Vector2> path;

    public float Score(LevelRepresentation ind)
        => Score(GetPath(ind, controller), ind);

    private float Score(List<IGameAction> path, LevelRepresentation levelRepresentation) {
        return Mathf.Min(1, (path?.Count ?? 0) / (100.0f + levelRepresentation.Obstacles.Count));
    }

    private List<IGameAction>? GetPath(LevelRepresentation ind, GameController gc)
        => pather.GetPath(gc.GetStaticGameRepr(), ind);


    [ContextMenu(nameof(TestScore))]
    public void TestScore() {
        Debug.Log("TEST: " + Score(testProvider.GetLevel()));
    }

    private void Start() {
        ResetPather();
    }

    private void ResetPather() {
        if(noEnemyPather) {
            pather = new GameCreatingCore.GamePathing.NoEnemyGamePather(inflateObstacles);
        } else {
            pather = new GameCreatingCore.GamePathing.FullGamePather(
                innerViewconeRayCount: innerViewconeRayCount,
                viewconesGraphDistances: viewconesGraphDistances,
                timestepSize: pathTimeStep,
                maximumLevelTime: 60,
                skillUsePointsCount: skillUsePointsCount);
        }
    }

    [ContextMenu(nameof(ShowPath))] 
    public void ShowPath() {
        StartCoroutine(PlayPathCoroutine());
    }

    IEnumerator PlayPathCoroutine() {
        if(path == null)
            yield break;
        var actions = GetPath(level, controller) ?? new List<IGameAction>();
        state = LevelState.GetInitialLevelState(level);
        yield return new WaitForSeconds(secondsBetwwenPathSteps);
        var simulator = new GameSimulator(controller.GetStaticGameRepr(), level)
            .Initialized(state, staticNavGraph);
        int iterations = 0;
        do {
            state = simulator.Simulate(new LevelStateTimed(state, secondsBetwwenPathSteps),
                new ViewconeNavGraph(level, staticNavGraph, controller.GetStaticGameRepr(), 100, 1, 1), actions);
            actions = actions.Where(a => !a.Done).ToList();
            if(iterations++ == 1000) {
                Debug.LogWarning("Level path too long to display, probably nonfuctional game action.");
                break;
            }
            yield return new WaitForSeconds(secondsBetwwenPathSteps);
        } while(actions.Count > 0);
    }

    private StaticNavGraph staticNavGraph;
    private ViewconeNavGraph targetGraph;
    private LevelState state;
    private LevelRepresentation level;

    private void OnValidate() { 
        ResetPather();
        level = testProvider.GetLevel();
        staticNavGraph = new StaticNavGraph(level, true).Initialized();
        targetGraph = new ViewconeNavGraph(level, staticNavGraph, controller.GetStaticGameRepr(),
            innerViewconeRayCount,
            viewconesGraphDistances,
            1);

        state = LevelState.GetInitialLevelState(level);

        if(showPathToGoal && testProvider != null) {
            var actions = GetPath(level, controller) ?? new List<IGameAction>();
            var innerState = state;
            var simulator = new GameSimulator(controller.GetStaticGameRepr(), level)
                .Initialized(innerState, staticNavGraph);
            List<Vector2> playerPositions = new List<Vector2>();
            var iterations = 0;
            do {
                innerState = simulator.Simulate(new LevelStateTimed(innerState, secondsBetwwenPathSteps),
                    new ViewconeNavGraph(level, staticNavGraph, controller.GetStaticGameRepr(), 100, 1, 1), actions);
                playerPositions.Add(innerState.playerState.Position);
                actions = actions.Where(a => !a.Done).ToList();
                if(iterations++ == 1000) {
                    Debug.LogWarning("Level path too long to display, probably nonfuctional game action.");
                    break;
                }
            } while(actions.Count > 0);
            path = playerPositions;
        } else
            path = new List<Vector2>();

        if(navGraphTiming > 0) {
            var simu = new GameSimulator(controller.GetStaticGameRepr(), level)
                .Initialized(state, staticNavGraph);
            var st = new LevelStateTimed(state, navGraphTiming);
            state = simu.Simulate(st, targetGraph, new List<IGameAction>());
        }

    }

    private void OnDrawGizmos() {
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
        for(int i = 0; i < level.Enemies.Count; i++) {
            DrawEnemy(level.Enemies[i], state.enemyStates[i]);
        }
        DrawNavGraph(targetGraph, staticNavGraph, state);
        DrawViewcones(targetGraph, staticNavGraph, state);
        DrawPlayerAndPath(state);
    }

    private void DrawPlayerAndPath(LevelState state) {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(state.playerState.Position, 2);
        if(path == null)
            return;
        var pre = state.playerState.Position;
        Gizmos.color = Color.Lerp(Color.yellow, Color.white, 0.5f);
        for(int i = 0; i < path.Count; i++) {
            Gizmos.DrawLine(pre, path[i]);
            pre = path[i];
        }
    }

    private void DrawGoal(LevelRepresentation lvl) {
        Gizmos.color = Color.Lerp(Color.yellow, Color.white, 0.8f);
        Gizmos.DrawSphere(lvl.Goal.Position, lvl.Goal.Radius);
    }
    
    private void DrawOuter(Obstacle outer) {
        Gizmos.color = SetAlpha(Color.gray, outerAlpha);;
        var m = initializor.PolygonToMesh(initializor.GetOuterObstacleShape(outer.Shape.ToArray(), gameObject));
        m.RecalculateNormals();
        Gizmos.DrawMesh(m);
    }

    private Color SetAlpha(Color color, float alpha)
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

    private void DrawEnemy(Enemy enemy, EnemyState state) {
        Gizmos.color = new Color(1, 0.1f, 0.1f, 0.5f);
        Gizmos.DrawSphere(state.Position, 2);
        if(enemy.Path != null && showEnemyPatrols) {
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
        var offset = Vector2Utils.VectorFromAngle(state.Rotation);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(state.Position + offset.normalized, 0.5f);
    }

    private void DrawNavGraph(ViewconeNavGraph navGraph, StaticNavGraph staticNavGraph, LevelState state) {
        if(!showNavGraph)
            return;
            
        if(noEnemyPather) {
            DrawGraph(staticNavGraph.EnemyNavGraph, e => Color.white, e => Color.blue);
        } else {
            var graph = navGraph.GetScoredNavGraph(state);
            DrawGraph(graph, (e) => {
                    if(e.EdgeInfo.ViewconeIndex.HasValue) {
                        return Color.cyan;
                    } else {
                        return Color.white;
                    }
                }, (e) => {
                    if(e.EdgeInfo.ViewconeIndex.HasValue) {
                        return Color.Lerp(Color.blue, Color.red, 0.6f);
                    } else {
                        return Color.blue;
                    }
                });
        }
    }

    void DrawViewcones(ViewconeNavGraph viewconeNavGraph, StaticNavGraph staticNavGraph, LevelState state) {
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
            var views = viewconeNavGraph.GetViewConeGraphs(state, false, 
                viewconeVisualization == ViewconeVisualization.CutoffGraphs, removeNonTraversableEdgesFromGraph)
                .ToList();
            var sc = Color.Lerp(Color.green, Color.yellow, 0.5f);
            var dc = Color.Lerp(Color.green, Color.black, 0.5f);
            for(int i = 0; i < views.Count; i++) {
                if(showSpecificViewcone != -1 && showSpecificViewcone != i)
                    continue;
                DrawGraph(views[i], e => sc, e => dc);
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
                Gizmos.DrawLine(e.First.Position, e.Second.Position);
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
}
