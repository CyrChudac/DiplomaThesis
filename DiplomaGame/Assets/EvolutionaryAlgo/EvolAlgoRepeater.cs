using UnityEngine;

public class EvolAlgoRepeater : MonoBehaviour
{
    public EvolAlgoLevelGenerator evolAlgoGenerator;

    public int seedRangeMin = -1_000_000;
    public int seedRangeMax = 1_000_000;

    public int generateCount = 1;

    [ContextMenu("Generate")]
    public void Generate() {
        for(int i = 0; i < generateCount; i++) {
            evolAlgoGenerator.seed = Random.Range(seedRangeMin, seedRangeMax);
            evolAlgoGenerator.CreateLevels();
        }
    }
}
