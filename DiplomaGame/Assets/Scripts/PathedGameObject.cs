using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore;

public class PathedGameObject : MonoBehaviour
{
	protected Path? Path { get; private set; }
	public void SetPath(Path path) {
		this.Path = path;
		OnResetPath();
	}

	protected virtual void OnResetPath() { }

	public void OnDrawGizmosSelected() {
		if(Path == null)
			return;
		Gizmos.color = Color.Lerp(Color.red, Color.black, 0.1f);
		foreach(var p in Path.Commands) {
			Gizmos.DrawSphere(p.Position, 1);
		}
		for(int i = 0; i < Path.Commands.Count - 1; i++) {
			Gizmos.DrawLine(Path.Commands[i].Position, Path.Commands[i + 1].Position);
		}
		if(Path.Cyclic && Path.Commands.Count > 2 ) {
			Gizmos.DrawLine(Path.Commands[Path.Commands.Count - 1].Position, Path.Commands[0].Position);
		}
	}
}
