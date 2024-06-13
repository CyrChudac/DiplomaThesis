using GameCreatingCore.GamePathing.GameActions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones
{

    public class ScoredActionedNode : ScoredNode
    {
        public IGameAction? NodeAction { get; }
        public ScoredActionedNode(float score, float previousScore, ScoredNode? previous, Vector2 value,
            IGameAction? action) : base(score, previousScore, previous, value)
        {
            NodeAction = action;
        }

        public override string ToString()
        {
            var b = base.ToString();
            b = b[0..(b.Length - 2)] + $"; {NodeAction?.GetType().Name}" + b[b.Length - 1];
            return b;
        }
    }
}
