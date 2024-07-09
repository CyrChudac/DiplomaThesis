using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GameCreatingCore.GameActions;
using GameCreatingCore.LevelRepresentationData;
using GameCreatingCore.StaticSettings;
using UnityEngine;

namespace GameCreatingCore.GamePathing
{
    /// <summary>
    /// The navigation graph generated from the obstacles. Not considering the player or the enemies.
    /// </summary>
    public class StaticNavGraph
    {
        private readonly List<Obstacle> obstacles;
        private readonly Obstacle outerObstacle;
        private readonly LevelGoal levelGoal;
        private bool initialized = false;
        private bool innerInitialized = false;


        private bool computeEnemyNavMesh;
        private Graph<Node>? _enemyNavGraph;
        public Graph<Node> EnemyNavGraph
            => _enemyNavGraph ?? throw new InvalidOperationException();

        private Graph<ScoredNode>? _playerStaticNavGraph;
        public Graph<ScoredNode> PlayerStaticNavGraph
            => _playerStaticNavGraph ?? throw new InvalidOperationException();

        protected bool IsInitialized => initialized && innerInitialized;

        public StaticNavGraph(LevelRepresentation level, bool computeEnemyNavGraph)
            :this(level.Obstacles, level.OuterObstacle, level.Goal, computeEnemyNavGraph) { }

        public StaticNavGraph(List<Obstacle> obstacles, Obstacle outerObstacle, LevelGoal goal, bool computeEnemyMesh)
        {
            this.obstacles = new List<Obstacle>(obstacles);
            levelGoal = goal;
            this.outerObstacle = outerObstacle;
            computeEnemyNavMesh = computeEnemyMesh;
        }

        public StaticNavGraph Initialized()
        {
            //this way doesn't become initialized if exception is thrown and catched
            InnerInitialize();
            initialized = true;
            return this;
        }

        protected virtual void InnerInitialize()
        {
            if(computeEnemyNavMesh) {
                _enemyNavGraph = ComputeNavGraph(
                    o => o.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable);
            }
            _playerStaticNavGraph = ComputeScoredNavGraph(
                o => o.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable);
            _enemyObstacles = obstacles
                .Where(o => o.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable)
                .ToList();
            _playerObstacles = obstacles
                .Where(o => o.Effects.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable)
                .ToList();
            innerInitialized = true;
        }

        private List<Obstacle>? _enemyObstacles = null;
        public IReadOnlyList<Obstacle> EnemyObstacles => _enemyObstacles
            ?? throw new InvalidOperationException();

        private List<Obstacle>? _playerObstacles;
        public IReadOnlyList<Obstacle> PlayerObstacles => _playerObstacles
            ?? throw new InvalidOperationException();

        public bool CanEnemyGetToStraight(Vector2 from, Vector2 to)
            => CanGetToStraight(from, to, EnemyObstacles);
        public List<Vector2>? GetEnemyPath(Vector2 from, Vector2 to) 
            => EnemyNavGraph.GetPath(from, to, (f, t) => CanEnemyGetToStraight(f, t) && !IsLineInsideEnemyObstacle(f, t));

        public bool IsInPlayerObstacle(Vector2 vec, bool isBoundaryInside)
            => PlayerObstacles.Any(o => o.ContainsPoint(vec, isBoundaryInside));
        public bool CanPlayerGetToStraight(Vector2 from, Vector2 to)
            => CanGetToStraight(from, to, PlayerObstacles);
        public bool CanPlayerGetToStraight<T>(T from, T to) where T : Node
            => CanPlayerGetToStraight(from.Position, to.Position);
        public List<Vector2>? GetEnemylessPlayerPath(Vector2 from)
            => PlayerStaticNavGraph.GetPath(from, CanPlayerGetToStraight);
        public float? GetEnemylessPlayerPathLength(Vector2 from)
            => PlayerStaticNavGraph.GetPathScore(from, CanPlayerGetToStraight);
        public List<Vector2>? GetEnemylessPlayerPath(Vector2 from, Vector2 to)
            => PlayerStaticNavGraph.GetPath(from, to, (f, t) => CanPlayerGetToStraight(f, t) && !IsLineInsidePlayerObstacle(f, t));
        public int? InWhichPlayerObstacle(Vector2 vec, bool isBoundaryInside) {
            for(int i = 0; i < PlayerObstacles.Count; i++) {
                if(PlayerObstacles[i].ContainsPoint(vec, isBoundaryInside))
                    return i;
            }
            return null;
        }

