using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreatingCore.GameActions
{

    public class WalkAlongPath : ChainedAction
    {
        public WalkAlongPath(IEnumerable<Vector2>? path, int? enemyIndex, bool throughViewcone, bool running,
            bool turnWhileMoving, MovementSettingsProcessed movementSettings, TurnSideEnum turningSide)
            : base(GetActions(path, enemyIndex, throughViewcone, running, turnWhileMoving, movementSettings, turningSide)) 
            {}

        private static List<IGameAction> GetActions(IEnumerable<Vector2>? path, int? enemyIndex, 
            bool throughViewcone, bool running,
            bool turnWhileMoving, MovementSettingsProcessed movementSettings,
            TurnSideEnum turningSide)
        {
            List<IGameAction> actions = new List<IGameAction>();
            if(path == null) { 
                //the path is null when there is no possible way to get to final destination, then we simply give up on going there
                actions.Add(new EmptyAction(enemyIndex));
                return actions;
            }
            foreach (var pos in path)
            {
                var ta = new TurnTowardsPositionAction(enemyIndex, movementSettings, turnWhileMoving, 10, 
                    pos, turningSide);
                actions.Add(ta);
                WalkAction wa;
                if (throughViewcone && !enemyIndex.HasValue)
                {
                    wa = new WalkThroughViewConePlayerAction(movementSettings, pos, running);
                }
                else
                {
                    wa = new OnlyWalkAction(movementSettings, pos, enemyIndex, running);
                }
                actions.Add(wa);
            }
            return actions;
        }
    }
}