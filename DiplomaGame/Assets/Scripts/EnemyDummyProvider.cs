using GameCreatingCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDummyProvider : EnemyProvider
{
	[SerializeField]
	private EnemyController enemy;
	public override EnemyController GetEnemy(EnemyType type) {
		var result = Instantiate(enemy);
		return result;
	}
}
