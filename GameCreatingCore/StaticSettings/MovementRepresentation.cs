using System;
using UnityEngine;

namespace GameCreatingCore.StaticSettings
{

    [Serializable]
    public class MovementSettings
    {
        [SerializeField]
        public float WalkSpeedModifier = 1;
        [SerializeField]
        public float RunSpeedModifier = 1;

        [SerializeField]
        public readonly float TurningSpeed = defaultTurningSpeed;
        private const float defaultTurningSpeed = 180;

        public MovementSettingsProcessed GetProcessed(StaticMovementSettings settings) {
            return new MovementSettingsProcessed(
                walkSpeed: WalkSpeedModifier * settings.Speed,
                runSpeed: RunSpeedModifier * settings.Speed,
                turningSpeed: TurningSpeed
            );
        }

    }

    
    public class MovementSettingsProcessed
    {
        /// <summary>
        /// How many distance units the character traverses within a second while walking.
        /// </summary>
        public readonly float WalkSpeed;
        /// <summary>
        /// How many distance units the character traverses within a second while running.
        /// </summary>
        public readonly float RunSpeed;
        /// <summary>
        /// How big angle in degrees does the character turn within 1 second.
        /// </summary>
        public readonly float TurningSpeed;

        internal MovementSettingsProcessed(float walkSpeed, float runSpeed, float turningSpeed) {
            WalkSpeed = walkSpeed;
            RunSpeed = runSpeed;
            TurningSpeed = turningSpeed;
        }

    }
}