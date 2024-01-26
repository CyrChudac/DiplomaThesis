using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgoBase {
	public delegate void EvolutionaryPlotter<T>(IEnumerable<Scored<T>> generation);
}
