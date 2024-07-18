using GameCreatingCore.GameActions;
using GameCreatingCore.LevelRepresentationData;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCreatingCore.GamePathing;
using GameCreatingCore.LevelStateData;

namespace GameCreatingCore.LevelSolving.Viewcones
{

    public class ViewconeNavGraph : ViewconesCreator 
    {
        public readonly StaticNavGraph staticNavGraph;

        private readonly float viewconeTravelConsideredStep;
        private readonly StaticGameRepresentation staticgameRepr;
        private readonly int skillUsePointsAround;


        private Dictionary<EnemyState, ViewconeGraph> _enemyViewconeDict
            = new Dictionary<EnemyState, ViewconeGraph>();

        /// <param name="innerRays">Determines how many times are the viewcones fragmented.</param>
        /// <param name="viewconeTravelConsideredStep">When creating the inner graph within the viewcone,
        /// how far are the respective points from one another.</param>
        /// <param name="viewconeLengthMod">The  viewcone is actually not a cone, it is section of triangles. 
        /// What is their length? 0 = viewcone length; 1 = minimal length within the triangle.</param>
        public ViewconeNavGraph(LevelRepresentation level, StaticNavGraph staticNavGraph,
            StaticGameRepresentation staticGameRepr, int innerRays, float viewconeTravelConsideredStep,
            int skillUsePointsAround, float viewconeLengthMod = 0.5f)
            :base(level, staticGameRepr, innerRays, viewconeLengthMod)
        {

            this.skillUsePointsAround = skillUsePointsAround;
            this.viewconeTravelConsideredStep = viewconeTravelConsideredStep;
            this.staticNavGraph = staticNavGraph;
            staticgameRepr = staticGameRepr;
        }


        Dictionary<int, GraphWithViewcones> fullGraphsDict
            = new Dictionary<int, GraphWithViewcones>();

        public int GraphsComputed { get; private set; } = 0;
        public int GraphsReturned { get; private set; } = 0;

        public GraphWithViewcones GetScoredNavGraph(LevelState state)
        {
            var enemyListHash = HashedReadOnlyList<object>.GetListHashCode(
                state.enemyStates.Cast<object>().Concat(state.pickupableSkillsPicked.Cast<object>()).ToList());
            GraphWithViewcones views;
            if(!fullGraphsDict.TryGetValue(enemyListHash, out views)) {
                var viewcones = GetTraversableViewcones(state);
                viewcones = CutOffCommonParts(viewcones);
                var combiner = new GraphPartsCombiner(staticNavGraph);
                var pickables = GetPickupables(state);
                var usables = GetAvailableSkills(state);
                var data = combiner.Combine(viewcones, pickables, usables);
                views = new GraphScorer(staticNavGraph).ScoreAndCombine(data);
                GraphsComputed++;
                fullGraphsDict.Add(enemyListHash, views);
            }
            GraphsReturned++;
            return views;
        }


        public IEnumerable<Graph<Node>> GetViewConeGraphs(LevelState state, bool computeEdges, 
            bool cutoffCommon, bool removeNoTraversables) 
        {
            var res = GetTraversableViewcones(state);
            if(cutoffCommon) {
                res = CutOffCommonParts(res);
            }
            return res.Select(v => new Graph<Node>(v.vertices.Cast<Node>().ToList(),
                    v.edges
                        .Where(e => e.EdgeInfo.Traversable || (!removeNoTraversables))
                        .Select(e => new Edge<Node>(e.First, e.Second, e.EdgeInfo))
                        .ToList(),
                    computeEdges));
        }

        /// <summary>
        /// Gets the list of viewcones in the given <paramref name="state"/>.
        /// </summary>
        private List<ViewconeGraph> GetTraversableViewcones(LevelState state)
        {
            List<ViewconeGraph> viewcones = new List<ViewconeGraph>();
            for (int i = 0; i < state.enemyStates.Count; i++)
            {
                var es = state.enemyStates[i];
                if (!es.Alive)
                    continue;
                ViewconeGraph view;
                if (!_enemyViewconeDict.TryGetValue(es, out view))
                {
                    var length = GetViewconeLength(i, state);
                    var preview = GetViewcone(state, i);
                    view = AddTraversePoints(preview, lengthsDict[level.Enemies[i].Type].AlertingDistance, length, i);
                    _enemyViewconeDict.Add(es, view);
                }
                viewcones.Add(view);
            }
            return viewcones;
        }

