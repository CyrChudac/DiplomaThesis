using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore {
	public static class FloatEquality {
		public static bool Equals(float a, float b) {
			float epsilon = Math.Max(Math.Abs(a), Math.Abs(b)) * 0.000_001f;
			return Math.Abs(a - b) <= epsilon;
		}
		
		public static bool LessOrEqual(float a, float b)
			=> a < b || Equals(a, b);

		public static bool MoreOrEqual(float a, float b)
			=> a > b || Equals(a, b);
	}
}
