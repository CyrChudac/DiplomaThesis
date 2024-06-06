using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.NavGraphs {
	
    internal enum NodeType
    {
        Obstacle,
        Goal,
        Viewcone,
        Enemy
    }
}
