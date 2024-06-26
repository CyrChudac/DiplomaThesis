using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.StaticSettings {

    [Serializable]
    public class ViewconeRepresentation {
        /// <summary>
        /// Full length when enemy is not alerted. For basic needs use <b>ComputeLength</b> function instead.
        /// </summary>
        [SerializeField]
        public float Length;

        /// <summary>
        /// In degrees.
        /// </summary>
        [SerializeField]
        public float Angle;

        /// <summary>
        /// In degrees.
        /// </summary>
        [SerializeField]
        public float WiggleAngle;

        /// <summary>
        /// In seconds.
        /// </summary>
        [SerializeField]
        public float WiggleTime;
        
        /// <summary>
        /// How many seconds does it take to spot the whole viewcone given the the game difficulity.
        /// </summary>
        [SerializeField]
        public float AlertingTimeModifier;

        /// <summary>
        /// How many seconds would it take to decrease the alert level through the whole viewcone given the the game difficulity.
        /// </summary>
        [SerializeField]
        public float DisalertingTimeModifier;

        [SerializeField]
        public float AlertedLengthModifier;

        /// <summary>
        /// When Length and alert length are different, how fast does the length change to alert length. (distance/seconds)
        /// </summary>
        [SerializeField]
        public float AlertLengthChangeSpeedIn;

        /// <summary>
        /// When Length and alert length are different, how fast does the length change back to length. (distance/seconds)
        /// </summary>
        [SerializeField]
        public float AlertLengthChangeSpeedOut;
        [SerializeField]
        public float AlertedWiggleAngle;
        [SerializeField]
        public float AlertedWiggleTime;

        public float ComputeLength(float alerting)
            => Length * (1 + alerting * (AlertedLengthModifier - 1));
    }
}