using System;
using System.Collections.Generic;
using System.Text;

namespace GameCreatingCore.GameScoring {
	public interface IGameScorer {
		/// <summary>
		/// Scores the given <paramref name="levelRepresentation"/> within the rules given by <paramref name="staticGameRepresentation"/>.
		/// The higher the score, the better.
		/// </summary>
		/// <returns>the score.</returns>
		float Score(
			StaticSettings.StaticGameRepresentation staticGameRepresentation,
			LevelRepresentation levelRepresentation);
	}
}
