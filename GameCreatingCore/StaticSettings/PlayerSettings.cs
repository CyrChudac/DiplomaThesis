using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.StaticSettings {
	 //this way we only support one settings for all playable characters
	 //TODO: how to change it?
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
