using GameCreatingCore.GamePathing.GameActions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing {
	public interface IActiveGameActionProvider {
		GameActionReachType ReachType { get; }
        float MaxUseDistance { get; }
        float MinUseDistance { get; }
        bool CanTargetEnemy { get; }
        bool CanTargetPlayer { get; }
        bool CanTargetGround { get; }
        bool CanTargetGroundObstacle { get; }
        double Uses { get; }
		IGameAction Get(int? performingCharacterIndex, GameActionTarget target);
		IActiveGameActionProvider DecreaseUses(int byCount = 1);
	}

    public class GameActionTarget {
		public GameActionTarget(Vector2? groundTarget, int? enemyTarget, bool playerTarget) {
			GroundTarget = groundTarget;
			EnemyTarget = enemyTarget;
			PlayerTarget = playerTarget;
		}

		public Vector2? GroundTarget { get; }
        public int? EnemyTarget { get; }
        public bool PlayerTarget { get; }

	}
}
