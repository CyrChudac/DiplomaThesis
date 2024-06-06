using System;
using System.Collections.Generic;
using System.Text;
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
        /// <summary>
        /// How many seconds it takes the character to change from walk speed to run speed.
        /// </summary>
        [SerializeField]
        public float RunStartTime = defaultStartTime;
        private const float defaultStartTime = 0;
        /// <summary>
        /// How many seconds it takes the character to change from run speed to walk speed.
        /// </summary>
        [SerializeField]
        public float RunEndTime = defaultEndTime;
        private const float defaultEndTime = 0;

        [SerializeField]
        public readonly float TurningSpeed = defaultTurningSpeed;
        private const float defaultTurningSpeed = 180;

        public MovementSettingsProcessed GetProcessed(StaticMovementSettings settings) {
            return new MovementSettingsProcessed(
                walkSpeed: WalkSpeedModifier * settings.Speed,
                runSpeed: RunSpeedModifier * settings.Speed,
                runStartTime: RunStartTime,
                runEndTime: RunEndTime,
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
        /// How many seconds it takes the character to change from walk speed to run speed.
        /// </summary>
        public readonly float RunStartTime;
        /// <summary>
        /// How many seconds it takes the character to change from run speed to walk speed.
        /// </summary>
        public readonly float RunEndTime;
        /// <summary>
        /// How big angle in degrees does the character turn within 1 second.
        /// </summary>
        public readonly float TurningSpeed;

        internal MovementSettingsProcessed(float walkSpeed, float runSpeed,
            float runStartTime, float runEndTime, float turningSpeed) {
            WalkSpeed = walkSpeed;
            RunSpeed = runSpeed;
            RunStartTime = runStartTime;
            RunEndTime = runEndTime;
            TurningSpeed = turningSpeed;
        }

    }
}