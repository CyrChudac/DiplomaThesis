using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	class ViewMidEdgeInfo : EdgeInfo
    {
        /// <summary>
        /// When removing a part of viewcone, a new edge is added at the new border. Is this the case?
        /// </summary>
        public bool IsEdgeOfRemoved { get; }
        /// <summary>
        /// Is it possible to use this edge for moving?
        /// </summary>
        public bool Traversable { get; }
        /// <summary>
        /// Is this edge inside of the viewcone or on the outer edge?
        /// </summary>
        public bool IsInMidViewcone { get; }

        public float AlertingIncrease { get; }
        public ViewMidEdgeInfo(float score, bool isEdgeOfRemoved, bool traversable, bool isInMidViewcone, float alertingIncrease)
            : base(score)
        {

            IsEdgeOfRemoved = isEdgeOfRemoved;
            Traversable = traversable;
            IsInMidViewcone = isInMidViewcone;
            AlertingIncrease = alertingIncrease;
        }
    }
}
