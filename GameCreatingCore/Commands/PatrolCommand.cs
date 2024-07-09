using System;
using UnityEngine;
using GameCreatingCore.LevelRepresentationData;
using GameCreatingCore.GameActions;
using System.Collections.Generic;
using GameCreatingCore.StaticSettings;
using GameCreatingCore.GamePathing;

namespace GameCreatingCore.Commands
{

    [Serializable]
    public abstract class PatrolCommand
    {
        [SerializeField]
        public Vector2 Position;
        [SerializeField]
        public bool Running;
        [SerializeField]
        public bool TurnWhileMoving;
        [SerializeField]
        public TurnSideEnum TurningSide;

        /// <summary>
        /// Signifies, whether the command execution can be done whilst moving towards the next command.
        /// </summary>
        [SerializeField]
        public bool ExecuteDuringMoving;

        public PatrolCommand(Vector2 position, bool executeDuringMoving,
            bool running, bool turnWhileMoving, TurnSideEnum turningSide)
        {
            Position = position;
            ExecuteDuringMoving = executeDuringMoving;
            Running = running;
            TurnWhileMoving = turnWhileMoving;
            TurningSide = turningSide;
        }

        public IGameAction GetAction(int enemyIndex, Vector2 currentPos, StaticGameRepresentation staticGameRepresentation,
            StaticNavGraph navGraph, LevelRepresentation level, bool backwards)
        {

            var mr = staticGameRepresentation.GetEnemySettings(level.Enemies[enemyIndex].Type).movementRepresentation;
            var path = navGraph.GetEnemyPath(currentPos, Position);
            TurnSideEnum turnStyle = backwards ? TurningSide.Opposite() : TurningSide;
            var wa = new WalkAlongPath(path, enemyIndex, false, Running, TurnWhileMoving, mr, turnStyle);
            var inner = InnerGetAction(enemyIndex, staticGameRepresentation, level);
            return new ChainedAction(new List<IGameAction>(2) { wa, inner });
        }

        /// <summary>
        /// After the enemy walks to the desired position, proceed with the command action.
        /// </summary>
        protected abstract IGameAction InnerGetAction(int enemyIndex,
            StaticGameRepresentation staticGameRepresentation, LevelRepresentation level);


        public override string ToString()
        {
            return $"GenericCommand: {Position}";
        }
    }
}