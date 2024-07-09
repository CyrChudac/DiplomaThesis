using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore {
	public static class FloatEquality {
		public static bool AreEqual(float a, float b, float toleranceMultiplier = 1) {
			float epsilon = Math.Max(Math.Abs(a), Math.Abs(b)) * 0.000_1f * toleranceMultiplier;
			return Math.Abs(a - b) <= epsilon;
		}
		public static bool AreEqual(double a, double b, double toleranceMultiplier = 1) {
			double epsilon = Math.Max(Math.Abs(a), Math.Abs(b)) * 0.000_1D * toleranceMultiplier;
			return Math.Abs(a - b) <= epsilon;
		}
		
		public static bool AreEqual(Vector2 v1, Vector2 v2)
			=> AreEqual(v1.x, v2.x) && AreEqual(v1.y, v2.y);
		
		public static bool LessOrEqual(float a, float b, float toleranceMultiplier = 1)
			=> a < b || AreEqual(a, b, toleranceMultiplier);
		public static bool LessOrEqual(double a, double b, double toleranceMultiplier = 1)
			=> a < b || AreEqual(a, b, toleranceMultiplier);
		
		public static bool MoreOrEqual(float a, float b, float toleranceMultiplier = 1)
			=> a > b || AreEqual(a, b, toleranceMultiplier);
		public static bool MoreOrEqual(double a, double b, double toleranceMultiplier = 1)
			=> a > b || AreEqual(a, b, toleranceMultiplier);
	}
}
