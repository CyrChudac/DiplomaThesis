﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

namespace GameCreatingCore.GamePathing
{
    internal static class GraphExtensions {
		public static List<Vector2>? GetPath<T>(this Graph<T> g, Vector2 from, Vector2 to, 
            Func<Vector2, Vector2, bool> canConnect) where T : Node{

            if(canConnect(from, to)) {
                return new List<Vector2>() { to };
            }
            
            PriorityQueue<float, (ScoredNode, T)> que = new PriorityQueue<float, (ScoredNode, T)>();

            for(int k = 0; k < g.vertices.Count; k++) {
                if(canConnect(from, g.vertices[k].Position)) {
                    float dist = Vector2.Distance(from, g.vertices[k].Position);
                    que.Enqueue(dist, (new ScoredNode(dist, 0, null, g.vertices[k].Position), g.vertices[k]));
                }
            }

            ScoredNode? final = null;
            List<Vector2> seen = new List<Vector2>();
            int iterations = 0;
            while(que.Any()) { //similar to the algo in ComputeScoredNavGraph function, also same notes
                (var curr, var currPre) = que.DequeueMin();
                var score = curr.Score;
                if(curr.Position == to) {
                    final = curr;
                    break;
                }
                if(seen.Contains(curr.Position))
                    continue;
                if(iterations++ > g.vertices.Count + 5)
                    throw new Exception("Possible infinite loop, investigate.");
                var outEdges = g.GetOutEdges(currPre);
                foreach(var (second, edgeInfo) in outEdges) {
                    var next = new ScoredNode(score + edgeInfo.Score, score, curr, second.Position);
                    que.Enqueue(score + edgeInfo.Score, (next, second));
                }
                if(canConnect(curr.Position, to)) {
                    var dist = Vector2.Distance(curr.Position, to);
                    //the null in the following line signifies that node for the final destination doesn't exist
                    //however it doen't matter, because it is never used 
                    que.Enqueue(score + dist, (new ScoredNode(score + dist, score, curr, to), null!));
                }
                seen.Add(curr.Position);
            }
            if(final == null)
                return null;
            var result = ReconstructPath(final);
            result.Reverse();
            return result;
        }
        
        public static List<Vector2>? GetPath<T>(this Graph<T> g, Vector2 from,
            Func<Vector2, Vector2, bool> canConnect) where T : ScoredNode{
            var best = GetBestStraight(g, from, canConnect);
            if(best == null) 
                return null;
            return ReconstructPath(best);
        }
        public static float? GetPathScore<T>(this Graph<T> g, Vector2 from,
            Func<Vector2, Vector2, bool> canConnect) where T : ScoredNode{
            var best = GetBestStraight(g, from, canConnect);
            if(best == null) 
                return null;
            return best.Score + Vector2.Distance(from, best.Position);
        }

        private static T? GetBestStraight<T>(this Graph<T> g, Vector2 from,
            Func<Vector2, Vector2, bool> canConnect) where T : ScoredNode { 

            var to = g.vertices.FirstOrDefault(v => v.Previous == null);
            if(to == null) {
                throw new NotSupportedException($"Graph<{typeof(T)}>: no root found.");
            }

            if(canConnect(from, to.Position)) {
                return to;
            }

            int index = -1;
            float score = float.MaxValue;
            for(int k = 0; k < g.vertices.Count; k++) {
                if(canConnect(from, g.vertices[k].Position)) {
                    float dist = Vector2.Distance(from, g.vertices[k].Position);
                    var s = dist + g.vertices[k].Score;
                    if(s < score) {
                        score = s;
                        index = k;
                    }
                }
            }

            if(index == -1) {
                //starting position is in a closed space that cannot reach any point
                return null;
            }

            return g.vertices[index];
        }

        static List<Vector2> ReconstructPath(ScoredNode? node) {
            if (node == null) {
                throw new InvalidConstraintException("Path reconstruction not possible");
            }
            List<Vector2> result = new List<Vector2>();
            int iterations = 0;
            while(node != null) {
                result.Add(node.Position);
                node = node.Previous;
                if(iterations++ > 1000)
                    throw new Exception("Possible infinite loop, investigate.");
            }
            return result;
        }
	}
}
