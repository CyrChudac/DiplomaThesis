using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreationCore;

public class PathedGameObject : MonoBehaviour
{
	protected Path Path { get; private set; }
	public void SetPath(Path path) {
		this.Path = path;
		OnResetPath();
	}

	protected virtual void OnResetPath() { }
}
