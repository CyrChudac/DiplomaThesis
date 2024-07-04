using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GameCreatingCore;


public class ObstacleMaterial : ScriptableObject
{
	public WalkObstacleEffect FriendlyWalkEffect;
	public WalkObstacleEffect EnemyWalkEffect;

	public VisionObstacleEffect EnemyVision;

	public Material Material;

#if UNITY_EDITOR
    [MenuItem("Assets/Create/Obstacle Material")]
    public static void CreateScriptableObject()
    {
        ObstacleMaterial asset = ScriptableObject.CreateInstance<ObstacleMaterial>();

        AssetDatabase.CreateAsset(asset, "Assets/Resources/ObstacleMaterials/newObstacleMaterial.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }
#endif
}
