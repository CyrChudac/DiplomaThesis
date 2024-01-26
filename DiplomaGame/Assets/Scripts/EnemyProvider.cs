using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;

public abstract class EnemyProvider : MonoBehaviour
{
	public abstract PathedGameObject GetEnemy(EnemyType type, Path? path);
}
