using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;
using System.Linq;
using System;

//works with all obstacles as rectangles
public class ObstaclesMutator : MonoBehaviour
{
    [SerializeField]
    private float mutationProb = 0.07f;
    [SerializeField]
    private AnimationCurve addObstacleProbCurve;
    [SerializeField]
    private AnimationCurve removeObstacleProbCurve;
    [SerializeField]
    private float offsetObstacleProb;
    [SerializeField]
    private float offsetObstacleMax = 0.3f;
    [SerializeField]
    private float offsetOuterObstacleMax = 0.03f;
    [Header("Effects mutations")]
    [SerializeField]
    private float changeFriendlyWalkProb = 0.03f;
    [SerializeField]
    private float changeEnemyWalkProb = 0.03f;
    [SerializeField]
    private float changeFriendlyVisionProb = 0.03f;
    [SerializeField]
    private float changeEnemyVisionProb = 0.03f;
    [Header("New obstacles")]
    [SerializeField]
    private float initFriendlyWalkable = 0.5f;
    [SerializeField]
    private float initEnemyWalkable = 0.5f;
    [SerializeField]
    private float initFriendlySeeThrough = 0.5f;
    [SerializeField]
    private float initEnemySeeThrough = 0.5f;
    [SerializeField]
    private float minInitObstacleArea = 40f;
    [SerializeField]
    private float maxInitObstacleArea = 300f;
    [SerializeField]
    private float minObstacleOneAxisWidth = 5f;
    public Obstacle MutateOuterObstacle(Obstacle previous, EvolAlgoUtils utils, 
        ICollection<Vector2> enemyPositions, ICollection<Vector2> playerPositions) {

        var newoo = OffsetObstMutation(previous, utils, offsetOuterObstacleMax);
        int i = 0;
        while(!ContainsAllItShould(newoo, enemyPositions, playerPositions)) {
            newoo = OffsetObstMutation(previous, utils, offsetOuterObstacleMax);
            i++;
            if(i == 1000) {
                throw new Exception("Too many mutation iterations for outer obstacle.");
            }
        }
        return newoo;
    }

    public List<Obstacle> MutateObsts(List<Obstacle> prev, EvolAlgoUtils utils, Obstacle outerObstacle,
        ICollection<Vector2> enemyPositions, ICollection<Vector2> playerPositions) {
        var outer = EnlargeOuterObstacle(outerObstacle, utils, 0.25f);
        var result = new List<Obstacle>(prev);
        if(utils.RandomFloat() < addObstacleProbCurve.Evaluate(prev.Count)) {
            Obstacle toAdd = RandomObstacle(utils, outer, minInitObstacleArea, maxInitObstacleArea, minObstacleOneAxisWidth);
            while(IsObstacleWrong(toAdd, enemyPositions, playerPositions, outerObstacle)) {
                toAdd = RandomObstacle(utils, outer, minInitObstacleArea, maxInitObstacleArea, minObstacleOneAxisWidth);
            }
            result.Add(toAdd);
        }
        if(utils.RandomFloat() < removeObstacleProbCurve.Evaluate(prev.Count) && prev.Count > 0) {
            result.RemoveAt(utils.RandomInt(result.Count));
        }
        for(int i = 0; i < result.Count; i++) {
            if(utils.RandomFloat() < mutationProb) {
                var toChange = MutateObst(result[i], utils);
                while(IsObstacleWrong(toChange, enemyPositions, playerPositions, outerObstacle)) {
                    toChange = MutateObst(result[i], utils);
                }
                result[i] = toChange;
            }
        }
        return result;
    }

    
    private bool IsObstacleWrong(Obstacle o, ICollection<Vector2> enemyPositions,
        ICollection<Vector2> playerPositions, Obstacle outerObstacle) {

        return ContainsWhatItShouldnt(o, enemyPositions, playerPositions)
            || (!o.Shape.Any(p => outerObstacle.ContainsPoint(p)));
    }

    private bool ContainsWhatItShouldnt(Obstacle o, ICollection<Vector2> enemyPositions,
        ICollection<Vector2> playerPositions) {
        //TODO: o = Inflate(o);
        return (o.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable && enemyPositions.Any(ep => o.ContainsPoint(ep)))
            || (o.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable && playerPositions.Any(ep => o.ContainsPoint(ep)));
    }
    
    private bool ContainsAllItShould(Obstacle o, ICollection<Vector2> enemyPositions,
        ICollection<Vector2> playerPositions) {
        return (o.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable && enemyPositions.All(ep => o.ContainsPoint(ep)))
            || (o.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable && playerPositions.All(ep => o.ContainsPoint(ep)));
    }

