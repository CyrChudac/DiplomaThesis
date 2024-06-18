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
            var p2 = Vector3.Distance(a, c);
            var p3 = Vector3.Distance(b, c);
            //by the law of cosines from:
            //https://stackoverflow.com/questions/1211212/how-to-calculate-an-angle-from-three-points
            var angle = (p1 * p1 + p3 * p3 - p2 * p2) / (2 * p1 * p3);
            angle = RadiansToDegrees((float)Math.Acos(angle));
            if(!IsLeftOfLine(a, b, c))
                angle = 360 - angle;
            return angle;
        }

        /// <returns>Angle in degrees <paramref name="from"/> -> <paramref name="towards"/> 
        /// compared to base rotation 0.</returns>
        public static float AngleTowards(Vector2 from, Vector2 towards) 
            => AngleOfABC(from + VectorFromAngle(0), from, towards);
        
        /// <summary>
        /// Finds out if the point <paramref name="c"/> is to the left of the line 
        /// going from <paramref name="lineA"/> to <paramref name="lineB"/>.
        /// </summary>
        public static bool IsLeftOfLine(Vector2 lineA, Vector2 lineB, Vector2 c) {
            return (lineB.x - lineA.x)*(c.y - lineA.y) - (lineB.y - lineA.y)*(c.x - lineA.x) > 0;
        }

        /// <summary>
        /// Finds out if the point <paramref name="c"/> is on the line 
        /// going from <paramref name="lineA"/> to <paramref name="lineB"/>.
        /// </summary>
        public static bool IsOnLine(Vector2 lineA, Vector2 lineB, Vector2 c) {
            return (lineB.x - lineA.x)*(c.y - lineA.y) - (lineB.y - lineA.y)*(c.x - lineA.x) == 0;
        }
    }
}
