using GameCreatingCore.LevelRepresentationData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDummyProvider : EnemyProvider
{
	[SerializeField]
	private EnemyObject enemy;
	public override EnemyObject GetEnemy(EnemyType type) {
		var result = Instantiate(enemy);
		return result;
	}
}
