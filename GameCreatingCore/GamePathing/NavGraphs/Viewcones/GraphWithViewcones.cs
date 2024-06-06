using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	internal class GraphWithViewcones : Graph<ScoredActionedNode, GraphWithViewconeEdgeInfo> {
		public IReadOnlyList<(Viewcone InnerViewcone, IReadOnlyList<ScoredActionedNode> Nodes)> Viewcones { get; }
		public GraphWithViewcones(IReadOnlyList<ScoredActionedNode> vertices, 
			IReadOnlyList<Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>> edges,
			IReadOnlyList<(Viewcone, IReadOnlyList<ScoredActionedNode>)> viewcones, bool computeEdgesDict) 
			: base(vertices, edges, computeEdgesDict) {

			this.Viewcones = viewcones;
		}

	}

	internal class GraphWithViewconeEdgeInfo : EdgeInfo {
		public int? ViewconeIndex { get; }
		public float? AlertingIncrease { get; }
		public GraphWithViewconeEdgeInfo(float score, int? viewconeIndex, float? alertingIncrease) : base(score) {
			this.ViewconeIndex = viewconeIndex;
			this.AlertingIncrease = alertingIncrease;
		}
	}
}
