using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;

public abstract class EnemyProvider : MonoBehaviour
{
	public abstract EnemyController GetEnemy(EnemyType type);
}
