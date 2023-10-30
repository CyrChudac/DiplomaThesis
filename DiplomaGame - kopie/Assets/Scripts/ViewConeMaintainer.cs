using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Animations;
using UnityEngine.UIElements;
using System.Linq;
using Unity.VisualScripting;

public class ViewConeMaintainer : MonoBehaviour {
    [Tooltip("The person this viewcone belongs to.")]
    [SerializeField] private GameObject agent;
    /// <summary>
    /// The person this viewcone belongs to.
    /// </summary>
    public void SetAgent(GameObject agent) => this.agent = agent;

    [Tooltip("What can the agent NOT see through.")]
    [SerializeField] private LayerMask notSeeThrough;
    /// <summary>
    /// What can the agent NOT see through.
    /// </summary>
    public void SetNotSeeThrough(LayerMask notSeeThrough) => this.notSeeThrough = notSeeThrough;

    [Tooltip("What is the agent trying to spot.")]
    [SerializeField] private LayerMask whatToSpot;
    /// <summary>
    /// What is the agent trying to spot.
    /// </summary>
    public void SetWhatToSpot(LayerMask whatToSpot) => this.whatToSpot = whatToSpot;

    [Tooltip("What is the smallest distance the enemy sees.")]
    [Min(0f)]
    [SerializeField] protected float startAt = 0f;
    /// <summary>
    /// What is the smallest distance the enemy sees.
    /// </summary>
    public float SetStartAt(float startAt) => this.startAt = Mathf.Max(startAt, 0);

    [Tooltip("How wide the viewcone is (angle in degrees).")]
    [Range(0f, 360f)]
    [SerializeField] private float viewAngle = 60;
    /// <summary>
    /// How wide the viewcone is (angle in degrees).
    /// </summary>
    public float SetViewAngle(float viewAngle) => this.viewAngle = Mathf.Clamp(viewAngle, 0, 360);

    [Tooltip("Determines whether the viewcone is displayed.")]
    [SerializeField] private bool displayViewcone = true;
    /// <summary>
    /// Determines whether the viewcone is displayed.
    /// </summary>
    public void SetDisplayViewcone(bool displayViewcone) => this.displayViewcone = displayViewcone;

    [Tooltip("Modifies how many rays will be casted. Smaller number = more rays and less performence.")]
    [SerializeField] private float viewRaysModifier = 50f;
    /// <summary>
    /// Modifies how many rays will be casted. Smaller number = more rays and less performence.
    /// </summary>
    public void SetViewRaysModifier(float viewRaysModifier) => this.viewRaysModifier = viewRaysModifier;

    [Tooltip("The sorting order of the meshes (it can be the same for all since they do not overlap).")]
    [SerializeField] private int sortingOrder = -2;
    /// <summary>
    /// The sorting order of the meshes (it can be the same for all since they do not overlap).
    /// </summary>
    public void SetSortingOrder(int sortingOrder) => this.sortingOrder = sortingOrder;

    /// <summary>
    /// The meshes generated by this script.
    /// </summary>
    private Mesh[] Meshes => meshFilters.Select(m => m.mesh).ToArray();
    private MeshFilter[] meshFilters;

    protected IViewconePartSpecifier[] ViewconeParts { private get; set; }

    public List<GameObject>[] spottedObjects;

    /// <summary>
    /// How many rays will be cast to form the round shape ot the viewcone
    /// </summary>
    int RayCount => Mathf.RoundToInt(viewAngle / (viewRaysModifier / 10f));
    /// <summary>
    /// Which direction does the first of the viewcone go
    /// </summary>
    float StartingAngle => -viewAngle / 2;
    /// <summary>
    /// Angle after which to check if there is an obstacle in the way
    /// </summary>
    private float AngleIncrease => viewAngle / RayCount;


    protected virtual void Start() {
        meshFilters = new MeshFilter[ViewconeParts.Length];
        spottedObjects = new List<GameObject>[ViewconeParts.Length];
        //generate the meshes and attach it to the object
        for(int i = 0; i < ViewconeParts.Length; i++) {
            var part = ViewconeParts[i];
            meshFilters[i] = CreateMeshBearer(part);
            spottedObjects[i] = new List<GameObject>();
        }
    }

