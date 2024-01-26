using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameCreatingCore {

	[System.Serializable]
	public sealed class Obstacle {
		[SerializeField]
		public IReadOnlyList<Vector2> Shape;
		public readonly Rect BoundingBox;
		[SerializeField]
		public WalkObstacleEffect FriendlyWalkEffect;
		[SerializeField]
		public WalkObstacleEffect EnemyWalkEffect;
		[SerializeField]
		public VisionObstacleEffect FriendlyVisionEffect;
		[SerializeField]
		public VisionObstacleEffect EnemyVisionEffect;

		public Obstacle(IReadOnlyList<Vector2> shape, WalkObstacleEffect friendlyWalkEffect, WalkObstacleEffect enemyWalkEffect, VisionObstacleEffect friendlyVisionEffect, VisionObstacleEffect enemyVisionEffect) {
			Shape = shape;
			BoundingBox = GetBoundingBox(shape);
			FriendlyWalkEffect = friendlyWalkEffect;
			EnemyWalkEffect = enemyWalkEffect;
			FriendlyVisionEffect = friendlyVisionEffect;
			EnemyVisionEffect = enemyVisionEffect;
		}

		// from https://codereview.stackexchange.com/questions/108857/point-inside-polygon-check
		public bool ContainsPoint(Vector2 point) {
			if(!IsInBoundingBox(point)) {
				return false;
			}
			int polygonLength = Shape.Count, i=0;
			bool inside = false;
			// x, y for tested point.
			float pointX = point.x, pointY = point.y;
			// start / end point for the current polygon segment.
			float startX, startY, endX, endY;
			Vector2 endPoint = Shape[polygonLength-1];           
			endX = endPoint.x; 
			endY = endPoint.y;
			while (i<polygonLength) {
				startX = endX;           startY = endY;
				endPoint = Shape[i++];
				endX = endPoint.x;       endY = endPoint.y;
				//
				inside ^= ( endY > pointY ^ startY > pointY ) /* ? pointY inside [startY;endY] segment ? */
						&& /* if so, test if it is under the segment */
						( (pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY) ) ;
			}
			return inside;
		}

		private bool IsInBoundingBox(Vector2 point) {
			return point.x > BoundingBox.x && point.x < BoundingBox.x + BoundingBox.width
				&& point.y > BoundingBox.y && point.y < BoundingBox.y + BoundingBox.height;
		}
		
		public static Rect GetBoundingBox(IReadOnlyCollection<Vector2> shape) {
			float minX = float.MaxValue, maxX = float.MinValue
				, minY = float.MaxValue, maxY = float.MinValue;
			foreach(Vector2 vec in shape) {
				minX = Mathf.Min(minX, vec.x);
				maxX = Mathf.Max(maxX, vec.x);
				minY = Mathf.Min(minY, vec.y);
				maxY = Mathf.Max(maxY, vec.y);
			}
			return new Rect(minX, minY, maxX - minX, maxY - minY);
		}
	}
}