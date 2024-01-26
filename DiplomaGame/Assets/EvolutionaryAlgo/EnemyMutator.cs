using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;
using System;

public class EnemyMutator : MonoBehaviour
{
    
    [Tooltip("Given the amount of enemies, what is the probability of adding a new one.")]
    [SerializeField] private AnimationCurve enemyAddProbCurve;

    [Tooltip("Given the amount of enemies, what is the probability of adding a new one.")]
    [SerializeField] private AnimationCurve enemyRemoveProbCurve;
    
    [Range(0f, 1f)]
    [SerializeField] private float mutateProb = 0.05f;

    [Range(0f, 1f)]
    [SerializeField] private float rotateProb = 0.05f;
    [Range(0f, 180f)]
    [SerializeField] private float _rotateMaxChange = 10f;

    [Header("Path Mutations")]
    [Range(0f, 1f)]
    [SerializeField] private float _pathAddProb = 0.015f;
    [Range(0f, 1f)]
    [SerializeField] private float _pathRemoveProb = 0.07f;
    [Range(0f, 1f)]
    [SerializeField] private float _pathMutationProb = 0.15f;
    [Range(0f, 1f)]
    [SerializeField] private float _pathCyclicStartProb = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float _pathCyclicChangeProb = 0.0f;

    
    [Tooltip("Given the amount of path commands, what is the probability of adding a new one.")]
    [SerializeField] private AnimationCurve _pathAddPointProbCurve;

    [Tooltip("Given the amount of path commnads, what is the probability of removing a random one.")]
    [SerializeField] private AnimationCurve _pathRemovePointProbCurve;
    
    [Range(0f, 0.15f)]
    [SerializeField] private float _pathFirstPointNotPosProb = 0.0075f;
    
    [Range(0f, 0.25f)]
    [SerializeField] private float _pathSwapCommandsProb = 0.01f;
    
    [Range(0f, 0.25f)]
    [SerializeField] private float _pathChangeCommandPosProb = 0.005f;

    [Min(0f)]
    [SerializeField] private float _pathCommandPosChangeMax = 10f;

    public List<Enemy> MutateEnems(List<Enemy> prev, EvolAlgoUtils utils, Obstacle outerObstacle) {
        var result = new List<Enemy>(prev);
        int count = result.Count;
        if(enemyRemoveProbCurve.Evaluate(count) > utils.RandomFloat()) {
            result.RemoveAt(utils.RandomInt(0, count));
        }
        if(enemyAddProbCurve.Evaluate(count) > utils.RandomFloat()) {
            
            result.Add(new Enemy(
                RandomPosInside(utils, outerObstacle),
                utils.RandomInt(0, 360),
                EnemyType.Basic,
                path: null));
        }
        for(int i = 0; i < result.Count; i++) {
            if(utils.RandomFloat() < mutateProb) {
                result[i] = MutateEnem(result[i], utils, outerObstacle);
            }
        }
        return result;
    }

    private Vector2 RandomPosInside(EvolAlgoUtils utils, Obstacle outerObstacle)
        => RandomPosInside(outerObstacle, () => utils.RandomVec(outerObstacle.BoundingBox));
    private Vector2 RandomPosInside(Obstacle outerObstacle, Func<Vector2> vecFunc) {
        var pos = vecFunc();
        while(!outerObstacle.ContainsPoint(pos)) {
            pos = vecFunc();
        }
        return pos;
    }

    private Enemy MutateEnem(Enemy prev, EvolAlgoUtils utils, Obstacle outerObstacle) {
        var pos = prev.Position;
        var path = EnemyPathCorrection(prev.Path, prev.Position, pos);
        return new Enemy(
            pos,
            EnemyRotationMutation(prev.Rotation, utils),
            prev.Type,
            AddDeleteOrMutateEnemyPath(path, pos, utils, outerObstacle));
    }
    
    private float EnemyRotationMutation(float previous, EvolAlgoUtils utils) {
        return utils.RandomFloat() < rotateProb
            ? previous + (utils.RandomFloat() - 0.5f) * 2 * _rotateMaxChange
            : previous;
    }

    private Path? EnemyPathCorrection(Path? path, Vector2 prePos, Vector2 newPos) {
        if(path != null && path.Commands[0].Position == prePos)
            path.Commands[0].Position = newPos;
        return path;
    }

    private Path? AddDeleteOrMutateEnemyPath(Path? previous, Vector2 position, EvolAlgoUtils utils, Obstacle outerObstacle) {
        if(previous == null) {
            if(utils.RandomFloat() < _pathAddProb) {
                return EnemyPathMutation(
                    new Path(utils.RandomFloat() < 0.2f, new List<PatrolCommand>()), position, utils, outerObstacle);
            }
        } else {
            if(utils.RandomFloat() < _pathRemoveProb) {
                return null;
            }
            if(utils.RandomFloat() < _pathMutationProb)
                return EnemyPathMutation(previous, position, utils, outerObstacle);
        }
        return previous;
    }
    
    private Path EnemyPathMutation(Path pre, Vector2 position, EvolAlgoUtils utils, Obstacle outerObstacle) {
        var cmds = new List<PatrolCommand>(pre.Commands);
        int count = cmds.Count;
        if(_pathRemovePointProbCurve.Evaluate(count) > utils.RandomFloat()) {
            cmds.RemoveAt(utils.RandomInt(0, count));
        }
        if(_pathAddPointProbCurve.Evaluate(count) > utils.RandomFloat()) {
            Vector2 pos;
            if(count == 0) {
                if(utils.RandomFloat() < _pathFirstPointNotPosProb) {
                    pos = RandomPosInside(utils, outerObstacle);
                } else {
                    pos = position;
                }
            } else {
                pos = RandomPosInside(outerObstacle,
                    () => cmds[count - 1].Position + utils.RandomNormVec(6, 6));
            }
            cmds.Add(
                new OnlyWalkCommand(pos));
        }
        for(int i = 0; i < cmds.Count; i++) {
            if(utils.RandomFloat() < _pathChangeCommandPosProb) {
                cmds[i].Position = RandomPosInside(
                    outerObstacle,
                    () => cmds[i].Position + utils.RandomNormVec(_pathCommandPosChangeMax, _pathCommandPosChangeMax));
            }
        }
        if(utils.RandomFloat() < _pathSwapCommandsProb) {
            var f = utils.RandomInt(cmds.Count);
            int s;
            while(true) {
                s = utils.RandomInt(cmds.Count);
                if(s != f) 
                    break;
            }
            var tmp = cmds[s]; cmds[s] = cmds[f]; cmds[f] = tmp;
        }
        return new Path(
            utils.RandomFloat() < _pathCyclicChangeProb ? !pre.Cyclic : pre.Cyclic,
            cmds);
    }
}
