using GameCreatingCore.GameActions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using GameCreatingCore.GamePathing;
using GameCreatingCore.LevelRepresentationData;

namespace GameCreatingCore.LevelSolving.Viewcones
{

    internal class GraphPartsCombiner {
        private readonly StaticNavGraph staticNavGraph;

		public GraphPartsCombiner(StaticNavGraph staticNavGraph) {
			this.staticNavGraph = staticNavGraph;
		}

		/// <summary>
		/// Adds the given <paramref name="viewcones"/>, <paramref name="pickupNodes"/> and <paramref name="skillNodes"/> into 
        /// the static nav graph and thus creates one final graph.
		/// </summary>
		public ViewconeNavGraphDataHolder Combine(List<ViewconeGraph> viewcones,
        IReadOnlyList<(IGameAction, Vector2)> pickupNodes,
        IReadOnlyList<(IGameAction, Vector2)> skillNodes) {
            var pregraph = staticNavGraph.PlayerStaticNavGraph;
            var rootIndex = pregraph.vertices.IndexOfMin(v => v.Score);
            var maxScore = pregraph.vertices.Select(v => v.Score).Append(100).Max();
            var vertices = pregraph.vertices
                .Select(v => new ViewActionedNode(v.Position, null, null, null))
                .ToList();
            var viewconesAsObstacles = viewcones
                .Select(v => v.vertices.Select(x => x.Position).ToList())
                .Cast<IReadOnlyList<Vector2>>()
                .ToList();

            var whichContainsWhat = WhichViewconesContainWhichPoints(viewconesAsObstacles, vertices.Select(v => v.Position));
            vertices = SetVerticesInViewcones(vertices, whichContainsWhat, viewcones);
            var edges = GetEdgesAmongstStaticGraph(viewcones, vertices, whichContainsWhat, viewconesAsObstacles);
            //edges within the static graph are now solved

            var staticGraphCount = vertices.Count;
            //for non of these we want edges amongst the group it self
            var pickUpIndices = AddNodesAndAllEdges(pickupNodes!, vertices, edges, viewconesAsObstacles);
            var skillIndices = AddNodesAndAllEdges(skillNodes!, vertices, edges, viewconesAsObstacles);
            
            List<EdgePlaceHolder> viewconeEdges = new List<EdgePlaceHolder>();
            List<List<int>> viewconeIndices = new List<List<int>>();
            for(int i = 0; i < viewcones.Count; i++) {
                viewconeIndices.Add(AddNodes(viewcones[i].vertices.Select(v => (v, (IGameAction?)null)), i,
                    n => Vector2.Distance(viewcones[i].Viewcone.StartPos, n.Position), vertices));
                viewconeEdges.AddRange(GetEdgesInViewcone(viewcones[i], vertices, i));
            }
            viewconeEdges = CombineEdges(viewconeEdges);
            //now all edges should be distinct...
            //but we still need to add edges in between viewcones and from viewcones to static graph
            AddEdgesBetweenViewcones(viewconeIndices, vertices, viewconeEdges, viewconesAsObstacles);
            var moreEdges = GetEdgesBetweenGraphAndViewcones(
                vertices,
                Enumerable.Range(0, pregraph.vertices.Count).Concat(skillIndices).Concat(pickUpIndices),
                viewconeIndices,
                viewcones,
                viewconesAsObstacles,
                whichContainsWhat).ToList();
            edges.AddRange(CombineEdges(moreEdges));
            edges.AddRange(viewconeEdges);

            var vs = Enumerable
                .Range(0, viewcones.Count)
                .Select(i => (viewcones[i].Viewcone, (IReadOnlyList<int>)viewconeIndices[i]))
                .ToList();
            return new ViewconeNavGraphDataHolder(edges, vertices, vs, 
                new List<int>() { rootIndex }, pickUpIndices, skillIndices);
        }

