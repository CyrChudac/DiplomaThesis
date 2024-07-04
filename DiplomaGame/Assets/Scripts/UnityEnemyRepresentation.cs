using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;

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

[System.Serializable]
public class UnityCommandRepr {
	
}