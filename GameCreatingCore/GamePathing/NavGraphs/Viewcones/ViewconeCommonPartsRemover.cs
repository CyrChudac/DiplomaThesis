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
                if(Obstacle.IsInPolygon(p, shape, true))
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
                if(!Obstacle.IsInPolygon(vertices[i].Position, secondVer, true)) {
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
                    while (Obstacle.IsInPolygon(vertices[preFirst].Position, secondVer, true))
                    {
                        preFirst--;
                        if (preFirst < 0)
                        { //only happens if the whole first viewcone is within the other one
                            return new ViewconeGraph(new List<ViewNode>(),
                                new List<Edge<ViewNode, ViewMidEdgeInfo>>(), result.Viewcone, result.Index);
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
                } while (Obstacle.IsInPolygon(vertices[i].Position, secondVer, true));
                var last = i - 1;
                var postLast = i % vertices.Count;
                //now we know 4 points in the first viewcone:
                // the first one in and the one before it
                // the last one in and the one after it
                //we need to find the intersections of these lines with the second viewcone
                if(last == preFirst && first == postLast)
                    throw new NotSupportedException($"{nameof(CutOffCommonParts)}: The perimeter of " +
                        $"{nameof(secondIn)} starts inside the perimeter of {nameof(firstIn)}, but does not end there.");

				var firstInters = GetFirstIntersection(vertices[preFirst].Position, 
                    vertices[first].Position, secondVer);
                if(! firstInters.HasValue)
                    throw new NotSupportedException($"{nameof(CutOffCommonParts)}: There is no interseciton of " +
                        $"the perimeter of {nameof(secondIn)} and the FIRST 2 points" +
                        $" of {nameof(firstIn)}.");
                
                var lastInters = GetFirstIntersection(vertices[postLast].Position, 
                    vertices[last].Position, secondVer);
                if(! lastInters.HasValue)
                    throw new NotSupportedException($"{nameof(CutOffCommonParts)}: There is no interseciton of " +
                        $"the perimeter of {nameof(secondIn)} and the LAST 2 points" +
                        $" of {nameof(firstIn)}.");

                result = CutoffFromTo(result,
                    firstInters.Value.Intersection, preFirst, first,
                    lastInters.Value.Intersection, postLast, last,
                    staticNavGraph);
                vertices = new List<ViewNode>(result.vertices);
                
                //we removed some nodes, so we have to adjust i accordingly
                if(backRemovedNow) {
                    i -= last;
                } else {
                    i -= last - 1 - first;
                }

                if(secondOut.vertices.Any(x => (x.Position - firstInters.Value.Intersection).sqrMagnitude < 0.005f)
                    && secondOut.vertices.Any(x => (x.Position - lastInters.Value.Intersection).sqrMagnitude < 0.005f)) { 
                    continue;
                    }
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
                    secondOut = CutoffSecondary(secondOut, firstInters.Value.Intersection, lastInters.Value.Intersection, result,
                        firstInters.Value.PerimeterIndex,  PreviousIndex(firstInters.Value.PerimeterIndex, secondVer),
                        lastInters.Value.PerimeterIndex,  PreviousIndex(lastInters.Value.PerimeterIndex, secondVer),
                        staticNavGraph);
                }
                secondVer = secondOut.vertices.Select(v => v.Position).ToList();
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


        private ViewconeGraph CutoffSecondary(ViewconeGraph initial, Vector2 e1Intersection, Vector2 e2Intersection, ViewconeGraph primary,
            int edge1First, int edge1Second, int edge2First, int edge2Second, StaticNavGraph staticNavGraph) {
            var verts = primary.vertices.Select(v => v.Position).ToList();
            (int preFirst, int first, int last, int postLast)
                = GetOrderForWholeInside(edge1First, edge1Second, edge2First, edge2Second, initial.vertices, 
                verts, e1Intersection, e2Intersection);
            //edges might got swapped, so we need to swap intersections in that case as well
            var sqrDistsNow = (e1Intersection - initial.vertices[preFirst].Position).sqrMagnitude
                + (e2Intersection - initial.vertices[postLast].Position).sqrMagnitude;
            var sqrDistsPotential = (e2Intersection - initial.vertices[preFirst].Position).sqrMagnitude
                + (e1Intersection - initial.vertices[postLast].Position).sqrMagnitude;
            if(sqrDistsNow > sqrDistsPotential) {
                var tmp = e1Intersection;
                e1Intersection = e2Intersection;
                e2Intersection = tmp;
            }

            bool wholeInPoly = true;
            int firstOut = preFirst;

            for(int i = first; true; i++) {
                i %= initial.vertices.Count;
                if(!Obstacle.IsInPolygon(initial.vertices[i].Position, verts, true)) {
                    wholeInPoly = false;
                    firstOut = i;
                    break;
                }
                if(i == last)
                    break;
            }
            if(wholeInPoly) { //the whole part to be cutoof is inside the primary polygon
                return CutoffFromTo(initial, e1Intersection, preFirst, first, e2Intersection, postLast, last, staticNavGraph);
            } else {

                int preFirstOut = PreviousIndex(firstOut, initial.vertices);
                var inters = GetFirstIntersection(initial.vertices[firstOut].Position, 
                    initial.vertices[preFirstOut].Position, verts);
                if(!inters.HasValue) {
                    throw new NotSupportedException("Even though second perimeter is cut in half by first, it doesn't have " +
                        $"the {nameof(preFirstOut)} intersection with it.");
                }
                initial = SecondaryCutInHalf(initial, primary, staticNavGraph, e1Intersection, preFirst, first,
                    inters.Value.Intersection, preFirstOut, firstOut);
                int change;
                if(first > firstOut) {
                    change = firstOut - 1;
                } else {
                    change = firstOut - first - 2;
                }
                if(firstOut < last) {
                    last -= change;
                    postLast = (last + 1) % initial.vertices.Count;
                }
                firstOut -= change;
                var firstOut2 = firstOut;
                for(int i = last; i != firstOut; i = PreviousIndex(i, initial.vertices)) {
                    if(!Obstacle.IsInPolygon(initial.vertices[i].Position, verts, true)) {
                        firstOut2 = i;
                        break;
                    }
                }
                var preFirstOut2 = (firstOut2 + 1) % initial.vertices.Count;
                inters = GetFirstIntersection(initial.vertices[firstOut2].Position, 
                    initial.vertices[preFirstOut2].Position, verts);
                if(!inters.HasValue) {
                    throw new NotSupportedException("Even though second perimeter is cut in half by first, it doesn't have " +
                        $"the {nameof(firstOut2)} intersection with it.");
                }
                initial = SecondaryCutInHalf(initial, primary, staticNavGraph,
                    inters.Value.Intersection, firstOut2, preFirstOut2,
                    e2Intersection, last, postLast);
                return initial;
            }
        }

        ViewconeGraph SecondaryCutInHalf(ViewconeGraph initial, ViewconeGraph primary, StaticNavGraph staticNavGraph,
            Vector2 e1Intersection, int e1Start, int e1End, Vector2 e2Intersection, int e2Start, int e2End) {
            
            Func<Vector2, Vector2, (bool, float)> canGoFunc = (v1, v2) => {
                var poss = initial.Viewcone.CanGoFromTo(v1, v2, out float alert);
                if(!poss)
                    return (false, alert);
                poss = primary.Viewcone.CanGoFromTo(v1, v2, out float alert2);
                return (poss, Math.Max(alert, alert2));
            };
            if(e1End == e2End) {
                var vertexList = initial.vertices.Select(x => x.Position).ToList();
                var e = GetEdgeWithRightEnds(initial.edges.Where(e => !e.EdgeInfo.IsInMidViewcone), vertexList, e1End);
                if(e == null) {
                    var vertices = string.Join(", ", vertexList);
                    throw new NotSupportedException($"Perimeter edge does not exist. Vertices: {e1End}, " +
                        $"{PreviousIndex(e1End, vertexList)} ; Perimeter: {vertices}.");
                }
                initial = BreakPerimeterEdgeIntoThree(e1Intersection, e2Intersection, e,
                    e1Start, e1End, initial, staticNavGraph);
            } else {
                initial = CutoffFromTo(initial, e1Intersection, e1Start, e1End, 
                    e2Intersection, e2End, e2Start, staticNavGraph, canGoFunc);
            }
            return initial;
        }

        /// <summary>
        /// Given indices of ends of 2 edges on a perimeter, finds out which part of the perimeter is inside the against obstacle.
        /// If none part is whole inside, return
        /// </summary>
        (int PreFirst, int First, int Last, int PostLast) GetOrderForWholeInside<N>(int edge1First, int edge1Second, 
            int edge2First, int edge2Second, 
            IReadOnlyList<N> onPerimeter,
            IReadOnlyList<Vector2> against,
            Vector2 edge1Intersection, Vector2 edge2Intersection) where N : Node{

            List<int> ordered = new List<int>() { 
                edge1First, edge1Second, edge2First, edge2Second
            };
            ordered.Sort();

            float offsetChange = 0.01f;

            if(ordered.Contains(0) && ordered.Contains(onPerimeter.Count - 1) 
                && ((!ordered.Contains(1)) || !ordered.Contains(onPerimeter.Count - 2))) 
                //that would be pairs 0,1...N-1,N, we are looking for a pair 0,N
            {
                if((onPerimeter[0].Position - edge1Intersection).sqrMagnitude +
                 (onPerimeter[ordered[3]].Position - edge1Intersection).sqrMagnitude < 
                 (onPerimeter[0].Position - edge2Intersection).sqrMagnitude +
                 (onPerimeter[ordered[3]].Position - edge2Intersection).sqrMagnitude) {
                    var tmp = edge1Intersection; edge1Intersection = edge2Intersection; edge2Intersection = tmp;
                }
                var offset1 = onPerimeter[ordered[1]].Position - onPerimeter[ordered[2]].Position;
                offset1 = offset1.normalized;
                var offset2 = onPerimeter[0].Position - onPerimeter[ordered[3]].Position;
                offset2 = offset2.normalized;

                bool isPoly = 
                    Obstacle.IsInPolygon(edge1Intersection + offset1 * offsetChange, against, true)
                    && Obstacle.IsInPolygon(edge2Intersection + offset2 * offsetChange, against, true);

                if(isPoly) {
                    return (ordered[3], ordered[0], ordered[1], ordered[2]);
                } else {
                    return (ordered[1], ordered[2], ordered[3], ordered[0]);
                }

            } else { //then we need to get 2 consectuve pairs

                if((onPerimeter[0].Position - edge1Intersection).sqrMagnitude +
                 (onPerimeter[ordered[1]].Position - edge1Intersection).sqrMagnitude > 
                 (onPerimeter[0].Position - edge2Intersection).sqrMagnitude +
                 (onPerimeter[ordered[1]].Position - edge2Intersection).sqrMagnitude) {
                    var tmp = edge1Intersection; edge1Intersection = edge2Intersection; edge2Intersection = tmp;
                }
                var offset1 = onPerimeter[ordered[0]].Position - onPerimeter[ordered[1]].Position;
                offset1 = offset1.normalized;
                var offset2 = onPerimeter[ordered[3]].Position - onPerimeter[ordered[2]].Position;
                offset2 = offset2.normalized;

                
                bool isPoly = 
                    Obstacle.IsInPolygon(edge1Intersection + offset1 * offsetChange, against, true)
                    && Obstacle.IsInPolygon(edge2Intersection + offset2 * offsetChange, against, true);
                if(isPoly) {
                    return (ordered[2], ordered[3], ordered[0], ordered[1]);
                } else {
                    return (ordered[0], ordered[1], ordered[2], ordered[3]);
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
            var f = new ViewNode(firstInters, viewconeGraph, edge.EdgeInfo.IsEdgeOfRemoved);
            var s = new ViewNode(lastInters, viewconeGraph, edge.EdgeInfo.IsEdgeOfRemoved);
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
            var hasEdgeOfRemoved = Enumerable.Empty<int>();
            if(!(FloatEquality.AreEqual(vs[edgeEnd2].Position, s.Position)
                || FloatEquality.AreEqual(vs[edgeEnd1].Position, s.Position))) { 
                if(wasRemoved) {
                    hasEdgeOfRemoved = hasEdgeOfRemoved.Append(edgeEnd1).Append(edgeEnd2 + 1);
                }
                AddVertexOnPerimeter(s, edgeEnd2, viewconeGraph.Viewcone, vs, es, staticNavGraph, hasEdgeOfRemoved);
            }
            if(!(FloatEquality.AreEqual(vs[(edgeEnd2 + 1) % vs.Count].Position, f.Position)
                || FloatEquality.AreEqual(vs[edgeEnd2].Position, f.Position)
                || FloatEquality.AreEqual(vs[edgeEnd1].Position, f.Position))) {
                hasEdgeOfRemoved = Enumerable.Empty<int>().Append(edgeEnd1 + 1);
                if(wasRemoved) {
                    hasEdgeOfRemoved = hasEdgeOfRemoved.Append(edgeEnd1).Append(edgeEnd2 + 2);
                }
                AddVertexOnPerimeter(f, edgeEnd1 + 1, viewconeGraph.Viewcone, vs, es, staticNavGraph, hasEdgeOfRemoved);
            }

            return new ViewconeGraph(vs, es, viewconeGraph.Viewcone, viewconeGraph.Index, false);
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
            if(FloatEquality.AreEqual(node.Position, nodes[previous].Position)
                || FloatEquality.AreEqual(node.Position, nodes[next].Position))
                return;
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
                    var info = new ViewMidEdgeInfo(dist, wasRemoved, canGet, Math.Abs(index - i) > 1, hasPrio ? 0 : alertIncr);
                    edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(node, v, info));
                }

                canGet = viewcone.CanGoFromTo(node.Position, v.Position, dist, out alertIncr)
                    && canGetStraight;
                //if it's the other one, we cannot add edge, cause it was already added by the other
                if(canGet || hasPrio){
                    var info = new ViewMidEdgeInfo(dist, wasRemoved, canGet, Math.Abs(index - i) > 1, hasPrio ? 0 : alertIncr);
                    edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(v, node, info));
                }
            }
        }

        ViewconeGraph CutoffFromTo(ViewconeGraph initial, 
            Vector2 firstAdd, int preFirst, int first,
            Vector2 lastAdd, int postLast, int last,
            StaticNavGraph staticNavGraph,
            Func<Vector2, Vector2, (bool IsPossible, float ResultingAlert)>? canGo = null) {
            
            if(last == first
                && FloatEquality.AreEqual(initial.vertices[last].Position, lastAdd)
                && FloatEquality.AreEqual(firstAdd, lastAdd)) {
                //this viewcone touches the other only only with one single point,
                //so we don't need to do anything
                return initial;
            }

            Func<Vector2, Vector2, (bool IsPossible, float ResultingAlert)> primaryCanGoFunc = (v1, v2) => {
                var poss = initial.Viewcone.CanGoFromTo(v1, v2, out float aler);
                return (poss, aler);
            };
            canGo ??= primaryCanGoFunc;

            var fMid = IsMiddleEdge(first, preFirst, initial);
            var sMid = IsMiddleEdge(postLast, last, initial);

            ViewNode firstView  = new ViewNode(firstAdd, initial, fMid);
            var lastView = new ViewNode(lastAdd, initial, sMid);
            

            Edge<ViewNode, ViewMidEdgeInfo> GetOneDirectionEdge(ViewNode first, ViewNode second, bool traversable, bool edgeOfRemoved,
                Func<Vector2, Vector2, (bool IsPossible, float ResultingAlert)> canGoFunc) {
                var dist = Vector2.Distance(first.Position, second.Position);
                var can = canGoFunc(first.Position, second.Position);
                var e = new Edge<ViewNode, ViewMidEdgeInfo>(first, second, 
                    new ViewMidEdgeInfo(dist, true, traversable && can.IsPossible, false, can.ResultingAlert));
                return e;
            }

            IEnumerable<Edge<ViewNode, ViewMidEdgeInfo>> GetBothDirectionsEdges(ViewNode first, ViewNode second, bool edgeOfRemoved,
                Func<Vector2, Vector2, (bool IsPossible, float ResultingAlert)> canGoFunc) {
                bool canGo = staticNavGraph.CanPlayerGetToStraight(first, second)
                    && staticNavGraph.IsLineInsidePlayerObstacle(first.Position, second.Position);
                yield return GetOneDirectionEdge(first, second, canGo, edgeOfRemoved, canGoFunc);
                if(canGo)
                    yield return GetOneDirectionEdge(second, first, true, edgeOfRemoved, canGoFunc);
            }

            bool notFirstSameAsFirst = !FloatEquality.AreEqual(initial.vertices[preFirst].Position, firstAdd);
            bool notSecondSameAsLast = !FloatEquality.AreEqual(initial.vertices[postLast].Position, lastAdd);

            IEnumerable<ViewNode> tmpVertices;
            if(first - 1 < last + 1) {  //when last == vertices count and postLast = 0, we need last + 1 
                tmpVertices = initial.vertices.Take(first);
                if(notFirstSameAsFirst)
                    tmpVertices = tmpVertices.Append(firstView);
                if(notSecondSameAsLast)
                    tmpVertices = tmpVertices.Append(lastView);
                tmpVertices = tmpVertices
                    .Concat(initial.vertices
                        .Skip(last + 1));
            } else {
                tmpVertices = Enumerable.Empty<ViewNode>();
                if(notSecondSameAsLast)
                    tmpVertices = tmpVertices.Append(lastView);
                tmpVertices = tmpVertices
                    .Concat(
                        initial.vertices
                            .Skip(postLast)
                            .Take(preFirst + 1 - postLast));//prefirst + 1 instead of first, cause first can be 0
                if(notFirstSameAsFirst)
                    tmpVertices = tmpVertices.Append(firstView);
            }
            List<ViewNode> vertices = tmpVertices.ToList();

            var edges = initial.edges
                .Where(e => vertices.Contains(e.First) && vertices.Contains(e.Second))
                .ToList();
            if(notFirstSameAsFirst)
                edges.AddRange(GetBothDirectionsEdges(initial.vertices[preFirst], firstView, false, (_, __) => (true, 0)));
            if(!FloatEquality.AreEqual(firstAdd, lastAdd))
                edges.AddRange(GetBothDirectionsEdges(firstView, lastView, true, canGo));
            if(notSecondSameAsLast)
                edges.AddRange(GetBothDirectionsEdges(lastView, initial.vertices[postLast], false, (_, __) => (true, 0)));

            return new ViewconeGraph(vertices, edges, initial.Viewcone, initial.Index, false);
        }
        

        //finds the firt intersection of perimeter and line
        //and returns the first edge that intersects
        private (Vector2 Intersection, int PerimeterIndex)? GetFirstIntersection(
            Vector2 from, Vector2 to, IReadOnlyList<Vector2> perimeterNodes)
        {
            Vector2 finalIntersection = Vector2.zero;
            float sqrDist = float.PositiveInfinity;
            int? index = null;
            var previous = perimeterNodes[perimeterNodes.Count - 1];
            for(int i = 0; i < perimeterNodes.Count; i++)
            {
                var curr = perimeterNodes[i];
                var intersection = LineIntersectionDecider.FindFirstIntersection(
                    previous, curr, from, to, true, true);
                if(intersection.HasValue) {
                    var currDist = (from - intersection.Value).sqrMagnitude;
                    if(currDist < sqrDist) {
                        sqrDist = currDist;
                        index = i;
                        finalIntersection = intersection.Value;
                    }
                }
                previous = curr;
            }
            if(!index.HasValue)
                return null;
            return (finalIntersection, index.Value);
        }
    }
}
