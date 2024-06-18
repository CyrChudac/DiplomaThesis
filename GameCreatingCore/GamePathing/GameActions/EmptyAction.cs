using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.GameActions {
	public class EmptyAction : IGameAction {
		public bool IsIndependentOfCharacter => false;

		public bool Done => true;

		public bool IsCancelable => true;

		public int? EnemyIndex { get; }
		public EmptyAction(int? enemyIndex) { 
			EnemyIndex = enemyIndex;
		}
		public LevelStateTimed CharacterActionPhase(LevelStateTimed input) {
			return input;
		}

		public LevelStateTimed AutonomousActionPhase(LevelStateTimed input) {
			return input;
		}
	}
}
