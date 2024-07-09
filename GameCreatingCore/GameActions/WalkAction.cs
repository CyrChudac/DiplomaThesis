using GameCreatingCore.StaticSettings;
using UnityEngine;
using GameCreatingCore.LevelStateData;

namespace GameCreatingCore.GameActions
{
    /// <summary>
    /// Use WalkAlongPath to walk.
    /// </summary>
    public abstract class WalkAction : IGameAction
    {
        protected Vector2 Position { get; }
        protected MovementSettingsProcessed MovementSettings { get; }
        protected bool Running { get; }

        public int? EnemyIndex { get; }

        public bool IsIndependentOfCharacter => false;
        public bool IsCancelable { get; }

        //the action must have beeen already called (by TimeUntilCancelable definition)
        //so we can access _leftoverTime directly
		public virtual float TimeUntilCancelable => IsCancelable ? 0 : _leftoverTime!.Value;
        public virtual bool Done { get; private set; } = false;

        private float? _leftoverTime = null;

        /// <param name="enemyIndex">Null if player action.</param>
        internal WalkAction(MovementSettingsProcessed movementSettings, Vector2 position,
            int? enemyIndex, bool running = false, bool isCancelable = true)
        {

            this.EnemyIndex = enemyIndex;
            MovementSettings = movementSettings;
            Running = running;
            Position = position;
            this.IsCancelable = isCancelable;
        }
        
		public void Reset() {
            _leftoverTime = null;
            Done = false;
		}

        public LevelStateTimed CharacterActionPhase(LevelStateTimed input)
        {
            LevelStateTimed result;
            (Vector2 Pos, float LeftoverTime, float LeftoverWalkTime) res;
            if(!EnemyIndex.HasValue) {
                res = Walk(input.playerState.Position, Position, input.Time, MovementSettings, Running);
                result = InnerWalkingProceed(input,
                    new LevelStateTimed(input.ChangePlayer(position: res.Pos), res.LeftoverTime));
            } else {
                res = Walk(input.enemyStates[EnemyIndex!.Value].Position, 
                    Position, input.Time, MovementSettings, Running);
                result = InnerWalkingProceed(input,
                    new LevelStateTimed(input.ChangeEnemy(EnemyIndex.Value, position: res.Pos), res.LeftoverTime));
            }
            
            Done = FloatEquality.AreEqual(res.LeftoverWalkTime, 0);
            _leftoverTime = res.LeftoverWalkTime;

            return result;
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
                pos = from + direction * speed * time;
            }
            return (pos, leftoverTime, leftoverWalkTime);
        }

        protected abstract LevelStateTimed InnerWalkingProceed(LevelStateTimed input, LevelStateTimed walkResult);

        public LevelStateTimed AutonomousActionPhase(LevelStateTimed input)
        {
            return input;
        }
        
		public override string ToString() {
			return $"{this.GetType().Name}: {Position}";
		}

        public abstract IGameAction Duplicate();

        protected T SetDuplicateInner<T>(T action) where T : WalkAction {
            action._leftoverTime = _leftoverTime;
            action.Done = Done;
            return action;
        }
	}
}