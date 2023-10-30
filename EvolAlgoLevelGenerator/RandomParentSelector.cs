using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgoLevelGenerator {
	public class RandomParentSelector<T> : IParentSelector<T>{
		private readonly Random _random;
		public RandomParentSelector(Random r) {
			_random = r;
		}
		public List<Scored<T>> GetParents(IEnumerable<Scored<T>> population, int count) {
			var pop = population.ToList();
			List<Scored<T>> result = new List<Scored<T>>();
			for(int i = 0; i < count; i++) {
				result.Add(pop[_random.Next(pop.Count)]);
			}
			return result;
		}
	}
}
