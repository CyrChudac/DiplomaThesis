using GameCreatingCore.StaticSettings;
using UnityEngine;
using GameCreatingCore.LevelStateData;

namespace GameCreatingCore.GameActions
{

    public class WalkThroughViewConePlayerAction : WalkAction
    {
        internal WalkThroughViewConePlayerAction(MovementSettingsProcessed movementSettings, Vector2 position, bool running = false)
            : base(movementSettings, position, null, running, false)
        {
        }

        protected override LevelStateTimed InnerWalkingProceed(
            LevelStateTimed input, LevelStateTimed walkResult)
        {

            return walkResult;
        }

		public override IGameAction Duplicate() {
			var result = new WalkThroughViewConePlayerAction(MovementSettings, Position, Running);
            return SetDuplicateInner(result);
		}
	}
}