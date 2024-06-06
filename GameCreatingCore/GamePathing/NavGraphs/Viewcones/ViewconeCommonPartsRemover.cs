using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
        /// <param name="secondOut">The function can also change the shape of the 
        /// <paramref name="secondIn"/> paramter. This change will be outputed here.</param>
        private ViewconeGraph CutOffCommonParts(ViewconeGraph firstIn, ViewconeGraph secondIn,
            out ViewconeGraph secondOut)
        {


            ViewconeGraph result = firstIn;
            secondOut = secondIn;

            var vertices = new List<ViewNode>(result.vertices);
            var secondVer = secondOut.vertices.Select(v => v.Position).ToList();

            for (int i = 0; i < vertices.Count; i++)
            {
                if (Obstacle.IsInPolygon(vertices[i].Position, secondVer))
                {
                    var first = i;
                    int preFirst = first - 1;
                    if (i == 0)
                    {
                        preFirst = vertices.Count - 1;
                        while (Obstacle.IsInPolygon(vertices[preFirst].Position, secondVer))
                        {
                            preFirst--;
                            if (preFirst < 0)
                            { //only happens if the whole first viewcone is within the other one
                                secondOut = null;
                                return new ViewconeGraph(new List<ViewNode>(),
                                    new List<Edge<ViewNode, ViewMidEdgeInfo>>(), result.Viewcone);
                            }
                        }
                        first = (preFirst + 1) % vertices.Count;
                    }

                    do
                    {
                        i++;
                        if (i == vertices.Count)
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

                    var secondPerimeter = secondOut.edges.Where(e => !e.EdgeInfo.IsInMidViewcone).ToList();

                    var firstInters = GetFirstIntersection(vertices[preFirst].Position, 
                        vertices[first].Position, secondPerimeter);
                    var lastInters = GetFirstIntersection(vertices[last].Position, 
                        vertices[postLast].Position, secondPerimeter);
                    
                    if(! firstInters.HasValue)
                        throw new NotSupportedException($"{nameof(CutOffCommonParts)}: There is no interseciton of " +
                            $"the perimeter of {nameof(secondIn)} and the 2 first points" +
                            $" of {nameof(firstIn)}.");
                    if(! lastInters.HasValue)
                        throw new NotSupportedException($"{nameof(CutOffCommonParts)}: There is no interseciton of " +
                            $"the perimeter of {nameof(secondIn)} and the 2 last points" +
                            $" of {nameof(firstIn)}.");

                    bool isIntersectionTraversable = true;

                    result = CutoffFromTo(result,
                        firstInters.Value.Intersection, preFirst,
                        lastInters.Value.Intersection, postLast);

                    if(!(isIntersectionTraversable && firstInters.Value.PerimeterIndex == lastInters.Value.PerimeterIndex))
                    { //we need to change the second in => either there is a nontraversable part
                      //that previously was traversable or some part has to be cutoff

                        if(firstInters.Value.PerimeterIndex == lastInters.Value.PerimeterIndex) {
                            //the only problem is, that a part of the perimeter is not traversable
                            var one = secondVer.IndexOf(secondOut.edges[firstInters.Value.PerimeterIndex].First.Position);
                            var two = secondVer.IndexOf(secondOut.edges[firstInters.Value.PerimeterIndex].Second.Position);
                            if(two > one) {
                                var tmp = two;
                                two = one;
                                one = tmp;
                            }
                            var f = new ViewNode(firstInters.Value.Intersection,
                                secondOut.edges[firstInters.Value.PerimeterIndex].EdgeInfo.IsInMidViewcone);
                            var s = new ViewNode(lastInters.Value.Intersection,
                                secondOut.edges[firstInters.Value.PerimeterIndex].EdgeInfo.IsInMidViewcone);
                            bool firstActuallyFirst = (firstInters.Value.Intersection - secondVer[one]).sqrMagnitude
                                < (lastInters.Value.Intersection - secondVer[one]).sqrMagnitude;
                            if(!firstActuallyFirst) {
                                var tmp = f;
                                f = s;
                                s = tmp;
                            }
                            var vs = secondOut.vertices
                                .Take(one + 1)
                                .Append(f)
                                .Append(s)
                                .Concat(secondOut.vertices.Skip(two))
                                .ToList();
                            var es = secondOut.edges
                                .Where(e =>
                                    e.First.Position != firstInters.Value.Intersection &&
                                    e.First.Position != lastInters.Value.Intersection &&
                                    e.Second.Position != firstInters.Value.Intersection &&
                                    e.Second.Position != lastInters.Value.Intersection)
                                .ToList();
                            var wasRemoved = secondOut.edges[firstInters.Value.PerimeterIndex].EdgeInfo.IsEdgeOfRemoved;
                            var wasMidView = secondOut.edges[firstInters.Value.PerimeterIndex].EdgeInfo.IsInMidViewcone;
                            void AddEdges(ViewNode node1, ViewNode node2) {
                                var dist = Vector2.Distance(node1.Position, node2.Position);
                                float alertingAdditive;
                                es.Add(new Edge<ViewNode, ViewMidEdgeInfo>(node1, node2,
                                    new ViewMidEdgeInfo(dist,
                                        wasRemoved,
                                        secondIn.Viewcone.CanGoFromTo(node1.Position, node2.Position, out alertingAdditive),
                                        wasMidView,
                                        alertingAdditive)));
                                es.Add(new Edge<ViewNode, ViewMidEdgeInfo>(node2, node1,
                                    new ViewMidEdgeInfo(dist,
                                        wasRemoved,
                                        secondIn.Viewcone.CanGoFromTo(node2.Position, node1.Position, out alertingAdditive),
                                        wasMidView,
                                        alertingAdditive)));

                            }
                            AddEdges(secondOut.edges[firstInters.Value.PerimeterIndex].First, f);
                            AddEdges(f, s);
                            AddEdges(s, secondOut.edges[firstInters.Value.PerimeterIndex].Second);
                        } else {

                        }
                    }
                    vertices = new List<ViewNode>(result.vertices);
                }
            }
            return result;
        }
        
        bool IsMiddleEdge(int index1, int index2, ViewconeGraph viewGraph) {
            var ei = viewGraph[index1, index2];
            if(ei == null)
                throw new NotSupportedException($"Edge between {index1} and {index2} was assumed.");
            return ei.IsInMidViewcone;
        }

        ViewconeGraph CutoffFromTo(ViewconeGraph initial, 
            Vector2 firstAdd, int firstIndex,
            Vector2 lastAdd, int lastIndex) {

            var fMid = IsMiddleEdge(firstIndex, firstIndex - 1, initial);
            var sMid = IsMiddleEdge(lastIndex, lastIndex + 1, initial);

            var first = new ViewNode(firstAdd, fMid);
            var last = new ViewNode(lastAdd, sMid);

            var dist = Vector2.Distance(firstAdd, lastAdd);
            var canGoForw = initial.Viewcone.CanGoFromTo(firstAdd, lastAdd, out float alertRatioForw);
            var canGoBack = initial.Viewcone.CanGoFromTo(lastAdd, firstAdd, out float alertRatioBackw);

            var e1 = new Edge<ViewNode, ViewMidEdgeInfo>(first, last, 
                new ViewMidEdgeInfo(dist, true, canGoForw, false, alertRatioForw));
            var e2 = new Edge<ViewNode, ViewMidEdgeInfo>(last, first, new 
                ViewMidEdgeInfo(dist, true, canGoBack, false, alertRatioBackw));

            List<ViewNode> vertices;
            if(firstIndex < lastIndex)
                vertices = initial.vertices
                    .Take(firstIndex)
                    .Append(first)
                    .Append(last)
                    .Concat(initial.vertices
                        .Skip(lastIndex))
                    .ToList();
            else {
                vertices = Enumerable.Repeat(last,1)
                    .Concat(
                        initial.vertices
                            .Skip(lastIndex)
                            .Take(firstIndex - lastIndex))
                    .Append(first)
                    .ToList();
            }

            var edges = initial.edges
                .Where(e => vertices.Contains(e.First) && vertices.Contains(e.Second))
                .Append(e1)
                .Append(e2)
                .ToList();

            return new ViewconeGraph(vertices, edges, initial.Viewcone);
        }

        private (Vector2 Intersection, int PerimeterIndex)? GetFirstIntersection<N, E>(
            Vector2 from, Vector2 to, List<Edge<N, E>> perimeterEdges)
            where N : Node where E : ViewMidEdgeInfo
        {
            for(int i = 0; i < perimeterEdges.Count; i++)
            {
                var e = perimeterEdges[i];
                var inter = LineIntersectionDecider.FindFirstIntersection(
                    from, to, e.First.Position, e.Second.Position);

                if (inter.HasValue)
                {
                    return (inter.Value, i);
                }
            }
            return null;
        }
    }
}
