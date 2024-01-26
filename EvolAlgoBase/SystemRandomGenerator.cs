using System;
using System.Collections.Generic;
using System.Text;

namespace EvolAlgoBase {

    public class SystemRandomGenerator : IRandomGen {
        private readonly System.Random _random;
        public SystemRandomGenerator(System.Random random) {
            _random = random;
        }

	    public float RandomFloat() {
            return (float)_random.NextDouble();
	    }

	    public float RandomFloat(float min, float max)
            => min + RandomFloat() * (max - min);

	    public int RandomInt() {
            return _random.Next();
	    }

	    public int RandomInt(int min, int max) {
            return _random.Next(min, max);
	    }

		public int RandomInt(int max) => RandomInt(0, max);
	}
}