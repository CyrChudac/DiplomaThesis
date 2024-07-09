//#define DEBUG_UNITY_CCHUDAC
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
	private GameObject blackInPrefab;
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

#if DEBUG_UNITY_CCHUDAC
	[SerializeField] 
	private int defaultLevelDone;
#endif

	[SerializeField]
    private UnityStaticGameRepresentation staticGameRepr;
	public StaticGameRepresentation GetStaticGameRepr() => staticGameRepr.ToGameDescription();
	[SerializeField]
	private bool inflatedObstacles = true;
	public bool InflatedObstacles => inflatedObstacles;

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
			levelsDone = 
#if DEBUG_UNITY_CCHUDAC
				defaultLevelDone;
#else
				PlayerPrefs.GetInt(levelsDoneName, 0);
			if(levelsDone != 0) {
				levelsDone--;
				GameWon(0);
			}
#endif
		} else if(ctrls.Length == 2){ 
			var c = ctrls[0] == this ? ctrls[1] : ctrls[0];

			blackInPrefab = c.blackInPrefab;
			gameSceneName = c.gameSceneName;
			breakSceneName = c.breakSceneName;
			finalSceneName = c.finalSceneName;
			levelBreaks = c.levelBreaks;
			explantions = c.explantions;
			levelCount = c.levelCount;
			staticGameRepr = c.staticGameRepr;
			inflatedObstacles = c.inflatedObstacles;
			
#if DEBUG_UNITY_CCHUDAC
			defaultLevelDone = c.defaultLevelDone;
#endif

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
			var gos = Instantiate(blackInPrefab, canvas);
			gos.transform.localPosition = Vector3.zero;
			isGameOver = true;
			StartCoroutine(GameSceneCoroutine(gameLoseTimeBeforeSceneLoad, gameSceneName));
		}
	}

	bool isGameWon = false;
	public void GameWon() => GameWon(null);
	private void GameWon(float? forceTime) {
		if((!isGameOver) && (!isGameWon)) {
			var gws = Instantiate(blackInPrefab, canvas);
			gws.transform.localPosition = Vector3.zero;
			isGameWon = true;
			levelsDone++;
			PlayerPrefs.SetInt(levelsDoneName, levelsDone);
			float time = forceTime ?? gameWinTimeBeforeSceneLoad;
			if(levelCount <= levelsDone) {
				StartCoroutine(GameSceneCoroutine(time, finalSceneName));
			}else if(levelBreaks.Contains(levelsDone)) {
				StartCoroutine(GameSceneCoroutine(time, breakSceneName));
			} else {
				StartCoroutine(GameSceneCoroutine(time, gameSceneName));
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
		}else if(Input.GetKey(KeyCode.R)) {
			GameOver();
		}
	}

	public void Quit() {
		Application.Quit();
	}

	public void ResetAndReload() {
		ResetLevels();
		levelsDone = 
#if DEBUG_UNITY_CCHUDAC
			defaultLevelDone;
#else
			0;
#endif
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
