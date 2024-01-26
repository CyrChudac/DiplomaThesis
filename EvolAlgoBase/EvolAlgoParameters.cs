using System.Collections.Generic;

namespace EvolAlgoBase {

	public class EvolAlgoParameters<TIndividuum> {
		public virtual int PopulationSize { get; }
		public virtual int OffspringCount { get; }
		public virtual IParentSelector<TIndividuum> ParentSelector { get; }
		public virtual EvolutionBreeding<TIndividuum> Breeding { get; }
		public virtual Mutator<TIndividuum> MutateFunc { get; }
		public virtual Selection<TIndividuum> Selection { get; }
		public virtual EndingPredicate EndingPredicate { get; }
		public virtual IEnumerable<TIndividuum> Initial { get; }
		public virtual ScoringFunc<TIndividuum> ScoreFunc { get; }
		public virtual EvolutionaryPlotter<TIndividuum> Plotter { get; }
		public EvolAlgoParameters(int populationSize, int offspringCount, IParentSelector<TIndividuum> parentSelector, EvolutionBreeding<TIndividuum> breeding, Mutator<TIndividuum> mutateFunc, Selection<TIndividuum> selection, EndingPredicate endingPredicate, IEnumerable<TIndividuum> initial, ScoringFunc<TIndividuum> scoreFunc, EvolutionaryPlotter<TIndividuum> plotter) {
			PopulationSize = populationSize;
			OffspringCount = offspringCount;
			ParentSelector = parentSelector;
			Breeding = breeding;
			MutateFunc = mutateFunc;
			Selection = selection;
			EndingPredicate = endingPredicate;
			Initial = initial;
			ScoreFunc = scoreFunc;
			Plotter = plotter;
		}
	}
}