using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCreatingCore;
using NavMeshPlus.Components;
using UnityEngine.AI;
using GameCreatingCore.LevelRepresentationData;

public class LevelInitializor : MonoBehaviour
{
    [SerializeField]
    private GameRunner gameRunner;
    [SerializeField]
    private GameController gameController;

    [Header("Obstacles")]
    [SerializeField]
    private Material changeableMaterial;
    [SerializeField]
    private Texture2D BlankTexture;
    [SerializeField]
    private Texture2D EnemyVisionModifiedTexture;
    [SerializeField]
    private Texture2D EnemyWalkModifiedTexture;
    [SerializeField]
    private Color friendlyWalkableColor;
    [SerializeField]
    private Color friendlyUNWalkableColor;
    [Tooltip("Obstacles take different sorting orders given their importance, reserve up to 10 numbers below this for obstacles.")]
    [SerializeField]
    private int obstacleSortingOrderMax;
    [SerializeField]
    private float outerMargin = 300f;

    [Header("Other")]
    [SerializeField]
    private EnemyProvider enemyProvider;
    [SerializeField]
    private PlayerProvider playerProvider;
    [SerializeField]
    private GameObject goal;
    [SerializeField]
    private CameraMovement camMovement;
    [SerializeField]
    private LevelProvider levelProvider;

    public LevelRepresentation currentLevel;

    // Start is called before the first frame update
    void Start()
    {
        currentLevel = levelProvider.GetLevel(true);
        var lr = currentLevel;
        var farOut = CalcFarOuter(lr.OuterObstacle.Shape);
        foreach(var o in lr.Obstacles.Where(o =>
            o.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable
            || o.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable
            || o.Effects.EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough)) {
            CreateObstacle(o, false, farOut);
        }
        CreateObstacle(lr.OuterObstacle, true, farOut);

        var statRepr = gameController.GetStaticGameRepr();
        for(int i = 0; i < NavMesh.GetSettingsCount(); i++) {
            var set = NavMesh.GetSettingsByIndex(i);
            set.agentRadius = statRepr.StaticMovementSettings.CharacterMaxRadius / 2;
        }

        CreateEnemies(lr.Enemies);

        var p = playerProvider.GetPlayer();
        p.transform.position = lr.FriendlyStartPos;

        var g = Instantiate(goal, transform);
        g.transform.position = lr.Goal.Position;
        g.transform.localScale *= lr.Goal.Radius * 2;

        camMovement.SetBounds(lr.OuterObstacle.BoundingBox);
        Vector2 camPosition = (lr.FriendlyStartPos + lr.OuterObstacle.BoundingBox.center) / 2;
        if(lr.OuterObstacle.BoundingBox.height * 1.1f < camMovement.cameraToMove.orthographicSize * 2) {
            camPosition = new Vector2(camPosition.x, lr.OuterObstacle.BoundingBox.center.y);
        }
        if(lr.OuterObstacle.BoundingBox.width * 1.1f < camMovement.cameraToMove.orthographicSize * 2 * camMovement.cameraToMove.aspect) {
            camPosition = new Vector2(lr.OuterObstacle.BoundingBox.center.x, camPosition.y);
        }
        camMovement.transform.position = new Vector3(camPosition.x, camPosition.y, camMovement.transform.position.z);
    }

    void CreateEnemies(IEnumerable<Enemy> enemies) {
        foreach(var e in enemies) {
            var go = enemyProvider.GetEnemy(e.Type);
            go.transform.position = e.Position;
            go.transform.rotation = Quaternion.Euler(0, 0, -e.Rotation);
        }
    }