        /// <summary>
        /// Given already existing <paramref name="viewcones"/> as graphs, cuts off the overlaping parts.
        /// </summary>
        private List<ViewconeGraph> CutOffCommonParts(List<ViewconeGraph> viewcones)
        {
            List<Rect> bbs = viewcones
                .Select(v => v.Viewcone)
                .Select(v => Obstacle.GetBoundingBox(v.EndingPoints.Append(v.StartPos)))
                .ToList();
            List<(int, int)> toConsider = new List<(int, int)>();
            for (int i = 0; i < bbs.Count; i++)
            {
                for (int j = i + 1; j < bbs.Count; j++)
                {
                    if (LineIntersectionDecider.GetForOverlaping(bbs[i].x, bbs[i].xMax, bbs[j].x, bbs[j].xMax,
                        true, true, true, false))
                        if (LineIntersectionDecider.GetForOverlaping(bbs[i].y, bbs[i].yMax, bbs[j].y, bbs[j].yMax,
                            true, true, true, false))
                            toConsider.Add((i, j));
                }
            }
            List<ViewconeGraph> result = new List<ViewconeGraph>(viewcones);
            var rem = new ViewconeCommonPartsRemover();
            foreach (var (i, j) in toConsider)
            {
                result[i] = rem.CutOffCommonParts(result[i], result[j], staticNavGraph, out var tmp);
                result[j] = tmp;
            }
            return result;
        }

        private List<(IGameAction, Vector2)> GetPickupables(LevelState state) {
            List<(IGameAction, Vector2)> pickupNodes = new List<(IGameAction, Vector2)>();
            for(int i = 0; i < level.SkillsToPickup.Count; i++) {
                if(state.pickupableSkillsPicked[i])
                    continue;
                pickupNodes.Add((new PickupAction(i, level, 5, 1), level.SkillsToPickup[i].position));
            }
            return pickupNodes;
        }
        
        private bool CanDoActionReach(GameActionReachType reachType, Vector2 from, Vector2 to) {
            switch(reachType) {
                case GameActionReachType.Instant:
                case GameActionReachType.RangedCurved:
                    return true;
                case GameActionReachType.RangedStraight:
                    return staticNavGraph.CanPlayerGetToStraight(from, to);
                default:
                    throw new NotImplementedException($"Behaviour for" +
                        $" {nameof(GameActionReachType)} with value {reachType} is not defined.");
            }
        }

        private List<(IGameAction, Vector2)> GetAvailableSkills(LevelState state) {
            List<(IGameAction, Vector2)> points = new List<(IGameAction, Vector2)>();

            foreach(var s in state.playerState.AvailableSkills) {
                if(s.CanTargetPlayer) {
                    points.Add((s.Get(null, new GameActionTarget(null, null, true)), state.playerState.Position));
                }
                if(s.CanTargetGround) {
                    var pts = PointsAround(state.playerState.Position, 1 + s.MaxUseDistance, skillUsePointsAround);
                    foreach(var p in pts) {
                        if(s.CanTargetGroundObstacle || !staticNavGraph.IsInPlayerObstacle(p, false)) {
                            if(CanDoActionReach(s.ReachType, state.playerState.Position, p)) {
                                var pos = state.playerState.Position
                                    + (p - state.playerState.Position)/s.MaxUseDistance;
                                points.Add((s.Get(null, new GameActionTarget(p, null, false)), pos));
                            }
                        }
                    }
                }
                if(s.CanTargetEnemy) {
                    for(int i = 0; i < state.enemyStates.Count; i++) {
                        var e = state.enemyStates[i];
                        if(!e.Alive)
                            continue;
                        var pts = PointsAround(e.Position, 0.05f + s.MaxUseDistance, skillUsePointsAround, (e.Rotation + 180) % 360);
                        foreach(var p in pts) {
                            if(staticNavGraph.CanPlayerGetToStraight(e.Position, p)){
                                points.Add((s.Get(null, new GameActionTarget(null, i, false)), p));
                            }
                        }
                    }
                }
            }
            return points;
        }

