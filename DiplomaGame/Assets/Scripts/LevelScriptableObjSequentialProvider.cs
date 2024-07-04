using GameCreatingCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelScriptableObjSequentialProvider : LevelProvider
{
    [SerializeField] private List<UnityLevelRepresentation> levels;
	[SerializeField] private GameController gameController;

	protected override LevelRepresentation GetLevelInner(bool vocal) {
		return levels[gameController.levelsDone].GetLevelRepresentation();
	}
}
