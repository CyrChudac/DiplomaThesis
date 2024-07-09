using UnityEngine;
using GameCreatingCore.GamePathing;

namespace GameCreatingCore.LevelSolving.Viewcones {
	class ViewNode : Node
    {
        public bool IsMiddleNode { get; }
        public float? DistanceFromEnemy { get; }
        public int? EnemyIndex { get; }
        public ViewNode(Vector2 value, ViewconeGraph viewconeGraph, bool isMiddleNode = false) 
            : this(value, Vector2.Distance(viewconeGraph.Viewcone.StartPos, value), viewconeGraph.Index, isMiddleNode) { }

        public ViewNode(Vector2 value, float? distanceFromEnemy, int? enemyIndex, bool isMiddleNode = false) : base(value)
        {
            IsMiddleNode = isMiddleNode;
            DistanceFromEnemy = distanceFromEnemy;
            EnemyIndex = enemyIndex;
        }

		public override int GetHashCode() {
			return base.GetHashCode() * 2 + (IsMiddleNode ? 1 : 0);
		}

        public override bool Equals(object obj) {
            return base.Equals(obj) && (obj is ViewNode) && (((ViewNode)obj).IsMiddleNode == IsMiddleNode);
        }
	}
}