        List<EdgePlaceHolder> GetEdgesAmongstStaticGraph(IReadOnlyList<ViewconeGraph> viewcones, IReadOnlyList<ViewNode> vertices,
            Dictionary<int, List<int>> whichContainsWhat, IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles) {
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
                            int? viewIndexRes = null;
                            foreach(var viewIndex in viewIndices) {
                                if(viewcones[viewIndex].Viewcone.CanGoFromTo(vertices[i].Position,
                                    vertices[j].Position, e.Score, out var aler2)) {
                                    if(aler < aler2) {
                                        aler = aler2;
                                        viewIndexRes = viewIndex;
                                    }
                                } else {
                                    canGo = false;
                                    break;
                                }
                            }
                            if(canGo) {
                                edges.Add(new EdgePlaceHolder(i,j,
                                    new ViewMidEdgeInfo(e.Score, false, true, true, aler), viewIndexRes));
                            }
                        }
                    }
                }
            }
            return edges;
        } 

        Dictionary<int, List<int>> WhichViewconesContainWhichPoints(IEnumerable<IReadOnlyList<Vector2>> viewcones, 
            IEnumerable<Vector2> points) {
            Dictionary<int, List<int>> pointsInViewcones = new Dictionary<int, List<int>>();
            int viewCounter = 0;
            foreach(var v in viewcones) {
                List<int> contains = new List<int>();
                int pointCounter = 0;
                foreach(var p in points) {
                    if(Obstacle.IsInPolygon(p, v, true))
                        contains.Add(pointCounter);
                    pointCounter++;
                }
                pointsInViewcones.Add(viewCounter, contains);
                viewCounter++;
            }
            return pointsInViewcones;
        }

        IEnumerable<EdgePlaceHolder> GetEdgesBetweenGraphAndViewcones(IReadOnlyList<ViewNode> vertices, 
            IEnumerable<int> staticGraphIndices,
            IReadOnlyList<IReadOnlyList<int>> viewconeIndices, 
            IReadOnlyList<ViewconeGraph> viewcones,
            IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles,
            IReadOnlyDictionary<int, List<int>> whichContainsWhat) {
            
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
                        bool isInOtherViewcone = false;
                        foreach(var key in whichContainsWhat.Keys) {
                            if(!(key == i || !whichContainsWhat[key].Contains(k))) {
                                //this would mean that the 2 viewcones are one next to each other and share some edge,
                                //where vertices[k] is inside the other viewcone
                                isInOtherViewcone = true;
                                break;
                            }
                        }
                        if(!isInOtherViewcone) {
                            var info = new ViewMidEdgeInfo(dist, false, true, false, 0);
                            result = result
                                .Append(new EdgePlaceHolder(k, j, info, null))
                                .Append(new EdgePlaceHolder(j, k, info, null));
                        }
                    }
                    return result;
                };

                var es = GetEdgesBetweenAll(staticGraphIndices,
                    viewconeIndices[i],
                    vertices,
                    (i, j) => true,
                    viewconesAsObstacles,
                    createFunc,
                    true);
                result = result.Concat(es);
            }
            return result;
        }
        
        void AddEdgesBetweenViewcones(List<List<int>> viewconeIndices, IReadOnlyList<ViewNode> nodes, 
            List<EdgePlaceHolder> currentEdges, IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles) {

            Func<int, int, bool> addingPredicate = (currX, currY) =>
                currentEdges.IndexOf(x => x.FirstIndex == currX && x.SecondIndex == currY) == -1
                && currentEdges.IndexOf(x => x.FirstIndex == currY && x.SecondIndex == currX) == -1;

            for(int i = 0; i < viewconeIndices.Count; i++) {
                for(int j = i + 1; j < viewconeIndices.Count; j++) {
                    var es = GetEdgesBetweenAll(viewconeIndices[i], viewconeIndices[j], nodes, 
                        addingPredicate, viewconesAsObstacles, false);
                    currentEdges.AddRange(es);
                }
            }
        }

        IEnumerable<EdgePlaceHolder> GetEdgesBetweenAll(IEnumerable<int> indices1, IEnumerable<int> indices2,
            IReadOnlyList<ViewNode> nodes, Func<int, int, bool> addingPredicate,
            IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles,
            bool allowInsideViewcones) { 
        
            Func<int, int, float, IEnumerable<EdgePlaceHolder>> createFunc = (i, j, dist) => {
                var info = new ViewMidEdgeInfo(dist, false, true, false, 0);
                return Enumerable.Empty<EdgePlaceHolder>()
                    .Append(new EdgePlaceHolder(i, j, info, null))
                    .Append(new EdgePlaceHolder(j, i, info, null));
            };
            return GetEdgesBetweenAll(indices1, indices2, nodes, addingPredicate, viewconesAsObstacles, createFunc, allowInsideViewcones);
        }
        IEnumerable<EdgePlaceHolder> GetEdgesBetweenAll(IEnumerable<int> indices1, IEnumerable<int> indices2, 
            IReadOnlyList<ViewNode> nodes, Func<int, int, bool> addingPredicate, 
            IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles,
            Func<int, int, float, IEnumerable<EdgePlaceHolder>> createFunc,
            bool allowInsideViewcones) {

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
                            && staticNavGraph.CanGetToStraight(nodes[i].Position, nodes[j].Position, viewconesAsObstacles, true)
                            && (allowInsideViewcones || 
                                !staticNavGraph.IsLineInsideAnyObstacle(nodes[i].Position, nodes[j].Position, viewconesAsObstacles))) {

                            var dist = Vector2.Distance(nodes[i].Position, nodes[j].Position);
                            result = result.Concat(createFunc(i, j, dist));
                        }
                    }
                }
            }
            return result;
        }

        private List<int> AddNodes<N>(IEnumerable<(N Node, IGameAction? Action)> vertices, int? inViewcone,
            Func<N, float?> distanceToEnemyFunc,
            List<ViewActionedNode> nodes) where N : ViewNode {

            List<int> onIndices = new List<int>();
            foreach(var v in vertices) {
                int index = nodes.IndexOf(n => FloatEquality.AreEqual(n.Position, v.Node.Position));
                if(index == -1) {
                    nodes.Add(new ViewActionedNode(v.Node.Position, distanceToEnemyFunc(v.Node), 
                        inViewcone, v.Action, v.Node.IsMiddleNode));
                    onIndices.Add(nodes.Count - 1);
                } else {
                    nodes[index] = NodeCombine(nodes[index], v.Node);
                    onIndices.Add(index);
                }
            }
            return onIndices;
        }

        private List<int> AddNodesAndAllEdges(IEnumerable<(IGameAction? Action, Vector2 Position)> toAdd, 
            List<ViewActionedNode> nodes, List<EdgePlaceHolder> edges, IReadOnlyList<IReadOnlyList<Vector2>> viewconesAsObstacles) {
            var whichContainsWhat = WhichViewconesContainWhichPoints(viewconesAsObstacles, toAdd.Select(x => x.Position));

            toAdd = toAdd
                //we basically say that the player will not use or pickup skills when inside enemy viewcones
                //it is weird to do so, not needed and it worsens performence
                .Where(x => !viewconesAsObstacles.Any(o => Obstacle.IsInPolygon(x.Position, o, false)));

            var result = AddNodes(toAdd.Select(n => (new ViewNode(n.Position, null, null), n.Action)), null, n => null, nodes);

            edges.AddRange(GetEdgesBetweenAll(
                Enumerable.Range(0, nodes.Count),
                result,
                nodes,
                (i, j) => true,
                viewconesAsObstacles,
                false));

            return result;
        }

        private List<EdgePlaceHolder> GetEdgesInViewcone<N>(ViewconeGraph viewconeGraph, List<N> nodes, int viewconeIndex)
            where N : Node{
            var preEdges = new List<EdgePlaceHolder>();
            foreach(var e in viewconeGraph.edges) {
                int i1 = nodes.IndexOf(n => FloatEquality.AreEqual(n.Position, e.First.Position));
                int i2 = nodes.IndexOf(n => FloatEquality.AreEqual(n.Position, e.Second.Position));
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
            Dictionary<int, List<int>> whichViewconeHasWhichPoints) {
            foreach(var p in whichViewconeHasWhichPoints) {
                if(p.Value.Contains(index1) &&
                    p.Value.Contains(index2))
                    yield return p.Key;
            }
        }

        private List<ViewActionedNode> SetVerticesInViewcones(List<ViewActionedNode> nodes, 
            Dictionary<int, List<int>> insideViewcones, IReadOnlyList<ViewconeGraph> viewcones) {

            List<ViewActionedNode?> result = Enumerable.Range(0, nodes.Count).Select(i => (ViewActionedNode?)null).ToList();

            foreach(var k in insideViewcones.Keys) {
                foreach(int i in insideViewcones[k]) {
                    float dist = Vector2.Distance(nodes[i].Position, viewcones[k].Viewcone.StartPos);
                    if(result[i] == null || result[i]!.DistanceFromEnemy > dist) {
                        result[i] = new ViewActionedNode(nodes[i].Position, dist, k, nodes[i].NodeAction, nodes[i].IsMiddleNode);
                    }
                }
            }

            for(int i = 0; i < result.Count; i++) {
                result[i] = result[i] ?? nodes[i];
            }

            return result!;
        }
        
        private ViewActionedNode NodeCombine<N>(ViewActionedNode n1, N n2) where N : ViewNode{
            var dist = n2.DistanceFromEnemy;
            var en = n2.EnemyIndex;
            if(n1.DistanceFromEnemy < n2.DistanceFromEnemy) { 
                //this is not very accurate, since different enemy types can see different distances
                // and in different speed, but good enough
                dist = n1.DistanceFromEnemy;
                en = n1.EnemyIndex;
            }

            return new ViewActionedNode(n1.Position, dist, en,
                n1.NodeAction, n1.IsMiddleNode || n2.IsMiddleNode);
        }

        private ViewMidEdgeInfo EdgeCombine(ViewMidEdgeInfo e1, ViewMidEdgeInfo e2) {
            return new ViewMidEdgeInfo(Math.Max(e1.Score, e2.Score),
                e1.IsEdgeOfRemoved || e2.IsEdgeOfRemoved,
                e1.Traversable && e2.Traversable,
                e1.IsInMidViewcone && e2.IsInMidViewcone,
                Math.Max(e1.AlertingIncrease, e2.AlertingIncrease));
        }

	}
}