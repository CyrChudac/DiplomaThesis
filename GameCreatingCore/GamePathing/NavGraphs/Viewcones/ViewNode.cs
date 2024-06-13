using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
	class ViewNode : Node
    {
        public bool IsMiddleNode { get; }
        public ViewNode(Vector2 value, bool isMiddleNode = false) : base(value)
        {
            IsMiddleNode = isMiddleNode;
        }

		public override int GetHashCode() {
			return base.GetHashCode() * 2 + (IsMiddleNode ? 1 : 0);
		}

        public override bool Equals(object obj) {
            return base.Equals(obj) && (obj is ViewNode) && (((ViewNode)obj).IsMiddleNode == IsMiddleNode);
        }
	}
}
