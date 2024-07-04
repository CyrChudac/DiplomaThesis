using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GamePathing.NavGraphs {
	public class Edge<T, EI> where T : Node where EI : EdgeInfo
    {
        public T First { get; }
        public T Second { get; }
        public EI EdgeInfo { get; }
        public float Score => EdgeInfo.Score;

        public Edge(T first, T second, EI info)
        {
            First = first;
            Second = second;
            EdgeInfo = info;
            if (FloatEquality.AreEqual(first.Position, second.Position)) {
				throw new NotSupportedException($"This program does not support loop edges ({first}->{second})");
            }
        }

		public override string ToString() {
			return $"{First} -> {Second}";
		}
	}

	public class Edge<T> : Edge<T, EdgeInfo> where T : Node {
		public Edge(T first, T second, EdgeInfo info) : base(first, second, info) {
		}
	}

	public class EdgeInfo {
        public float Score { get; }

        public EdgeInfo(float score) {
            Score = score;
        }

        public static implicit operator EdgeInfo(float f)
            => new EdgeInfo(f);
    }
}
