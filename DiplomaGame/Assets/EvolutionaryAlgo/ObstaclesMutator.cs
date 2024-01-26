using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;

public class ObstaclesMutator : MonoBehaviour
{


    public Obstacle MutateOuterObstacle(Obstacle previous) {
        return MutateObst(previous);
    }

    public List<Obstacle> MutateObsts(List<Obstacle> prev) {
        return prev;
    }

    private Obstacle MutateObst(Obstacle prev) {
        return prev;
    }

}
