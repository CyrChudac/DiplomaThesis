using GameCreatingCore.GamePathing.GameActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	internal class GraphScorer {

        readonly StaticNavGraph staticNavGraph;

		public GraphScorer(StaticNavGraph staticNavGraph) {
			this.staticNavGraph = staticNavGraph;
		}

		public GraphWithViewcones ScoreAndCombine(ViewconeNavGraphDataHolder data) {
            var goalScore = EvaluateData(data.GoalIndices, data, false);
            var skillScore = EvaluateData(data.UseSkillIndices, data, true);
            var pickScoore = EvaluateData(data.PickupIndices, data, true);
            return CombineScores(data, goalScore, pickScoore, skillScore);
        }

        /// <summary>
        /// Given some <paramref name="startingIndices"/> from <paramref name="data"/> finds for each node index
        /// how far from
        /// </summary>
        /// <param name="startingIndices"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<ScoreHolder?> EvaluateData(IReadOnlyList<int> startingIndices, ViewconeNavGraphDataHolder data,
            bool addStaticDistance) {
            var nodes = Enumerable.Repeat<ScoreHolder?>(null, data.Vertices.Count).ToList();
            List<int> seen = new List<int>();
            var que = new PriorityQueue<float, (int? Previous, float Alert, int Index)>();
            foreach(var si in startingIndices) {
                var score = staticNavGraph.GetEnemylessPlayerPathLength(data.Vertices[si].Position);
                if(!score.HasValue)
                    continue;
                que.Enqueue(score.Value, (null, 0, si));
            }
            int iterations = 0;
            while(que.Any()) {
                var curr = que.DequeueMinWithKey();
                var score = curr.Key;
                var currIndex = curr.Value.Index;
                if(seen.Contains(currIndex))
                    continue;
                iterations++;
                if(iterations > data.Vertices.Count + 5)
                    throw new Exception("Possible infinite loop, investigate.");
                seen.Add(currIndex);
                nodes[currIndex] = new ScoreHolder(curr.Value.Previous, score);
                //we are asking if it's possible to get to this state from the next one, so e.SecondIndex
                foreach(var e in data.Edges.Select((e, i) => (e, i)).Where(e => e.e.SecondIndex == currIndex)) {
                    if(e.e.Info.Traversable && IsAlertOk(e.e, data, curr.Value.Alert, out var alert)) { 
                        que.Enqueue(score + ScoreEdge(e.e.Info), (currIndex, alert, e.e.FirstIndex));
                    }
                }
            }
            return nodes;
        }

        private bool IsAlertOk(EdgePlaceHolder edge, ViewconeNavGraphDataHolder data, float currAlert, out float newAlert) {
            newAlert = 0;
            if(!FloatEquality.AreEqual(edge.Info.AlertingIncrease, 0)) {
                newAlert = currAlert + edge.Info.AlertingIncrease;
                if(!edge.ViewconeIndex.HasValue) {
                    throw new Exception($"Edge with alerting ratio > 0 ({edge.Info.AlertingIncrease}) must have " +
                        $"a viewcone index.(edge=\"{edge.FirstIndex} -> {edge.SecondIndex}\")");
                }
                if(!data.Vertices[edge.SecondIndex].DistanceFromEnemy.HasValue) {
                    throw new Exception($"If edge with alerting ratio > 0 ends in a node N, it must have " +
                        $"distance from enemy computed.(edge=\"{edge.FirstIndex} -> {edge.SecondIndex}\")");
                }
                return data.Viewcones[edge.ViewconeIndex!.Value].Viewcone
                    .IsAlertOkForDistance(data.Vertices[edge.SecondIndex].DistanceFromEnemy!.Value, newAlert);
            }
            return true;
        }

        private GraphWithViewcones CombineScores(ViewconeNavGraphDataHolder data, List<ScoreHolder?> goalScores,
            List<ScoreHolder?> pickupScores, List<ScoreHolder?> skillUseScores) {

            var nodes = new List<ScoredActionedNode?>(data.Vertices.Count);
            Func<int, bool> nodeRemovePredicate = 
                i => (goalScores[i] == null && pickupScores[i] == null && skillUseScores[i] == null);
            var nullsBefore = HowManyBefore(Enumerable.Range(0, data.Vertices.Count), nodeRemovePredicate);
            var nulllessGoalScores = ReduceByNulls(goalScores, nullsBefore);
            var nulllessPickupScores = ReduceByNulls(pickupScores, nullsBefore);
            var nulllessSkillUseScores = ReduceByNulls(skillUseScores, nullsBefore);

            for(int i = 0; i < data.Vertices.Count; i++) {
                ScoredActionedNode? n;
                if(nodeRemovePredicate(i)) {
                    n = null;
                } else {
                    var g = nulllessGoalScores[i];
                    var s = nulllessSkillUseScores[i];
                    var p = nulllessPickupScores[i];
                    n = new ScoredActionedNode(data.Vertices[i].Position, data.Vertices[i].NodeAction, g, s, p, 
                        data.Vertices[i].DistanceFromEnemy, data.Vertices[i].EnemyIndex);
                }
                nodes.Add(n);
            }
            //from here their score previous (int index) do not actually correspond to nodes[i]
            //we need to remove the null values... which we do after we assign edges and viewcones

            var finalEdges = new List<Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>>();

            for(int i = 0; i < data.Edges.Count; i++) {
                var first = nodes[data.Edges[i].FirstIndex];
                var second = nodes[data.Edges[i].SecondIndex];
                if(first != null && second != null) {
                    var score = ScoreEdge(data.Edges[i].Info);
                    var info = new GraphWithViewconeEdgeInfo(score, 
                        data.Edges[i].ViewconeIndex, 
                        data.Edges[i].Info.AlertingIncrease,
                        !data.Edges[i].Info.IsInMidViewcone);
                    finalEdges.Add(new Edge<ScoredActionedNode, GraphWithViewconeEdgeInfo>(first, second, info));
                }
            }
            
            List<(Viewcone, IReadOnlyList<ScoredActionedNode>)> viewcones
                = new List<(Viewcone, IReadOnlyList<ScoredActionedNode>)>();
            foreach(var v in data.Viewcones) {
                List<ScoredActionedNode> list = v.Indices
                    .Select(i => nodes[i])
                    .Where(n => n != null)
                    .Cast<ScoredActionedNode>()
                    .ToList();
                viewcones.Add((v.Viewcone, list));
            }

            var finalNodes = nodes
                .Where(n => n != null)
                .Cast<ScoredActionedNode>()
                .ToList();

            return new GraphWithViewcones(finalNodes, finalEdges, viewcones, true);
        }

        float ScoreEdge(ViewMidEdgeInfo info) {
            var s = info.Score;
            if(info.IsInMidViewcone || info.IsEdgeOfRemoved) {
                //we need a fast a way that makes edges inside viewcone always have bigger score than the rest
                //hence even though it is ugly we just add a really big number
                s += ScoreAddion;
            }
            return s;
        }

        public const float ScoreAddion = 1000; //100_000_000;

        private List<int> HowManyBefore(IEnumerable<int> indices, Func<int, bool> predicateFunc){
            List<int> result = new List<int>();
            int curr = 0;
            foreach(var i in indices) {
                if(predicateFunc(i))
                    curr++;
                result.Add(curr);
            }
            return result;
        }

        private List<ScoreHolder?> ReduceByNulls(List<ScoreHolder?> scores, List<int> nullsBefore){
            List<ScoreHolder?> result = new List<ScoreHolder?>(scores.Count);
            for(int i = 0; i < scores.Count; i++) {
                if(scores[i] != null) {
                    if(!scores[i]!.Previous.HasValue)
                        result.Add(scores[i]);
                    else
                        result.Add(new ScoreHolder(scores[i]!.Previous - nullsBefore[scores[i]!.Previous!.Value], scores[i]!.Score));
                } else {
                    result.Add(null);
                }
            }
            return result;
        } 
	}
}
