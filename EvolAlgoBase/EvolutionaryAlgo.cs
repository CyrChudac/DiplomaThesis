using System.Collections.Generic;
using System.Linq;
using EvolAlgoBase.Exceptions;

namespace EvolAlgoBase {
	public class EvolutionaryAlgo<TIndividuum> {

		private readonly EvolAlgoParameters<TIndividuum> _parameters;
		private readonly bool _quiet;
		
		public EvolutionaryAlgo(EvolAlgoParameters<TIndividuum> parameters, bool quiet = false) {
			_parameters = parameters;
			_quiet = quiet;
		}

		public IEnumerable<TIndividuum> Run() {
			double timeStart = GetTime();
			int generations = 0;
			List<Scored<TIndividuum>> population
				= _parameters.Initial
				.Take(_parameters.PopulationSize)
				.Select(i => new Scored<TIndividuum>(i, _parameters.ScoreFunc(i)))
				.ToList();
			if(population.Count() < _parameters.PopulationSize) {
				throw new InitialPopCountException(_parameters.PopulationSize, population.Count());
			}
			CurrentGeneratingState state = new CurrentGeneratingState(0, 0, 0);
			while(! _parameters.EndingPredicate(state)) {

				var iterations = _parameters.OffspringCount * 1.0f / _parameters.Breeding.ChildCount;
				_parameters.ParentSelector.InitializeNewGeneration();
				var offsprings =
					Enumerable.Range(0, (int)System.Math.Ceiling(iterations))
					.Select(i => _parameters.ParentSelector.GetParents(
							population,
							_parameters.Breeding.ParentCount
						))
					.Select(p => _parameters.Breeding.Breed(p.Select(i => i.Value).ToList()))
					.SelectMany(x => x)
					.Select(ind =>  _parameters.MutateFunc.Invoke(ind))
					.Select(o => new Scored<TIndividuum>(o, _parameters.ScoreFunc(o)))
					.ToList();
				population = _parameters.Selection
					.GetPopulation(offsprings, population, _parameters.PopulationSize);
				
				if(!_quiet) {
					_parameters.Plotter.Invoke(population);
				}

				state = new CurrentGeneratingState(
					++generations,
					GetTime() - timeStart,
					population.Select(p => p.Score).Max());

			}
			return population.OrderByDescending(p => p.Score).Select(p => p.Value);
		}

		private double GetTime() => System.DateTime.Now.Ticks;

		public override string ToString() {
			return $"{nameof(EvolutionaryAlgo<TIndividuum>)}";
		}
	}
}