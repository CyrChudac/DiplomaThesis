using System;
using System.Collections.Generic;
using System.Text;

namespace EvolAlgoBase {
	public interface IRandomGen {
        float RandomFloat();
        float RandomFloat(float min, float max);
        int RandomInt();
        int RandomInt(int max);
        int RandomInt(int min, int max);
    }
}
