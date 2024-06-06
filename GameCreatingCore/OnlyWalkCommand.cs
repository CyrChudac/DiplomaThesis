using GameCreatingCore.GamePathing;
using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.StaticSettings;
using UnityEngine;

namespace GameCreatingCore
{

    [System.Serializable]
	public sealed class OnlyWalkCommand : PatrolCommand {
		public OnlyWalkCommand(Vector2 Position, StaticGameRepresentation staticGameRepresentation, 
			bool turnWhileMoving, TurnSideEnum turnSide) 
			: base(Position, staticGameRepresentation, false, false, turnWhileMoving, turnSide){ }

		public override string ToString() {
			return $"Walk: {Position}";
		}

		protected override IGameAction InnerGetAction(int enemyIndex, LevelRepresentation level) {
			return new EmptyAction(enemyIndex);
		}
	}
}