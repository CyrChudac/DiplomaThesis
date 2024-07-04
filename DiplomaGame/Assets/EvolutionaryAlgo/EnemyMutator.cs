using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;
using GameCreatingCore.GamePathing;
using System;
using static UnityEditor.PlayerSettings;
using System.Linq;
using UnityEngine.UIElements;

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
    
    [Range(0f, 1f)]
    [SerializeField] private float changePosProb = 0.1f;
    [Min(1f)]
    [SerializeField] private float changePosMax = 20f;

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

    [SerializeField] private GameController gameController;

    public List<Enemy> MutateEnems(List<Enemy> prev, EvolAlgoUtils utils, 
        Obstacle outerObstacle, ICollection<Obstacle> enemyWalkObstacles) {
        var result = new List<Enemy>(prev);
        int count = result.Count;
        if(enemyRemoveProbCurve.Evaluate(count) > utils.RandomFloat() && result.Count > 0) {
            result.RemoveAt(utils.RandomInt(0, count));
        }
        if(enemyAddProbCurve.Evaluate(count) > utils.RandomFloat()) {
            var pos = RandomPosInside(utils, outerObstacle, enemyWalkObstacles);
            result.Add(new Enemy(
                pos,
                utils.RandomInt(0, 360),
                EnemyType.Basic,
                path: null));
        }
        for(int i = 0; i < result.Count; i++) {
            if(utils.RandomFloat() < mutateProb) {
                result[i] = MutateEnem(result[i], utils, enemyWalkObstacles, outerObstacle);
            }
        }
        return result;
    }

    private Vector2 RandomPosInside(EvolAlgoUtils utils, Obstacle outerObstacle, IEnumerable<Obstacle> obsts)
        => RandomPosInside(outerObstacle, obsts, () => utils.RandomVec(outerObstacle.BoundingBox));

    private Vector2 RandomPosInside(Obstacle outerObstacle, IEnumerable<Obstacle> obsts, Func<Vector2> vecFunc) {
        
        var pos = vecFunc();
        while((outerObstacle.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable && !outerObstacle.ContainsPoint(pos, false))
            || obsts.Any(o => o.ContainsPoint(pos, true))) {
            pos = vecFunc();
        }
        return pos;
    }

    private Enemy MutateEnem(Enemy prev, EvolAlgoUtils utils, 
        IEnumerable<Obstacle> enemyObsts, Obstacle outerObstacle) {

        var pos = EnemyPositionMutation(prev.Position, utils, enemyObsts, outerObstacle);
        var path = EnemyPathCorrection(prev.Path, prev.Position, pos);
        var newPath = AddDeleteOrMutateEnemyPath(path, pos, utils, outerObstacle, enemyObsts);
        newPath = EnemyPathCorrection(path, pos, newPath);
        return new Enemy(
            pos,
            EnemyRotationMutation(prev.Rotation, utils),
            prev.Type,
            newPath);
    }
    
    private Vector2 EnemyPositionMutation(Vector2 pos, EvolAlgoUtils utils, 
        IEnumerable<Obstacle> enemyObsts, Obstacle outerObst) {
        if(utils.RandomFloat() < changePosProb) {
            var ch = RandomPosInside(outerObst, enemyObsts,
                () => pos + utils.RandomNormVec(changePosMax, changePosMax));
            pos = ch;
        }
        return pos;
    }

    private float EnemyRotationMutation(float previous, EvolAlgoUtils utils) {
        return utils.RandomFloat() < rotateProb
            ? previous + (utils.RandomFloat() - 0.5f) * 2 * _rotateMaxChange
            : previous;
    }
    
    /// <summary>
    /// Checks whether <paramref name="path"/> has the first position equal to <paramref name="prePos"/> and if so,
    /// sets it's first position to <paramref name="newPos"/>. In other words use after POS change.
    /// </summary>
    private Path? EnemyPathCorrection(Path? path, Vector2 prePos, Vector2 newPos) {
        if(path != null && path.Commands[0].Position == prePos)
            path.Commands[0].Position = newPos;
        return path;
    }
    
    /// <summary>
    /// Checks whether <paramref name="path"/> has the first position equal to <paramref name="prePos"/> and if so,
    /// sets it's first position of <paramref name="newPath"/> to <paramref name="prePos"/>. In other words use after PATH change.
    /// </summary>
    private Path? EnemyPathCorrection(Path? path, Vector2 prePos, Path? newPath) {
        if(path != null && newPath != null && path.Commands[0].Position == prePos)
            newPath.Commands[0].Position = prePos;
        return newPath;
    }

    private Path? AddDeleteOrMutateEnemyPath(Path? previous, Vector2 position, EvolAlgoUtils utils, 
        Obstacle outerObstacle, IEnumerable<Obstacle> obsts) {

        if(previous == null) {
            if(utils.RandomFloat() < _pathAddProb) {
                var pos = CreateFirstCommandPos(position, utils, outerObstacle, obsts);

                return EnemyPathMutation(
                    new Path(utils.RandomFloat() < _pathCyclicStartProb, new List<PatrolCommand>() {
                        GetOnlyWalkCommand(pos)
                    }), position, utils, outerObstacle, obsts);
            }
        } else {
            if(utils.RandomFloat() < _pathRemoveProb) {
                return null;
            }
            if(utils.RandomFloat() < _pathMutationProb)
                return EnemyPathMutation(previous, position, utils, outerObstacle, obsts);
        }
        return previous;
    }
    
    private Vector2 CreateFirstCommandPos(Vector2 prevPos, EvolAlgoUtils utils, 
        Obstacle outerObstacle, IEnumerable<Obstacle> obsts) {
        Vector2 pos;

        if(utils.RandomFloat() < _pathFirstPointNotPosProb) {
            pos = RandomPosInside(utils, outerObstacle, obsts);
        } else {
            pos = prevPos;
        }
        return pos;
    }

    private OnlyWalkCommand GetOnlyWalkCommand(Vector2 pos)
        => new OnlyWalkCommand(pos, 
            false, GameCreatingCore.GamePathing.GameActions.TurnSideEnum.ShortestPrefereClockwise);

    private Path? EnemyPathMutation(Path pre, Vector2 position, EvolAlgoUtils utils, 
        Obstacle outerObstacle, IEnumerable<Obstacle> obsts) {

        var cmds = new List<PatrolCommand>(pre.Commands);
        if(_pathRemovePointProbCurve.Evaluate(cmds.Count) > utils.RandomFloat()) {
            cmds.RemoveAt(utils.RandomInt(0, cmds.Count));
            if(cmds.Count == 0)
                return null;
        }
        if(_pathAddPointProbCurve.Evaluate(cmds.Count) > utils.RandomFloat()) {
            Vector2 pos;
            if(cmds.Count == 0) {
                pos = CreateFirstCommandPos(position, utils, outerObstacle, obsts);
            } else {
                pos = RandomPosInside(outerObstacle, obsts,
                    () => cmds[cmds.Count - 1].Position + utils.RandomNormVec(15, 15));
            }
            cmds.Add(
                GetOnlyWalkCommand(pos));
        }
        for(int i = 0; i < cmds.Count; i++) {
            if(utils.RandomFloat() < _pathChangeCommandPosProb) {
                cmds[i].Position = RandomPosInside(
                    outerObstacle,
                    obsts,
                    () => cmds[i].Position + utils.RandomNormVec(_pathCommandPosChangeMax, _pathCommandPosChangeMax));
            }
        }
        if(cmds.Count > 1 && utils.RandomFloat() < _pathSwapCommandsProb) {
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
