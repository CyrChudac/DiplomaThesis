using UnityEngine;

namespace GameCreatingCore {

	[System.Serializable]
	public sealed class LevelGoal {
		[SerializeField]
		public Vector2 Position;
		[SerializeField]
		public float Radius;

		public LevelGoal(Vector2 position, float radius) {
			Position = position;
			Radius = radius;
		}

		public override string ToString() {
			return $"Goal: {Position}; {Radius}";
		}
	}
}