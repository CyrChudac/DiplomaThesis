using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GameScoring.NavGraphs {
	
    internal class ScoredNode : Node
    {
        public ScoredNode? Previous { get; }
        public float PreviousScore { get; }
        public float Score { get; }
        public ScoredNode(float score, float previousScore, ScoredNode? previous, Vector2 value) : base(value)
        {
            Score = score;
            Previous = previous;
            PreviousScore = previousScore;
        }
    }
}
