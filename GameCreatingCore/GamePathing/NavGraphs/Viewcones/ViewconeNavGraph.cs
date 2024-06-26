using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Playables;
using static UnityEngine.Random;
using static UnityEngine.RectTransform;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones
{
    public class ViewconeNavGraph : IViewconesBearer 
    {
        private readonly IReadOnlyDictionary<EnemyType, EnemyTypeInfo> lengthsDict;
        private readonly LevelRepresentation level;
        private readonly int innerRays;
        private readonly float viewconeTravelConsideredStep;
        private readonly StaticGameRepresentation staticgameRepr;
        private readonly int skillUsePointsAround;

        public readonly StaticNavGraph staticNavGraph;
        //private readonly ViewconePruningStyle ViewconePruning;

        private Dictionary<EnemyState, ViewconeGraph> _enemyViewconeDict
            = new Dictionary<EnemyState, ViewconeGraph>();

        /// <param name="innerRays">Determines how many times are the viewcones fragmented.</param>
        /// <param name="viewconeTravelConsideredStep">When creating the inner graph within the viewcone,
        /// how far are the respective points from one another.</param>
        /// <param name="viewconeLengthMod">The  viewcone is actually not a cone, it is section of triangles. 
        /// What is their length? 0 = viewcone length; 1 = minimal length within the triangle.</param>
        public ViewconeNavGraph(LevelRepresentation level, StaticNavGraph staticNavGraph,
            StaticGameRepresentation staticGameRepr, int innerRays, float viewconeTravelConsideredStep,
            //ViewconePruningStyle viewconePruning,
            int skillUsePointsAround,
            float viewconeLengthMod = 0.5f)
        {
            var playerSpeed = staticGameRepr.PlayerSettings.movementRepresentation.WalkSpeed;
            var lenDic = new Dictionary<EnemyType, EnemyTypeInfo>();
            foreach (var t in level.Enemies
                    .Select(e => e.Type)
                    .Distinct())
            {
                var es = staticGameRepr.GetEnemySettings(t);
                var vr = es.viewconeRepresentation;
                var alertTime = staticGameRepr.GameDifficulty * vr.AlertingTimeModifier - 0.25f;
                var alertDistance = playerSpeed * alertTime;
                var angleIncrease = AngleIncr(vr.Angle);
                float GetLength(float alerted)
                {
                    var currLen = vr.ComputeLength(alerted);
                    var minViewLength = currLen / (float)Math.Cos(Vector2Utils.DegreesToRadians(angleIncrease / 2));
                    return currLen + (minViewLength - currLen) * viewconeLengthMod;
                } 
                lenDic.Add(t, new EnemyTypeInfo(GetLength, vr.Angle, alertDistance));
            }
            lengthsDict = lenDic;

            this.skillUsePointsAround = skillUsePointsAround;
            this.level = level;
            this.innerRays = innerRays;
            this.viewconeTravelConsideredStep = viewconeTravelConsideredStep;
            this.staticNavGraph = staticNavGraph;
            //ViewconePruning = viewconePruning;
            staticgameRepr = staticGameRepr;
        }

        float AngleIncr(float angle) => angle / (innerRays + 1);

        public GraphWithViewcones GetScoredNavGraph(LevelState state)
        {
            var viewcones = GetTraversableViewcones(state);
            viewcones = CutOffCommonParts(viewcones);
            var views = new ViewconeAdder(staticNavGraph).AddViewconesToGraph(viewcones);
            views = AddPickupables(views, state);
            views = AddAvailableSkills(views, state);
            return views;
            //graf se specoš edges, který mají proritu?
            //výpočet toho, jak daleko jde projít viewconem, podle toho nový body po jeho obvodu a přidaný hrany
            //potřeba vyřešit killení
        }

        /// <summary>
        /// Gets the list of viewcones in the given <paramref name="state"/>.
        /// </summary>
        public List<List<Vector2>> GetRawViewcones(LevelState state)
        {
            List<List<Vector2>> viewcones = new List<List<Vector2>>();
            for (int i = 0; i < state.enemyStates.Count; i++)
            {
                var es = state.enemyStates[i];
                if(!es.Alive) {
                    viewcones.Add(new List<Vector2>());
                    continue;
                }
                Viewcone view;
                if (!_enemyViewconeDict.TryGetValue(es, out var vg))
                {
                    var length = lengthsDict[level.Enemies[i].Type].GetLength(es.ViewconeAlertLengthModifier);
                    view = EnemyToViewcone(level.Enemies[i].Type, i, es, innerRays, length);
                }
                else
                {
                    view = vg.Viewcone;
                }
                viewcones.Add(
                    Enumerable.Empty<Vector2>()
                        .Append(view.StartPos)
                        .Concat(view.EndingPoints)
                        .ToList()
                    );
            }
            return viewcones;
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
                    var length = lengthsDict[level.Enemies[i].Type].GetLength(es.ViewconeAlertLengthModifier);
                    var preview = EnemyToViewcone(level.Enemies[i].Type, i, es, innerRays, length);
                    view = AddTraversePoints(preview, lengthsDict[level.Enemies[i].Type].AlertingDistance, length);
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

        private GraphWithViewcones AddPickupables(GraphWithViewcones input, LevelState state) {
            List<(IGameAction, Vector2)> pickupNodes = new List<(IGameAction, Vector2)>();
            for(int i = 0; i < level.SkillsToPickup.Count; i++) {
                if(state.pickupableSkillsPicked[i])
                    continue;
                pickupNodes.Add((new PickupAction(i, level, 5, 0), level.SkillsToPickup[i].Item2));
            }
            return AddPoints(input, pickupNodes);
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

        private GraphWithViewcones AddAvailableSkills(GraphWithViewcones input,
            LevelState state) {
            List<(IGameAction, Vector2)> points = new List<(IGameAction, Vector2)>();

            foreach(var s in state.playerState.AvailableSkills) {
                if(s.CanTargetPlayer) {
                    points.Add((s.Get(null, new GameActionTarget(null, null, true)), state.playerState.Position));
                }
                if(s.CanTargetGround) {
                    var pts = PointsAround(state.playerState.Position, 1 + s.MaxUseDistance, skillUsePointsAround);
                    foreach(var p in pts) {
                        if(s.CanTargetGroundObstacle || !staticNavGraph.IsInPlayerObstacle(p)) {
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
                        var pts = PointsAround(e.Position, 0.05f + s.MaxUseDistance, skillUsePointsAround);
                        foreach(var p in pts) {
                            if(staticNavGraph.CanPlayerGetToStraight(e.Position, p)){
                                points.Add((s.Get(null, new GameActionTarget(null, i, false)), p));
                            }
                        }
                    }
                }
            }
            return AddPoints(input, points);
        }

        private List<Vector2> PointsAround(Vector2 position, float distance, int count) {
            List<Vector2> result = new List<Vector2>();
            var degIncrease = 360.0f / count;
            for(float i = 0; i < 360; i += degIncrease) {
                result.Add(position + Vector2Utils.VectorFromAngle(i) * distance);
            }
            return result;
        }

        private GraphWithViewcones AddPoints(GraphWithViewcones input, 
            List<(IGameAction Action, Vector2 Pos)> points) {
            
            List<IReadOnlyList<Vector2>> viewconeEdges = input.Viewcones
                .Select(v => v.InnerViewcone.EndingPoints.Append(v.InnerViewcone.StartPos).ToList())
                .Cast<IReadOnlyList<Vector2>>()
                .ToList();

            List<(ScoredActionedNode Node, int Index)> addedInViewcones
                = new List<(ScoredActionedNode Node, int Index)>();

            foreach(var pair in points) {
                int? viewconeIndex = null;
                for(int i = 0; i < input.Viewcones.Count; i++) {
                    if(Obstacle.IsInPolygon(pair.Pos, input.Viewcones[i].Nodes
                        .Select(p => p.Position)
                        .ToList())) {
                        viewconeIndex = i;
                        break;
                    }
                }
                if(viewconeIndex.HasValue) {
                    var newPair = AddPointInViewcone(input, pair.Action, pair.Pos, 
                        viewconeIndex.Value, addedInViewcones.Where(p => p.Index == viewconeIndex).Select(p => p.Node));
                    input = newPair.Item1;
                    addedInViewcones.Add((newPair.Item2, viewconeIndex.Value));
                    continue;
                }

                List<(ScoredActionedNode Node, float Distance)> neighbours 
                    = new List<(ScoredActionedNode Node, float Distance)>();
                foreach(var node in input.vertices) {
                    if(staticNavGraph.CanPlayerGetToStraight(pair.Pos, node.Position)
                        && staticNavGraph.CanGetToStraight(pair.Pos, node.Position, viewconeEdges)){
                        neighbours.Add((node, Vector2.Distance(pair.Pos, node.Position)));
                    }
                }
                //here we compute the best neighbour and add it as previous
                var best = GetBest(neighbours);
                //replace the old graph with a new one with added node
                var verts = input.vertices.ToList();
                var newNode = new ScoredActionedNode(best.ScoreThen, best.Node.Score, best.Node, pair.Pos, pair.Action);
                verts.Add(newNode);
                var edgs = input.edges.ToList();
                foreach(var n in neighbours) {
                    edgs.Add(new Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>(n.Node, newNode,
                        new GraphWithViewconeEdgeInfo(n.Distance, null, null)));
                    edgs.Add(new Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>(newNode, n.Node,
                        new GraphWithViewconeEdgeInfo(n.Distance, null, null)));
                }
                input = new GraphWithViewcones(verts, edgs, input.Viewcones, false);
            }
            return new GraphWithViewcones(input.vertices, input.edges, input.Viewcones, true);
        }

        private (GraphWithViewcones, ScoredActionedNode) AddPointInViewcone(GraphWithViewcones input, IGameAction action,
            Vector2 point, int viewconeIndex, IEnumerable<ScoredActionedNode> alsoConnectTo) {

            var allNeighbours = input.Viewcones[viewconeIndex].Nodes
                .Concat(alsoConnectTo)
                .Where(n => staticNavGraph.CanPlayerGetToStraight(point, n.Position))
                .Select(n => (n, 
                    Vector2.Distance(n.Position, point),
                    input.Viewcones[viewconeIndex].InnerViewcone.CanGoFromTo(point, n.Position, out var alert1),
                    alert1,
                    input.Viewcones[viewconeIndex].InnerViewcone.CanGoFromTo(n.Position, point, out var alert2),
                    alert2
                    ))
                .Cast<(ScoredActionedNode Node, float Distance, bool CanForwards, float ForwardsAlertIncrease, bool CanBackwards, float BackwardsAlertIncrease)>()
                .ToList();

            var best = GetBest(allNeighbours.Select(t => (t.Node, t.Distance)));
            var node = new ScoredActionedNode(best.ScoreThen, best.Node.Score, best.Node, point, action);
            var vertices = input.vertices.ToList();
            vertices.Add(node);
            var edges = input.edges.ToList();
            foreach(var tuple in allNeighbours) {
                if(tuple.CanForwards) {
                    edges.Add(new Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>(node, tuple.Node,
                        new GraphWithViewconeEdgeInfo(tuple.Distance, viewconeIndex, tuple.ForwardsAlertIncrease)));
                }
                if(tuple.CanBackwards) {
                    edges.Add(new Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>(tuple.Node, node,
                        new GraphWithViewconeEdgeInfo(tuple.Distance, viewconeIndex, tuple.BackwardsAlertIncrease)));
                }
            }
            return (new GraphWithViewcones(vertices, edges, input.Viewcones, false), node);
        }

        private (T Node, float ScoreThen) GetBest<T>(Vector2 from, IEnumerable<T> possibles) where T : ScoredNode
            => GetBest(possibles.Select(n => (n, Vector2.Distance(n.Position, from))));

        private (T Node, float ScoreThen) GetBest<T>(IEnumerable<(T Node, float Distance)> possibles) where T : ScoredNode {

            float min = float.MaxValue;
            T? best = null;
            foreach(var node in possibles) {
                if(node.Node.Score + node.Distance < min) {
                    min = node.Node.Score + node.Distance;
                    best = node.Node;
                }
            }
            if(best == null)
                throw new Exception("This should never happen, " +
                    "there is always something in the graph that we can go to.");
            return (best, min);
        }


        private Viewcone EnemyToViewcone(EnemyType type, int index, EnemyState state, int innerRays, float length)
        {

            var enemVisionObsts = level.Obstacles
                .Append(level.OuterObstacle)
                .Where(o => o.Effects.EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough)
                .ToList();

            var angle = lengthsDict[type].Angle;
            var angleIncrease = AngleIncr(angle);
            var startAngle = state.Rotation - angle / 2;

            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < innerRays + 2; i++)
            {
                var vec = Vector2Utils.VectorFromAngle(startAngle + angleIncrease * i);
                var p = GetIntersectionWithObstacles(state.Position, vec, length, enemVisionObsts);
                points.Add(p);
            }
            return new Viewcone(state.Position, points, index, lengthsDict[type], length);
        }

        /// <summary>
        /// Finds the closest intersection with <paramref name="obstacles"/> from <paramref name="from"/> in the given <paramref name="direction"/>.
        /// If there is no such intesection, return <paramref name="direction"/> * <paramref name="maxDistance"/>.
        /// </summary>
        /// <param name="direction">Is expected as a normalized vector.</param>
        private Vector2 GetIntersectionWithObstacles(Vector2 from, Vector2 direction,
            float maxDistance, IEnumerable<Obstacle> obstacles)
        {

            var end = from + direction * maxDistance;

            var result = end;
            var magn = maxDistance * maxDistance;

            foreach (var obst in obstacles)
            {
                var pre = obst.Shape[obst.Shape.Count - 1];
                foreach (var p in obst.Shape)
                {
                    var i = LineIntersectionDecider.FindFirstIntersection(from, end, pre, p, true, true);
                    if (i.HasValue)
                    {
                        var currMagn = (i - from).Value.sqrMagnitude;
                        if (currMagn < magn)
                        {
                            magn = currMagn;
                            result = i.Value;
                        }
                    }
                    pre = p;
                }
            }
            return result;
        }


        private ViewconeGraph AddTraversePoints(Viewcone view, float alertingDistance, float viewConeMaxLength)
        {
            var first = GetPointInAlertingDistance(
                view.EndingPoints[0], view.StartPos, alertingDistance, viewConeMaxLength);
            var last = GetPointInAlertingDistance(
                view.EndingPoints[view.EndingPoints.Count - 1], view.StartPos, alertingDistance, viewConeMaxLength);
            var includedValues = new List<int>() { 0, 1, view.EndingPoints.Count, view.EndingPoints.Count + 1 };
            var slices = GetSlicesOnPath(
                Enumerable.Empty<Vector2>()
                    .Append(first)
                    .Concat(view.EndingPoints)
                    .Append(last),
                viewconeTravelConsideredStep,
                includedValues);

            var obsts = level.Obstacles
                .Where(o => o.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable)
                .ToList();
            var sliceArray = slices
                .Where(p => obsts.All(o => !o.ContainsPoint(p)))
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

            List<ViewNode> vertices = new List<ViewNode>(sliceArray
                .Select((p, i) => new ViewNode(p.Vector,
                //if its distance to enemy is shorter than view length and it's not on the side of the view
                //then it is a middle node
                (i > maxFirst && i < maxLast && (p.Vector - view.StartPos).sqrMagnitude < viewConeMaxLength * viewConeMaxLength))));
            List<Edge<ViewNode, ViewMidEdgeInfo>> edges = new List<Edge<ViewNode, ViewMidEdgeInfo>>();
            
            //it is not traversable, so the score and alerting ratio values ar enot relevant
            ViewMidEdgeInfo defaultNonTraversableInfo = new ViewMidEdgeInfo(float.MaxValue, false, false, false, 1);

            bool IsEdgeOfRemoved(int i, int j) {
                return i < maxLast && i > maxFirst && j < maxLast && j > maxFirst
                    && ((vertices[i].Position - view.StartPos).sqrMagnitude < viewConeMaxLength * viewConeMaxLength
                    || (vertices[j].Position - view.StartPos).sqrMagnitude < viewConeMaxLength * viewConeMaxLength);
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
                        if(!isMidView && IsOnSideOfViewcone(i, j)) {
                            //it is an edge before the viewcone hits anything, so it is surely on the
                            //viewcone perimeter and in the open, hence it is considered outside of viewcone
                            info = new ViewMidEdgeInfo(len.Value, false, 
                                true, false, 0);
                        } else {
                            info = new ViewMidEdgeInfo(len.Value, isEdgeOfRemoved, 
                                true, isMidView, view.AlertingRatioIncrease(len.Value));
                        }
                    } else if(j == i - 1) { 
                        //we need to add it so that it forms the perimeter
                        info = defaultNonTraversableInfo;
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
                        edges.Add(new Edge<ViewNode, ViewMidEdgeInfo>(vertices[i], vertices[i - 1], defaultNonTraversableInfo));
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
            var start = new ViewNode(view.StartPos);

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

            return new ViewconeGraph(vertices, edges, view);
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
                if (index == nextFixed && overflowDist != sliceSize)
                {
                    yield return point;
                    nextFixed = e.Get();
                    overflowDist = sliceSize;
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