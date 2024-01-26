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
        public float RunStartTime = 0;
        /// <summary>
        /// How many seconds it takes the character to change from run speed to walk speed.
        /// </summary>
        [SerializeField]
        public float RunEndTime = 0;

        public MovementSettingsProcessed GetProcessed(StaticMovementSettings settings) {
            return new MovementSettingsProcessed() {
                WalkSpeed = WalkSpeedModifier * settings.Speed,
                RunSpeed = RunSpeedModifier * settings.Speed,
                RunStartTime = RunStartTime,
                RunEndTime = RunEndTime
            };
        }

    }

    
    public class MovementSettingsProcessed
    {
        /// <summary>
        /// How many distance units the character traverses within a second while walking.
        /// </summary>
        public float WalkSpeed;
        /// <summary>
        /// How many distance units the character traverses within a second while running.
        /// </summary>
        public float RunSpeed;
        /// <summary>
        /// How many seconds it takes the character to change from walk speed to run speed.
        /// </summary>
        public float RunStartTime = 0;
        /// <summary>
        /// How many seconds it takes the character to change from run speed to walk speed.
        /// </summary>
        public float RunEndTime = 0;

    }
}