    private Obstacle RandomObstacle(EvolAlgoUtils utils, Rect inArea, float minSize, float maxSize, float minAxisSize) {
        var first = utils.RandomVec(inArea);
        var x = utils.RandomFloat(minAxisSize, maxSize / minAxisSize);

        var y = utils.RandomFloat(Mathf.Max(minAxisSize, minSize / x), maxSize / x);
        
        if(utils.RandomFloat() < 0.5f)
            x *= -1;
        if(utils.RandomFloat() < 0.5f)
            y *= -1;
        var second = new Vector2(first.x + x, first.y + y);
        return new Obstacle(
            new List<Vector2>() {
                first,
                new Vector2(second.x, first.y),
                second,
                new Vector2(first.x, second.y)
            },
            new ObstacleEffect(
                utils.RandomFloat() < initFriendlyWalkable ? WalkObstacleEffect.Walkable : WalkObstacleEffect.Unwalkable,
                utils.RandomFloat() < initEnemyWalkable ? WalkObstacleEffect.Walkable : WalkObstacleEffect.Unwalkable,
                utils.RandomFloat() < initFriendlySeeThrough ? VisionObstacleEffect.SeeThrough : VisionObstacleEffect.NonSeeThrough,
                utils.RandomFloat() < initEnemySeeThrough ? VisionObstacleEffect.SeeThrough : VisionObstacleEffect.NonSeeThrough)
        );

    }

    private Obstacle ClampObstacle(Obstacle prev, Rect outer) {
        var bb = prev.BoundingBox;
        float x = bb.x,
            y = bb.y,
            width = bb.width, 
            height = bb.height;
        bool changed = false;
        if(x < outer.x) {
            x = outer.x;
            changed = true;
        }
        if(y < outer.y) {
            y = outer.y;
            changed = true;
        }
        if(x + width > outer.xMax) {
            width = outer.xMax - x;
            changed = true;
        }
        if(y + height > outer.yMax) {
            height = outer.yMax - y;
            changed = true;
        }
        if(!changed)
            return prev;
        return new Obstacle(
            new List<Vector2>() {
                new Vector2(x, y),
                new Vector2(x + width, y),
                new Vector2(x + width, y + height),
                new Vector2(x, y + height),
            },
            new ObstacleEffect(
                prev.Effects.FriendlyWalkEffect,
                prev.Effects.EnemyWalkEffect,
                prev.Effects.FriendlyVisionEffect,
                prev.Effects.EnemyVisionEffect)
        );
    }

    private Obstacle MutateObst(Obstacle prev, EvolAlgoUtils utils) {
        if(utils.RandomFloat() < offsetObstacleProb) {
            prev = OffsetObstMutation(prev, utils, offsetObstacleMax);
        }
        return ChangeObstacleEffect(prev, utils);
    }

    private Rect EnlargeOuterObstacle(Obstacle outerObst, EvolAlgoUtils utils, float ratio) {

        var bb = outerObst.BoundingBox;
        float x = bb.x - bb.width * ratio / 2,
            y = bb.y - bb.height * ratio / 2, 
            width = bb.width * (1 + ratio), 
            height = bb.height * (1 + ratio);
        return new Rect(x, y, width, height);
    }

    private Obstacle OffsetObstMutation(Obstacle prev, EvolAlgoUtils utils, float maxRatio) {

        var bb = prev.BoundingBox;
        float x = bb.x, y = bb.y, width = bb.width, height = bb.height;
        if(utils.RandomFloat() < 0.5f) {
            var change = utils.RandomNormFloat(-maxRatio * bb.width, 0, maxRatio * bb.width);
            width += change;
            x -= change * utils.RandomFloat();
        } else {
            var change = utils.RandomNormFloat(-maxRatio * bb.height, 0, maxRatio * bb.height);
            height += change;
            y -= change * utils.RandomFloat();
        }
        return new Obstacle(
            new List<Vector2>() {
                new Vector2(x, y),
                new Vector2(x + width, y),
                new Vector2(x + width, y + height),
                new Vector2(x, y + height),
            },
            new ObstacleEffect(
                prev.Effects.FriendlyWalkEffect,
                prev.Effects.EnemyWalkEffect,
                prev.Effects.FriendlyVisionEffect,
                prev.Effects.EnemyVisionEffect)
        );
    }

    private Obstacle ChangeObstacleEffect(Obstacle prev, EvolAlgoUtils utils) {
        var fWalkE = prev.Effects.FriendlyWalkEffect;
        var eWalkE = prev.Effects.EnemyWalkEffect;
        var fVisionE = prev.Effects.FriendlyVisionEffect;
        var eVisionE = prev.Effects.EnemyVisionEffect;
        if(utils.RandomFloat() < changeFriendlyWalkProb) {
            fWalkE = GetRandomEnumValue<WalkObstacleEffect>(utils);
        }
        if(utils.RandomFloat() < changeEnemyWalkProb) {
            eWalkE = GetRandomEnumValue<WalkObstacleEffect>(utils);
        }
        if(utils.RandomFloat() < changeFriendlyVisionProb) {
            fVisionE = GetRandomEnumValue<VisionObstacleEffect>(utils);
        }
        if(utils.RandomFloat() < changeEnemyVisionProb) {
            eVisionE = GetRandomEnumValue<VisionObstacleEffect>(utils);
        }
        return new Obstacle(prev.Shape, new ObstacleEffect(fWalkE, eWalkE, fVisionE, eVisionE));
    }

    private TEnum GetRandomEnumValue<TEnum>(EvolAlgoUtils utils) {
        var vals = System.Enum.GetValues(typeof(TEnum));
        var v = vals.GetValue(utils.RandomInt(vals.Length));
        return (TEnum)v;
    }
}
