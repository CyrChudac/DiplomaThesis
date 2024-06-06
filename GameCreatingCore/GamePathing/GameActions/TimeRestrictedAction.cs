using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.GameActions {
	/// <summary>
	/// Will execute the inner action but it will stop it after a given time.
	/// </summary>
	public class TimeRestrictedAction : IGameAction{
		private readonly IGameAction _innerAction;
		private readonly float _maxTime;
		private float _elapsedTime;

		public TimeRestrictedAction(IGameAction innerAction, float maxTime) {
			_innerAction = innerAction;
			_maxTime = maxTime;
		}

		public bool IsIndependentOfCharacter => _innerAction.IsIndependentOfCharacter;

		public bool Done => _innerAction.Done || (IsCancelable && _elapsedTime >= _maxTime);

		public bool IsCancelable => _innerAction.IsCancelable;

		public int? EnemyIndex => _innerAction.EnemyIndex;

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
	}
}
