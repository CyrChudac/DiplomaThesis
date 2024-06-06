using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore.GamePathing;
using GameCreatingCore;
using System.Linq;
using UnityEditor;

public class LevelTester : MonoBehaviour
{
    IGamePathSolver pather;
    public GameController controller;
    public UnityLevelRepresentation testLevel;
    public LevelInitializor initializor;
    public bool inflateObstacles = true;

    private List<Vector2> path;

    public float Score(LevelRepresentation ind)
        => Score(GetPath(ind, controller), ind);

    private float Score(List<Vector2> path, LevelRepresentation levelRepresentation) {
        return Mathf.Min(1, (path?.Count ?? 0) / (100.0f + levelRepresentation.Obstacles.Count));	
    }

    private List<Vector2> GetPath(LevelRepresentation ind, GameController gc)
        => pather.GetPath(gc.GetStaticGameRepr(), ind);
    

    [ContextMenu(nameof(Test))]
    public void Test() {
        Debug.Log("TEST: " + Score(testLevel.GetLevelRepresentation()));
    }

	private void OnValidate() {
        pather = new GameCreatingCore.GamePathing.NoEnemyGamePather(inflateObstacles);
        if(testLevel != null)
            path = GetPath(testLevel.GetLevelRepresentation(), controller);
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
        var lvl = testLevel.GetLevelRepresentation();
        DrawOuter(lvl.OuterObstacle);
        DrawObstacles(lvl.Obstacles);
        foreach(var e in lvl.Enemies) {
            DrawEnemy(e);
        }
        Gizmos.color = Color.Lerp(Color.yellow, Color.white, 0.8f);
        Gizmos.DrawSphere(lvl.Goal.Position, lvl.Goal.Radius);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(lvl.FriendlyStartPos, 2);
        if(path == null)
            return;
        Gizmos.color = new Color(0.1f, 1, 0.1f);
        var pre = lvl.FriendlyStartPos;
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
            if(obst.FriendlyWalkEffect == WalkObstacleEffect.Walkable)
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
        var offset = Vector2Utils.VectorFromAngle(enemy.Rotation);
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(enemy.Position + offset.normalized, 0.75f);
    }
}
