using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore {
	
    public static class Vector2Utils {
        public static Vector2 Scaled(Vector2 from, Vector2 scale) {
            var res = new Vector2(from.x, from.y);
            res.Scale(scale);
            return res;
        }
    
        /// <summary>
        /// Make a vector of magnitude = 1 pointing at a given angle.
        /// </summary>
        /// <param name="degrees">Angle in degrees</param>
        /// <returns>A normalized 2D vector pointing at that angle</returns>
        public static Vector2 VectorFromAngle(float degrees)
        {
            float angle = DegreesToRadians(degrees);
            return new Vector2(-Mathf.Cos(angle), Mathf.Sin(angle));
        }
        
        public static float DegreesToRadians(float degrees)
            => degrees * (Mathf.PI / 180f);

        public static float RadiansToDegrees(float radians)
            => radians / (Mathf.PI / 180f);
        /// <summary>
        /// Anagle in degrees between lines |<paramref name="a"/><paramref name="b"/>| and |<paramref name="b"/><paramref name="c"/>|.
        /// </summary>

        public static float AngleOfABC(Vector2 a, Vector2 b, Vector2 c) {
            var p1 = Vector3.Distance(a, b);
            var p2 = Vector3.Distance(b, c);
            var p3 = Vector3.Distance(c, a);
            //by the law of cosine from:
            //https://stackoverflow.com/questions/1211212/how-to-calculate-an-angle-from-three-points
            var angle = DegreesToRadians((p1 * p1 + p3 * p3 - p2 * p2) / (2 * p1 * p3));
            return RadiansToDegrees((float)Math.Acos(angle)); 
        }

        /// <returns>Angle in degrees <paramref name="from"/> -> <paramref name="towards"/> 
        /// compared to base rotation 0.</returns>
        public static float AngleTowards(Vector2 from, Vector2 towards) 
            => AngleOfABC(VectorFromAngle(0), from, towards);
    }
}
