using GameCreatingCore.StaticSettings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
	[SerializeField] private Transform canvas;
	[SerializeField]
	private float gameWinTimeBeforeSceneLoad = 1f;
	[SerializeField]
	private float gameLoseTimeBeforeSceneLoad = 1f;
	[Header("Only set the rest for the first instantiated GameController.")]
	[SerializeField]
	private GameObject gameOverScreen;
	[SerializeField]
	private GameObject gameWonScreen;
	[SerializeField]
	private string gameSceneName;
	[SerializeField]
	private string breakSceneName;
	[SerializeField]
	private string finalSceneName;

	[SerializeField] 
	private List<int> levelBreaks;
	[SerializeField]
	private List<LevelExplanation> explantions;
	[SerializeField] 
	private int levelCount;

	[SerializeField] 
	private int defaultLevelDone;

	[SerializeField]
    private UnityStaticGameRepresentation staticGameRepr;
	public StaticGameRepresentation GetStaticGameRepr() => staticGameRepr.ToGameDescription();

	[NonSerialized]
	public int levelsDone;

	public float GameDifficulty => staticGameRepr.gameDifficulty;

	const string levelsDoneName = "levelsDone";

	//takes the settings from the previous gamecontroller and copies them here.
	//then destroys the previous one
	//this way all objects from the current scene can reference this object
	//in the scene view without having to find it through code
	public void Start() { 
		var ctrls = FindObjectsOfType<GameController>();
		if(ctrls.Length == 1) {
			DontDestroyOnLoad(gameObject);
			levelsDone = defaultLevelDone; //TODO: change it back: PlayerPrefs.GetInt(levelsDoneName, 0);
		} else if(ctrls.Length == 2){ 
			var c = ctrls[0] == this ? ctrls[1] : ctrls[0];

			gameOverScreen = c.gameOverScreen;
			gameWonScreen = c.gameWonScreen;
			gameSceneName = c.gameSceneName;
			breakSceneName = c.breakSceneName;
			finalSceneName = c.finalSceneName;
			levelBreaks = c.levelBreaks;
			explantions = c.explantions;
			levelCount = c.levelCount;
			staticGameRepr = c.staticGameRepr;
			defaultLevelDone = c.defaultLevelDone;

			levelsDone = c.levelsDone;

			Destroy(c.gameObject);
			DontDestroyOnLoad(gameObject);
		} else {
			throw new System.NotImplementedException();
		}
		if(explantions.Count > levelsDone) {
			foreach(var e in explantions[levelsDone].list) {
				if(e.onCanvas)
					Instantiate(e.obj, canvas);
				else
					Instantiate(e.obj);
			}
		}
	}

	bool isGameOver = false;
	public void GameOver() {
		if((!isGameOver) && !isGameWon) {
			var gos = Instantiate(gameOverScreen);
			gos.transform.position = Vector3.zero;
			isGameOver = true;
			StartCoroutine(GameSceneCoroutine(gameLoseTimeBeforeSceneLoad, gameSceneName));
		}
	}

	bool isGameWon = false;
	public void GameWon() {
		if((!isGameOver) && (!isGameWon)) {
			var gws = Instantiate(gameWonScreen);
			gws.transform.position = Vector3.zero;
			isGameWon = true;
			levelsDone++;
			PlayerPrefs.SetInt(levelsDoneName, levelsDone);
			if(levelCount <= levelsDone) {
				StartCoroutine(GameSceneCoroutine(gameWinTimeBeforeSceneLoad, finalSceneName));
			}else if(levelBreaks.Contains(levelsDone)) {
				StartCoroutine(GameSceneCoroutine(gameWinTimeBeforeSceneLoad, breakSceneName));
			} else {
				StartCoroutine(GameSceneCoroutine(gameWinTimeBeforeSceneLoad, gameSceneName));
			}
		}
	}

	IEnumerator GameSceneCoroutine(float waitTime, string sceneName) {
		yield return new WaitForSeconds(waitTime);
		SceneManager.LoadScene(sceneName);
	}

	private void Update() {
		if(Input.GetKey(KeyCode.Escape)) {
			Quit();
		}
	}

	public void Quit() {
		Application.Quit();
	}

	public void ResetAndReload() {
		ResetLevels();
		levelsDone = defaultLevelDone;
		GameOver();
	}

	[ContextMenu("Reset Progress")]
	public void ResetLevels() {
		PlayerPrefs.DeleteKey(levelsDoneName);
	}

	[Serializable]
	public class LevelExplanation {
		public List<PotentiallyCanvasedObject> list;
	}
	
	[Serializable]
	public class PotentiallyCanvasedObject {
		public bool onCanvas = true;
		public GameObject obj;
	}
}
