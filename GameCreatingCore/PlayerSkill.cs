using GameCreatingCore.GamePathing.GameActions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore
{
    public abstract class PlayerSkill {
		protected abstract bool CanTargetLocation { get; }
		protected abstract bool CanTargetEnemy { get; }
		public IGameAction Create(Vector2 location) {
			if(!CanTargetLocation)
				throw new NotSupportedException();
			return Create(location, null);
		}

		public IGameAction Create(int enemyIndex) {
			if(!CanTargetEnemy)
				throw new NotSupportedException();
			return Create(null, enemyIndex);
		}

		protected abstract IGameAction Create(Vector2? location, int? enemyIndex);
	}
}
