using System;
using System.Collections.Generic;
using System.Linq;

namespace EvolAlgoBase {
	public class RuletWheelSelection : ISelectionMechanism{
		private readonly float scoreAdd;

		public RuletWheelSelection(float scoreAdd = 0f) {
			this.scoreAdd = scoreAdd;
		}

		/// <summary>
		/// Converts float scores to integers to avoid float precision errors.
		/// </summary>
		public IList<Scored<T>> Select<T>(IEnumerable<Scored<T>> from, int count, IRandomGen random) {

			var max = from.Max(f => f.Score) + scoreAdd;
			var elemCount = from.Count();
			int i = 1;
			int mult;
			bool isLessThenIntMax;
			if(max * elemCount < int.MaxValue) {
				isLessThenIntMax = true;
				while(i < max * elemCount)
					i *= 10;
				mult = int.MaxValue / i;
			} else {
				isLessThenIntMax = false;
				while(int.MaxValue < max * elemCount / i)
					i *= 10;
				mult = i;

			}

			var ordered = from
				.OrderByDescending(p => Scorify(p.Score, mult, isLessThenIntMax));

			int sum = ordered.Sum(p => Scorify(p.Score, mult, isLessThenIntMax));

			return Enumerable.Range(0, count)
				.Select(i => random.RandomFloat() * sum)
				.Select((r) => {
					float curr = 0;
					foreach(var a in ordered) {
						curr += Scorify(a.Score, mult, isLessThenIntMax);
						if(curr >= r) {
							return a;
						}
					}
					throw new System.Exception($"{nameof(RuletWheelSelection)}: this code should never be reached. " +
						$"(sum={sum}; desired value={r}; innerSum={curr})");
				})
				.ToList();
		}

		private int Scorify(float f, int mult, bool isLessThenIntMax)
			=> isLessThenIntMax ? (int)((f+scoreAdd) * mult) : (int)((f + scoreAdd) / mult);
	}
}