    private MeshFilter CreateMeshBearer(IViewconePartSpecifier viewconePart) {
        Mesh mesh = new Mesh();
        //create child
        GameObject go = new GameObject("viewcone: " + viewconePart.Name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.SetActive(viewconePart.DisplayThis);
        //set mesh filter on it
        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        //and also mesh meshRenderer
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = viewconePart.Material;
        mr.sortingOrder = sortingOrder;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return mf;
    }

    protected virtual void Update() {
        UpdateVision();
        if(displayViewcone) {
            var dists = PrecomputeMeshDistances(CumulLen());
            float curr = startAt;
            for(int i = 0; i < ViewconeParts.Length; i++) {
                meshFilters[i].gameObject.SetActive(ViewconeParts[i].DisplayThis);
                if(ViewconeParts[i].DisplayThis) {
                    UpdateMesh(Meshes[i], curr, ViewconeParts[i].Length, dists);
                }
                curr += ViewconeParts[i].Length;
            }
        }
    }

    private float CumulLen()
        => ViewconeParts
            .Select(p => p.Length)
            .Append(startAt)
            .Sum();

    private float[] PrecomputeMeshDistances(float maxLen) {

        float currAngle = StartingAngle;

        float[] result = new float[RayCount];

        for(int i = 0; i < RayCount; i++) {
            result[i] = GetDistanceInDirection(currAngle, maxLen);
            currAngle += AngleIncrease;
        }

        return result;
    }

    private float GetDistanceInDirection(float angle, float maxLen) {
        //find where is the viewcone cast from
        Vector3 origin = transform.position;
        RaycastHit2D raycast;
        //find out what does the ray from the agent hit in the given angle
        raycast = Physics2D.Raycast(origin,
            Vector2Utils.VectorFromAngle(angle - agent.transform.rotation.eulerAngles.z + 180),
            maxLen,
            notSeeThrough);
        //if nothing, simply set the end of the view at this angle to max distance
        if(raycast.collider == null) {
            return maxLen;
        }
        //and if something, set it to be at the position of the thing hit
        else {
            return Vector3.Distance(raycast.point, origin);
        }
    }

    private void UpdateMesh(Mesh mesh, float from, float length, float[] distances)
    {
        List<Vector3> vertices = new List<Vector3>(2 * RayCount);
        List<int> triangles = new List<int>(2 * (RayCount - 1) * 3);

        Vector2 antiscale = new Vector2(1 / agent.transform.localScale.x, 1 / agent.transform.localScale.y);

        int? lastLastMax = null;
        (float, int) lastMax = (-1, -1);
        int? lastMin = null;

        Vector3 GetVertex(float angle, float vertexLength) {
            var offset = (Vector3)Vector2Utils.VectorFromAngle(angle) * vertexLength;
            return Vector2Utils.Scaled(offset, antiscale);
        }

        float currAngle = StartingAngle;
        for (int i = 1; i < RayCount; i++)
        {
            float currMax = Mathf.Min(distances[i], distances[i - 1], length + from);
            if(currMax <= from) {
                lastLastMax = null;
                lastMax = (-1, -1);
                lastMin = null;
                currAngle += AngleIncrease;
                continue;
            }
            int maxIpre;
            if(currMax == lastMax.Item1) {
                maxIpre = lastMax.Item2;
            } else {
                vertices.Add(GetVertex(currAngle, currMax));
                maxIpre = vertices.Count - 1;
            }
            int minIpre;
            if(lastMin.HasValue) {
                minIpre = lastMin.Value;
            } else {
                vertices.Add(GetVertex(currAngle, from));
                minIpre = vertices.Count - 1;
            }

            currAngle += AngleIncrease;
            
            vertices.Add(GetVertex(currAngle, currMax));
            int maxInext = vertices.Count - 1;
            vertices.Add(GetVertex(currAngle, from));
            int minInext = vertices.Count - 1;
            
            if(currMax < lastMax.Item1) {
                triangles.Add(maxInext);
                triangles.Add(maxIpre);
                triangles.Add(lastMax.Item2);
            }
            else if(currMax > lastMax.Item1 && lastLastMax.HasValue) {
                triangles.Add(maxIpre);
                triangles.Add(lastMax.Item2);
                triangles.Add(lastLastMax.Value);
            }

            //add triangles to the mesh
            triangles.Add(minIpre);
            triangles.Add(maxIpre);
            triangles.Add(maxInext);

            triangles.Add(minIpre);
            triangles.Add(maxInext);
            triangles.Add(minInext);

            lastLastMax = maxIpre;
            lastMax = (currMax, maxInext);
            lastMin = minInext;
        }

        //set the meshes to have the current vertices, triangles and uv
        mesh.triangles = new int[0];
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = new Vector2[vertices.Count];
    }

	private void UpdateVision() {
        Vector3 origin = transform.position;
        float currAngle = StartingAngle;
        List<GameObject>[] spottedObjects = new List<GameObject>[this.spottedObjects.Length];
        for(int i = 0; i < spottedObjects.Length; i++) {
            spottedObjects[i] = new List<GameObject>();
        }

        for (int i = 1; i < RayCount; i++)
        {
            RaycastHit2D raycast;
            float maxLen = CumulLen();
            //find out what does the ray from the agent hit in the given angle
            raycast =  Physics2D.Raycast(origin, 
                Vector2Utils.VectorFromAngle(currAngle + agent.transform.rotation.eulerAngles.y),
                maxLen,
                whatToSpot | notSeeThrough);
            //if it's something and it is the thing we are spotting
            if(raycast.collider != null 
                && whatToSpot == (whatToSpot | 1 << raycast.collider.gameObject.layer))
            {
                //then we add it to the spotted objects of that viewcone part
                int part = GetWhichPart(raycast.distance);
                spottedObjects[part].Add(raycast.collider.gameObject);
            }
            currAngle += AngleIncrease;
        }
        SetSpottedObjects(spottedObjects);
	}

    private int GetWhichPart(float distance) {
        float curr = startAt;
        for(int i = 0; i < ViewconeParts.Length; i++) {
            curr += ViewconeParts[i].Length;
            if(distance < curr) {
                return i;
            }
        }
        return -1;
    }

    private void SetSpottedObjects(List<GameObject>[] observed) {
        // match the found objects to the already known ones
        for(int i = 0; i < observed.Length; i++) {
            foreach(var g in observed[i].Distinct()) {
                if(!spottedObjects[i].Remove(g)) {
                    ViewconeParts[i].OnEnteredBy(g);
                }
            }
        }
        // removed the found objects, the remaining ones are now set as left
        for(int i = 0; i < spottedObjects.Length; i++) {
            foreach(var g in spottedObjects[i]) {
                ViewconeParts[i].OnLeftBy(g);
            }
        }
        spottedObjects = observed.Select(o => o.Distinct().ToList()).ToArray();
    }
}

static class Vector2Utils {
    public static Vector2 Scaled(Vector2 from, Vector2 scale) {
        var res = new Vector2(from.x, from.y);
        res.Scale(scale);
        return res;
    }
    
    /// <summary>
    /// Make a vector pointing at a given angle.
    /// </summary>
    /// <param name="degrees">Angle in degrees</param>
    /// <returns>A normalized 2D vector pointing at that angle</returns>
    public static Vector2 VectorFromAngle(float degrees)
    {
        float angle = degrees * (Mathf.PI / 180f);
        return new Vector2(-Mathf.Cos(angle), Mathf.Sin(angle));
    }
}

public interface IViewconePartSpecifier {
    string Name { get; }
    float Length { get; }
    void OnEnteredBy(GameObject by);
    void OnLeftBy(GameObject by);
    Material Material { get; }
    bool DisplayThis { get; }
}