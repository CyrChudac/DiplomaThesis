using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolAlgoLevelGenerator {
	public record Scored<T>(T Value, float Score);
}
