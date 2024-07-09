using GameCreatingCore.GameActions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.LevelRepresentationData {
	public class PickupableActionProvider {
		public readonly IActiveGameActionProvider action;
		public readonly Vector2 position;

		public PickupableActionProvider(IActiveGameActionProvider action, Vector2 position) {
			this.action = action;
			this.position = position;
		}
	}
}
