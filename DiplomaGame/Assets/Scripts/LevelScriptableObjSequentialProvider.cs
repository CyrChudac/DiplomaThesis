using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore.LevelRepresentationData;

public class LevelScriptableObjSequentialProvider : LevelProvider
{
    [SerializeField] private List<UnityLevelRepresentation> levels;
	[SerializeField] private GameController gameController;

	protected override LevelRepresentation GetLevelInner(bool vocal) {
		return levels[gameController.levelsDone].GetLevelRepresentation();
	}
}