    GameObject CreateObstacle(Obstacle obstacle, bool outside, Rect farOut) {
        var go = new GameObject("obstacle");

        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var pc = go.AddComponent<PolygonCollider2D>();
        pc.SetPath(0, obstacle.Shape.ToArray());

        System.Func<Vector2[], GameObject, PolygonCollider2D> pcFun = 
            outside ? ((vs, o) => GetOuterObstacleShape(vs, o, farOut)) : GetObstacleShape;

        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = PolygonToMeshWithUVs(pcFun(obstacle.Shape.ToArray(), gameObject), farOut);

        var mr = go.AddComponent<MeshRenderer>();
        mr.material = changeableMaterial;
        mr.material.SetColor("_Color", obstacle.Effects.FriendlyWalkEffect == WalkObstacleEffect.Walkable ?
            friendlyWalkableColor : friendlyUNWalkableColor);

        bool addNonSeeThrough = (obstacle.Effects.FriendlyWalkEffect == WalkObstacleEffect.Walkable
            && obstacle.Effects.EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough)
            || (obstacle.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable
            && obstacle.Effects.EnemyVisionEffect == VisionObstacleEffect.SeeThrough);
        mr.material.SetTexture("_Texture1", addNonSeeThrough ? EnemyVisionModifiedTexture : BlankTexture);
        
        bool addUNWalkable = 
            //(obstacle.Effects.FriendlyWalkEffect == WalkObstacleEffect.Walkable
            //&& obstacle.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable)
            //|| //this seems logical, but they player actually doesn't need to see where the enemy cannot go
            (obstacle.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable
            && obstacle.Effects.EnemyWalkEffect == WalkObstacleEffect.Walkable);
        mr.material.SetTexture("_Texture2", addUNWalkable ? EnemyWalkModifiedTexture : BlankTexture);
        mr.sortingOrder = GetSortingOrder(obstacle.Effects, outside);
        for(int i = 0; i < obstacle.Shape.Count; i++) {
            var child = new GameObject(i.ToString());
            child.transform.SetParent(go.transform, true);
            child.transform.position = obstacle.Shape[i];
        }
        AddObstacleEffects(obstacle, go, (g) => pcFun(obstacle.Shape.ToArray(), g));

        return go;
    }

    private int GetSortingOrder(ObstacleEffect obstacleEffect, bool isOutsideObstacle) {
        if(isOutsideObstacle)
            return obstacleSortingOrderMax;
        int val = obstacleSortingOrderMax - 1;
        val -= (obstacleEffect.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable ? 0 : 4);
        val -= (obstacleEffect.EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough ? 0 : 2);
        val -= (obstacleEffect.EnemyWalkEffect == WalkObstacleEffect.Unwalkable ? 0 : 1);
        return val;
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

    public Mesh PolygonToMeshWithUVs(PolygonCollider2D polygon, Rect farOut) {
        var mesh = PolygonToMesh(polygon);
        var uvs = new Vector2[mesh.vertices.Length];
        for(int i = 0; i < mesh.vertices.Length; i++) {
            float x = (mesh.vertices[i].x - farOut.xMin) / (farOut.width);
            float y = (mesh.vertices[i].y - farOut.yMin) / (farOut.height);
            uvs[i] = new Vector2(x, y);
        }
        mesh.uv = uvs;
        return mesh;
    }

    public PolygonCollider2D GetObstacleShape(Vector2[] obstacleShape, GameObject go) {
        var pc = go.AddComponent<PolygonCollider2D>();
        pc.pathCount = 1;
        pc.SetPath(0, obstacleShape);
        return pc;
    }

    public PolygonCollider2D GetOuterObstacleShape(Vector2[] obstacleShape, GameObject go) 
        => GetOuterObstacleShape(obstacleShape, go, CalcFarOuter(obstacleShape));
    private PolygonCollider2D GetOuterObstacleShape(Vector2[] obstacleShape, GameObject go, Rect farOutRect) { 
        var pc = go.AddComponent<PolygonCollider2D>();
        pc.pathCount = 2;
        pc.SetPath(0, new Vector2[] {
            new Vector2(farOutRect.xMin, farOutRect.yMin),
            new Vector2(farOutRect.xMin, farOutRect.yMax),
            new Vector2(farOutRect.xMax, farOutRect.yMax),
            new Vector2(farOutRect.xMax, farOutRect.yMin),
        });
        pc.SetPath(1, obstacleShape);
        return pc;
    }

    Rect CalcFarOuter(IReadOnlyList<Vector2> obstacleShape) {
        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
        foreach(var vec in obstacleShape) {
            xMin = Mathf.Min(xMin, vec.x);
            xMax = Mathf.Max(xMax, vec.x);
            yMin = Mathf.Min(yMin, vec.y);
            yMax = Mathf.Max(yMax, vec.y);
        }
        return new Rect(xMin - outerMargin,
            yMin - outerMargin,
            xMax - xMin + 2 * outerMargin,
            yMax - yMin + 2 * outerMargin);
    }
}
