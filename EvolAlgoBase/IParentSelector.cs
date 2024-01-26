using System.Collections.Generic;
namespace EvolAlgoBase {

	public interface IParentSelector<TIndividuum> {
		List<Scored<TIndividuum>> GetParents(List<Scored<TIndividuum>> population, int count);
		void InitializeNewGeneration();
	}
}