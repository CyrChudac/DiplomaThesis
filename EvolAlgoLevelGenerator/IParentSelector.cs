namespace EvolAlgoLevelGenerator {

	public interface IParentSelector<TIndividuum> {
		List<Scored<TIndividuum>> GetParents(IEnumerable<Scored<TIndividuum>> population, int count);
	}
}