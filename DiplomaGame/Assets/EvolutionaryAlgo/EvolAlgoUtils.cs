using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using EvolAlgoBase;

public class EvolAlgoUtils : IRandomGen
{
    private readonly IRandomGen _randGenerator;
	public EvolAlgoUtils(IRandomGen r) {
        _randGenerator = r;
    }
    public int RandomNormInt(int minBoundary, int middle, int maxBoundary) {
        int min = _randGenerator.RandomInt(minBoundary, middle);
        int max = _randGenerator.RandomInt(middle, maxBoundary);
        return _randGenerator.RandomInt(min, max);
    }

    public float RandomNormFloat(float minBoundary, float middle, float maxBoundary) {
        float min = _randGenerator.RandomFloat(minBoundary, middle);
        float max = _randGenerator.RandomFloat(middle, maxBoundary);
        return _randGenerator.RandomFloat(min, max);
    }
    
    public Vector2 RandomNormVec(float xBound, float yBound) {
        var x = RandomNormFloat(-xBound, 0, xBound);
        var y = RandomNormFloat(-yBound, 0, yBound);
        return new Vector2(x, y);
    }
    public Vector2 RandomVec(Rect rect) {
        var x = RandomFloat(rect.x, rect.x + rect.width);
        var y = RandomFloat(rect.y, rect.y + rect.height);
        return new Vector2(x, y);
    }

    public Vector2 ClampInBounds(Vector2 vec, Rect bounds) {
        return new Vector2(
            Mathf.Clamp(vec.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(vec.y, bounds.yMin, bounds.yMax)
            );
    }

	public float RandomFloat() {
		return _randGenerator.RandomFloat();
	}

	public float RandomFloat(float min, float max) {
		return _randGenerator.RandomFloat(min, max);
	}

	public int RandomInt() {
		return _randGenerator.RandomInt();
	}

	public int RandomInt(int max) {
		return _randGenerator.RandomInt(max);
	}

	public int RandomInt(int min, int max) {
		return _randGenerator.RandomInt(min, max);
	}
}
