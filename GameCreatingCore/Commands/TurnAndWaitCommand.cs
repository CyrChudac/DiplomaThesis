using GameCreatingCore.GameActions;
using GameCreatingCore.StaticSettings;
using GameCreatingCore.LevelRepresentationData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreatingCore.Commands
{

    [Serializable]
    public class TurnAndWaitCommand : OnlyWaitCommand
    {

        [SerializeField]
        private float _rotation;
        public float Rotation => _rotation;

        [SerializeField]
        private TurnSideEnum _turnSideOnSpot;
        public TurnSideEnum TurnSideOnSpot => _turnSideOnSpot;

        public TurnAndWaitCommand(Vector2 position, bool running,
            bool turnWhileMoving, TurnSideEnum turningSide, float waitTime,
            float rotation, TurnSideEnum turnSideOnSpot)
            : base(position, running, turnWhileMoving, turningSide, waitTime)
        {
            _turnSideOnSpot = turnSideOnSpot;
            _rotation = rotation;
        }

        protected override IGameAction InnerGetAction(int enemyIndex,
            StaticGameRepresentation staticGameRepresentation, LevelRepresentation level)
        {

            var waitAction = base.InnerGetAction(enemyIndex, staticGameRepresentation, level);
            MovementSettingsProcessed movSettings = staticGameRepresentation
                .GetEnemySettings(level.Enemies[enemyIndex].Type)
                .movementRepresentation;
            var turnAction = new TurnTowardsPositionAction(enemyIndex, movSettings, false, 10,
                Position + Vector2Utils.VectorFromAngle(Rotation),
                TurnSideOnSpot);
            var actions = new List<IGameAction>() { turnAction, waitAction };
            return new ChainedAction(actions);
        }
    }
}
