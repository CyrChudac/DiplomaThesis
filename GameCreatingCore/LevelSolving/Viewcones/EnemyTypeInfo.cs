using System;

namespace GameCreatingCore.LevelSolving.Viewcones
{
    class EnemyTypeInfo
    {
        public float Angle { get; }
        /// <summary>
        /// How far can the player go through this enemies view and not be detected 
        /// (assuming he ends on the view outer edge).
        /// </summary>
        public float AlertingDistance { get; }

        private readonly Func<float, float> lengthFunc;
        public EnemyTypeInfo(Func<float, float> lengthFunc, float angle, float alertingDistance)
        {
            this.lengthFunc = lengthFunc;
            Angle = angle;
            AlertingDistance = alertingDistance;
        }

        public float GetLength(float alerted)
            => lengthFunc(alerted);
    }
}
