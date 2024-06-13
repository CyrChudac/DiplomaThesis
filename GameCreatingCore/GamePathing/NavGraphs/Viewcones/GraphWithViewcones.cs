using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	public class GraphWithViewcones : Graph<ScoredActionedNode, GraphWithViewconeEdgeInfo> {
		internal IReadOnlyList<(Viewcone InnerViewcone, IReadOnlyList<ScoredActionedNode> Nodes)> Viewcones { get; }
		internal GraphWithViewcones(IReadOnlyList<ScoredActionedNode> vertices, 
			IReadOnlyList<Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>> edges,
			IReadOnlyList<(Viewcone, IReadOnlyList<ScoredActionedNode>)> viewcones, bool computeEdgesDict) 
			: base(vertices, edges, computeEdgesDict) {

			this.Viewcones = viewcones;
		}

	}

	public class GraphWithViewconeEdgeInfo : EdgeInfo {
		public int? ViewconeIndex { get; }
		public float? AlertingIncrease { get; }
		internal GraphWithViewconeEdgeInfo(float score, int? viewconeIndex, float? alertingIncrease) : base(score) {
			this.ViewconeIndex = viewconeIndex;
			this.AlertingIncrease = alertingIncrease;
		}
	}
}
