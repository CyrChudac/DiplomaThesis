using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;
using static UnityEngine.RectTransform;

namespace GameCreatingCore.GameScoring.NavGraphs
{
    /// <summary>
    /// The navmesh generated from the obstacles and the goal. Not considering the player or the enemies.
    /// </summary>
    internal class StaticNavGraph
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
        public Graph<ScoredNode> PlayerStaticNavmesh
            => _playerStaticNavGraph ?? throw new InvalidOperationException();

        protected bool Initialized => initialized && innerInitialized;

        public StaticNavGraph(List<Obstacle> obstacles, Obstacle outerObstacle, LevelGoal goal, bool computeEnemyMesh)
        {
            this.obstacles = obstacles;
            levelGoal = goal;
            this.outerObstacle = outerObstacle;
            computeEnemyNavMesh = computeEnemyMesh;
        }

        public void Initialize()
        {
            //this way doesn't become initialized if exception is thrown and catched
            InnerInitialize();
            initialized = true;
        }

        protected virtual void InnerInitialize()
        {
            if(computeEnemyNavMesh) {
                _enemyNavGraph = ComputeNavGraph(
                    o => o.EnemyWalkEffect == WalkObstacleEffect.Unwalkable);
            }
            _playerStaticNavGraph = ComputeScoredNavGraph(
                o => o.FriendlyWalkEffect == WalkObstacleEffect.Unwalkable);
            innerInitialized = true;
        }

        private Graph<ScoredNode> ComputeScoredNavGraph(Func<Obstacle, bool> whatToInclude) {
            var graph = ComputeNavGraph(whatToInclude);

            var obsts = obstacles
                .Where(whatToInclude)
                .ToList();
            if(whatToInclude(outerObstacle))
                obsts.Add(outerObstacle);


            ScoredNode g = new ScoredNode(0, 0, null, levelGoal.Position);
            graph.vertices.Add(g);

            for(int k = 0; k < graph.vertices.Count; k++) {
                if(CanGetToStraight(levelGoal.Position, graph.vertices[k].Value, obsts)) {
                    float dist = Vector2.Distance(g.Value, graph.vertices[k].Value);
                    dist = Math.Max(0, dist - levelGoal.Radius);
                    graph.edges.Add(new Edge<Node>(g, graph.vertices[k], dist));
                    graph.edges.Add(new Edge<Node>(graph.vertices[k], g, dist));
                }
            }
            
            List<ScoredNode> nodes = new List<ScoredNode>();
            List<Edge<ScoredNode>> edges = new List<Edge<ScoredNode>>();
            List<Vector2> seen = new List<Vector2>();
            //priority queue not supported in .NET standard
            //hence I use SortedDictionary which has roughly the same cost of operations
            SortedDictionary<float, ScoredNode> que = new SortedDictionary<float, ScoredNode>();
            que.Add(0, g);
            while(que.Any()) { //here we assume all scores are different (because it is sorted dictionary)
                var score = que.Keys.First();
                var curr = que[score];
                que.Remove(score);
                if(seen.Contains(curr.Value))
                    continue;
                var outEdges = graph.edges.Where(e => e.First.Value == curr.Value);
                foreach(var edge in outEdges) {
                    var next = new ScoredNode(score + edge.Score, score, curr, edge.Second.Value);
                    que.Add(score + edge.Score, next);
                    edges.Add(new Edge<ScoredNode>(curr, next, edge.Score)); 
                }
                nodes.Add(curr);
                seen.Add(curr.Value);
            }

            //now edges are a mess, they contain different nodes on one dirrection than the other
            //so we need to tidy them up
            foreach(var node in nodes) {
                for(int i = 0; i < edges.Count; i++) {
                    //first doesn't have to be chacked -> because the algo above
                    //always creates edges from the point with the lowest score
                    if(edges[i].Second.Value == node.Value) {
                        edges[i] = new Edge<ScoredNode>(edges[i].First, node, edges[i].Score);
                    }
                }
            }

            return new Graph<ScoredNode>(nodes, edges);
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
                    Node f = new Node(o.Shape[j]);
                    nodes.Add(f);
                    for(int L = j + 1; L < o.Shape.Count; L++) {
                        if(CanGetToStraight(o.Shape[j], o.Shape[L], o) 
                            && ! IsInsideObstacle(o.Shape[j], o.Shape[L], o)) {
                            Node s = new Node(o.Shape[L]);
                            float dist = Vector2.Distance(o.Shape[L], o.Shape[j]);
                            edges.Add(new Edge<Node>(f, s, dist));
                            edges.Add(new Edge<Node>(s, f, dist));
                        }
                    }
                    for(int k = i + 1; k < consideredObsts.Count; k++) {
                        var o2 = consideredObsts[k];
                        for(int L = 0; L < o2.Shape.Count; L++) {
                            if(CanGetToStraight(o.Shape[j], o2.Shape[L], consideredObsts)) {
                                Node s = new Node(o.Shape[L]);
                                float dist = Vector2.Distance(o.Shape[L], o.Shape[j]);
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
                        //different if - we want the IsInsideObstacle to be true
                        if(CanGetToStraight(outerObstacle.Shape[i], outerObstacle.Shape[j], outerObstacle) 
                            && IsInsideObstacle(outerObstacle.Shape[i], outerObstacle.Shape[j], outerObstacle)) {
                            Node s = new Node(outerObstacle.Shape[j]);
                            float dist = Vector2.Distance(outerObstacle.Shape[i], outerObstacle.Shape[j]);
                            edges.Add(new Edge<Node>(f, s, dist));
                            edges.Add(new Edge<Node>(s, f, dist));
                        }
                    }
                }
            }
            return new Graph<Node>(nodes, edges);
        }

        /// <summary>
        /// Makes sure that none of the the <paramref name="obstacles"/> block the straight path 
        /// <paramref name="from"/> -> <paramref name="to"/>.
        /// </summary>
        public bool CanGetToStraight(Vector2 from, Vector2 to, List<Obstacle> obstacles) {
            foreach(var o in obstacles) {
                if(!CanGetToStraight(from, to, o))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Does the <paramref name="obstacle"/> block the straight path <paramref name="from"/> -> <paramref name="to"/>?
        /// </summary>
        protected bool CanGetToStraight(Vector2 from, Vector2 to, Obstacle obstacle) { 
            for(int i = 0; i < obstacle.Shape.Count; i++) {
                var next = (i + 1) % obstacle.Shape.Count;
                if(obstacle.Shape[i] != from
                    && obstacle.Shape[next] != from
                    && obstacle.Shape[i] != to
                    && obstacle.Shape[next] != to
                    && LineIntersectionDecider.HasIntersection(from, to, obstacle.Shape[i], obstacle.Shape[next]))

                    return false;
            }
            return true;
        }

        /// <summary>
        /// Considering it is possible to go <paramref name="from"/> -> <paramref name="to"/> straight
        /// while they are both in the same obstacle, does the path go in the inside of the obstacle?
        /// (false possible only for a) adjecent points in the shape; b) the shape is convex
        /// </summary>
        protected bool IsInsideObstacle(Vector2 from, Vector2 to, Obstacle obstacle) {
            var mid = (from + to) / 2;
            return obstacle.ContainsPoint(mid);
        }
    }
}