        private Graph<ScoredNode> ComputeScoredNavGraph(Func<Obstacle, bool> whatToInclude) {
            var graph = ComputeNavGraph(whatToInclude);

            var obsts = obstacles
                .Where(whatToInclude)
                .ToList();
            if(whatToInclude(outerObstacle))
                obsts.Add(outerObstacle);


            ScoredNode g = new ScoredNode(0, 0, null, levelGoal.Position);
            List<ScoredNode> nodes = new List<ScoredNode>();
            List<Edge<ScoredNode>> edges = new List<Edge<ScoredNode>>();
            nodes.Add(g);
            
            List<Vector2> seen = new List<Vector2>();
            //priority queue not supported in .NET standard
            //hence we use our own Priority queue implementation that uses SortedDictionary
            PriorityQueue<float, ScoredNode> que = new PriorityQueue<float, ScoredNode>();

            for(int k = 0; k < graph.vertices.Count; k++) {
                if(CanGetToStraight(levelGoal.Position, graph.vertices[k].Position, obsts)) {
                    float dist = Vector2.Distance(g.Position, graph.vertices[k].Position);
                    //dist = Math.Max(0, dist - levelGoal.Radius);
                    var n = new ScoredNode(dist, 0, g, graph.vertices[k].Position);
                    que.Enqueue(dist, n);
                    edges.Add(new Edge<ScoredNode>(g, n, dist));
                    edges.Add(new Edge<ScoredNode>(n, g, dist));
                }
            }

            int iterations = 0;

            while(que.Any()) {
                var curr = que.DequeueMin();
                if(seen.Contains(curr.Position))
                    continue;
                if(iterations++ > graph.vertices.Count + 5)
                    throw new Exception("Possible infinite loop, investigate.");
                var score = curr.Score;
                var outEdges = graph.GetOutEdges(curr);
                foreach(var (second, edgeInfo) in outEdges) {
                    var next = new ScoredNode(score + edgeInfo.Score, score, curr, second.Position);
                    que.Enqueue(score + edgeInfo.Score, next);
                    edges.Add(new Edge<ScoredNode>(curr, next, edgeInfo.Score)); 
                }
                nodes.Add(curr);
                seen.Add(curr.Position);
            }
            //important aspect is, that nodes that could not be reached from the goal were not added
            //so the algorithm inherently prunes the unreachable areas

            //now edges are a mess, they contain different nodes on one dirrection than the other
            //so we need to tidy them up
            foreach(var node in nodes) {
                for(int i = 0; i < edges.Count; i++) {
                    //first doesn't have to be checked -> because the algo above
                    //always creates edges from the point with the lowest score
                    if(edges[i].Second.Position == node.Position) {
                        edges[i] = new Edge<ScoredNode>(edges[i].First, node, edges[i].Score);
                    }
                }
            }

            return new Graph<ScoredNode>(nodes, edges, true);
        }

        /// <summary>
        /// Computes a graph of possible walking with distances given the <paramref name="whatToInclude"/> criteria
        /// on the obstacles.
        /// </summary>
        private Graph<Node> ComputeNavGraph(Func<Obstacle, bool> whatToInclude) {
            var dueObsts = obstacles
                .Where(whatToInclude)
                .ToList();
            var consideredObsts = new List<Obstacle>(dueObsts);
            if(whatToInclude(outerObstacle))
                consideredObsts.Add(outerObstacle);
            List<Edge<Node>> edges = new List<Edge<Node>>();
            List<Node> nodes = new List<Node>();
            for(int i = 0; i < dueObsts.Count; i++) {
                var o = dueObsts[i];
                for(int j = 0; j < o.Shape.Count; j++) {
                    if(!outerObstacle.ContainsPoint(o.Shape[j], false))
                        continue;
                    Node f = new Node(o.Shape[j]);
                    nodes.Add(f);
                    for(int L = j + 1; L < o.Shape.Count; L++) {
                        if(!outerObstacle.ContainsPoint(o.Shape[L], false))
                            continue;
                        if(IsLineInsideAnyObstacle(o.Shape[j], o.Shape[L], dueObsts))
                            continue;
                        if(CanGetToStraight(o.Shape[j], o.Shape[L], consideredObsts) 
                            && (AreNeighboring(j, L, o) || (! IsLineInsideObstacle(o.Shape[j], o.Shape[L], o)))) {
                            Node s = new Node(o.Shape[L]);
                            float dist = Vector2.Distance(o.Shape[L], o.Shape[j]);
                            edges.Add(new Edge<Node>(f, s, dist));
                            edges.Add(new Edge<Node>(s, f, dist));
                        }
                    }
                    for(int k = i + 1; k < dueObsts.Count; k++) {
                        var o2 = dueObsts[k];
                        if(o2.ContainsPoint(o.Shape[j], true))
                            break;
                        for(int L = 0; L < o2.Shape.Count; L++) {
                            if(!outerObstacle.ContainsPoint(o2.Shape[L], false))
                                continue;
                            if(o.ContainsPoint(o2.Shape[L], true))
                                continue;
                            if(IsLineInsideAnyObstacle(o.Shape[j], o2.Shape[L], dueObsts))
                                continue;
                            if(IsLineInsideObstacle(o.Shape[j], o2.Shape[L], o2))
                                continue;
                            if(CanGetToStraight(o.Shape[j], o2.Shape[L], consideredObsts)) {
                                Node s = new Node(o2.Shape[L]);
                                float dist = Vector2.Distance(o2.Shape[L], o.Shape[j]);
                                edges.Add(new Edge<Node>(f, s, dist));
                                edges.Add(new Edge<Node>(s, f, dist));
                            }
                        }
                    }
                }
            }
            return new Graph<Node>(nodes, edges, true);
        }
        
