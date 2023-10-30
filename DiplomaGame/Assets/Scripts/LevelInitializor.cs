using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCreationCore;
using NavMeshPlus.Components;
using Unity.VisualScripting;

public class LevelInitializor : MonoBehaviour
{
    [SerializeField]
    private LevelCreatorProvider levelProvider;
    [SerializeField]
    private EnemyProvider enemyProvider;
    [SerializeField]
    private bool vocal = true;
    [SerializeField]
    private List<ObstacleMaterial> materials;
    [SerializeField]
    private Material defaultMaterial;
    [SerializeField]
    private GameObject player;
    [SerializeField]
    private List<NavMeshSurface> surfacesToBake;
    [SerializeField]
    private float outerMargin = 300f;

    // Start is called before the first frame update
    void Start()
    {
        int seed = Random.Range(0, int.MaxValue);
        if(vocal)
            Debug.Log($"Creating level using seed " + seed);
        var lr = levelProvider.GetLevelCreator().CreateLevel(seed);
        foreach(var o in lr.Obstacles) {
            CreateObstacle(o, false);
        }
        CreateObstacle(lr.OuterObstacle, true);

        foreach(var s in surfacesToBake) {
            s.BuildNavMeshAsync();
        }

        CreateEnemies(lr.Enemies);

        var p = Instantiate(player);
        p.transform.position = lr.FriendlyStartPos;
    }

    void CreateEnemies(IEnumerable<Enemy> enemies) {
        foreach(var e in enemies) {
            var go = enemyProvider.GetEnemy(e.Type, e.Path);
            go.transform.position = e.Position;
            go.transform.rotation = Quaternion.Euler(0, 0, e.Rotation);
        }
    }

    GameObject CreateObstacle(Obstacle obstacle, bool outside) {
        var go = new GameObject("obstacle");

        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var pc = go.AddComponent<PolygonCollider2D>();
        pc.SetPath(0, obstacle.Shape);

        System.Func<List<Vector2>, GameObject, PolygonCollider2D> pcFun = 
            outside ? AddOuterObstacleShape : AddObstacleShape;


        var mf = go.AddComponent<MeshFilter>();
        var pc2D = pcFun(obstacle.Shape, gameObject);
        mf.mesh = pc2D.CreateMesh(false, false);
        Destroy(pc2D);

        var mr = go.AddComponent<MeshRenderer>();

        var mater = materials
            ?.Where(m => m.FriendlyWalkEffect == obstacle.FriendlyWalkEffect &&
                 m.EnemyWalkEffect == obstacle.EnemyWalkEffect &&
                 m.FriendlyVision == obstacle.FriendlyVisionEffect &&
                 m.EnemyVision == obstacle.EnemyVisionEffect)
            .FirstOrDefault();
        mr.material = mater?.Material ?? defaultMaterial;

        AddObstacleEffects(obstacle, go, (g) => pcFun(obstacle.Shape, g));

        return go;
    }

    void AddObstacleEffects(Obstacle obstacle, GameObject go, 
        System.Func<GameObject, PolygonCollider2D> pcFun) {

        if(obstacle.FriendlyVisionEffect == VisionObstacleEffect.NonSeeThrough) {
            GetCollidingGameObject(obstacle.Shape, pcFun, go.transform, "FriendlySee");
        }
        if(obstacle.EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough) {
            GetCollidingGameObject(obstacle.Shape, pcFun, go.transform, "EnemySee");
        }
        if(obstacle.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable) {
            var ch = GetCollidingGameObject(obstacle.Shape, pcFun, go.transform, "FriendlyWalk");
            AddToNavmesh(ch);
        }else if(obstacle.FriendlyWalkEffect == WalkObstacleEffect.Fliable) {
            var ch = GetCollidingGameObject(obstacle.Shape, pcFun, go.transform, "FriendlyFly");
            AddToNavmesh(ch);
        }
        if(obstacle.EnemyWalkEffect == WalkObstacleEffect.Unwalkable) {
            var ch = GetCollidingGameObject(obstacle.Shape, pcFun, go.transform, "EnemyWalk");
            AddToNavmesh(ch);
        }else if(obstacle.EnemyWalkEffect == WalkObstacleEffect.Fliable) {
            var ch = GetCollidingGameObject(obstacle.Shape, pcFun, go.transform, "EnemyFly");
            AddToNavmesh(ch);
        }
    }

