using System;

namespace EvolAlgoBase {
	public class Scored<T> {
		public T Value { get; set; }
		public float Score { get; set; }
		public Scored(T value, float score) { 
			Value = value;
			Score = score;
		}
	}
}