        /// <summary>
        /// Makes sure that none of the the <paramref name="obstacles"/> block the straight path 
        /// <paramref name="from"/> -> <paramref name="to"/>.
        /// </summary>
        private bool CanGetToStraight(Vector2 from, Vector2 to, IReadOnlyList<Obstacle> obstacles)
            => CanGetToStraight(from, to, obstacles.Select(o => o.Shape));

        public bool CanGetToStraight<T>(T from, T to, IEnumerable<IReadOnlyList<Vector2>> obstacles) where T : Node
            => CanGetToStraight(from.Position, to.Position, obstacles);

        /// <summary>
        /// Finds out if none of the the <paramref name="obstacles"/> block the straight path 
        /// <paramref name="from"/> -> <paramref name="to"/>.
        /// </summary>
        public bool CanGetToStraight(Vector2 from, Vector2 to, IEnumerable<IReadOnlyList<Vector2>> obstacles, 
            bool overlapIsIntersection = false) {

            foreach(var o in obstacles) {
                if(!CanGetToStraight(from, to, o, overlapIsIntersection))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Does the <paramref name="shape"/> block the straight path <paramref name="from"/> -> <paramref name="to"/>?
        /// </summary>
        protected bool CanGetToStraight(Vector2 from, Vector2 to, IReadOnlyList<Vector2> shape, bool overlapIsIntersection = false) { 
            for(int i = 0; i < shape.Count; i++) {
                var next = (i + 1) % shape.Count;
                if(shape[i] != from
                    && shape[next] != from
                    && shape[i] != to
                    && shape[next] != to
                    && LineIntersectionDecider.HasIntersection(from, to, shape[i], shape[next], overlapIsIntersection, false))

                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Determines whether line: <paramref name="from"/> -> <paramref name="to"/> is inside
        /// obstacles unwalkable for player.
        /// </summary>
        internal bool IsLineInsidePlayerObstacle(Vector2 from, Vector2 to) {
            return IsLineInsideAnyObstacle(from, to, PlayerObstacles);
        }
        internal bool IsLineInsidePlayerObstacle<T>(T from, T to) where T : Node{
            return IsLineInsidePlayerObstacle(from.Position, to.Position);
        }

        /// <summary>
        /// Determines whether line: <paramref name="from"/> -> <paramref name="to"/> is inside
        /// obstacles unwalkable for enemy.
        /// </summary>
        internal bool IsLineInsideEnemyObstacle(Vector2 from, Vector2 to) {
            return IsLineInsideAnyObstacle(from, to, EnemyObstacles);
        }
        
        public bool IsLineInsideAnyObstacle(Vector2 from, Vector2 to, IReadOnlyList<Obstacle> obsts) {
            foreach(var o in obsts) {
                if(IsLineInsideObstacle(from, to, o))
                    return true;
            }
            return false;
        }

        public bool IsLineInsideAnyObstacle(Vector2 from, Vector2 to, IEnumerable<IReadOnlyList<Vector2>> obsts) {
            foreach(var o in obsts) {
                if(IsLineInsideObstacle(from, to, o))
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Considering it is possible to go <paramref name="from"/> -> <paramref name="to"/> straight
        /// while they are both on the border of the same obstacle, does the path go in the inside of the obstacle?
        /// - false possible only for a) adjecent points in the shape; b) the shape is convex
        /// </summary>
        public bool IsLineInsideObstacle(Vector2 from, Vector2 to, Obstacle obstacle) {
            var mid = (from + to) / 2;
            return obstacle.ContainsPoint(mid, false);
        }

        /// <summary>
        /// Considering it is possible to go <paramref name="from"/> -> <paramref name="to"/> straight
        /// while they are both on the border of the same obstacle, does the path go in the inside of the obstacle?
        /// - false possible only for a) adjecent points in the shape; b) the shape is convex
        /// </summary>
        public bool IsLineInsideObstacle(Vector2 from, Vector2 to, IReadOnlyList<Vector2> obstacle) {
            var mid = (from + to) / 2;
            return Obstacle.IsInPolygon(mid, obstacle, false);
        }

        /// <summary>
        /// Within obstacle <paramref name="o"/> is point on <paramref name="index1"/> next to point on <paramref name="index2"/>?
        /// </summary>
        protected bool AreNeighboring(int index1, int index2, Obstacle o) {
            return Math.Abs(index1 - index2) == 1
                || (index1 == 0 && index2 == o.Shape.Count - 1)
                || (index2 == 0 && index1 == o.Shape.Count - 1);
        }
    }
}
