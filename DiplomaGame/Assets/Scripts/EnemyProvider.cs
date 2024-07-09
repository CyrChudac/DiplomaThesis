using UnityEngine;
using GameCreatingCore.LevelRepresentationData;

public abstract class EnemyProvider : MonoBehaviour
{
	public abstract EnemyObject GetEnemy(EnemyType type);
}
