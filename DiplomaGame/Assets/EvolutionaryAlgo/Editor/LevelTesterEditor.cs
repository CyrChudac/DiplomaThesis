using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using GameCreatingCore.GamePathing.NavGraphs.Viewcones;

[CustomEditor(typeof(LevelTester))]
public class LevelTesterEditor : Editor
{
	public void OnSceneGUI()
    {
        // Handles behind gizmos
     
        // Draws gizmos defined in MonoBehaviour's OnDrawGizmos
        Handles.DrawGizmos(SceneView.lastActiveSceneView.camera);
        DrawObstacleNumbers();
        DrawNavGraphNumbers();
    }
    
    void DrawObstacleNumbers() {
        if(!(target as LevelTester).showObstacleNumbers)
            return;
        
        GUIStyle obstCounterStyle = new GUIStyle();
        obstCounterStyle.normal.textColor = Color.white;
        obstCounterStyle.fontSize = 12;

        GUIStyle shapeCounterStyle = new GUIStyle();
        shapeCounterStyle.normal.textColor = Color.white;
        shapeCounterStyle.fontSize = 8;

        // Handles in front of gizmos
        var t = target as LevelTester;
        var lvl = t.level;
        for(int i = 0; i < lvl.Obstacles.Count; i++) {
            var obst = lvl.Obstacles[i];
            Vector3 obstMid = Vector2.zero;
            foreach(var p in obst.Shape) {
                obstMid += (Vector3)p;
            }
            obstMid /= obst.Shape.Count;

            Handles.Label(obstMid, i.ToString(), obstCounterStyle);

            for(int j = 0; j < obst.Shape.Count; j++) {
                Handles.Label(obst.Shape[j], j.ToString(), shapeCounterStyle);
            }
        }
    }

    void DrawNavGraphNumbers() {
        var t = target as LevelTester;
        if((!t.showNavGraphNumbers) && t.scoreVisualization == ScoreVisualization.None)
            return;
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.black;
        style.fontSize = 10;

        System.Func<ScoredActionedNode, float?> scoreFunc;
		switch(t.scoreVisualization) {
			case ScoreVisualization.GoalScore:
                scoreFunc = n => n.GoalScore?.Score;
				break;
			case ScoreVisualization.UseSkillScore:
                scoreFunc = n => n.SkillUseScore?.Score;
				break;
			case ScoreVisualization.PickUpSkillScore:
                scoreFunc = n => n.SkillPickupScore?.Score;
				break;
            case ScoreVisualization.None:
                scoreFunc = n => null;
				break;
            default:
                throw new System.NotImplementedException($"Behaviour for value {t.scoreVisualization} of " +
                    $"enum {nameof(ScoreVisualization)} is not implemented.");
		};

		var g = t.graphWithViewcones;
        for(int i = 0; i < g.vertices.Count; i++) {
            var number = t.showNavGraphNumbers ? i.ToString() : "";
            var mid = (t.showNavGraphNumbers && t.scoreVisualization != ScoreVisualization.None) ? ";" : "";
            var s = scoreFunc(g.vertices[i]);
            var score = s.HasValue ? $"{((int)(s * 100))/100f}" : "";
            Handles.Label(g.vertices[i].Position, number + mid + score, style);
        }
	}

}
