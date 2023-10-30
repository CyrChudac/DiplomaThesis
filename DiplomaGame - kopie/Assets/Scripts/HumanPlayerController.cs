using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class HumanPlayerController : MonoBehaviour
{
    [SerializeField] 
    private NavmeshAgent2D player;
    [SerializeField] 
    private Camera cam;
    [SerializeField]
    [Range(0f, 0.25f)]
    private float timeCutoff = 0.05f;
    [SerializeField]
    private float distanceCutoff = 0.2f;

    [Header("Events")]

    [SerializeField]
    private UnityEvent OnDestinationUnreachable;

	private void Start() {
		if (cam == null) {
            Debug.LogWarning($"Player controller camera defaulting to {nameof(Camera)}.{nameof(Camera.main)}");
            cam = Camera.main;
        }
	}

    private float lastTime = -5000;
    private Vector3 lastHit;

	// Update is called once per frame
	void Update()
    {
        if(!player.Agent.isOnNavMesh) {
            return;
        }
        if(Input.GetMouseButton(0)) {
            var hit = Physics2D.Raycast(cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if(hit.collider != null 
                && ( (((Vector2)lastHit) - hit.point).sqrMagnitude > distanceCutoff * distanceCutoff || 
                Time.timeSinceLevelLoad > lastTime + timeCutoff)) {

                var path = new NavMeshPath();
                player.CalculatePath(hit.point, path);
                if(path.corners.Length > 0
                    && Vector2.Distance(hit.point, path.corners[path.corners.Length - 1]) < 0.3f) {
                    lastTime = Time.timeSinceLevelLoad;
                    lastHit = hit.point;
                    player.SetPath(path);
                } else {
                    player.Stop();
                    OnDestinationUnreachable.Invoke();
                }
            }
        }
    }
}
