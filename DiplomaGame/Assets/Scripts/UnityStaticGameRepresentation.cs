using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GameCreatingCore;
using GameCreatingCore.StaticSettings;
using System;
using System.Linq;

public class UnityStaticGameRepresentation : ScriptableObject
{
	public int gameDifficulty = 0;

    public StaticMovementSettings movementSettings;

	public List<UnityViewconeDescription> viewconeDescriptions;

    public GameCreatingCore.StaticSettings.PlayerSettings playerSettings;


	public StaticGameRepresentation ToGameDescription()
		=> new StaticGameRepresentation(
			viewconeDescriptions.ToDictionary(v => v.enemyType, v => v.enemySettings),
			gameDifficulty,
            movementSettings,
            playerSettings);

#if UNITY_EDITOR
    [MenuItem("Assets/Create/Static Game Repr")]
    public static void CreateScriptableObject()
    {
        UnityStaticGameRepresentation asset = ScriptableObject.CreateInstance<UnityStaticGameRepresentation>();

        AssetDatabase.CreateAsset(asset, "Assets/Resources/StaticGameRepresentations/newDifficulty.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }
#endif
}

[Serializable]
public class UnityViewconeDescription {
	public EnemyType enemyType;
	public EnemySettings enemySettings;
}