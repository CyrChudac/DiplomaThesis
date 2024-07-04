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