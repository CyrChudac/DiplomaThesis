using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GameCreatingCore.GamePathing {

    public class Graph<N, EDGE_INFO> where N : Node where EDGE_INFO : EdgeInfo {
        public IReadOnlyList<N> vertices;
        public IReadOnlyList<Edge<N, EDGE_INFO>> edges;

		private readonly Dictionary<N, Dictionary<N, EDGE_INFO?>>? edgesDict;

		public readonly bool AreEdgesComputed;

        public Graph(IReadOnlyList<N> vertices, IReadOnlyList<Edge<N, EDGE_INFO>> edges, bool computeEdgesDict){
            this.vertices = new List<N>(vertices);
            this.edges = new List<Edge<N, EDGE_INFO>>(edges);

			if(computeEdgesDict) {
				edgesDict = new Dictionary<N, Dictionary<N, EDGE_INFO?>>();
				foreach(var v1 in vertices) {
					Dictionary<N, EDGE_INFO?> inner = new Dictionary<N, EDGE_INFO?>();
					foreach(var v2 in vertices) {
						inner.Add(v2, null);
					}
					edgesDict.Add(v1, inner);
				}
				foreach(var e in edges) {
					edgesDict[e.First][e.Second] = e.EdgeInfo;
				}
			}
			AreEdgesComputed = computeEdgesDict;
        }
		
		public EDGE_INFO? this[N node1, N node2]
			=> edgesDict != null ? edgesDict[node1][node2] : throw new InvalidOperationException();

		public EDGE_INFO? this[int node1Index, int node2Index]
			=> edgesDict != null
				? edgesDict[vertices[node1Index]][vertices[node2Index]] 
				: throw new InvalidOperationException();

		public List<(N EndingIn, EDGE_INFO EdgeInfo)> GetOutEdges(N node) {
			if(edges.Count > vertices.Count && edgesDict != null) {
				var dict = edgesDict[node];
				return dict.Keys
					.Select(k => (k, dict[k]))
					.Where(k => k.Item2 != null)
					.Cast<(N EndingIn, EDGE_INFO EdgeInfo)>()
					.ToList();
			} else {
				return edges
					.Where(e => e.First.Equals(node))
					.Select(e => (e.Second, e.EdgeInfo))
					.ToList();
			}
		}

		public override string ToString() {
			return $"Graph<{typeof(N).Name}>: V-{vertices.Count}; E-{edges.Count}";
		}
	}

	public class Graph<N> : Graph<N, EdgeInfo> where N : Node {
		public Graph(List<N> vertices, List<Edge<N>> edges, bool computeEdgesDict)
			: base(vertices, edges, computeEdgesDict) {
		}
	}
}
