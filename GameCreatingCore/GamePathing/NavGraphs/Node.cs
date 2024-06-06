using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs {
	public class Node
    {
        public Vector2 Position { get; }
        public Node(Vector2 value)
        {
            Position = value;
        }

        public override string ToString() {
            var res = Position.ToString();
            res = $"<{res[1..(res.Length-1)]}>";
            return res ;
        }
    }
}
