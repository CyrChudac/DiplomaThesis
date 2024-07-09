using UnityEngine;
using GameCreatingCore.LevelRepresentationData;

[System.Serializable]
public class UnityEnemyRepresentation
{
	public Vector2 position;
	public float rotation;
	public EnemyType type;

	//public Enemy ToEnemy() {
	//	return new Enemy()
	//}
}

[System.Serializable]
public class UnityPathRepr {
	public bool cyclic;
}
