using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GameScoring.NavGraphs {
	internal static class GraphExtensions {
		public static List<Vector2> GetPath<T>(this Graph<T> g, Vector2 from, Vector2 to, 
            Func<Vector2, Vector2, bool> canConnect) where T : Node{

            if(canConnect(from, to)) {
                return new List<Vector2>() { to };
            }
            
            SortedDictionary<float, ScoredNode> que = new SortedDictionary<float, ScoredNode>();

            for(int k = 0; k < g.vertices.Count; k++) {
                if(canConnect(from, g.vertices[k].Value)) {
                    float dist = Vector2.Distance(from, g.vertices[k].Value);
                    que.Add(dist, new ScoredNode(dist, 0, null, g.vertices[k].Value));
                }
            }

            ScoredNode? final = null;
            List<Vector2> seen = new List<Vector2>();
            while(que.Any()) { //similar to the algo in ComputeScoredNavGraph function, also same notes
                var score = que.Keys.First();
                var curr = que[score];
                que.Remove(score);
                if(curr.Value == to) {
                    final = curr;
                    break;
                }
                if(seen.Contains(curr.Value))
                    continue;
                var outEdges = g.edges.Where(e => e.First.Value == curr.Value);
                foreach(var edge in outEdges) {
                    var next = new ScoredNode(score + edge.Score, score, curr, edge.Second.Value);
                    que.Add(score + edge.Score, next);
                }
                if(canConnect(curr.Value, to)) {
                    var dist = Vector2.Distance(curr.Value, to);
                    que.Add(score + dist, new ScoredNode(score + dist, score, curr, to));
                }
                seen.Add(curr.Value);
            }
            return ReconstructPath(final);
        }

        public static List<Vector2> GetPath<T>(this Graph<T> g, Vector2 from,
            Func<Vector2, Vector2, bool> canConnect) where T : ScoredNode{

            var to = g.vertices.FirstOrDefault(v => v.Previous == null);
            if(to == null) {
                throw new NotSupportedException($"Graph<{typeof(T)}>: no root found.");
            }

            if(canConnect(from, to.Value)) {
                return new List<Vector2>() { to.Value };
            }

            int index = -1;
            float score = float.MaxValue;
            for(int k = 0; k < g.vertices.Count; k++) {
                if(canConnect(from, g.vertices[k].Value)) {
                    float dist = Vector2.Distance(from, g.vertices[k].Value);
                    var s = dist + g.vertices[k].Score;
                    if(s < score) {
                        score = s;
                        index = k;
                    }
                }
            }

            if(index == -1) {
                throw new NotSupportedException($"Graph<{typeof(T)}>: starting position cannot reach any point");
            }

            var result = ReconstructPath(g.vertices[index]);
            result.Add(to.Value);
            return result;
        }

        static List<Vector2> ReconstructPath(ScoredNode? node) {
            if (node == null) {
                throw new InvalidConstraintException("Path reconstruction not possible");
            }
            List<Vector2> result = new List<Vector2>();
            while(node != null) {
                result.Add(node.Value);
                node = node.Previous;
            }
            result.Reverse();
            return result;
        }
	}
}
