using GameCreatingCore.StaticSettings;
using UnityEngine;
using GameCreatingCore.LevelStateData;

namespace GameCreatingCore.GameActions
{
    public class OnlyWalkAction : WalkAction
    {
        public OnlyWalkAction(MovementSettingsProcessed movementSettings, Vector2 position, int? enemyIndex, bool running = false)
            : base(movementSettings, position, enemyIndex, running)
        {
        }

        protected override LevelStateTimed InnerWalkingProceed(LevelStateTimed input, LevelStateTimed walkResult)
        {
            return walkResult;
        }


		public override IGameAction Duplicate() {
			var result = new OnlyWalkAction(MovementSettings, Position, EnemyIndex, Running);
            return SetDuplicateInner(result);
		}
	}
}