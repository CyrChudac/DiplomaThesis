using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.StaticSettings {

    [Serializable]
    public class ViewconeRepresentation {
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
        public float AlertingSpeedModifier;

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
    }
}