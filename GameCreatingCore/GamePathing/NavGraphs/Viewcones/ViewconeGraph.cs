﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	class ViewconeGraph : Graph<ViewNode, ViewMidEdgeInfo>
    {
        public Viewcone Viewcone { get; }
        public int Index { get; }
        public ViewconeGraph(IReadOnlyList<ViewNode> vertices,
            IReadOnlyList<Edge<ViewNode, ViewMidEdgeInfo>> edges, Viewcone viewcone, int index, bool computeEdges = true)
            : base(vertices, edges, computeEdges)
        {

            Viewcone = viewcone;
            Index = index;
        }
    }
}
