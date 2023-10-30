using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgoLevelGenerator {
	public interface ISelectionMechanism {
		IList<Scored<T>> Select<T>(IEnumerable<Scored<T>> from, int count, Random random);
	}
}
