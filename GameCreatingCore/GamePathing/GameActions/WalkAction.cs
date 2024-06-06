using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.Windows;

namespace GameCreatingCore.GamePathing.GameActions
{

    public abstract class WalkAction : IGameAction
    {
        protected Vector2 Position { get; }
        MovementSettingsProcessed MovementSettings { get; }
        bool Running { get; }

        public int? EnemyIndex { get; }

        public bool IsIndependentOfCharacter => false;
        public bool IsCancelable => true;

        private bool _done = false;
        public virtual bool Done => _done;

        private float? _leftoverTime = null;
        public virtual float LeftoverPlayerTime => _leftoverTime
            ?? throw new NotSupportedException();

        /// <param name="enemyIndex">Null if player action.</param>
        internal WalkAction(MovementSettingsProcessed movementSettings, Vector2 position,
            int? enemyIndex, bool running = false)
        {

            this.EnemyIndex = enemyIndex;
            MovementSettings = movementSettings;
            Running = running;
            Position = position;
        }

        public LevelStateTimed CharacterActionPhase(LevelStateTimed input)
        {
            var res = Walk(input.playerState.Position, Position, input.Time, MovementSettings, Running);
            _done = res.LeftoverWalkTime == 0;
            _leftoverTime = res.LeftoverWalkTime;
            if (!EnemyIndex.HasValue)
                return InnerWalkingProceed(input,
                    new LevelStateTimed(input.ChangePlayer(position: res.Pos), res.LeftoverTime));

            var enems = new List<EnemyState>(input.enemyStates);
            enems[EnemyIndex.Value] = enems[EnemyIndex.Value].Change(position: res.Pos);

            return InnerWalkingProceed(input,
                new LevelStateTimed(input.Change(enemyStates: enems), res.LeftoverTime));
        }

        public static (Vector2 Pos, float LeftoverTime, float LeftoverWalkTime) Walk(Vector2 from, Vector2 to, float time,
            MovementSettingsProcessed settings, bool running = false)
        {
            var speed = running ? settings.RunSpeed : settings.WalkSpeed;
            var dist = Vector2.Distance(from, to);
            float leftoverTime;
            float leftoverWalkTime;
            Vector2 pos;
            if (speed * time > dist)
            {
                leftoverTime = time - dist / speed;
                leftoverWalkTime = 0;
                pos = to;
            }
            else
            {
                leftoverTime = 0;
                leftoverWalkTime = dist / speed - time;
                var direction = (to - from).normalized;
                pos = direction * speed * time;
            }
            return (pos, leftoverTime, leftoverWalkTime);
        }

        protected abstract LevelStateTimed InnerWalkingProceed(LevelStateTimed input, LevelStateTimed walkResult);

        public LevelStateTimed AutonomousActionPhase(LevelStateTimed input)
        {
            return input;
        }
    }
}