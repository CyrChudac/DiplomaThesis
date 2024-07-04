using GameCreatingCore.GamePathing.GameActions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones {
    internal class ViewconeNavGraphDataHolder {

		public IReadOnlyList<EdgePlaceHolder> Edges { get; }
        public IReadOnlyList<ViewActionedNode> Vertices { get; }
        public IReadOnlyList<(Viewcone Viewcone, IReadOnlyList<int> Indices)> Viewcones { get; }
        public IReadOnlyList<int> GoalIndices { get; }
        public IReadOnlyList<int> PickupIndices { get; }
        public IReadOnlyList<int> UseSkillIndices { get; }
		public ViewconeNavGraphDataHolder(IReadOnlyList<EdgePlaceHolder> edges, IReadOnlyList<ViewActionedNode> vertices,
			IReadOnlyList<(Viewcone, IReadOnlyList<int> Indices)> viewcones, IReadOnlyList<int> goalIndices,
			IReadOnlyList<int> pickupIndices, IReadOnlyList<int> useSkillIndices) {
			this.Edges = edges;
			this.Vertices = vertices;
			this.Viewcones = viewcones;
			GoalIndices = goalIndices;
			PickupIndices = pickupIndices;
			UseSkillIndices = useSkillIndices;
		}

	}
	
    internal class EdgePlaceHolder  {

		public ViewMidEdgeInfo Info { get; }
        public int FirstIndex { get; }
        public int SecondIndex { get; }

        public int? ViewconeIndex { get; }
            
		public EdgePlaceHolder(int firstIndex, int secondIndex, ViewMidEdgeInfo info, int? viewconeIndex) {
			Info = info;
			FirstIndex = firstIndex;
			SecondIndex = secondIndex;
            ViewconeIndex = viewconeIndex;
			if(firstIndex == secondIndex) {
				throw new NotSupportedException($"This program does not support loop edges ({firstIndex}->{firstIndex})");
			}
			if(!FloatEquality.AreEqual(info.AlertingIncrease, 0)) {
				if(!viewconeIndex.HasValue) {
					throw new Exception($"Edge with an alerting ratio should always be in a viewcone. " +
						$"Edge is {firstIndex} -> {secondIndex} with alertingIncrease {info.AlertingIncrease}");
				}
			}
		}

		public override string ToString() {
			return $"{this.GetType().Name}: {FirstIndex} -> {SecondIndex}; A:{Info.AlertingIncrease}";
		}
	}

	internal class ViewActionedNode : ViewNode {
		public IGameAction? NodeAction { get; }

		public ViewActionedNode(Vector2 value, float? distanceFromEnemy, int? enemyIndex, IGameAction? action, bool isMiddleNode = false) 
			: base(value, distanceFromEnemy, enemyIndex, isMiddleNode) {
			NodeAction = action;
		}
	}
}
