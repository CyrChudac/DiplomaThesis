using GameCreatingCore.GamePathing;
using GameCreatingCore.GamePathing.GameActions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore {
	public class OnlyWaitCommand : PatrolCommand {
		readonly float _waitTime;

		public OnlyWaitCommand(Vector2 position, 
			bool running, bool turnWhileMoving, TurnSideEnum turningSide, float waitTime) 
			: base(position, false, running, turnWhileMoving, turningSide) {
			this._waitTime = waitTime;
		}

		protected override IGameAction InnerGetAction(int enemyIndex, LevelRepresentation level) {
			return new StartAfterAction(enemyIndex, _waitTime);
		}
	}
}
