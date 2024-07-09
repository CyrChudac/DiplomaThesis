using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class RepairCommands : MonoBehaviour
{
	[TextArea(1,5)]
	public string directory;
	[TextArea(1,5)]
	public string removeString;
	[TextArea(1,5)]
	public string replaceWith;
	public bool recursive = true;

	[ContextMenu("Repair Assests")]
	public void Repair() 
		=> Repair(directory);

	public void Repair(string directory) {
		foreach(var f in Directory.GetFiles(directory)) {
			if(f.EndsWith(".asset"))
				RepairFile(f);
		}
		if(recursive) {
			foreach(var d in Directory.GetDirectories(directory)) {
				Repair(d);
			}
		}
	}

	private void RepairFile(string path) {
		var text = File.ReadAllText(path);
		text = text.Replace(removeString, replaceWith);
		File.WriteAllText(path, text);
	}
}