    GameObject GetCollidingGameObject(List<Vector2> points, 
        System.Func<GameObject, PolygonCollider2D> pcFun, Transform parent, string layer) {

        var go = new GameObject();
        pcFun(go);
        go.transform.SetParent(parent);
        go.layer = LayerMask.NameToLayer(layer);
        return go;
    }

    void AddToNavmesh(GameObject go) {
        var nvm = go.AddComponent<NavMeshModifier>();
        nvm.overrideArea = true;
        nvm.area = 1;
    }
    
    PolygonCollider2D AddObstacleShape(List<Vector2> obstacleShape, GameObject go) {
        var pc = go.AddComponent<PolygonCollider2D>();
        pc.pathCount = 1;
        pc.SetPath(0, obstacleShape);
        return pc;
    }

    PolygonCollider2D AddOuterObstacleShape(List<Vector2> obstacleShape, GameObject go) {
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

    //Mesh GetObstacleMesh(Rect obstacle) {
    //    var m = new Mesh();

    //    Vector3[] vertices = new Vector3[4];
    //    Vector2[] uv = new Vector2[vertices.Length];
    //    int[] triangles = new int[6];

    //    vertices[0] = new Vector3(obstacle.x, obstacle.y);
    //    vertices[1] = new Vector3(obstacle.x, obstacle.yMax);
    //    vertices[2] = new Vector3(obstacle.xMax, obstacle.y);
    //    vertices[3] = new Vector3(obstacle.xMax, obstacle.yMax);

    //    triangles[0] = 0;
    //    triangles[1] = 3;
    //    triangles[2] = 1;
    //    triangles[3] = 0;
    //    triangles[3] = 2;
    //    triangles[5] = 3;

    //    m.vertices = vertices;
    //    m.triangles = triangles;
    //    m.uv = uv;

    //    return m;
    //}

    //Mesh GetOuterObstacleMesh(Rect obstacle) {
        
    //    var m = new Mesh();
        
    //    Vector3[] vertices = new Vector3[8];
    //    Vector2[] uv = new Vector2[vertices.Length];
    //    int[] triangles = new int[24];

    //    vertices[0] = new Vector3(obstacle.x, obstacle.y);
    //    vertices[1] = new Vector3(obstacle.x, obstacle.yMax);
    //    vertices[2] = new Vector3(obstacle.xMax, obstacle.y);
    //    vertices[3] = new Vector3(obstacle.xMax, obstacle.yMax);
    //    var mult = 5;
    //    vertices[4] = new Vector3(obstacle.x - obstacle.width * mult, obstacle.y - obstacle.height * mult);
    //    vertices[5] = new Vector3(obstacle.x - obstacle.width * mult, obstacle.yMax + obstacle.height * mult);
    //    vertices[6] = new Vector3(obstacle.xMax + obstacle.width * mult, obstacle.y - obstacle.height * mult);
    //    vertices[7] = new Vector3(obstacle.xMax + obstacle.width * mult, obstacle.yMax + obstacle.height * mult);
        
    //    triangles[0] = 4;
    //    triangles[1] = 0;
    //    triangles[2] = 1;
    //    triangles[3] = 4;
    //    triangles[4] = 1;
    //    triangles[5] = 5;
        
    //    triangles[6] = 4;
    //    triangles[7] = 2;
    //    triangles[8] = 0;
    //    triangles[9] = 4;
    //    triangles[10] = 6;
    //    triangles[11] = 2;
        
    //    triangles[12] = 7;
    //    triangles[13] = 5;
    //    triangles[14] = 1;
    //    triangles[15] = 7;
    //    triangles[16] = 1;
    //    triangles[17] = 3;
        
    //    triangles[18] = 7;
    //    triangles[19] = 3;
    //    triangles[20] = 2;
    //    triangles[21] = 7;
    //    triangles[22] = 2;
    //    triangles[23] = 6;

    //    m.vertices = vertices;
    //    m.triangles = triangles;
    //    m.uv = uv;

    //    return m;
    //}
}
