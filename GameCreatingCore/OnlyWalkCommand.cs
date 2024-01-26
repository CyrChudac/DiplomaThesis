using UnityEngine;

namespace GameCreatingCore {

	[System.Serializable]
	public sealed class OnlyWalkCommand : PatrolCommand {
		public OnlyWalkCommand(Vector2 Position) : base(Position, false){ }

		public override void StartExecution() {}
		public override bool ExecutionFinished => true;
	}
}