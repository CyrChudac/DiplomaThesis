using GameCreatingCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDummyProvider : EnemyProvider
{
	[SerializeField]
	private PathedGameObject enemy;
	public override PathedGameObject GetEnemy(EnemyType type, Path path) {
		var result = Instantiate(enemy);
		result.SetPath(path);
		return result;
	}
}
