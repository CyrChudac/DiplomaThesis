using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	
    class Viewcone
    {
        public Vector2 StartPos { get; }
        public IReadOnlyList<Vector2> EndingPoints { get; }
        public int EnemyIndex { get; }
        public float FullLength { get; }
        public EnemyTypeInfo EnemyTypeInfo { get; }

        public Viewcone(Vector2 startPos, IReadOnlyList<Vector2> endingPoints, int enemyindex, 
            EnemyTypeInfo enemyTypeInfo, float fullLength)
        {
            StartPos = startPos;
            EndingPoints = endingPoints;
            EnemyIndex = enemyindex;
            EnemyTypeInfo = enemyTypeInfo;
            FullLength = fullLength;
        }

        /// <summary>
        /// When traveling between two points (<paramref name="from"/> -> <paramref name="to"/>) within the viewcone,
        /// is the detection slow enough to not get cought?
        /// </summary>
        /// <param name="initialRatio">It's possible, that when starting on <paramref name="from"/>, the 
        /// initial detection ratio is non-zero.</param>
        public bool CanGoFromTo(Vector2 from, Vector2 to, float initialRatio = 0)
            => CanGoFromTo(from, to, out var _, initialRatio);
        
        /// <summary>
        /// When traveling between two points (<paramref name="from"/> -> <paramref name="to"/>) within the viewcone,
        /// is the detection slow enough to not get cought?
        /// </summary>
        /// <param name="initialRatio">It's possible, that when starting on <paramref name="from"/>, the 
        /// initial detection ratio is non-zero.</param>
        /// <param name="resultingRatio">When we get to <paramref name="to"/>, this is the final alerting ratio.</param>
        public bool CanGoFromTo(Vector2 from, Vector2 to, out float resultingRatio, float initialRatio = 0)
            =>CanGoFromTo(from, to, (from - to).magnitude, out resultingRatio, initialRatio);
        
        /// <summary>
        /// When traveling between two points (<paramref name="from"/> -> <paramref name="to"/>) within the viewcone,
        /// is the detection slow enough to not get cought?
        /// </summary>
        /// <param name="initialRatio">It's possible, that when starting on <paramref name="from"/>, the 
        /// initial detection ratio is non-zero.</param>
        /// <param name="resultingRatio">When we get to <paramref name="to"/>, this is the final alerting ratio.</param>
        public bool CanGoFromTo(Vector2 from, Vector2 to, float theirDistance, 
            out float resultingRatio, float initialRatio = 0) {

            resultingRatio = initialRatio + AlertingRatioIncrease(theirDistance);
            
            bool s = IsAlertOkOn(from, initialRatio);
            bool e = IsAlertOkOn(to, resultingRatio);
            return s && e;
        }

        /// <summary>
        /// When assuming that the enemy sees the player and the alerting ratio=<paramref name="alert"/> and player is on 
        /// <paramref name="position"/>. Is the player still unnoticed by the enemy?.
        /// </summary>
        public bool IsAlertOkOn(Vector2 position, float alert)
            =>  IsAlertOkForDistance((StartPos - position).magnitude, alert);

        /// <summary>
        /// When assuming that the enemy sees the player and the alerting ratio=<paramref name="alert"/> and player is on
        /// <paramref name="distance"/> from the enemy. Is the player still unnoticed by the enemy?.
        /// </summary>
        public bool IsAlertOkForDistance(float distance, float alert)
            =>  distance / FullLength > alert;

        public float AlertingRatioIncrease(float walkedDistance)
            => walkedDistance / EnemyTypeInfo.AlertingDistance;
    }
}
