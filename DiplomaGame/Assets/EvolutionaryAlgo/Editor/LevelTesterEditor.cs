using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(LevelTester))]
public class LevelTesterEditor : Editor
{
	public void OnSceneGUI()
    {
        // Handles behind gizmos
     
        // Draws gizmos defined in MonoBehaviour's OnDrawGizmos
        Handles.DrawGizmos(SceneView.lastActiveSceneView.camera);
        
        GUIStyle obstCounterStyle = new GUIStyle();
        obstCounterStyle.normal.textColor = Color.white;
        obstCounterStyle.fontSize = 12;

        GUIStyle shapeCounterStyle = new GUIStyle();
        shapeCounterStyle.normal.textColor = Color.white;
        shapeCounterStyle.fontSize = 8;

        // Handles in front of gizmos
        var t = target as LevelTester;
        var lvl = t.testLevel.GetLevelRepresentation();
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

}
