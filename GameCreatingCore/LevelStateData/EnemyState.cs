using GameCreatingCore.GameActions;
using UnityEngine;

namespace GameCreatingCore.LevelStateData
{

    public sealed class EnemyState : CharacterState
    {
        public int? PathIndex { get; }

        /// <summary>
        /// True when going through the path commands backwards.
        /// </summary>
        public bool IsReturning { get; }

        public bool Alive { get; }
        public bool Alerted { get; }

        public float TimeOfPlayerInView { get; }

        public IGameAction? CurrentPathAction { get; }

        /// <summary>
        /// How much is the viewcone length towards the alerted length. (0 = NormalLengh; 1 = AlertedLength)
        /// </summary>
        public float ViewconeAlertLengthModifier { get; }

        public EnemyState(Vector2 position, float rotation, IGameAction? performingAction, bool alive, bool alerted,
            float viewconeAlertLengthModifier, float timeOfPlayerInView, IGameAction? currentPathAction,
            int? pathIndex = null, bool isReturning = false)
            : base(position, rotation, performingAction)
        {

            PathIndex = pathIndex;
            Alive = alive;
            Alerted = alerted;
            ViewconeAlertLengthModifier = viewconeAlertLengthModifier;
            IsReturning = isReturning;
            TimeOfPlayerInView = timeOfPlayerInView;
            CurrentPathAction = currentPathAction;
        }

        public EnemyState Change(float? rotation = null, Vector2? position = null,
            IGameAction? performingAction = null, bool performingActionToNull = false,
            bool? alive = null, bool? alerted = null, float? timeOfPlayerInView = null,
            IGameAction? currentPathAction = null, bool removePathAction = false,
            float? viewconeAlertLengthModifier = null, int? pathIndex = null, bool? isReturning = null)
        {
            return new EnemyState(
                position ?? Position,
                rotation ?? Rotation,
                performingActionToNull ? null : performingAction ?? PerformingAction,
                alive ?? Alive,
                alerted ?? Alerted,
                viewconeAlertLengthModifier ?? ViewconeAlertLengthModifier,
                timeOfPlayerInView ?? TimeOfPlayerInView,
                removePathAction ? null : currentPathAction ?? CurrentPathAction,
                pathIndex ?? PathIndex,
                isReturning ?? IsReturning);
        }

        public override int GetHashCode()
        {
            return (int)((Alive ? 1 : 0)
                + Rotation * 2
                + Position.GetHashCode() * 721);
        }
        public override string ToString()
        {
            if (!Alive)
            {
                return $"{nameof(EnemyState)}: Dead";
            }
            return $"{nameof(EnemyState)}: {Position}; {Rotation}";
        }

        public EnemyState Duplicate()
        {
            return new EnemyState(
                Position,
                Rotation,
                PerformingAction?.Duplicate(),
                Alive,
                Alerted,
                ViewconeAlertLengthModifier,
                TimeOfPlayerInView,
                CurrentPathAction?.Duplicate(),
                PathIndex,
                IsReturning);
        }
    }
}