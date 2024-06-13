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

public class LevelTester : MonoBehaviour
{
    IGamePathSolver pather;
    public GameController controller;
    public UnityLevelRepresentation testLevel;
    public LevelInitializor initializor;
    public float pathTimeStep = 2.0f;
    public int innerViewconeRayCount = 6;
    public int skillUsePointsCount = 4;
    [Min(1)]
    public float viewconesGraphDistances = 10;
    public bool inflateObstacles = true;
    public bool showPathToGoal = false;
    public bool showNavGraph = false;
    public bool showViewcones = false;
    public bool noEnemyPather = false;
    [Min(0)]
    public float navGraphTiming = 0f;
    [Range(0.05f, 1)]
    public float secondsBetwwenPathSteps = 1;

    [SerializeField]
    private List<Vector2> path;

    public float Score(LevelRepresentation ind)
        => Score(GetPath(ind, controller), ind);

    private float Score(List<IGameAction> path, LevelRepresentation levelRepresentation) {
        return Mathf.Min(1, (path?.Count ?? 0) / (100.0f + levelRepresentation.Obstacles.Count));	
    }

    private List<IGameAction>? GetPath(LevelRepresentation ind, GameController gc)
        => pather.GetPath(gc.GetStaticGameRepr(), ind);
    

    [ContextMenu(nameof(Test))]
    public void Test() {
        Debug.Log("TEST: " + Score(testLevel.GetLevelRepresentation()));
    }

	private void Start() {
        ResetPather();
	}

	private void ResetPather() {
        if (noEnemyPather)
        {
            pather = new GameCreatingCore.GamePathing.NoEnemyGamePather(inflateObstacles);
        }
        else
        {
            pather = new GameCreatingCore.GamePathing.FullGamePather(
                viewconeLengthModifier: 0.5f,
                innerViewconeRayCount: innerViewconeRayCount,
                viewconesGraphDistances: viewconesGraphDistances,
                timestepSize: pathTimeStep,
                maximumLevelTime: 60,
                skillUsePointsCount: skillUsePointsCount);
        }
    }

	private void OnValidate() {
        ResetPather();
        if(showPathToGoal && testLevel != null) {
            var level = BackwardsCompatibleLevel();
            var actions = GetPath(level, controller) ?? new List<IGameAction>();
            var staticNavGraph = new StaticNavGraph(level, true)
                .Initialized();
            var state = LevelState.GetInitialLevelState(level, 0.5f);
            var simulator = new GameSimulator(controller.GetStaticGameRepr(), level)
                .Initialized(state, staticNavGraph);
            List<Vector2> playerPositions = new List<Vector2>();
            var iterations = 0;
            do {
                state = simulator.Simulate(new LevelStateTimed(state, secondsBetwwenPathSteps),
                    new ViewconeNavGraph(level, staticNavGraph, controller.GetStaticGameRepr(), 100, 1, 1), actions);
                playerPositions.Add(state.playerState.Position);
                actions = actions.Where(a => !a.Done).ToList();
                if(iterations++ == 1000) {
                    Debug.LogWarning("Level path too long to display, probably nonfuctional game action.");
                    break;
                }
            }while(actions.Count > 0);
            path = playerPositions;
        }
        else
            path = new List<Vector2>();
	}

	private void OnDrawGizmosSelected() {
        if(initializor == null) {
            Debug.LogWarning("Initializor not set!");
            return;
        }
        if(testLevel == null) {
            Debug.LogWarning("Test level not set!"); 
            return;
        }
        var lvl = BackwardsCompatibleLevel();
        Gizmos.color = Color.Lerp(Color.yellow, Color.white, 0.8f);
        Gizmos.DrawSphere(lvl.Goal.Position, lvl.Goal.Radius);
        DrawOuter(lvl.OuterObstacle);
        DrawObstacles(lvl.Obstacles);
        foreach(var e in lvl.Enemies) {
            DrawEnemy(e);
        }
        DrawNavGraph(lvl);
        DrawViewcones(lvl);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(lvl.FriendlyStartPos, 2);
        Gizmos.color = new Color(0.1f, 1, 0.1f);
        var pre = lvl.FriendlyStartPos;
        if(path == null)
            return;
        for(int i = 0; i < path.Count; i++) {
            Gizmos.DrawLine(pre, path[i]);
            pre = path[i];
        }
    }
    
    private void DrawOuter(Obstacle outer) {
        Gizmos.color = Color.gray;
        var m = initializor.PolygonToMesh(initializor.GetOuterObstacleShape(outer.Shape.ToArray(), gameObject));
        m.RecalculateNormals();
        Gizmos.DrawMesh(m);
    }

    private void DrawObstacles(IReadOnlyList<Obstacle> obsts) {
        for(int i = 0; i < obsts.Count; i++) {
            var obst = obsts[i];
            Gizmos.color = Color.Lerp(Color.red, Color.blue, 0.55f);
            if(obst.Effects.FriendlyWalkEffect == WalkObstacleEffect.Walkable)
                Gizmos.color = Color.cyan;
            var m = initializor.PolygonToMesh(initializor.GetObstacleShape(obst.Shape.ToArray(), gameObject));
            m.RecalculateNormals();
            Gizmos.DrawMesh(m);
        }
    }

