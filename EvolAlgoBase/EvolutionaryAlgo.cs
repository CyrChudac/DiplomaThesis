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
			//create initial population
			List<Scored<TIndividuum>> population
				= _parameters.Initial
				.Take(_parameters.PopulationSize)
				.Select(i => new Scored<TIndividuum>(i, _parameters.ScoreFunc(i)))
				.ToList();
			if(population.Count() < _parameters.PopulationSize) {
				throw new InitialPopCountException(_parameters.PopulationSize, population.Count());
			}
			//every generation we create generating state which we supply to the ending predicate
			CurrentGeneratingState state = new CurrentGeneratingState(0, 0, 0);
			while(! _parameters.EndingPredicate(state)) {

				var iterations = _parameters.OffspringCount * 1.0f / _parameters.Breeding.ChildCount;
				_parameters.ParentSelector.InitializeNewGeneration();
				var offsprings =
					Enumerable.Range(0, (int)System.Math.Ceiling(iterations))
					//from the population we select parents
					.Select(i => _parameters.ParentSelector.GetParents(
							population,
							_parameters.Breeding.ParentCount
						))
					//we create ofsprings (contains crossover)
					.Select(p => _parameters.Breeding.Breed(p.Select(i => i.Value).ToList()))
					.SelectMany(x => x)
					//mutate them
					.Select(ind =>  _parameters.MutateFunc.Invoke(ind))
					//and finally score them
					.Select(o => new Scored<TIndividuum>(o, _parameters.ScoreFunc(o)))
					.ToList();
				population = _parameters.Selection
					//then we select the new population
					.GetPopulation(offsprings, population, _parameters.PopulationSize);
				
				if(!_quiet) {
					//and report the population to the user
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