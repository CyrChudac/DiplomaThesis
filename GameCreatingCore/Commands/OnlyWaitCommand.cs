using GameCreatingCore.GameActions;
using GameCreatingCore.StaticSettings;
using GameCreatingCore.LevelRepresentationData;
using System;
using UnityEngine;

namespace GameCreatingCore.Commands
{
    [Serializable]
    public class OnlyWaitCommand : PatrolCommand
    {
        [SerializeField]
        private float waitTime;

        public float WaitTime => waitTime;

        public OnlyWaitCommand(Vector2 position,
            bool running, bool turnWhileMoving, TurnSideEnum turningSide, float waitTime)
            : base(position, false, running, turnWhileMoving, turningSide)
        {
            this.waitTime = waitTime;
        }

        protected override IGameAction InnerGetAction(int enemyIndex,
            StaticGameRepresentation staticGameRepresentation, LevelRepresentation level)
        {
            return new StartAfterAction(enemyIndex, waitTime);
        }
        public override string ToString()
        {
            return $"Wait: {Position}: {waitTime}";
        }
    }
}
