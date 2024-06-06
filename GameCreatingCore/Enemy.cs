using UnityEngine;

namespace GameCreatingCore {
	[System.Serializable]
	public sealed class Enemy {
		[SerializeField]
		public Vector2 Position;
		[SerializeField]
		public float Rotation;
		[SerializeField]
		public EnemyType Type;
		[SerializeField]
		public Path? Path;

		public Enemy(Vector2 position, float rotation, EnemyType type, Path? path) {
			Position = position;
			Rotation = rotation;
			Type = type;
			Path = path;
		}

		public override string ToString() {
			return $"Enemy:{Type}; {Position}";
		}
	}
}