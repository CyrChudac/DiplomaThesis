
using System;

namespace EvolAlgoLevelGenerator {
	public class RuletWheelSelection : ISelectionMechanism{

		public IList<Scored<T>> Select<T>(IEnumerable<Scored<T>> from, int count, Random random) {

			var ordered = from
				.OrderByDescending(p => p.Score);

			var max = ordered.Sum(p => p.Score);

			return Enumerable.Range(0, count)
				.Select(i => random.NextDouble() * max)
				.Select((r) => {
					float curr = 0;
					foreach(var a in ordered) {
						curr += a.Score;
						if(curr >= r) {
							return a;
						}
					}
					throw new Exception($"{nameof(RuletWheelSelection)}: this code should never be reached.");
				})
				.ToList();
		}
	}
}
