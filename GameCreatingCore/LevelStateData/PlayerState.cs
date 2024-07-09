using System.Collections.Generic;
using GameCreatingCore.GameActions;
using UnityEngine;

namespace GameCreatingCore.LevelStateData
{

    public sealed class PlayerState : CharacterState
    {
        /// <summary>
        /// What skills can the player use.
        /// </summary>
        public readonly IReadOnlyList<IActiveGameActionProvider> AvailableSkills;


        public PlayerState(Vector2 position, float rotation,
            IReadOnlyList<IActiveGameActionProvider> availableSkills,
            IGameAction? performingAction)
            : base(position, rotation, performingAction)
        {
            AvailableSkills = availableSkills;
        }

        public PlayerState Change(Vector2? position = null, float? rotation = null,
            IReadOnlyList<IActiveGameActionProvider>? availableSkills = null,
            IGameAction? performingAction = null, bool performingActionToNull = false)
        {

            return new PlayerState(
                position ?? Position,
                rotation ?? Rotation,
                availableSkills ?? AvailableSkills,
                performingActionToNull ? null : performingAction ?? PerformingAction);
        }

        public override string ToString()
        {
            return $"{nameof(PlayerState)}: {Position}; {Rotation}";
        }

        public PlayerState Duplicate()
        {
            return new PlayerState(Position, Rotation, AvailableSkills, PerformingAction?.Duplicate());
        }
    }
}