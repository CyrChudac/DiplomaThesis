using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EvolAlgoBase {
	public sealed class Selection<TIndividuum> {
		private readonly float _previousPopSelectionBias;
		private readonly int _elitism;
		private readonly IRandomGen _random;
		private readonly ISelectionMechanism _selectionMechanism;
		public Selection(float previousPopSelectionBias, int elitism, IRandomGen random, ISelectionMechanism selectionMechanism) {
			_elitism = Math.Max(0, elitism); 
			_previousPopSelectionBias = previousPopSelectionBias;
			_random = random;
			_selectionMechanism = selectionMechanism;
		}

		internal List<Scored<TIndividuum>> GetPopulation(
			IEnumerable<Scored<TIndividuum>> offsprings, 
			IEnumerable<Scored<TIndividuum>> previousPop,
			int newPopSize) {

			var all = offsprings
				.Select(p => (p, p.Score))
				.Concat(previousPop
					.Select(p => (p, p.Score * _previousPopSelectionBias)))
				.Select(p => new Scored<Scored<TIndividuum>>(p.p, p.Item2))
				.OrderByDescending(p => p.Score)
				.ToList();

			var result = all.Take(_elitism).Select(p => p.Value)
				.Concat(_selectionMechanism
					.Select(all, newPopSize - _elitism, _random)
					.Select(s => s.Value));

			return result.ToList();
		}
	}
}
