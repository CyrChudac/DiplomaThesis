using System;
using UnityEngine;

namespace GameCreatingCore.StaticSettings {
	[Serializable]
	public class PlayerSettings {
		[SerializeField]
		public MovementSettings movementRepresentation;
		public PlayerSettings(MovementSettings movementRepresentation) {
			this.movementRepresentation = movementRepresentation;
		}
		public PlayerSettingsProcessed ToProcessed(StaticMovementSettings movementSettings) {
			return new PlayerSettingsProcessed(movementRepresentation.GetProcessed(movementSettings));
		}
	}
	
	public class PlayerSettingsProcessed {
		public MovementSettingsProcessed movementRepresentation;
		public PlayerSettingsProcessed(MovementSettingsProcessed movementRepresentation) {
			this.movementRepresentation = movementRepresentation;
		}
	}
}
