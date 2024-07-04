using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;
using UnityEditor;
using System.Linq;
using UnityEngine.SocialPlatforms;

public class LevelScriptableObjRandomProvider : LevelProvider
{
	[SerializeField]
	private string FolderOrFilePath;
	[Tooltip("If a given specific level is the one to create, writes the filename here. Leave it empty otherwise.")]
	[SerializeField]
	private string specificFile = "";
	private readonly string outerDirectory = "Assets/Resources/";
	[SerializeField]
	private int seed = 42;
    [SerializeField]
    private bool showMessage = true;
	protected override LevelRepresentation GetLevelInner(bool vocal) { 
        int seed = Random.Range(0, int.MaxValue);
        if(vocal && this.showMessage)
            Debug.Log($"Initializing level using seed " + seed);
		var r = new System.Random(seed);
		UnityLevelRepresentation res;
		if(AssetDatabase.IsValidFolder($"{outerDirectory}{FolderOrFilePath}")) {
			var all = Resources.LoadAll<UnityLevelRepresentation>(FolderOrFilePath);
			if(all.Length == 0) {
				throw new System.ArgumentException(
					$"Directory \"{outerDirectory}{FolderOrFilePath}\" has no assets in it.");
			}
			if(specificFile.Length == 0) {
				res = all[r.Next(all.Length)];
				Debug.Log($"Initializing random level: '{res.name}'");
			} else
				res = all.First(a => a.name == specificFile);
		}else {
			var asset = Resources.Load<UnityLevelRepresentation>(FolderOrFilePath);
			if(asset != null) {
				res = asset;
			} else {
				throw new System.ArgumentException(
					$"Path \"{outerDirectory}{FolderOrFilePath}\" is not a folder nor an asset file. Level load failed.");
			}
		}
		return res.GetLevelRepresentation();
	}
}
