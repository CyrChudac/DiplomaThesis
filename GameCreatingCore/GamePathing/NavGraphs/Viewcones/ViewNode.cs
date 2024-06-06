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
    }
}
