using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.GameActions {
	/// <summary>
	/// Will execute a given action but only after an initial wait.
	/// </summary>
	internal class StartAfterAction : IGameAction{
		private readonly IGameAction _action;
		private readonly float _waitTime;
		private float _usedTime = 0;
		private bool _waitingDone = false;

		public StartAfterAction(IGameAction action, float waitTime) {
			_action = action;
			_waitTime = waitTime;
		}

		public bool IsIndependentOfCharacter => _action.IsIndependentOfCharacter;

		public bool Done => _waitingDone && _action.Done;

		public bool IsCancelable => _action.IsCancelable;

		public int? EnemyIndex => _action.EnemyIndex;

		public LevelStateTimed CharacterActionPhase(LevelStateTimed input) {
			return Proceed(input, _action.CharacterActionPhase);
		}

		public LevelStateTimed AutonomousActionPhase(LevelStateTimed input) {
			return Proceed(input, _action.AutonomousActionPhase);
		}

		private LevelStateTimed Proceed(LevelStateTimed input, Func<LevelStateTimed, LevelStateTimed> func) {
			if(!_waitingDone) {
				float time;
				if(_usedTime + input.Time > _waitTime) {
					_waitingDone = true;
					time = _usedTime + input.Time - _waitTime;
					_usedTime = _waitTime;
				} else {
					_usedTime += input.Time;
					time = 0;
				}
				return new LevelStateTimed(input, time);
			}
			else
				return func(input);

		}
	}
}
