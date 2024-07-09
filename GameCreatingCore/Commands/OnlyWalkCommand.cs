using GameCreatingCore.GameActions;
using GameCreatingCore.LevelRepresentationData;
using GameCreatingCore.StaticSettings;
using UnityEngine;

namespace GameCreatingCore.Commands
{

    [System.Serializable]
    public sealed class OnlyWalkCommand : PatrolCommand
    {
        public OnlyWalkCommand(Vector2 Position,
            bool turnWhileMoving, TurnSideEnum turnSide)
            : base(Position, false, false, turnWhileMoving, turnSide) { }

        public override string ToString()
        {
            return $"Walk: {Position}";
        }

        protected override IGameAction InnerGetAction(int enemyIndex,
            StaticGameRepresentation staticGameRepresentation, LevelRepresentation level)
        {
            return new EmptyAction(enemyIndex);
        }
    }
}