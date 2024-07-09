using GameCreatingCore.GameActions;
using UnityEngine;

namespace GameCreatingCore.LevelStateData
{

    public abstract class CharacterState
    {
        public readonly Vector2 Position;
        public readonly float Rotation;
        /// <summary>
        /// Those are the last actions and they were not cancellable, so finish them first and then proceed.
        /// </summary>
        public readonly IGameAction? PerformingAction;

        protected CharacterState(Vector2 position, float rotation, IGameAction? performingAction)
        {
            Position = position;
            Rotation = rotation;
            PerformingAction = performingAction;
        }
    }
}