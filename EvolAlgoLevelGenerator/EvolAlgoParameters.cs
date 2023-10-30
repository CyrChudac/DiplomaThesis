
namespace EvolAlgoLevelGenerator {
	
	public record EvolAlgoParameters<TIndividuum>(
		int PopulationSize,
		int OffspringCount,
		IParentSelector<TIndividuum> ParentSelector,
		EvolutionBreeding<TIndividuum> Breeding,
		MutateFunc<TIndividuum> MutateFunc,
		Selection<TIndividuum> Selection,
		EndingPredicate EndingPredicate,
		IEnumerable<TIndividuum> Initial,
		ScoringFunc<TIndividuum> ScoreFunc,
		EvolutionaryPlotter<TIndividuum> Plotter
	);
}