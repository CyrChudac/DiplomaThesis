using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore {
	public static class FloatEquality {
		public static bool AreEqual(float a, float b) {
			float epsilon = Math.Max(Math.Abs(a), Math.Abs(b)) * 0.000_1f;
			return Math.Abs(a - b) <= epsilon;
		}
		
		public static bool AreEqual(Vector2 v1, Vector2 v2)
			=> AreEqual(v1.x, v2.x) && AreEqual(v1.y, v2.y);

		public static bool LessOrEqual(float a, float b)
			=> a < b || AreEqual(a, b);

		public static bool MoreOrEqual(float a, float b)
			=> a > b || AreEqual(a, b);
	}
}
