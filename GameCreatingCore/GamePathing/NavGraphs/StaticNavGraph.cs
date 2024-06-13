using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;
using static UnityEngine.RectTransform;

namespace GameCreatingCore.GamePathing.NavGraphs
{
    /// <summary>
    /// The navigation graph generated from the obstacles. Not considering the player or the enemies.
    /// </summary>
    public class StaticNavGraph
    {
        //myšlenka:
        //	zapamatuju si statickej graf prostředí se vzdálenostmi od cíle (ten je taky statický)
        //	pak iteruju po čase, vždy připočtu do grafu pohledy enemáků
        //	a dám jim heuristicky nižší prioritu
        //	naopak cíli a vojákům dám prioritu vyšší
        //	v tom spočtu nejkratší cestu
        //	pak se posunu o kus po nejkratší cestě a opakuju
        //	pokud skončím v pohledu, přidávám dobu, po jakou tam jsem (přibližnou) do počítadla
        //	failnu, pokud je doba moc dlouhá (pokud jsem v takový části toho pohledu,
        //	že tam po té době enemy už vidí
        //		nějak je potřeba zařídit, že výstupem je cesta a akce, který se staly
        //		(průchod pohledem, zabití vojáka...)
        
        private readonly List<Obstacle> obstacles;
        private readonly Obstacle outerObstacle;
        private readonly LevelGoal levelGoal;
        private bool initialized = false;
        private bool innerInitialized = false;


        private bool computeEnemyNavMesh;
        private Graph<Node>? _enemyNavGraph;
        private Graph<Node> EnemyNavGraph
            => _enemyNavGraph ?? throw new InvalidOperationException();

        private Graph<ScoredNode>? _playerStaticNavGraph;
        internal Graph<ScoredNode> PlayerStaticNavGraph
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
        private List<Obstacle> EnemyObstacles => _enemyObstacles
            ?? throw new InvalidOperationException();

        private List<Obstacle>? _playerObstacles;
        private List<Obstacle> PlayerObstacles => _playerObstacles
            ?? throw new InvalidOperationException();

        public bool CanEnemyGetToStraight(Vector2 from, Vector2 to)
            => CanGetToStraight(from, to, EnemyObstacles);
        public List<Vector2> GetEnemyPath(Vector2 from, Vector2 to) 
            => EnemyNavGraph.GetPath(from, to, CanEnemyGetToStraight);

        public bool IsInPlayerObstacle(Vector2 vec)
            => PlayerObstacles.Any(o => o.ContainsPoint(vec));
        public bool CanPlayerGetToStraight(Vector2 from, Vector2 to)
            => CanGetToStraight(from, to, PlayerObstacles);
        public List<Vector2>? GetEnemylessPlayerPath(Vector2 from)
            => PlayerStaticNavGraph.GetPath(from, CanPlayerGetToStraight);
        public List<Vector2>? GetEnemylessPlayerPath(Vector2 from, Vector2 to)
            => PlayerStaticNavGraph.GetPath(from, to, CanPlayerGetToStraight);

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
                    dist = Math.Max(0, dist - levelGoal.Radius);
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
                    if(!outerObstacle.ContainsPoint(o.Shape[j]))
                        continue;
                    Node f = new Node(o.Shape[j]);
                    nodes.Add(f);
                    for(int L = j + 1; L < o.Shape.Count; L++) {
                        if(!outerObstacle.ContainsPoint(o.Shape[L]))
                            continue;
                        if(CanGetToStraight(o.Shape[j], o.Shape[L], consideredObsts) 
                            && (AreNeighboring(j, L, o) || (! IsInsideObstacle(o.Shape[j], o.Shape[L], o)))) {
                            Node s = new Node(o.Shape[L]);
                            float dist = Vector2.Distance(o.Shape[L], o.Shape[j]);
                            edges.Add(new Edge<Node>(f, s, dist));
                            edges.Add(new Edge<Node>(s, f, dist));
                        }
                    }
                    for(int k = i + 1; k < consideredObsts.Count; k++) {
                        var o2 = consideredObsts[k];
                        if(o2.ContainsPoint(o.Shape[j]))
                            break;
                        for(int L = 0; L < o2.Shape.Count; L++) {
                            if(!outerObstacle.ContainsPoint(o2.Shape[L]))
                                continue;
                            if(o.ContainsPoint(o2.Shape[L]))
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
            if(whatToInclude(outerObstacle)) {
                for(int i = 0; i < outerObstacle.Shape.Count; i++) {
                    Node f = new Node(outerObstacle.Shape[i]);
                    nodes.Add(f);
                    for(int j = i+1; j < outerObstacle.Shape.Count; j++) {
                        //different if than above - we want the IsInsideObstacle to be true
                        if(CanGetToStraight(outerObstacle.Shape[i], outerObstacle.Shape[j], consideredObsts) 
                            && IsInsideObstacle(outerObstacle.Shape[i], outerObstacle.Shape[j], outerObstacle)) {
                            Node s = new Node(outerObstacle.Shape[j]);
                            float dist = Vector2.Distance(outerObstacle.Shape[i], outerObstacle.Shape[j]);
                            edges.Add(new Edge<Node>(f, s, dist));
                            edges.Add(new Edge<Node>(s, f, dist));
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
        public bool CanGetToStraight(Vector2 from, Vector2 to, List<Obstacle> obstacles)
            => CanGetToStraight(from, to, obstacles.Select(o => o.Shape));

        /// <summary>
        /// Makes sure that none of the the <paramref name="obstacles"/> block the straight path 
        /// <paramref name="from"/> -> <paramref name="to"/>.
        /// </summary>
        public bool CanGetToStraight(Vector2 from, Vector2 to, IEnumerable<IReadOnlyList<Vector2>> obstacles) {
            foreach(var o in obstacles) {
                if(!CanGetToStraight(from, to, o))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Does the <paramref name="shape"/> block the straight path <paramref name="from"/> -> <paramref name="to"/>?
        /// </summary>
        protected bool CanGetToStraight(Vector2 from, Vector2 to, IReadOnlyList<Vector2> shape) { 
            for(int i = 0; i < shape.Count; i++) {
                var next = (i + 1) % shape.Count;
                if(shape[i] != from
                    && shape[next] != from
                    && shape[i] != to
                    && shape[next] != to
                    && LineIntersectionDecider.HasIntersection(from, to, shape[i], shape[next]))

                    return false;
            }
            return true;
        }

        /// <summary>
        /// Considering it is possible to go <paramref name="from"/> -> <paramref name="to"/> straight
        /// while they are both on the border of the same obstacle, does the path go in the inside of the obstacle?
        /// - false possible only for a) adjecent points in the shape; b) the shape is convex
        /// </summary>
        protected bool IsInsideObstacle(Vector2 from, Vector2 to, Obstacle obstacle) {
            var mid = (from + to) / 2;
            return obstacle.ContainsPoint(mid);
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
