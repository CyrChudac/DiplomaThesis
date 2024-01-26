using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GameScoring.NavGraphs
{
    internal class CurrentNavGraph
    {

        private Graph<ScoredTypedNode>? _currentNavGraph;
        public Graph<ScoredTypedNode> CurrentNav 
            => _currentNavGraph ?? throw new InvalidOperationException();

        private readonly List<Enemy> enemies;
        private readonly StaticNavGraph staticNavGraph;

        /// <param name="innerRays">Determines how many times is the viewcone fragmented.</param>
        /// <param name="viewconeLengthMod">The  viewcone is actually not a cone, it is section of triangles. 
        /// What is their length? 0 = viewcone length; 1 = maximal length within the trienalge.</param>
        /// <returns>List of viewcone bounderies of all given enemies.</returns>
        private List<List<Vector2>> EnemiesToViewcones(List<Enemy> enemies, int innerRays, StaticGameRepresentation staticGameRepr, float viewconeLengthMod = 0.5f) {
            var result = new List<List<Vector2>>();
            foreach(var e in enemies) {
                var es = staticGameRepr.GetEnemySettings(e.Type);
                var vr = es.viewconeRepresentation;
                var angleIncrease = vr.Angle / (innerRays + 1);
                var maxViewLength = vr.Length / (float)Math.Cos(angleIncrease / 2);
                //TODO: here we omit that viewcones can collide with objects
                //which is REALLY BAD
                var length = vr.Length + (maxViewLength - vr.Length) * viewconeLengthMod;

                var startAngle = e.Rotation - vr.Angle / 2;

                List<Vector2> points = new List<Vector2>();
                points.Add(e.Position);
                for(int i = 0; i < innerRays + 2; i++) {
                    var vec = Vector2Utils.VectorFromAngle(startAngle + angleIncrease * i);
                    points.Add(e.Position + vec * length);
                }
                result.Add(points);
            }
            return result;
        }

    }
}
