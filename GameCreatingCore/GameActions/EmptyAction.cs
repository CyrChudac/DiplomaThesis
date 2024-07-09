using GameCreatingCore.LevelStateData;

namespace GameCreatingCore.GameActions
{
    public class EmptyAction : IGameAction {
		public bool IsIndependentOfCharacter => false;

		public bool Done { get; private set; } = false;

		public bool IsCancelable => true;
		public float TimeUntilCancelable => 0;

		public int? EnemyIndex { get; }
		public EmptyAction(int? enemyIndex) { 
			EnemyIndex = enemyIndex;
		}
		public LevelStateTimed CharacterActionPhase(LevelStateTimed input) {
			Done = true;
			return input;
		}

		public LevelStateTimed AutonomousActionPhase(LevelStateTimed input) {
			Done = true;
			return input;
		}

		public void Reset() {
			Done = false;
		}

		public IGameAction Duplicate() {
			var res = new EmptyAction(EnemyIndex);
			res.Done = Done;
			return res;
		}
	}
}
