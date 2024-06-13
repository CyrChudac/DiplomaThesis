using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static UnityEngine.RectTransform;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	internal class ViewconeAdder {
        /// <summary>
        /// Adds the given <paramref name="viewcones"/> into the static nav graph and thus creates one final graph.
        /// </summary>
		public static GraphWithViewcones AddViewconesToGraph(List<ViewconeGraph> viewcones, StaticNavGraph staticNavGraph)
        {
            var pregraph = staticNavGraph.PlayerStaticNavGraph;
            var rootIndex = pregraph.vertices.IndexOfMin(v => v.Score);
            var maxScore = pregraph.vertices.Max(v => v.Score);
            var vertices = pregraph.vertices
                .Select(v => new ViewNode(v.Position))
                .ToList();
            var viewconesAsObstacles = viewcones
                .Select(v => v.vertices.Select(x => x.Position).ToList())
                .Cast<IReadOnlyList<Vector2>>()
                .ToList();
            var edges = new Dictionary<int, List<(int SecondIndex, ViewMidEdgeInfo Info, int? InViewcone)>>();
            for(int i = 0; i < vertices.Count; i++) {
                var list = new List<(int SecondIndex, ViewMidEdgeInfo Info, int? InViewcone)>();
                for(int j = 0; j < vertices.Count; j++) {
                    if(i == j)
                        continue;
                    var e = pregraph[i, j];
                    if(e != null &&
                        staticNavGraph.CanGetToStraight(vertices[i].Position, vertices[j].Position, viewconesAsObstacles)) {
                        list.Add((j, new ViewMidEdgeInfo(e.Score, false, true, false, 0), null));
                    } 
                }
                edges.Add(i, list);
            }
            var viewconeIndices = new List<(ViewconeGraph Viewcone, int FromIndex, int Count)>();
            for(int k = 0; k < viewcones.Count; k++) {
                var view = viewcones[k];
                var preIndex = vertices.Count;
                viewconeIndices.Add((view, preIndex, view.vertices.Count));
                vertices.AddRange(view.vertices);
                for(int i = 0; i < view.vertices.Count; i++) {
                    var list = new List<(int SecondIndex, ViewMidEdgeInfo Info, int? InViewcone)>();
                    for(int j = 0; j < view.vertices.Count; j++) {
                        if(i == j)
                            continue;
                        var e = view[i, j];
                        if(e != null)
                            list.Add((preIndex + j, e, k));
                    }
                    edges.Add(preIndex + i, list);
                }
                for(int i = 0; i < view.vertices.Count; i++) {
                    for(int j = 0; j < preIndex; j++) {
                        if(staticNavGraph.CanPlayerGetToStraight(view.vertices[i].Position, vertices[j].Position)
                            && staticNavGraph.CanGetToStraight(view.vertices[i].Position, vertices[j].Position, viewconesAsObstacles)) {
                            var dist = Vector2.Distance(view.vertices[i].Position, vertices[j].Position);
                            ViewMidEdgeInfo info = new ViewMidEdgeInfo(dist, false, true, false, 0);
                            edges[i + preIndex].Add((j, info, null));
                            edges[j].Add((i + preIndex, info, null));
                        }
                    }
                }
            }
            return FromLists(vertices, edges, viewconeIndices, rootIndex, maxScore);
        }
        private static GraphWithViewcones FromLists(List<ViewNode> prenodes, 
            Dictionary<int, List<(int SecondIndex, ViewMidEdgeInfo Info, int? InViewcone)>> preedges, 
            List<(ViewconeGraph Viewcone, int FromIndex, int Count)> previewcones,
            int rootIndex,
            float previousMaxScore) {

            var nodes = new List<(ScoredActionedNode Node, int PreIndex)>();
            var edgesPlaceholders = new List<EdgePlaceHolder>();
            List<int> seen = new List<int>();
            var que = new PriorityQueue<float, (ScoredActionedNode? Previous, int Index)>();
            que.Enqueue(0, (null, rootIndex));
            int iterations = 0;
            while(que.Any()) {
                var curr = que.DequeueMinWithKey();
                var score = curr.Key;
                var currIndex = curr.Value.Index;
                if(seen.Contains(currIndex))
                    continue;
                iterations++;
                if(iterations > prenodes.Count + 5)
                    throw new Exception("Possible infinite loop, investigate.");
                seen.Add(currIndex);
                var node = new ScoredActionedNode(score,
                    curr.Value.Previous != null ? curr.Value.Previous.Score : 0,
                    curr.Value.Previous,
                    prenodes[currIndex].Position,
                    null);
                nodes.Add((node, currIndex));
                foreach(var e in preedges[currIndex]) {
                    if(e.Info.Traversable) {
                        var edgeScore = e.Info.Score;
                        if(e.Info.IsEdgeOfRemoved || e.Info.IsInMidViewcone) {
                            edgeScore += previousMaxScore;
                        }
                        que.Enqueue(score + edgeScore, (node, e.SecondIndex));
                        edgesPlaceholders.Add(
                            new EdgePlaceHolder(node, e.SecondIndex, 
                                new GraphWithViewconeEdgeInfo(edgeScore, e.InViewcone, e.Info.AlertingIncrease)));
                    }
                }
            }

            var edges = new List<Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>>();
            foreach(var edge in edgesPlaceholders) {
                ScoredActionedNode? second = null;
                foreach(var node in nodes) {
                    if(edge.preIndexOfSecond == node.PreIndex) {
                        second = node.Node;
                        break;
                    }
                }
                if(second == null) {
                    throw new Exception("This should never be reached! All edges must have both of their ending points!.");
                }
                edges.Add(new Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>(
                    edge.first,
                    second,
                    edge.info));
            }

            List<(Viewcone, IReadOnlyList<ScoredActionedNode>)> viewcones
                = new List<(Viewcone, IReadOnlyList<ScoredActionedNode>)>();
            foreach(var v in previewcones) {
                List<ScoredActionedNode> list = new List<ScoredActionedNode>();
                for(int i = 0; i < v.Count; i++) {
                    foreach(var node in nodes) {
                        if(v.FromIndex + i == node.PreIndex) {
                            list.Add(node.Node);
                            break;
                        }
                    }
                }
                viewcones.Add((v.Viewcone.Viewcone, list));
            }

            return new GraphWithViewcones(nodes.Select(n => n.Node).ToList(), edges, viewcones, true);
        }

        class EdgePlaceHolder {
            public readonly ScoredActionedNode first;
            public readonly int preIndexOfSecond;
            public readonly GraphWithViewconeEdgeInfo info;

			public EdgePlaceHolder(ScoredActionedNode first, int preIndexOfSecond, GraphWithViewconeEdgeInfo info) {

				this.first = first;
				this.preIndexOfSecond = preIndexOfSecond;
                this.info = info;
			}
		}
	}
}
