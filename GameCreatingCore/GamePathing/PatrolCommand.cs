using GameCreatingCore.GamePathing.NavGraphs;
using System.Reflection.Emit;
using System;
using UnityEngine;
using GameCreatingCore.GamePathing.GameActions;
using System.Collections.Generic;
using GameCreatingCore.StaticSettings;
using static System.Collections.Specialized.BitVector32;

namespace GameCreatingCore.GamePathing
{

    [System.Serializable]
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

        private StaticGameRepresentation? _staticGameRepresentation;
        protected StaticGameRepresentation StaticGameRepresentation
            => _staticGameRepresentation ?? throw new NotSupportedException();

        public PatrolCommand(Vector2 position, bool executeDuringMoving, 
            bool running, bool turnWhileMoving, TurnSideEnum turningSide)
        {
            this.Position = position;
            this.ExecuteDuringMoving = executeDuringMoving;
            this.Running = running;
            this.TurnWhileMoving = turnWhileMoving;
            this.TurningSide = turningSide;
        }

        private IGameAction? _action = null;

        public IGameAction Action => _action
            ?? throw new NotSupportedException("Accessing enemy action without setting its value.");

        public void SetAction(int enemyIndex, Vector2 currentPos, StaticGameRepresentation staticGameRepresentation,
            StaticNavGraph navGraph, LevelRepresentation level, bool backwards) {
            
            this._staticGameRepresentation = staticGameRepresentation;
            var mr = StaticGameRepresentation.GetEnemySettings(level.Enemies[enemyIndex].Type).movementRepresentation;
            var path = navGraph.GetEnemyPath(currentPos, Position);
            TurnSideEnum turnStyle = backwards ? TurningSide.Opposite() : TurningSide;
            var wa = new WalkAlongPath(path, enemyIndex, false, Running, TurnWhileMoving, mr, turnStyle);
            var inner = InnerGetAction(enemyIndex, level);
            _action = new ChainedAction(new List<IGameAction>(2) { wa, inner });
        }

        /// <summary>
        /// After the enemy walks to the desired position, proceed with the command action.
        /// </summary>
        protected abstract IGameAction InnerGetAction(int enemyIndex, LevelRepresentation level);
        

        public override string ToString()
        {
            return $"GenericCommand: {Position}";
        }
    }
}