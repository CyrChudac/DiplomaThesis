using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GameScoring.NavGraphs {
	
    internal class ScoredTypedNode : ScoredNode
    {
        public NodeType NodeType { get; }
        public ScoredTypedNode(float score, float previousScore, ScoredNode? previous, Vector2 value,
            NodeType type) : base(score, previousScore, previous, value)
        {
            NodeType = type;
        }

    }
}
