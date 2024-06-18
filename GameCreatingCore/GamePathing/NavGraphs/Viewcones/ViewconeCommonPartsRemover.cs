using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using static UnityEngine.RectTransform;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones
{
    internal class ViewconeCommonPartsRemover
    {
        //private readonly ViewconePruningStyle style;
        //public ViewconeCommonPartsRemover(ViewconePruningStyle style)
        //{
        //    this.style = style;
        //}


        /// <summary>
        /// Cuts off parts of <paramref name="firstIn"/> viewcone, that are in <paramref name="secondIn"/>
        /// viewcone (if they include some point there). 
        /// </summary>
        /// <param name="secondOut">The function can also change the shape of 
        /// the <paramref name="secondIn"/> parameter. This change is going to be outputed here.</param>
        /// <param name="staticNavGraph">Since the function adds new vertices, it has to connect 
        /// them with the other ones, and so it has to know if it can.</param>
        public ViewconeGraph CutOffCommonParts(ViewconeGraph firstIn, ViewconeGraph secondIn,
            StaticNavGraph staticNavGraph, out ViewconeGraph secondOut) {
            var firstVertices = firstIn.vertices.Select(n => n.Position).ToList();
            var secondVertices = secondIn.vertices.Select(n => n.Position).ToList();
            var firstCount = NumberOfPointsInside(firstVertices, secondVertices);
            var secondCount = NumberOfPointsInside(secondVertices, firstVertices);
            if(firstCount == 0 && secondCount == 0) {
                secondOut = secondIn;
                return firstIn;
            }

            bool swap = secondCount > firstCount;
            if(swap) {
                secondOut = CutOff(secondIn, firstIn, staticNavGraph, out var result);
                return result;
            } else {
                return CutOff(firstIn, secondIn, staticNavGraph, out secondOut);
            }
            
        }

        private int NumberOfPointsInside(IEnumerable<Vector2> points, IReadOnlyList<Vector2> shape) {
            int counter = 0;
            foreach(var p in points) {
                if(Obstacle.IsInPolygon(p, shape))
                    counter++;
            }
            return counter;
        }

        private ViewconeGraph CutOff(ViewconeGraph firstIn, ViewconeGraph secondIn,
            StaticNavGraph staticNavGraph, out ViewconeGraph secondOut) {

            ViewconeGraph result = firstIn;
            secondOut = secondIn;

            var vertices = new List<ViewNode>(result.vertices);
            var secondVer = secondOut.vertices.Select(v => v.Position).ToList();

            bool backRemoved = false;
            for (int i = 0; i < vertices.Count - (backRemoved ? 1 : 0); i++)
            {
                if(!Obstacle.IsInPolygon(vertices[i].Position, secondVer)) {
                    continue;
                }
                var first = i;
                int preFirst = first - 1;
                bool backRemovedNow = false;
                if (i == 0)
                {
                    backRemoved = true;
                    backRemovedNow = true;
                    preFirst = vertices.Count - 1;
                    while (Obstacle.IsInPolygon(vertices[preFirst].Position, secondVer))
                    {
                        preFirst--;
                        if (preFirst < 0)
                        { //only happens if the whole first viewcone is within the other one
                            return new ViewconeGraph(new List<ViewNode>(),
                                new List<Edge<ViewNode, ViewMidEdgeInfo>>(), result.Viewcone);
                        }
                    }
                    first = (preFirst + 1) % vertices.Count;
                }
                do
                {
                    i++;
                    if (i >= vertices.Count)
                    {
                        break;
                    }
                } while (Obstacle.IsInPolygon(vertices[i].Position, secondVer));
                var last = i - 1;
                var postLast = i % vertices.Count;
                //now we know 4 points in the first viewcone:
                // the first one in and the one before it
                // the last one in and the one after it
                //we need to find the intersections of these lines with the second viewcone
                if(last == preFirst && first == postLast)
                    throw new NotSupportedException($"{nameof(CutOffCommonParts)}: The perimeter of " +
                        $"{nameof(secondIn)} starts inside the perimeter of {nameof(firstIn)}, but does not end there.");

				var firstInters = GetFirstInFirstIntersection(vertices[preFirst].Position, 
                    vertices[first].Position, secondVer);
                if(! firstInters.HasValue)
                    throw new NotSupportedException($"{nameof(CutOffCommonParts)}: There is no interseciton of " +
                        $"the perimeter of {nameof(secondIn)} and the FIRST 2 points" +
                        $" of {nameof(firstIn)}.");
                
                var lastInters = GetLastInFirstIntersection(vertices[last].Position, 
                    vertices[postLast].Position, secondVer);
                if(! lastInters.HasValue)
                    throw new NotSupportedException($"{nameof(CutOffCommonParts)}: There is no interseciton of " +
                        $"the perimeter of {nameof(secondIn)} and the LAST 2 points" +
                        $" of {nameof(firstIn)}.");

                result = CutoffFromTo(result,
                    firstInters.Value.Intersection, preFirst, first,
                    lastInters.Value.Intersection, postLast, last,
                    staticNavGraph);
                vertices = new List<ViewNode>(result.vertices);

                //we need to change the second in => either we need to split an edge
                //or some part has to be cutoff
                if(firstInters.Value.PerimeterIndex == lastInters.Value.PerimeterIndex) {
                    var firstEdge = GetEdgeWithRightEnds(secondOut.edges, secondVer, firstInters.Value.PerimeterIndex);

                    if(firstEdge == null) {
                        throw new NotSupportedException($"{nameof(CutOffCommonParts)}: Used edge not found.");
                    }

                    secondOut = BreakPerimeterEdgeIntoThree(firstInters.Value.Intersection, lastInters.Value.Intersection,
                        firstEdge, firstInters.Value.PerimeterIndex, PreviousIndex(firstInters.Value.PerimeterIndex, secondVer),
                        secondOut, staticNavGraph);
                } else {
                    secondOut = CutoffSecondary(secondOut, firstInters.Value.Intersection, lastInters.Value.Intersection, vertices,
                        firstInters.Value.PerimeterIndex,  PreviousIndex(firstInters.Value.PerimeterIndex, secondVer),
                        lastInters.Value.PerimeterIndex,  PreviousIndex(lastInters.Value.PerimeterIndex, secondVer),
                        staticNavGraph);
                }
                secondVer = secondOut.vertices.Select(v => v.Position).ToList();

                //we removed some nodes, so we have to adjust i accordingly
                if(backRemovedNow) {
                    i -= last;
                } else {
                    i -= last - 1 - first;
                }
            }
            return result;
        }

        private int PreviousIndex<T>(int index, IReadOnlyList<T> collection)
            => index != 0 ? (index - 1) : (collection.Count - 1);

        private Edge<N, E>? GetEdgeWithRightEnds<N, E> (IEnumerable<Edge<N, E>> edges, IReadOnlyList<Vector2> vertices, int vertexIndex)
            where N : Node where E : EdgeInfo{
            var end1 = vertices[vertexIndex];
            var end2 = vertices[PreviousIndex(vertexIndex, vertices)];
            foreach(var e in edges) {
                if((e.First.Position == end1 && e.Second.Position == end2)
                    ||(e.Second.Position == end1 && e.First.Position == end2)) {
                    return e;
                }
            }
            return null;
        }


        private ViewconeGraph CutoffSecondary(ViewconeGraph initial, Vector2 from, Vector2 to, List<ViewNode> primaryVertices,
            int edge1First, int edge1Second, int edge2First, int edge2Second, StaticNavGraph staticNavGraph) {
            var verts = primaryVertices.Select(v => v.Position).ToList();
            (int preFirst, int first, int last, int postLast)
                = GetOrder(edge1First, edge1Second, edge2First, edge2Second, initial.vertices, verts);

            var sqrDistsNow = (from - initial.vertices[preFirst].Position).sqrMagnitude
                + (to - initial.vertices[postLast].Position).sqrMagnitude;
            var sqrDistsPotential = (to - initial.vertices[preFirst].Position).sqrMagnitude
                + (from - initial.vertices[postLast].Position).sqrMagnitude;
            if(sqrDistsNow > sqrDistsPotential) {
                var tmp = from; from = to; to = tmp;
            }
            return CutoffFromTo(initial, from, preFirst, first, to, postLast, last, staticNavGraph);
        }

        (int PreFirst, int First, int Last, int PostLast) GetOrder<N>(int edge1First, int edge1Second, 
            int edge2First, int edge2Second, 
            IReadOnlyList<N> onPerimeter,
            IReadOnlyList<Vector2> against) where N : Node{

            List<int> ordered = new List<int>() { 
                edge1First, edge1Second, edge2First, edge2Second
            };
            ordered.Sort();

            if(ordered.Contains(0) && ordered.Contains(onPerimeter.Count - 1) 
                && ((!ordered.Contains(1)) || !ordered.Contains(onPerimeter.Count - 2))) 
                //that would be pairs 0,1...N-1,N, we are looking for a pair 0,N
            {
                bool isPoly = 
                    Obstacle.IsInPolygon(onPerimeter[0].Position, against)
                    && Obstacle.IsInPolygon(onPerimeter[ordered[1]].Position, against);
                if(isPoly) {
                    return (ordered[1], ordered[2], ordered[3], ordered[0]);
                } else {
                    return (ordered[3], ordered[0], ordered[1], ordered[2]);
                }

            } else { //then we need to get 2 consectuve pairs
                bool isPoly = 
                    Obstacle.IsInPolygon(onPerimeter[ordered[1]].Position, against)
                    && Obstacle.IsInPolygon(onPerimeter[ordered[2]].Position, against);
                if(isPoly) {
                    return (ordered[0], ordered[1], ordered[2], ordered[3]);
                } else {
                    return (ordered[2], ordered[3], ordered[0], ordered[1]);
                }
            }

        }


        /// <summary>
        /// Given one <paramref name="edge"/> within a <paramref name="viewconeGraph"/>, it breaks it into the following 3 new edges:
        /// <list type="bullet">
        /// <item><description><paramref name="edge"/>.First -> <paramref name="firstInters"/></description></item>
        /// <item><description><paramref name="firstInters"/> -> <paramref name="lastInters"/></description></item>
        /// <item><description><paramref name="lastInters"/> -> <paramref name="edge"/>.Second</description></item>
        /// </list>
        /// </summary>
        private ViewconeGraph BreakPerimeterEdgeIntoThree(Vector2 firstInters, Vector2 lastInters, 
            Edge<ViewNode, ViewMidEdgeInfo> edge, int edgeEnd1, int edgeEnd2, ViewconeGraph viewconeGraph, 
            StaticNavGraph staticNavGraph) {

            var vertices = viewconeGraph.vertices.Select(v => v.Position).ToList();
            var f = new ViewNode(firstInters, edge.EdgeInfo.IsEdgeOfRemoved);
            var s = new ViewNode(lastInters, edge.EdgeInfo.IsEdgeOfRemoved);
            bool firstActuallyFirst = (firstInters - vertices[edgeEnd1]).sqrMagnitude + (lastInters - vertices[edgeEnd2]).sqrMagnitude
                < (lastInters - vertices[edgeEnd1]).sqrMagnitude + (firstInters - vertices[edgeEnd2]).sqrMagnitude;
            if(!firstActuallyFirst) {
                var tmp = f;
                f = s;
                s = tmp;
            }
            var vs = viewconeGraph.vertices.ToList();
            var es = viewconeGraph.edges.ToList();
            var wasRemoved = edge.EdgeInfo.IsEdgeOfRemoved;
            var wasMidView = edge.EdgeInfo.IsInMidViewcone;
            var hasEdgeOfRemoved = Enumerable.Empty<int>();
            if(wasRemoved) {
                hasEdgeOfRemoved = hasEdgeOfRemoved.Append(edgeEnd1).Append(edgeEnd2 + 1);
            }
            AddVertexOnPerimeter(s, edgeEnd2 + 1, viewconeGraph.Viewcone, vs, es, staticNavGraph, hasEdgeOfRemoved);
            hasEdgeOfRemoved = Enumerable.Empty<int>().Append(edgeEnd1 + 1);
            if(wasRemoved) {
                hasEdgeOfRemoved = hasEdgeOfRemoved.Append(edgeEnd1).Append(edgeEnd2 + 2);
            }
            AddVertexOnPerimeter(f, edgeEnd2 + 1, viewconeGraph.Viewcone, vs, es, staticNavGraph, hasEdgeOfRemoved);

            return new ViewconeGraph(vs, es, viewconeGraph.Viewcone, false);
        }
        
        bool IsMiddleEdge(int index1, int index2, ViewconeGraph viewGraph) {
            ViewMidEdgeInfo? ei;
            if(viewGraph.AreEdgesComputed) {
                ei = viewGraph[index1, index2];
                if(ei == null) {
                    ei = viewGraph[index2, index1];
                }
            } else {
                var e = viewGraph.edges.FirstOrDefault(e =>
                    (e.First.Position == viewGraph.vertices[index1].Position
                    && e.Second.Position == viewGraph.vertices[index2].Position)
                    || (e.First.Position == viewGraph.vertices[index2].Position
                    && e.Second.Position == viewGraph.vertices[index1].Position));
                ei = e?.EdgeInfo;
            }
            if(ei == null) {
                var computedEdges = viewGraph.AreEdgesComputed ? "with" : "without";
                throw new NotSupportedException($"Edge between {index1} and {index2} was assumed. " +
                    $"Graph is {computedEdges} computed edges."); 
            }
            return ei.IsEdgeOfRemoved || ei.IsInMidViewcone;
        }

        /// <summary>
        /// Given a ViewconeGraph defined by <paramref name="node"/>, <paramref name="edges"/> and <paramref name="viewcone"/>,
        /// inserts the <paramref name="node"/> into the <paramref name="nodes"/> on the <paramref name="index"/>.
        /// Also add new edges where allowed by <paramref name="staticNavGraph"/>.
        /// </summary>
        /// <param name="haveEdgeOfRemoved">With nodes on these indices, the edges will be tagged as <c>edgeOfRemoved</c>.</param>
        void AddVertexOnPerimeter(ViewNode node, int index, Viewcone viewcone, List<ViewNode> nodes, 
            List<Edge<ViewNode, ViewMidEdgeInfo>> edges, StaticNavGraph staticNavGraph, IEnumerable<int> haveEdgeOfRemoved) {
            int previous = PreviousIndex(index, nodes);
            int next = index % nodes.Count; //index is inserted, so it can be nodes.count
            try {
                edges.RemoveAll(e =>
                    (e.First == nodes[previous] && e.Second == nodes[next]) ||
                    (e.First == nodes[next] && e.Second == nodes[previous]) ||
                        e.First == node ||
                        e.Second == node);
            }catch(Exception e) {
                throw new Exception($"Failed removing edges. Params: " +
                    $"{nameof(previous)}: {previous}; {nameof(next)}: {next}; {nameof(nodes)}.Count: {nodes.Count}.", e);
            }
            nodes.Insert(index, node);
            next = index < nodes.Count - 1 ? index + 1 : 0;
            for(int i = 0; i < nodes.Count; i++) {
                if(i == index)
                    continue;
                var v = nodes[i];
                var dist = Vector2.Distance(v.Position, node.Position);
                var canGetStraight = staticNavGraph.CanPlayerGetToStraight(v, node)
                    && !staticNavGraph.IsLineInsidePlayerObstacle(v.Position, node.Position);
                var canGet = viewcone.CanGoFromTo(v.Position, node.Position, dist, out var alertIncr)
                    && canGetStraight;
                var hasPrio = i == previous || i == next;
                var wasRemoved = haveEdgeOfRemoved.Contains(i);
                //if it is the other one we add edge no matter what
                if(canGet || hasPrio) {
                    var info = new ViewMidEdgeInfo(dist, wasRemoved, canGet, Math.Abs(index - i) > 1, alertIncr);
                    edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(node, v, info));
                }

                canGet = viewcone.CanGoFromTo(node.Position, v.Position, dist, out alertIncr)
                    && canGetStraight;
                //if it's the other one, we cannot add edge, cause it was already added by the other
                if(canGet || hasPrio){
                    var info = new ViewMidEdgeInfo(dist, wasRemoved, canGet, Math.Abs(index - i) > 1, alertIncr);
                    edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(v, node, info));
                }
            }
        }

        ViewconeGraph CutoffFromTo(ViewconeGraph initial, 
            Vector2 firstAdd, int preFirst, int first,
            Vector2 lastAdd, int postLast, int last,
            StaticNavGraph staticNavGraph) {

            var fMid = IsMiddleEdge(first, preFirst, initial);
            var sMid = IsMiddleEdge(postLast, last, initial);

            var firstView = new ViewNode(firstAdd, fMid);
            var lastView = new ViewNode(lastAdd, sMid);

            Edge<ViewNode, ViewMidEdgeInfo> GetOneDirectionEdge(ViewNode first, ViewNode second, bool traversable) {
                var dist = Vector2.Distance(first.Position, second.Position);
                var canGoForw = initial.Viewcone.CanGoFromTo(first.Position, second.Position, out float alertRatioForw);
                var e = new Edge<ViewNode, ViewMidEdgeInfo>(first, second, 
                    new ViewMidEdgeInfo(dist, true, traversable && canGoForw, false, alertRatioForw));
                return e;
            }

            IEnumerable<Edge<ViewNode, ViewMidEdgeInfo>> GetBothDirectionsEdges(ViewNode first, ViewNode second) {
                bool canGo = staticNavGraph.CanPlayerGetToStraight(first, second)
                    && staticNavGraph.IsLineInsidePlayerObstacle(first.Position, second.Position);
                yield return GetOneDirectionEdge(first, second, canGo);
                if(canGo)
                    yield return GetOneDirectionEdge(second, first, true);
            }

            List<ViewNode> vertices;
            if(first - 1 < last + 1) //when last == vertices count and postLast = 0, we need last + 1 
                vertices = initial.vertices
                    .Take(first)
                    .Append(firstView)
                    .Append(lastView)
                    .Concat(initial.vertices
                        .Skip(last + 1))
                    .ToList();
            else {
                vertices = Enumerable.Empty<ViewNode>()
                    .Append(lastView)
                    .Concat(
                        initial.vertices
                            .Skip(postLast)
                            .Take(preFirst + 1 - postLast))//prefirst + 1 instead of first, cause first can be 0
                    .Append(firstView)
                    .ToList();
            }

            var edges = initial.edges
                .Where(e => vertices.Contains(e.First) && vertices.Contains(e.Second))
                .Concat(GetBothDirectionsEdges(initial.vertices[preFirst], firstView))
                .Concat(GetBothDirectionsEdges(firstView, lastView))
                .Concat(GetBothDirectionsEdges(lastView, initial.vertices[postLast]))
                .ToList();

            return new ViewconeGraph(vertices, edges, initial.Viewcone, false);
        }
        
        //finds the firt intersection of perimeter and line
        //and returns the last edge that intersects
        private (Vector2 Intersection, int PerimeterIndex)? GetLastInFirstIntersection(
            Vector2 from, Vector2 to, IReadOnlyList<Vector2> perimeterNodes)
        {
            Vector2? inter = null;
            var previous = perimeterNodes[perimeterNodes.Count - 1];
            for(int i = 0; i < perimeterNodes.Count; i++)
            {
                var curr = perimeterNodes[i];
                var intersection = LineIntersectionDecider.FindFirstIntersection(
                    curr, previous, from, to, true, true);

                if(inter.HasValue && !intersection.HasValue) {
                    return (inter.Value, i - 1);
                }
                inter = intersection;
                previous = curr;
            }
            if(inter.HasValue) {
                return (inter.Value, perimeterNodes.Count - 1);
            }
            return null;
        }
        
        //finds the firt intersection of perimeter and line
        //and returns the first edge that intersects
        private (Vector2 Intersection, int PerimeterIndex)? GetFirstInFirstIntersection(
            Vector2 from, Vector2 to, IReadOnlyList<Vector2> perimeterNodes)
        {
            var previous = perimeterNodes[perimeterNodes.Count - 1];
            for(int i = 0; i < perimeterNodes.Count; i++)
            {
                var curr = perimeterNodes[i];
                var intersection = LineIntersectionDecider.FindFirstIntersection(
                    previous, curr, from, to, true, true);
                if(intersection.HasValue) {
                    //if i is bigger then 0, we found the first intersecting edge
                    if(i > 0)
                        return (intersection.Value, i);
                    //otherwise we have to go backwards and find the first edge that intersects
                    i = perimeterNodes.Count - 1;
					Vector2? inter;
					do {
                        inter = intersection;
                        curr = perimeterNodes[i];
                        intersection = LineIntersectionDecider.FindFirstIntersection(
                            curr, previous, from, to, true, true);
                        i--;
                    }while(intersection.HasValue);
                    i++;
                    if(i == perimeterNodes.Count - 1)
                        i = 0;
                    return (inter.Value, i);
                }
                previous = curr;
            }
            return null;
        }
    }
}
