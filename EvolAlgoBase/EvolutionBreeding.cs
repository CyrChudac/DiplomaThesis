using System;
using System.Collections.Generic;

namespace EvolAlgoBase {

	public abstract class EvolutionBreeding<TIndividuum> {
		public abstract int ParentCount { get; }
		public abstract int ChildCount { get; }
		protected abstract float CrossoverProb { get; }

		protected IRandomGen Random { get; }

		public EvolutionBreeding(IRandomGen random) { 
			Random = random;
		}

		public IList<TIndividuum> Breed(IList<TIndividuum> parents) {
			if(parents.Count != ParentCount) {
				throw new ParentCountException(ParentCount, parents.Count);
			}
			IList<TIndividuum> result;
			if(Random.RandomFloat() < CrossoverProb) {
				result = Crossover(parents);
			}else {
				result = WithoutCrossover(parents);
			}
			if(result.Count != ChildCount) {
				throw new ChildrenCountException(ChildCount, result.Count);
			}
			return result;
		}

		protected abstract IList<TIndividuum> Crossover(IList<TIndividuum> parents);
		protected abstract IList<TIndividuum> WithoutCrossover(IList<TIndividuum> parents);
	}
}