using System;
using System.Collections.Generic;
using System.Text;

namespace EvolAlgoBase {
	public interface ISelectionMechanism {
		IList<Scored<T>> Select<T>(IEnumerable<Scored<T>> from, int count, IRandomGen random);
	}
}
