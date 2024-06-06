using UnityEngine;
using System.Collections.Generic;
using GameCreatingCore.GamePathing;

namespace GameCreatingCore
{
    [System.Serializable]
	public sealed class Path {
		[SerializeField]
		public bool Cyclic;
		[SerializeField]
		[SerializeReference]
		public List<PatrolCommand> Commands;

		public Path(bool cyclic, List<PatrolCommand> commands) {
			Cyclic = cyclic;
			Commands = commands;
		}

		public override string ToString() {
			var s = $"{nameof(Path)}: {Commands.Count}";
			if(Cyclic)
				s += "; C";
			return s;
		}
	}
}