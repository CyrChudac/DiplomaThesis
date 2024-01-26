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
            float angle = degrees * (Mathf.PI / 180f);
            return new Vector2(-Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
