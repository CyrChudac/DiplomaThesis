using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GameCreatingCore.LevelStateData;
using GameCreatingCore.LevelSolving.Viewcones;
using System.Linq;
using System.Reflection.Emit;
using GameCreatingCore.LevelRepresentationData;
using GameCreatingCore.StaticSettings;
using System.Reflection;

namespace GameCreatingCore
{

    public class ViewconesCreator
    {
        protected readonly int innerRays;
        protected readonly LevelRepresentation level;
        
        protected readonly IReadOnlyDictionary<EnemyType, EnemyTypeInfo> lengthsDict;
        private readonly Dictionary<EnemyState, Viewcone> _enemyViewconeDict
            = new Dictionary<EnemyState, Viewcone>();

		public ViewconesCreator(LevelRepresentation level, StaticGameRepresentation staticGameRepr,
            int innerRays, float viewconeLengthMod = 0.5f) {

			this.level = level;
            this.innerRays = innerRays;

            var playerSpeed = staticGameRepr.PlayerSettings.movementRepresentation.WalkSpeed;
            var lenDic = new Dictionary<EnemyType, EnemyTypeInfo>();
            foreach (var t in level.Enemies
                    .Select(e => e.Type)
                    .Distinct())
            {
                var es = staticGameRepr.GetEnemySettings(t);
                var vr = es.viewconeRepresentation;
                var alertTime = staticGameRepr.GameDifficulty * vr.AlertingTimeModifier - 0.25f;
                var alertDistance = playerSpeed * alertTime;
                var angleIncrease = AngleIncr(vr.Angle);
                float GetLength(float alerted)
                {
                    var currLen = vr.ComputeLength(alerted);
                    var minViewLength = currLen / (float)Math.Cos(Vector2Utils.DegreesToRadians(angleIncrease / 2));
                    return currLen + (minViewLength - currLen) * viewconeLengthMod;
                } 
                lenDic.Add(t, new EnemyTypeInfo(GetLength, vr.Angle, alertDistance));
            }
            lengthsDict = lenDic;
		}

        float AngleIncr(float angle) => angle / (innerRays + 1);
        
		internal Viewcone GetViewcone(LevelState state, int index) {
            var es = state.enemyStates[index];
            Viewcone view;
            if(!_enemyViewconeDict.TryGetValue(es, out view)) {
                var length = GetViewconeLength(index, state);
                if(!es.Alive) {
                    view = new Viewcone(es.Position, new List<Vector2>(), index, lengthsDict[level.Enemies[index].Type], length, true);
                } else {
                    view = EnemyToViewcone(level.Enemies[index].Type, index, es, innerRays, length);
                } 
                _enemyViewconeDict.Add(es, view);
            }
            return view;
        }

        protected float GetViewconeLength(int enemyindex, LevelState state)
            => lengthsDict[level.Enemies[enemyindex].Type]
                .GetLength(state.enemyStates[enemyindex].ViewconeAlertLengthModifier);

		/// <summary>
		/// Gets the list of viewcones in the given <paramref name="state"/>.
		/// </summary>
		public List<List<Vector2>> GetRawViewcones(LevelState state)
        {
            var viewcones = new List<List<Vector2>>();
            for(int i = 0; i < state.enemyStates.Count; i++) {
                var view = GetViewcone(state, i);
                var rawV = Enumerable.Empty<Vector2>()
                        .Append(view.StartPos)
                        .Concat(view.EndingPoints)
                        .ToList();
                viewcones.Add(rawV);
            }
            return viewcones;
        }

        private Viewcone EnemyToViewcone(EnemyType type, int index, EnemyState state, int innerRays, float length)
        {

            var enemVisionObsts = level.Obstacles
                .Append(level.OuterObstacle)
                .Where(o => o.Effects.EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough)
                .ToList();

            float firstAngle = 2;

            var angle = lengthsDict[type].Angle;
            var angleIncrease = AngleIncr(angle - 2 * firstAngle);
            var startAngle = state.Rotation - angle / 2 + firstAngle;

            List<Vector2> points = new List<Vector2>();
            var vec = Vector2Utils.VectorFromAngle(startAngle - firstAngle);
            var p = GetIntersectionWithObstacles(state.Position, vec, length, enemVisionObsts);
            points.Add(p);
            for (int i = 0; i < innerRays + 2; i++)
            {
                vec = Vector2Utils.VectorFromAngle(startAngle + angleIncrease * i);
                p = GetIntersectionWithObstacles(state.Position, vec, length, enemVisionObsts);
                if(!(i == 0 && FloatEquality.AreEqual(p, points[0])))
                    points.Add(p);
            }
            vec = Vector2Utils.VectorFromAngle(state.Rotation + angle / 2);
            p = GetIntersectionWithObstacles(state.Position, vec, length, enemVisionObsts);
            if(!FloatEquality.AreEqual(p, points[^1]))
                points.Add(p);
            return new Viewcone(state.Position, points, index, lengthsDict[type], length, false);
        }
        
        /// <summary>
        /// Finds the closest intersection with <paramref name="obstacles"/> from <paramref name="from"/> in the given <paramref name="direction"/>.
        /// If there is no such intesection, return <paramref name="direction"/> * <paramref name="maxDistance"/>.
        /// </summary>
        /// <param name="direction">Is expected as a normalized vector.</param>
        private Vector2 GetIntersectionWithObstacles(Vector2 from, Vector2 direction,
            float maxDistance, IEnumerable<Obstacle> obstacles)
        {

            var end = from + direction * maxDistance;

            var result = end;
            var magn = maxDistance * maxDistance;

            foreach (var obst in obstacles)
            {
                if(obst.Shape.Count == 0)
                    continue;
                var pre = obst.Shape[obst.Shape.Count - 1];
                foreach (var p in obst.Shape)
                {
                    var i = LineIntersectionDecider.FindFirstIntersection(from, end, pre, p, true, true);
                    if (i.HasValue)
                    {
                        var currMagn = (i - from).Value.sqrMagnitude;
                        if (currMagn < magn)
                        {
                            magn = currMagn;
                            result = i.Value;
                        }
                    }
                    pre = p;
                }
            }
            return result;
        }


    }
}
