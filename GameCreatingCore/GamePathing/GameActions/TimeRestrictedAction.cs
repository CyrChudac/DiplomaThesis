using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameCreatingCore.GamePathing.GameActions {
	/// <summary>
	/// Will execute the inner action but it will stop it after a given time. 
	/// (If the action is not cancelable it just executes the whole action.)
	/// </summary>
	public class TimeRestrictedAction : IWithInnerActions {
		private readonly IGameAction _innerAction;
		private readonly float _maxTime;
		private float _elapsedTime;

		public TimeRestrictedAction(IGameAction innerAction, float maxTime) {
			_innerAction = innerAction;
			_maxTime = maxTime;
		}

		public bool IsIndependentOfCharacter => _innerAction.IsIndependentOfCharacter;

		public bool Done => _innerAction.Done || (IsCancelable && FloatEquality.MoreOrEqual(_elapsedTime, _maxTime));

		public bool IsCancelable => _innerAction.IsCancelable;
		public float TimeUntilCancelable => _innerAction.TimeUntilCancelable;

		public int? EnemyIndex => _innerAction.EnemyIndex;
		
		public void Reset() {
			_innerAction.Reset();
			_elapsedTime = 0;
		}
		public IEnumerable<IGameAction> GetInnerActions() {
			return Enumerable.Empty<IGameAction>().Append(_innerAction);
		}

		public IGameAction? CurrentInnerAction => FloatEquality.MoreOrEqual(_elapsedTime, _maxTime) ? _innerAction : null;

		public LevelStateTimed CharacterActionPhase(LevelStateTimed input)
			=> Proceed(input, _innerAction.CharacterActionPhase);
			

		public LevelStateTimed AutonomousActionPhase(LevelStateTimed input)
			=> Proceed(input, _innerAction.AutonomousActionPhase);

		private LevelStateTimed Proceed(LevelStateTimed input, Func<LevelStateTimed,LevelStateTimed> actionPhaseFunc) {
			if(_innerAction.IsCancelable) {
				input = new LevelStateTimed(input, Math.Min(input.Time, _maxTime - _elapsedTime));
			}
			var result = actionPhaseFunc(input);
			_elapsedTime += input.Time - result.Time;
			return result;
		}

		public override string ToString() {
			var nameChars = nameof(TimeRestrictedAction).Where(c => c.ToString() == c.ToString().ToUpper());
			var name = string.Concat(nameChars);
			return $"{name}({_maxTime}): {{{_innerAction}}}";
		}

		public IGameAction Duplicate() {
			var result = new TimeRestrictedAction(_innerAction.Duplicate(), _maxTime);
			result._elapsedTime = _elapsedTime;
			return result;
		}
	}
}