    private void DrawEnemy(Enemy enemy) {
        Gizmos.color = new Color(1, 0.1f, 0.1f, 0.5f);
        Gizmos.DrawSphere(enemy.Position, 2);
        if(enemy.Path != null) {
            if(enemy.Path.Commands.Count == 0) {
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(enemy.Position, 1.1f);
            } else {
                if(enemy.Position != enemy.Path.Commands[0].Position) {
                    Gizmos.color = new Color(1, 0, 0, 0.5f);
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
        var offset = Vector2Utils.VectorFromAngle(enemy.Rotation);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(enemy.Position + offset.normalized, 0.75f);
    }

    private void DrawNavGraph(LevelRepresentation level) {
        if(!showNavGraph)
            return;

        var staticNavGraph = new StaticNavGraph(level, true)
            .Initialized();

        var targetGraph = new ViewconeNavGraph(level, staticNavGraph, controller.GetStaticGameRepr(),
            innerViewconeRayCount,
            viewconesGraphDistances,
            1);

        var state = LevelState.GetInitialLevelState(level, 0.5f);
        if (navGraphTiming > 0)
        {
            var simu = new GameSimulator(controller.GetStaticGameRepr(), level)
                .Initialized(state, staticNavGraph);
            state = simu.Simulate(new LevelStateTimed(state, navGraphTiming), targetGraph, new List<IGameAction>());
        }

        var graph = targetGraph.GetScoredNavGraph(state);

        var edgesDict = new Dictionary<Vector2, List<Vector2>>();
        var doubleEdges = new List<(Vector2, Vector2, bool)>();
        foreach (var e in graph.edges)
        {
            if (edgesDict.TryGetValue(e.Second.Position, out var val) && val.Contains(e.First.Position))
            {
                doubleEdges.Add((e.First.Position, e.Second.Position, e.EdgeInfo.ViewconeIndex.HasValue));
            }
            else
            {
                if (e.EdgeInfo.ViewconeIndex.HasValue)
                {
                    Gizmos.color = Color.cyan;
                }
                else
                {
                    Gizmos.color = Color.white;
                }
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
            if (e.Item3)
            {
                Gizmos.color = Color.Lerp(Color.blue, Color.red, 0.35f);
            }
            else
            {
                Gizmos.color = Color.blue;
            }
            Gizmos.DrawLine(e.Item1, e.Item2);
        }
        Gizmos.color = Color.grey;
        foreach (var v in graph.vertices)
        {
            Gizmos.DrawSphere(v.Position, 0.2f);
        }
    }

    void DrawViewcones(LevelRepresentation level) {
        if(!showViewcones)
            return;
        var staticNavGraph = new StaticNavGraph(level, true)
            .Initialized();

        var targetGraph = new ViewconeNavGraph(level, staticNavGraph, controller.GetStaticGameRepr(),
            innerViewconeRayCount,
            viewconesGraphDistances,
            1);

        var state = LevelState.GetInitialLevelState(level, 0.5f);
        if(navGraphTiming > 0) {
            var simu = new GameSimulator(controller.GetStaticGameRepr(), level)
                .Initialized(state, staticNavGraph);
            var st = new LevelStateTimed(state, navGraphTiming);
            state = simu.Simulate(st, targetGraph, new List<IGameAction>());
        }

        var views = targetGraph.GetRawViewcones(state);

        Gizmos.color = Color.green;
        foreach(var view in views) {
            var previous = view[view.Count - 1];
            foreach(var pos in view) {
                Gizmos.DrawLine(previous, pos);
                previous = pos;
            }
        }
        Gizmos.color = Color.yellow;
        foreach(var view in views) {
            foreach(var pos in view) {
                Gizmos.DrawSphere(pos, 0.3f);
            }
        }
    }

    private LevelRepresentation BackwardsCompatibleLevel() {
        var level = testLevel.GetLevelRepresentation();
        var enemies = level.Enemies.ToList();
        for(int i = 0; i < enemies.Count; i++) {
            if(enemies[i].Path != null) {
                Path? path;
                if(enemies[i].Path.Commands == null)
                {
                    path = null;
                }else if(enemies[i].Path.Commands.Count == 1 
                    && enemies[i].Path.Commands[0] is OnlyWalkCommand) {
                    var c = enemies[i].Path.Commands[0];
                    path = new Path(true, new List<PatrolCommand>()
                        {new OnlyWaitCommand(c.Position, c.Running, c.TurnWhileMoving, c.TurningSide, 1)}
                        );
                }
                else
                {
                    path = enemies[i].Path;
                }
                enemies[i] = new Enemy(enemies[i].Position, enemies[i].Rotation, enemies[i].Type, path);
            }
        }
        return new LevelRepresentation(
            level.Obstacles,
            level.OuterObstacle,
            enemies,
            level.SkillsToPickup ?? new List<(IActiveGameActionProvider, Vector2)>(),
            level.SkillsStartingWith ?? new List<IActiveGameActionProvider>(),
            level.FriendlyStartPos,
            level.Goal);
    }
}
