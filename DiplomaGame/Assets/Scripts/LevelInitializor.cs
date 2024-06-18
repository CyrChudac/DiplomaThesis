using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCreatingCore;
using NavMeshPlus.Components;
using Unity.VisualScripting;
using UnityEngine.AI;

public class LevelInitializor : MonoBehaviour
{
    [SerializeField]
    private GameRunner gameRunner;
    [SerializeField]
    private EnemyProvider enemyProvider;
    [SerializeField]
    private List<ObstacleMaterial> materials;
    [SerializeField]
    private Material defaultMaterial;
    [SerializeField]
    private PlayerProvider playerProvider;
    [SerializeField]
    private GameObject goal;
    [SerializeField]
    private List<NavMeshSurface> surfacesToBake;
    [SerializeField]
    private float outerMargin = 300f;
    [SerializeField]
    private CameraMovement camMovement;
    [SerializeField]
    private GameController gameController;

    // Start is called before the first frame update
    void Start()
    {
        var lr = gameRunner.levelProvider.GetLevel();
        foreach(var o in lr.Obstacles) {
            CreateObstacle(o, false);
        }
        CreateObstacle(lr.OuterObstacle, true);

        var statRepr = gameController.GetStaticGameRepr();
        for(int i = 0; i < NavMesh.GetSettingsCount(); i++) {
            var set = NavMesh.GetSettingsByIndex(i);
            set.agentRadius = statRepr.StaticMovementSettings.CharacterMaxRadius / 2;
        }

        foreach(var s in surfacesToBake) {
            s.BuildNavMeshAsync();
        }

        CreateEnemies(lr.Enemies);

        var p = playerProvider.GetPlayer();
        p.transform.position = lr.FriendlyStartPos;

        var g = Instantiate(goal, transform);
        g.transform.position = lr.Goal.Position;
        g.transform.localScale *= lr.Goal.Radius;

        camMovement.SetBounds(lr.OuterObstacle.BoundingBox);
    }

    void CreateEnemies(IEnumerable<Enemy> enemies) {
        foreach(var e in enemies) {
            var go = enemyProvider.GetEnemy(e.Type);
            go.transform.position = e.Position;
            go.transform.rotation = Quaternion.Euler(0, 0, e.Rotation);
        }
    }

    GameObject CreateObstacle(Obstacle obstacle, bool outside) {
        var go = new GameObject("obstacle");

        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var pc = go.AddComponent<PolygonCollider2D>();
        pc.SetPath(0, obstacle.Shape.ToArray());

        System.Func<Vector2[], GameObject, PolygonCollider2D> pcFun = 
            outside ? GetOuterObstacleShape : GetObstacleShape;

        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = PolygonToMesh(pcFun(obstacle.Shape.ToArray(), gameObject));

        var mr = go.AddComponent<MeshRenderer>();

        var mater = materials
            ?.Where(m => m.FriendlyWalkEffect == obstacle.Effects.FriendlyWalkEffect &&
                 m.EnemyWalkEffect == obstacle.Effects.EnemyWalkEffect &&
                 m.FriendlyVision == obstacle.Effects.FriendlyVisionEffect &&
                 m.EnemyVision == obstacle.Effects.EnemyVisionEffect)
            .FirstOrDefault();
        mr.material = mater?.Material ?? defaultMaterial;
        for(int i = 0; i < obstacle.Shape.Count; i++) {
            var child = new GameObject(i.ToString());
            child.transform.SetParent(go.transform, true);
            child.transform.position = obstacle.Shape[i];
        }

        AddObstacleEffects(obstacle, go, (g) => pcFun(obstacle.Shape.ToArray(), g));

        return go;
    }

    void AddObstacleEffects(Obstacle obstacle, GameObject go, 
        System.Func<GameObject, PolygonCollider2D> pcFun) {

        if(obstacle.Effects.FriendlyVisionEffect == VisionObstacleEffect.NonSeeThrough) {
            GetCollidingGameObject(pcFun, go.transform, "FriendlySee");
        }
        if(obstacle.Effects.EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough) {
            GetCollidingGameObject(pcFun, go.transform, "EnemySee");
        }
        if(obstacle.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable) {
            var ch = GetCollidingGameObject(pcFun, go.transform, "FriendlyWalk");
            AddToNavmesh(ch);
        }
        if(obstacle.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable) {
            var ch = GetCollidingGameObject(pcFun, go.transform, "EnemyWalk");
            AddToNavmesh(ch);
        }
    }

    GameObject GetCollidingGameObject(System.Func<GameObject, PolygonCollider2D> pcFun, Transform parent, string layer) {
        var go = new GameObject(layer);
        pcFun(go); //adds polygon collider on the object
        go.transform.SetParent(parent);
        go.layer = LayerMask.NameToLayer(layer);
        return go;
    }

    void AddToNavmesh(GameObject go) {
        var nvm = go.AddComponent<NavMeshModifier>();
        nvm.overrideArea = true;
        nvm.area = 1;
    }
    
    public Mesh PolygonToMesh(PolygonCollider2D polygon) {
        var mesh = polygon.CreateMesh(false, false);
        DestroyImmediate(polygon);
        return mesh;
    }

    public PolygonCollider2D GetObstacleShape(Vector2[] obstacleShape, GameObject go) {
        var pc = go.AddComponent<PolygonCollider2D>();
        pc.pathCount = 1;
        pc.SetPath(0, obstacleShape);
        return pc;
    }

    public PolygonCollider2D GetOuterObstacleShape(Vector2[] obstacleShape, GameObject go) { 
        var pc = go.AddComponent<PolygonCollider2D>();
        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
        foreach(var vec in obstacleShape) {
            xMin = Mathf.Min(xMin, vec.x);
            xMax = Mathf.Max(xMax, vec.x);
            yMin = Mathf.Min(yMin, vec.y);
            yMax = Mathf.Max(yMax, vec.y);
        }
        pc.pathCount = 2;
        pc.SetPath(0, new Vector2[] {
            new Vector2(xMin - outerMargin, yMin - outerMargin),
            new Vector2(xMin - outerMargin, yMax + outerMargin),
            new Vector2(xMax + outerMargin, yMax + outerMargin),
            new Vector2(xMax + outerMargin, yMin - outerMargin),
        });
        pc.SetPath(1, obstacleShape);
        return pc;
    }
}
