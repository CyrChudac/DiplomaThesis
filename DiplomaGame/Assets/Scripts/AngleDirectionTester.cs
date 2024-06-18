using GameCreatingCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using UnityEngine;
using UnityEditor;
using System;

[ExecuteInEditMode]
public class AngleDirectionTester : MonoBehaviour
{
    public Transform from;
    [Range(0f, 0.1f)]
	public float fromSize = 0.1f;
    public Transform to;
    [Range(0f, 0.1f)]
	public float toSize = 0.07f;
	public Color GizmoColor = Color.yellow;
    [Header("Script Generated")]
	public float supposedAngle;
	public float distance;

	private void Update() {
		distance = Vector2.Distance(from.position, to.position);
	}

#if UNITY_EDITOR
	private void OnDrawGizmos() {
        if(to == null || from == null) 
            return;
        if(!(transform.GetComponentsInChildren<Transform>().Contains(Selection.activeTransform) 
            || Selection.activeTransform == from
            || Selection.activeTransform == to))
            return;
        supposedAngle = Vector2Utils.AngleTowards(from.position, to.position);
        var supposedDirection = Vector2Utils.VectorFromAngle(supposedAngle);
        supposedDirection = supposedDirection.normalized;
        var dist = Vector2.Distance(from.position, to.position);
		Gizmos.color = new Color(1 - GizmoColor.r, 1 - GizmoColor.g, 1 - GizmoColor.b);
		Gizmos.DrawSphere(from.position, dist * fromSize);
		Gizmos.DrawSphere(to.position, dist * toSize);
        Gizmos.color = GizmoColor;
        Gizmos.DrawLine(from.position, ((Vector2)from.position) + supposedDirection * dist);
	}
#endif
}
