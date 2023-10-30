using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{

	[SerializeField]
	private GameObject gameOverScreen;

	public void GameOver() {
		var gos = Instantiate(gameOverScreen);
		gos.transform.position = Vector3.zero;
	}
}
