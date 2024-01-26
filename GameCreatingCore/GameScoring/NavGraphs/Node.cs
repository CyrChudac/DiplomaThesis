using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GameScoring.NavGraphs {
	internal class Node
    {
        public Vector2 Value { get; }
        public Node(Vector2 value)
        {
            Value = value;
        }
    }
}
