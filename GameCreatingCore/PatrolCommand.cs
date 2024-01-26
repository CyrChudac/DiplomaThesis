using UnityEngine;

namespace GameCreatingCore {
	
	[System.Serializable]
	public abstract class PatrolCommand {
		[SerializeField]
		public Vector2 Position;
		[SerializeField]
		public bool ExecuteDuringMoving;

		public PatrolCommand(Vector2 Position, bool ExecuteDuringMoving) {
			this.Position = Position;
			this.ExecuteDuringMoving = ExecuteDuringMoving;
		}
		public abstract void StartExecution();
		public abstract bool ExecutionFinished { get; }
	}
}