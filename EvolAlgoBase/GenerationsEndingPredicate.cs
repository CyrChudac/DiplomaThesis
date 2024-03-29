﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgoBase {

	public static class GenerationsEndingPredicate {
		public static EndingPredicate Get(int maxGenerations)
			=> (state) => state.Generations >= maxGenerations;
	}
}
