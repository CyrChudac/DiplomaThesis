using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs
{

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

        public override string ToString()
        {
            var b = base.ToString();
            b = b[0..(b.Length - 2)] + $";{Score}" + b[b.Length - 1];
            return b;
        }
    }
}
