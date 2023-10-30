using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPathController : PathedGameObject
{
    [SerializeField]
    private NavmeshAgent2D agent;

    private int currentIndex = 0;
    private bool started = false;
    private bool executionStarted = false;
    private int step = 1;
    // Update is called once per frame
    void Update()
    {
        if(!started || !agent.Agent.isOnNavMesh || Path == null || Path.Commands.Count == 0)
            return;
        if(agent.DestinationReached()) {
            if(!executionStarted) {
                executionStarted = true;
                Path.Commands[currentIndex].StartExecution();
            }
            if(Path.Commands[currentIndex].ExecutionFinished 
                || Path.Commands[currentIndex].ExecuteDuringMoving) {

                executionStarted = false;
                currentIndex += step;
                if(currentIndex >= Path.Commands.Count || currentIndex < 0) {
                    if(Path.Cyclic) {
                        currentIndex %= Path.Commands.Count;
                    } else {
                        step *= -1;
                        currentIndex = Mathf.Clamp(currentIndex + 2 * step, 0, Path.Commands.Count);
                    }
                }
                agent.SetDestination(Path.Commands[currentIndex].Position);
            }
        }
    }

	protected override void OnResetPath() {
        started = true;
        currentIndex = 0;
	}
}
