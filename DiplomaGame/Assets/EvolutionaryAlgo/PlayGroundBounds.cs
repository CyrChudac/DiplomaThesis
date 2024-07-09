using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PlayGroundBounds : MonoBehaviour
{
	[SerializeField] private Transform rightTop;
	[SerializeField] private  Transform leftBot;

	public Vector2 LeftTop => new Vector2(leftBot.position.x, rightTop.position.y);
	public Vector2 RightTop => rightTop.position;
	public Vector2 RightBot => new Vector2(rightTop.position.x, leftBot.position.y);
	public Vector2 LeftBot => leftBot.position;

	public Rect Rect => new Rect(LeftBot, RightTop - LeftBot);

	public IEnumerable<Vector2> Shape => new List<Vector2>() {
		LeftTop,
		RightTop,
		RightBot,
		LeftBot
	};

#if UNITY_EDITOR
	public Color GizmoColor = Color.blue;
	private void OnDrawGizmos() {
		if(transform.parent.GetComponentsInChildren<Transform>().Contains(Selection.activeTransform)) {
			Gizmos.color = GizmoColor;
			var size = ((RightTop.x - LeftBot.x) * 0.5f + 0.5f * (RightTop.y - LeftBot.y)) / 40;
			Gizmos.DrawSphere(LeftBot, size);
			Gizmos.DrawSphere(RightTop, size);
			Gizmos.DrawLine(LeftBot, RightBot);
			Gizmos.DrawLine(RightBot, RightTop);
			Gizmos.DrawLine(RightTop, LeftTop);
			Gizmos.DrawLine(LeftTop, LeftBot);
		}
	}
#endif
}
