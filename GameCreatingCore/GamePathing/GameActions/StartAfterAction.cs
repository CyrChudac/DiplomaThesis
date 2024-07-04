using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameCreatingCore.GamePathing.GameActions {
	/// <summary>
	/// Will execute a given action but only after an initial wait.
	/// </summary>
	internal class StartAfterAction : IWithInnerActions{
		private readonly IGameAction _action;
		private readonly float _waitTime;
		private float _usedTime = 0;
		private bool _waitingDone = false;
		private bool _waitMakesCharacterBusy;

		public StartAfterAction(IGameAction action, float waitTime, bool waitMakesCharacterBusy) {
			_action = action;
			_waitTime = waitTime;
			_waitMakesCharacterBusy = waitMakesCharacterBusy;
		}

		private bool _isEmpty = false;

		public StartAfterAction(int? enemyIndex, float waitTime)
			:this(new EmptyAction(enemyIndex), waitTime, true) { 
			_isEmpty= true;
		}

		public bool IsIndependentOfCharacter 
			=> (!_waitMakesCharacterBusy) || (_waitingDone && _action.IsIndependentOfCharacter);

		public bool Done => _waitingDone && _action.Done;

		public bool IsCancelable => _isEmpty || (_waitingDone && _action.IsCancelable);
		public float TimeUntilCancelable => _waitingDone ? _action.TimeUntilCancelable : 0;

		public int? EnemyIndex => _action.EnemyIndex;
		
		public void Reset() {
			_action.Reset();
			_waitingDone = false;
			_usedTime = 0;
		}
		public IGameAction? CurrentInnerAction => _action;
		
		public IEnumerable<IGameAction> GetInnerActions() {
			return Enumerable.Empty<IGameAction>().Append(_action);
		}

		public LevelStateTimed CharacterActionPhase(LevelStateTimed input) {
			return Proceed(input, _action.CharacterActionPhase);
		}

		public LevelStateTimed AutonomousActionPhase(LevelStateTimed input) {
			return Proceed(input, _action.AutonomousActionPhase);
		}

		private LevelStateTimed Proceed(LevelStateTimed input, Func<LevelStateTimed, LevelStateTimed> func) {
			if(!_waitingDone) {
				if(_usedTime + input.Time > _waitTime) {
					_waitingDone = true;
					var time = _usedTime + input.Time - _waitTime;
					_usedTime = _waitTime;
					input = new LevelStateTimed(input, time);
				} else {
					_usedTime += input.Time;
					return new LevelStateTimed(input, 0);
				}
				
			}
			return func(input);
		}

		public IGameAction Duplicate() {
			var result = new StartAfterAction(_action.Duplicate(), _waitTime, _waitMakesCharacterBusy);
			result._waitingDone = _waitingDone;
			result._usedTime = _usedTime;
			result._isEmpty = _isEmpty;
			return result;
		}
	}
}
