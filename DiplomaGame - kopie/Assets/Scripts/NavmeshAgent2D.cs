using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavmeshAgent2D : MonoBehaviour
{
    [SerializeField]
    private RotationManager rotationManager;

    private NavMeshAgent agent;
    public  NavMeshAgent Agent => agent;

    private Coroutine movingCoroutine = null;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Repair2D(agent);
    }

    public static void Repair2D(NavMeshAgent agent) {
		agent.updateRotation = false;
		agent.updateUpAxis = false;
    }

    const float agentDrift = 0.0001f;
	public void SetDestination(Vector3 destination) {
        NavMeshPath path = new NavMeshPath();
        CalculatePath(destination, path);
        SetPath(path);
    }
    
    public bool CalculatePath(Vector3 destination, NavMeshPath storeIn) 
        => agent.CalculatePath(CalculateDestination(destination), storeIn);

    public void SetPath(NavMeshPath path) {
        if(movingCoroutine != null)
            StopCoroutine(movingCoroutine);
        movingCoroutine = StartCoroutine(MoveCoroutine(path));
    }

	public void Stop() {
        if(movingCoroutine != null)
            StopCoroutine(movingCoroutine);
        agent.ResetPath();
	}

	private IEnumerator MoveCoroutine(NavMeshPath path) {
        Debug.Log(path.corners.Length);
        foreach(var c in path.corners) {
            //calculating the path again, should be superquick since the points are in line of free walk
            agent.SetDestination(CalculateDestination(c)); 
            rotationManager.LookAt(c);
            while(!InterDestinationReached()) {
                yield return null;
            }
        }
        movingCoroutine = null;
    }
    
    public bool DestinationReached() {
        return movingCoroutine == null && InterDestinationReached();
    }

    private bool InterDestinationReached() {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private Vector3 CalculateDestination(Vector3 destination) {
		if(Mathf.Abs(transform.position.x - destination.x) < agentDrift)
            destination += new Vector3(-agentDrift, 0f, 0f);
        return destination;
    }
}
