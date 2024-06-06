using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameCreatingCore.GamePathing.GameActions {
	internal class KillGameAction : IGameAction {

		public bool IsIndependentOfCharacter => false;

		public bool IsCancelable => false;

		private int _targetEnemyIndex;
		public int? EnemyIndex => null;

		private bool _done = false;
		public bool Done => _done;

		private float KillingTime { get; set; }

		public KillGameAction(int targetEnemyIndex, float killingTime) {
			_targetEnemyIndex = targetEnemyIndex;
			KillingTime = killingTime;
		}

		public LevelStateTimed CharacterActionPhase(LevelStateTimed input) {
			if(KillingTime - input.Time <= 0) {
				KillingTime = 0;
				_done = true;
				var enems = input.enemyStates.ToList();
				enems[_targetEnemyIndex] = enems[_targetEnemyIndex].Change(alive: false);
				return new LevelStateTimed(input.Change(enemyStates: enems), input.Time - KillingTime);
			} else {
				KillingTime -= input.Time;
				return new LevelStateTimed(input, 0);
			}
		}

		public LevelStateTimed AutonomousActionPhase(LevelStateTimed input) {
			return input;
		}
	}

	public class KillActionProvider : IActiveGameActionProvider {

		public float KillingTime { get; }
		public float MaxUseDistance { get; }
		public GameActionReachType ReachType => GameActionReachType.Instant;

		public float MinUseDistance => 0;

		public bool CanTargetEnemy => true;

		public bool CanTargetPlayer => false;

		public bool CanTargetGround => false;
		public bool CanTargetGroundObstacle => false;

		public double Uses => double.PositiveInfinity;
		
		public KillActionProvider(float killingTime, float maxUseDistance) {
			KillingTime = killingTime;
			MaxUseDistance = maxUseDistance;
		}

		public IGameAction Get(int? enemyIndex, GameActionTarget target) {
			if(!enemyIndex.HasValue) {
				if(target.EnemyTarget.HasValue) {
					return new KillGameAction(target.EnemyTarget.Value, KillingTime);
				} else {
					throw new NotSupportedException(
						$"{nameof(KillGameAction)} is only designed to target enemies.");
				}
			} else {
				throw new NotSupportedException($"{nameof(KillGameAction)} is only designed for the player to be killing.");
			}
		}
	}
}
