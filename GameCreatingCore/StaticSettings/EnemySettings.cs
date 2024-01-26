using System;
using System.Collections.Generic;
using System.Text;
using GameCreatingCore.StaticSettings;
using UnityEngine;

namespace GameCreatingCore.StaticSettings
{

    [Serializable]
    public class EnemySettings
    {
        public ViewconeRepresentation viewconeRepresentation;
        public MovementSettings movementRepresentation;

        public EnemySettings(ViewconeRepresentation viewconeRepr, MovementSettings movementRepr) {
            this.viewconeRepresentation = viewconeRepr;
            this.movementRepresentation = movementRepr;
        }

        public EnemySettingsProcessed ToProcessed(StaticMovementSettings settings) {
            return new EnemySettingsProcessed(
                viewconeRepresentation, 
                movementRepresentation.GetProcessed(settings));
        }
    }

    public class EnemySettingsProcessed
    {
        public ViewconeRepresentation viewconeRepresentation;
        public MovementSettingsProcessed movementRepresentation;

        public EnemySettingsProcessed(ViewconeRepresentation viewconeRepr, MovementSettingsProcessed movementRepr) {
            this.viewconeRepresentation = viewconeRepr;
            this.movementRepresentation = movementRepr;
        }
    }
}