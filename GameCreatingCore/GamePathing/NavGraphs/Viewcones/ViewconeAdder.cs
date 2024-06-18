using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.RectTransform;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	internal class ViewconeAdder {
        private readonly StaticNavGraph staticNavGraph;

		public ViewconeAdder(StaticNavGraph staticNavGraph) {
			this.staticNavGraph = staticNavGraph;
		}

		/// <summary>
		/// Adds the given <paramref name="viewcones"/> into the static nav graph and thus creates one final graph.
		/// </summary>
		public GraphWithViewcones AddViewconesToGraph(List<ViewconeGraph> viewcones) {
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

            var whichContainsWhat = WhichViewconesContainWhichPoints(viewconesAsObstacles, vertices.Select(v => v.Position));
            var edges = GetEdgesAmongstStaticGraph(viewcones, vertices, whichContainsWhat, viewconesAsObstacles);
            //edges within the static graph are now solved

            List<EdgePlaceHolder> viewconeEdges = new List<EdgePlaceHolder>();
            List<List<int>> viewconeIndices = new List<List<int>>();
            for(int i = 0; i < viewcones.Count; i++) {
                viewconeIndices.Add(AddNodes(viewcones[i], vertices));
                viewconeEdges.AddRange(GetEdgesInViewcone(viewcones[i], vertices, i));
            }
            CombineEdges(viewconeEdges);
            //now all edges should be distinct...
            //but we still need to add edges in between viewcones and from viewcones to static graph
            AddEdgesBetweenViewcones(viewconeIndices, vertices, viewconeEdges, viewconesAsObstacles);
            var moreEdges = GetEdgesBetweenGraphAndViewcones(
                vertices,
                Enumerable.Range(0, pregraph.vertices.Count),
                viewconeIndices,
                viewcones,
                viewconesAsObstacles,
                whichContainsWhat).ToList();
            edges.AddRange(CombineEdges(moreEdges));
            edges.AddRange(viewconeEdges);

            return FromLists(vertices, edges, 
                Enumerable.Range(0, viewcones.Count).Select(i => (viewcones[i], viewconeIndices[i])),
                rootIndex,
                maxScore);
        }

        List<EdgePlaceHolder> GetEdgesAmongstStaticGraph(IReadOnlyList<ViewconeGraph> viewcones, IReadOnlyList<ViewNode> vertices,
            Dictionary<int, IReadOnlyList<int>> whichContainsWhat, IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles) {
            var pregraph = staticNavGraph.PlayerStaticNavGraph;
            var edges = new List<EdgePlaceHolder>();
            for(int i = 0; i < vertices.Count; i++) {
                for(int j = 0; j < vertices.Count; j++) {
                    if(i == j)
                        continue;
                    var e = pregraph[i, j];
                    if(e != null) {
                        var viewIndices = AreInSameViewcones(i, j, whichContainsWhat).ToList();
                        if(viewIndices.Count == 0) {
                            if(staticNavGraph.CanGetToStraight(vertices[i], vertices[j], viewconesAsObstacles)) {
                                edges.Add(new EdgePlaceHolder(i, j,
                                    new ViewMidEdgeInfo(e.Score, false, true, false, 0), null));
                            }
                        } else {
                            var aler = float.MinValue;
                            var canGo = true;
                            foreach(var viewIndex in viewIndices) {
                                if(viewcones[viewIndex].Viewcone.CanGoFromTo(vertices[i].Position,
                                    vertices[j].Position, e.Score, out var aler2)) {
                                    aler = Math.Max(aler, aler2);
                                } else {
                                    canGo = false;
                                    break;
                                }
                            }
                            if(canGo) {
                                edges.Add(new EdgePlaceHolder(i,j,
                                    new ViewMidEdgeInfo(e.Score, false, true, true, aler), null));
                            }
                        }
                    }
                }
            }
            return edges;
        } 

        Dictionary<int, IReadOnlyList<int>> WhichViewconesContainWhichPoints(IEnumerable<IReadOnlyList<Vector2>> viewcones, 
            IEnumerable<Vector2> points) {
            Dictionary<int, IReadOnlyList<int>> pointsInViewcones = new Dictionary<int, IReadOnlyList<int>>();
            int viewCounter = 0;
            foreach(var v in viewcones) {
                List<int> contains = new List<int>();
                int pointCounter = 0;
                foreach(var p in points) {
                    if(Obstacle.IsInPolygon(p, v))
                        contains.Add(pointCounter);
                    pointCounter++;
                }
                pointsInViewcones.Add(viewCounter, contains);
                viewCounter++;
            }
            return pointsInViewcones;
        }

        private static GraphWithViewcones FromLists(List<ViewNode> prenodes,
            List<EdgePlaceHolder> preedges,
            IEnumerable<(ViewconeGraph Viewcone, List<int> Indices)> previewcones,
            int rootIndex,
            float previousMaxScore) {

            var nodes = Enumerable.Repeat<ScoredActionedNode?>(null, prenodes.Count).ToList();
            var edgeScores = Enumerable.Repeat<float?>(null, preedges.Count).ToList();
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
                nodes[currIndex] = node;
                foreach(var e in preedges.Select((e, i) => (e, i)).Where(e => e.e.FirstIndex == currIndex)) {
                    if(e.e.Info.Traversable) {
                        var edgeScore = e.e.Info.Score;
                        if(e.e.Info.IsEdgeOfRemoved || e.e.Info.IsInMidViewcone) {
                            edgeScore += previousMaxScore;
                        }
                        que.Enqueue(score + edgeScore, (node, e.e.SecondIndex));
                        edgeScores[e.i] = edgeScore;
                    }
                }
            }

            var finalEdges = new List<Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>>();

            for(int i = 0; i < preedges.Count; i++) {
                if(!edgeScores[i].HasValue)
                    continue;
                var score = edgeScores[i]!.Value;
                var first = nodes[preedges[i].FirstIndex];
                var second = nodes[preedges[i].SecondIndex];
                if(first != null && second != null) {
                    var info = new GraphWithViewconeEdgeInfo(score, 
                        preedges[i].ViewconeIndex, 
                        preedges[i].Info.AlertingIncrease);
                    finalEdges.Add(new Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>(first, second, info));
                }
            }


            List<(Viewcone, IReadOnlyList<ScoredActionedNode>)> viewcones
                = new List<(Viewcone, IReadOnlyList<ScoredActionedNode>)>();
            foreach(var v in previewcones) {
                List<ScoredActionedNode> list = v.Indices
                    .Select(i => nodes[i])
                    .Where(n => n != null)
                    .Cast<ScoredActionedNode>()
                    .ToList();
                viewcones.Add((v.Viewcone.Viewcone, list));
            }

            var finalNodes = nodes
                .Where(n => n != null)
                .Cast<ScoredActionedNode>()
                .ToList();

            return new GraphWithViewcones(finalNodes, finalEdges, viewcones, true);
        }

        
        IEnumerable<EdgePlaceHolder> GetEdgesBetweenGraphAndViewcones(IReadOnlyList<ViewNode> vertices, 
            IEnumerable<int> staticGraphIndices,
            IReadOnlyList<IReadOnlyList<int>> viewconeIndices, 
            IReadOnlyList<ViewconeGraph> viewcones,
            IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles,
            IReadOnlyDictionary<int, IReadOnlyList<int>> whichContainsWhat) {
            
            IEnumerable<EdgePlaceHolder> result = Enumerable.Empty<EdgePlaceHolder>();

            for(int i = 0; i < viewconeIndices.Count; i++) {

                Func<int, int, float, IEnumerable<EdgePlaceHolder>> createFunc = (k, j, dist) => {
                    var result = Enumerable.Empty<EdgePlaceHolder>();
                    if(whichContainsWhat[i].Contains(k)) {
                        if(viewcones[i].Viewcone.CanGoFromTo(vertices[k].Position, vertices[j].Position, dist, out var aler)) {
                            var info = new ViewMidEdgeInfo(dist, false, true, true, aler);
                            result = result.Append(new EdgePlaceHolder(k, j, info, i));
                        }
                        if(viewcones[i].Viewcone.CanGoFromTo(vertices[j].Position, vertices[k].Position, dist, out aler)) {
                            var info = new ViewMidEdgeInfo(dist, false, true, true, aler);
                            result = result.Append(new EdgePlaceHolder(j, k, info, i));
                        }
                    } else {
                        var info = new ViewMidEdgeInfo(dist, false, true, false, 0);
                        result = result
                            .Append(new EdgePlaceHolder(k, j, info, null))
                            .Append(new EdgePlaceHolder(j, k, info, null));
                    }
                    return result;
                };

                var es = GetEdgesBetweenAll(staticGraphIndices,
                    viewconeIndices[i],
                    vertices,
                    (i, j) => true,
                    viewconesAsObstacles,
                    createFunc);
                result = result.Concat(es);
            }
            return result;
        }
        
        void AddEdgesBetweenViewcones(List<List<int>> viewconeIndices, IReadOnlyList<ViewNode> nodes, 
            List<EdgePlaceHolder> currentEdges, IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles) {

            List<EdgePlaceHolder> addingEdges = new List<EdgePlaceHolder>();
            Func<int, int, bool> addingPredicate = (currX, currY) =>
                currentEdges.IndexOf(x => x.FirstIndex == currX && x.SecondIndex == currY) == -1
                && currentEdges.IndexOf(x => x.FirstIndex == currY && x.SecondIndex == currX) == -1;

            Func<int, int, float, IEnumerable<EdgePlaceHolder>> createFunc = (i, j, dist) => {
                var info = new ViewMidEdgeInfo(dist, false, true, false, 0);
                return Enumerable.Empty<EdgePlaceHolder>()
                    .Append(new EdgePlaceHolder(i, j, info, null))
                    .Append(new EdgePlaceHolder(j, i, info, null));
            };

            for(int i = 0; i < viewconeIndices.Count; i++) {
                for(int j = i + 1; j < viewconeIndices.Count; j++) {
                    var es = GetEdgesBetweenAll(viewconeIndices[i], viewconeIndices[j], nodes, 
                        addingPredicate, viewconesAsObstacles, createFunc);
                    addingEdges.AddRange(es);
                }
            }
            currentEdges.AddRange(addingEdges);
        }

        IEnumerable<EdgePlaceHolder> GetEdgesBetweenAll(IEnumerable<int> indices1, IEnumerable<int> indices2, 
            IReadOnlyList<ViewNode> nodes, Func<int, int, bool> addingPredicate, 
            IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles,
            Func<int, int, float, IEnumerable<EdgePlaceHolder>> createFunc) {

            IEnumerable<EdgePlaceHolder> result = Enumerable.Empty<EdgePlaceHolder>();

            foreach(var i in indices1) {
                if(indices2.Contains(i))
                    continue;
                foreach(var j in indices2) {
                    if(i == j || indices1.Contains(j))
                        continue;
                    if(addingPredicate(i, j)) {
                        if(staticNavGraph.CanPlayerGetToStraight(nodes[i], nodes[j])
                            && (!staticNavGraph.IsLineInsidePlayerObstacle(nodes[i], nodes[j]))
                            && staticNavGraph.CanGetToStraight(nodes[i].Position, nodes[j].Position, viewconesAsObstacles)) {
                            var dist = Vector2.Distance(nodes[i].Position, nodes[j].Position);
                            result = result.Concat(createFunc(i, j, dist));
                        }
                    }
                }
            }
            return result;
        }

        private List<int> AddNodes(ViewconeGraph viewconeGraph, List<ViewNode> nodes) {
            List<int> onIndices = new List<int>();
            foreach(var v in viewconeGraph.vertices) {
                int index = nodes.IndexOf(n => n.Position == v.Position);
                if(index == -1) {
                    nodes.Add(v);
                    onIndices.Add(nodes.Count - 1);
                } else {
                    nodes[index] = NodeCombine(nodes[index], v);
                    onIndices.Add(index);
                }
            }
            return onIndices;
        }

        private List<EdgePlaceHolder> GetEdgesInViewcone(ViewconeGraph viewconeGraph, List<ViewNode> nodes, int viewconeIndex) {
            var preEdges = new List<EdgePlaceHolder>();
            foreach(var e in viewconeGraph.edges) {
                int i1 = nodes.IndexOf(n => n.Position == e.First.Position);
                int i2 = nodes.IndexOf(n => n.Position == e.Second.Position);
                preEdges.Add(new EdgePlaceHolder(i1, i2, e.EdgeInfo, viewconeIndex));
            }
            return preEdges;
        }

        List<EdgePlaceHolder> CombineEdges(List<EdgePlaceHolder> preEdges) {
            var edges = new List<EdgePlaceHolder>();
            List<int> used = new List<int>();
            for(int i = 0; i < preEdges.Count; i++) {
                if(used.Remove(i))
                    continue;
                ViewMidEdgeInfo info = preEdges[i].Info;
                int? viewconeIndex = preEdges[i].ViewconeIndex;
                for(int j = i + 1; j < preEdges.Count; j++) {
                    if(used.Contains(j))
                        continue;
                    if(preEdges[i].FirstIndex == preEdges[j].FirstIndex && preEdges[i].SecondIndex == preEdges[j].SecondIndex) {
                        info = EdgeCombine(info, preEdges[j].Info);
                        //we need any viewcone index, if it exists, so it's okey this way
                        viewconeIndex = viewconeIndex ?? preEdges[j].ViewconeIndex;
                    }
                }
                edges.Add(new EdgePlaceHolder(preEdges[i].FirstIndex, preEdges[i].SecondIndex, info, viewconeIndex));
            }
            return edges;
        }

        private IEnumerable<int> AreInSameViewcones(int index1, int index2, 
            Dictionary<int, IReadOnlyList<int>> whichViewconeHasWhichPoints) {
            foreach(var p in whichViewconeHasWhichPoints) {
                if(p.Value.Contains(index1) &&
                    p.Value.Contains(index2))
                    yield return p.Key;
            }
        }

        private ViewNode NodeCombine(ViewNode n1, ViewNode n2) {
            if(n1.IsMiddleNode != n2.IsMiddleNode) {
                //here we could do ... = node, but this way if ViewNodeSignature is changed,
                //we will have to change this too and make it match the actual meaning
                return new ViewNode(n1.Position, true);
            }
            return n1;
        }

        private ViewMidEdgeInfo EdgeCombine(ViewMidEdgeInfo e1, ViewMidEdgeInfo e2) {
            return new ViewMidEdgeInfo(Math.Max(e1.Score, e2.Score),
                e1.IsEdgeOfRemoved || e2.IsEdgeOfRemoved,
                e1.Traversable && e2.Traversable,
                e1.IsInMidViewcone && e2.IsInMidViewcone,
                Math.Max(e1.AlertingIncrease, e2.AlertingIncrease));
        }

        class EdgePlaceHolder  {

			public ViewMidEdgeInfo Info { get; }
            public int FirstIndex { get; }
            public int SecondIndex { get; }

            public int? ViewconeIndex { get; }
            
			public EdgePlaceHolder(int firstIndex, int secondIndex, ViewMidEdgeInfo info, int? viewconeIndex) {
				Info = info;
				FirstIndex = firstIndex;
				SecondIndex = secondIndex;
                ViewconeIndex = viewconeIndex;
			}
        }
	}
}