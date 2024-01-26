using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;
using UnityEditor;

public class LevelScriptableObjProvider : LevelCreatorProvider, ILevelCreator
{
	[SerializeField]
	private string FolderOrFilePath;
	private readonly string outerDirectory = "Assets/Resources/";
	public override ILevelCreator GetLevelCreator() {
		return this;
	}

	public LevelRepresentation CreateLevel(int seed) {
		var r = new System.Random(seed);
		UnityLevelRepresentation res;
		if(AssetDatabase.IsValidFolder($"{outerDirectory}{FolderOrFilePath}")) {
			var all = Resources.LoadAll<UnityLevelRepresentation>(FolderOrFilePath);
			if(all.Length == 0) {
				throw new System.ArgumentException(
					$"Directory \"{outerDirectory}{FolderOrFilePath}\" has no assets in it.");
			}
			res = all[r.Next(all.Length)];
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