        private List<Vector2> PointsAround(Vector2 position, float distance, int count, float startingAngle = 0) {
            List<Vector2> result = new List<Vector2>();
            var degIncrease = 360.0f / count;
            for(float i = 0; i < 360; i += degIncrease) {
                result.Add(position + Vector2Utils.VectorFromAngle(i + startingAngle) * distance);
            }
            return result;
        }


        private ViewconeGraph AddTraversePoints(Viewcone view, float alertingDistance, float viewConeMaxLength, int viewconeIndex)
        {
            var first = GetPointInAlertingDistance(
                view.EndingPoints[0], view.StartPos, alertingDistance, viewConeMaxLength);
            var last = GetPointInAlertingDistance(
                view.EndingPoints[view.EndingPoints.Count - 1], view.StartPos, alertingDistance, viewConeMaxLength);
            var includedValues = new List<int>() { 0, 1, 2, view.EndingPoints.Count - 1,
                view.EndingPoints.Count, view.EndingPoints.Count + 1 };
            var slices = GetSlicesOnPath(
                Enumerable.Empty<Vector2>()
                    .Append(first)
                    .Concat(view.EndingPoints)
                    .Append(last),
                viewconeTravelConsideredStep,
                includedValues)
                .ToList();

            var obsts = level.Obstacles
                .Where(o => o.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable)
                .ToList();
            var sliceArray = slices
                //.Where(p => obsts.All(o => !o.ContainsPoint(p, false)))
                .Select((Vector, Index) => (Vector, Index))
                .ToArray();
            int maxFirst = 0;
            foreach(var slice in sliceArray ) {
                if(slice.Vector == view.EndingPoints[0]) {
                    maxFirst = slice.Index;
                    break;
                }
            }
            int maxLast = sliceArray.Length - 1;
            for(int i = sliceArray.Length - 1; i >= 0; i--) {
                var slice = sliceArray[i];
                if(slice.Vector == view.EndingPoints[view.EndingPoints.Count - 1]) {
                    maxLast = slice.Index;
                    break;
                }
            }

            var compareProvider = new ReachableVector2ComparerProvider(this, view);

            List<ViewNode> vertices = new List<ViewNode>(
                sliceArray.Select((p, i) => {
                    var dist = (p.Vector - view.StartPos).magnitude;
                    //if its distance to enemy is shorter than view length and it's not on the side of the view
                    //then it is a middle node
                    return new ViewNode(p.Vector, dist, viewconeIndex, (i > maxFirst && i < maxLast && dist < viewConeMaxLength));
                }
            ));
                
            List<Edge<ViewNode, ViewMidEdgeInfo>> edges = new List<Edge<ViewNode, ViewMidEdgeInfo>>();
            
            //it is not traversable, so the score and alerting ratio values ar enot relevant
            ViewMidEdgeInfo defaultNonTraversableInfo = new ViewMidEdgeInfo(float.MaxValue, false, false, true, 1);
            ViewMidEdgeInfo defaultNonTraversablePerimeterInfo = new ViewMidEdgeInfo(float.MaxValue, false, false, false, 0);

            var viewMaxSqr = viewConeMaxLength * viewConeMaxLength;
            bool IsEdgeOfRemoved(int i, int j) {
                return i <= maxLast && i >= maxFirst && j <= maxLast && j >= maxFirst
                    && ((!FloatEquality.MoreOrEqual((vertices[i].Position - view.StartPos).sqrMagnitude + 5, viewMaxSqr))
                    || (!FloatEquality.MoreOrEqual((vertices[j].Position - view.StartPos).sqrMagnitude + 5, viewMaxSqr)));
            }

            bool IsOnSideOfViewcone(int i, int j) {
                return ((i <= maxFirst && j <= maxFirst) || (i >= maxLast && j >= maxLast));
            }

            for (int i = 0; i < sliceArray.Length; i++)
            {
                int minIndex = 0;
                //binary search should not find the exact occurance,
                //so it returns the negative index of the next element
                if (i != 0)
                    minIndex = - new Span<(Vector2, int)>(sliceArray, 0, i - 1)
                        .BinarySearch(compareProvider.GetComparer(sliceArray[i]));
                int maxIndex = sliceArray.Length - 1;
                if (i != sliceArray.Length - 1)
                    maxIndex = i - new Span<(Vector2, int)>(sliceArray, i + 1, sliceArray.Length - i - 1)
                        .BinarySearch(compareProvider.GetComparer(sliceArray[i]));
                for (int j = minIndex; j < maxIndex; j++)
                {
                    if (j == i)
                        continue;
                    var len = compareProvider.GetLength(i, j, sliceArray[i].Vector, sliceArray[j].Vector);
                    //if they are not adjacent, their common edge goes inside the viewcone
                    var isMidView = Math.Abs(i - j) > 1;
                    //if the edge goes towards the enemy and is not on the view side, then it is an edge of removed
                    var isEdgeOfRemoved = IsEdgeOfRemoved(i, j);
                    ViewMidEdgeInfo info;
                    if(len.HasValue) {
                        if(!(isMidView || isEdgeOfRemoved)) {
                            //it is an edge before the viewcone hits anything, so it is surely on the
                            //viewcone perimeter and in the open, hence it is considered outside of viewcone
                            info = new ViewMidEdgeInfo(len.Value, false, true, false, 0);
                        } else {
                            info = new ViewMidEdgeInfo(len.Value, isEdgeOfRemoved, 
                                true, isMidView, view.AlertingRatioIncrease(len.Value));
                        }
                    } else if(j == i - 1) { 
                        //we need to add it so that it forms the perimeter
                        info = defaultNonTraversablePerimeterInfo;
                    } else {
                        continue;
                    }
                    
                    edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(vertices[i], vertices[j], info));
                }
                if(i != 0 && minIndex > i - 1) {
                    var len = compareProvider.GetLength(i, i - 1, sliceArray[i].Vector, sliceArray[i - 1].Vector);
                    if(len.HasValue && (IsOnSideOfViewcone(i, i - 1) || !IsEdgeOfRemoved(i, i - 1))) {
                        //it is not possible to go there within the viewcone, but since it's the edge, you actually can walk there
                        //both ways
                        var info = new ViewMidEdgeInfo(len.Value, false, true, false, 0);
                        edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(vertices[i], vertices[i - 1], info));
                    } else {
                        //the edge is not at the max length, so it probably pressing against some obstacle
                        //hence you could only go there if you could go through the viewcone
                        //which is held in the for cycle above
                        edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(vertices[i], vertices[i - 1], defaultNonTraversablePerimeterInfo));
                    }
                }
                if(i != vertices.Count - 1 && maxIndex <= i + 1) {
                    var len = compareProvider.GetLength(i, i + 1, sliceArray[i].Vector, sliceArray[i + 1].Vector);
                    if(len.HasValue && (IsOnSideOfViewcone(i, i + 1) || !IsEdgeOfRemoved(i, i + 1))) {
                        //it is not possible to go there within the viewcone, but since it's the edge, you actually can walk there
                        //both ways
                        var info = new ViewMidEdgeInfo(len.Value, false, true, false, 0);
                        edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(vertices[i], vertices[i + 1], info));
                    }
                    //we do not add the non traversable edge - one is sufficient and it will be handled from the next index
                }
            }
            //adding edges from startPos to first and last pos
            var start = new ViewNode(view.StartPos, 0, viewconeIndex);

            void AddToStartEdge(ViewNode from) {
                //enemies can see there, but it is very well still possible, that players cannot go there
                var canGet = staticNavGraph.CanPlayerGetToStraight(start, from);
                var toStartInfo = new ViewMidEdgeInfo(
                    Vector2.Distance(start.Position, from.Position)
                    , false, canGet, false, 0);
                edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(start, from, toStartInfo));
                if(canGet)
                    edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(from, start, toStartInfo));

            }

            AddToStartEdge(vertices[0]);
            AddToStartEdge(vertices[^1]);

            vertices.Add(start);

            return new ViewconeGraph(vertices, edges, view, viewconeIndex);
        }

        class ReachableVector2ComparerProvider
        {
            private ViewconeNavGraph navGraph;
            private Viewcone viewcone;
            private Dictionary2D<int, float?> lengthsDict = new Dictionary2D<int, float?>();

            public ReachableVector2ComparerProvider(ViewconeNavGraph navGraph, Viewcone viewcone)
            {

                this.navGraph = navGraph;
                this.viewcone = viewcone;
            }

            public IComparable<(Vector2 Vector, int Index)> GetComparer((Vector2 Vector, int Index) from)
            {
                return new ReachableVector2Comparer(this, from.Index, from.Vector);
            }

            /// <summary>
            /// Gets the already computed length [<paramref name="fromIndex"/>] -> [<paramref name="toIndex"/>]. 
            /// If it was no computed yet, it computes and saves it.
            /// </summary>
            /// <returns>The legth between the two vectors.</returns>

            public float? GetLength(int fromIndex, int toIndex, Vector2 from, Vector2 to)
            {
                if (lengthsDict.TryGetValue(fromIndex, toIndex, out var res))
                    return res;
                if(navGraph.staticNavGraph.CanPlayerGetToStraight(from, to)) {
                    res = Vector2.Distance(from, to);
                } else {
                    res = null;
                }
                lengthsDict.Add(fromIndex, toIndex, res);
                return res;
            }

            class ReachableVector2Comparer : IComparable<(Vector2 Vector, int Index)>
            {
                readonly ReachableVector2ComparerProvider provider;
                readonly int fromIndex;
                readonly Vector2 from;

                public ReachableVector2Comparer(ReachableVector2ComparerProvider provider,
                    int fromIndex, Vector2 from)
                {
                    this.provider = provider;
                    this.fromIndex = fromIndex;
                    this.from = from;
                }
                public int CompareTo((Vector2 Vector, int Index) other)
                {
                    float? length = provider.GetLength(fromIndex, other.Index, from, other.Vector);

                    var isPossible = length.HasValue
                        && provider.viewcone.CanGoFromTo(from, other.Vector, length.Value, out var _);
                    if (!isPossible)
                        return other.Index < fromIndex ? 1 : -1;
                    else
                        return other.Index < fromIndex ? -1 : 1;
                }
            }
        }

        /// <summary>
        /// Given a <paramref name="path"/> of Vector2, gets points with a specific distance 
        /// (<paramref name="sliceSize"/>) along the path.
        /// </summary>
        /// <param name="includeIndices">The indices of turning points along the path, that should be included as well.
        /// Expected as sorted.</param>
        IEnumerable<Vector2> GetSlicesOnPath(IEnumerable<Vector2> path, float sliceSize, IEnumerable<int> includeIndices)
        {
            var pre = path.First();
            SimpleEnumerator<int?> e = new SimpleEnumerator<int?>(includeIndices.Cast<int?>(), null);
            var nextFixed = e.Get();
            if (nextFixed == 0)
                nextFixed = e.Get();
            float overflowDist = 0;
            int index = 1;
            foreach (var point in path.Skip(1))
            {
                var offset = point - pre;
                var magn = offset.magnitude;
                offset = offset.normalized;
                var curr = overflowDist;
                while(curr <= magn) {
                    yield return pre + offset * curr;
                    curr += sliceSize;
                }
                overflowDist = curr - magn;
                if (index == nextFixed)
                {
                    nextFixed = e.Get();
                    if(!FloatEquality.AreEqual(overflowDist, sliceSize)) {
                        yield return point;
                        overflowDist = sliceSize;
                    }
                }
                index++;
                pre = point;
            }
        }

        private Vector2 GetPointInAlertingDistance(Vector2 from, Vector2 to,
            float alertingDistance, float viewConeMaxLength)
        {

            var currDist = Vector2.Distance(from, to);
            var ratio = currDist / viewConeMaxLength;
            ratio = Mathf.Clamp01(ratio);
            var dist = alertingDistance * ratio;
            return from + (to - from).normalized * dist;
        }

    }
}