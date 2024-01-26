using GameCreatingCore.StaticSettings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
	[SerializeField]
	private GameObject gameOverScreen;
	
    [SerializeField]
    private UnityStaticGameRepresentation staticGameRepr;
	public StaticGameRepresentation GetStaticGameRepr() => staticGameRepr.ToGameDescription();

	public int GameDifficulty => staticGameRepr.gameDifficulty;

	//takes the settings from the previous gamecontroller and copies them here.
	//then destroys the previous one
	//this way all objects from the current scene can reference this object
	//in the scene view without having to find it through code
	public void Start() { 
		var ctrls = FindObjectsOfType<GameController>();
		if(ctrls.Length == 1) {
			DontDestroyOnLoad(gameObject);
		} else if(ctrls.Length == 2){
			var c = ctrls[0] == this ? ctrls[1] : ctrls[0];
			staticGameRepr = c.staticGameRepr;
			Destroy(c);
		} else {
			throw new System.NotImplementedException();
		}

	}


	public void GameOver() {
		var gos = Instantiate(gameOverScreen);
		gos.transform.position = Vector3.zero;
	}
}
