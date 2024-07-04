using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.GameActions {
	public class PickupAction : IGameAction {

		public bool IsIndependentOfCharacter => false;

		public bool Done { get; private set; } = false;

		public bool IsCancelable => false;
		public float TimeUntilCancelable => Done ? 0 : _pickUpTime - _usedTime;

		public int? EnemyIndex => null;

		private readonly int _pickupableIndex;
		private readonly float _pickupMaxDistance;
		private readonly float _pickUpTime;
		private float _usedTime = 0;
		private readonly LevelRepresentation _level;

		public PickupAction(int pickupableIndex, LevelRepresentation levelRepresentation, 
			float pickupMaxDistance, float pickUpTime) {

			_pickupableIndex = pickupableIndex;
			_level = levelRepresentation;
			_pickupMaxDistance = pickupMaxDistance;
			_pickUpTime = pickUpTime;
		}

		public LevelStateTimed CharacterActionPhase(LevelStateTimed input) {
			var dist = Vector2.SqrMagnitude(input.playerState.Position - _level.SkillsToPickup[_pickupableIndex].Item2);
			if(dist > _pickupMaxDistance * _pickupMaxDistance)
				return input;
			var newTime = _usedTime + input.Time;
			LevelStateTimed result;
			if(newTime > _pickUpTime) {
				var act = _level.SkillsToPickup[_pickupableIndex].Item1;
				var skills = input.playerState.AvailableSkills.ToList();
				skills.Add(act);
				var picked = new List<bool>(input.pickupableSkillsPicked);
				picked[_pickupableIndex] = true;
				result = new LevelStateTimed(input.ChangePlayer(availableSkills: skills).Change(pickupablesPickedUp: picked)
					, newTime - _pickUpTime);
				newTime = _pickUpTime;
				Done = true;
			} else {
				result = new LevelStateTimed(input, 0);
			}
			_usedTime = newTime;
			return result;
		}

		public LevelStateTimed AutonomousActionPhase(LevelStateTimed input) {
			return input;
		}

		public void Reset() {
			_usedTime = 0;
			Done = false;
		}

		public IGameAction Duplicate() {
			var result = new PickupAction(_pickupableIndex, _level, _pickupMaxDistance, _pickUpTime);
			result._usedTime = _usedTime;
			result.Done = Done;
			return result;
		}
	}
}
