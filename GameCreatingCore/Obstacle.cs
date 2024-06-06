using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System;

namespace GameCreatingCore {
	[System.Serializable]
	public sealed class Obstacle {
		/// <summary>
		/// The points defining the obstacle shape. Allways concave.
		/// </summary>
		[SerializeField]
		public IReadOnlyList<Vector2> Shape;
		public readonly Rect BoundingBox;
		public ObstacleEffect Effects;

		/// <param name="shape">The points defining the obstacle shape. Allways concave.</param>
		public Obstacle(IReadOnlyList<Vector2> shape, ObstacleEffect effect) {
			Shape = shape;
			var bb = GetBoundingBox(shape);
			BoundingBox = bb;
			Effects = effect;

			if(!IsConcave(shape, BoundingBox)) {
				throw new ObstacleNotConcaveException($"{shape.Count}: " + 
					string.Join(' ', shape));
			}
		}

		public bool ContainsPoint(Vector2 point)
			=> ContainsPoint(point, Shape, BoundingBox);

		public static bool ContainsPoint(Vector2 point, IReadOnlyList<Vector2> shape)
			=> ContainsPoint(point, shape, GetBoundingBox(shape));

		// from https://codereview.stackexchange.com/questions/108857/point-inside-polygon-check
		public static bool ContainsPoint(Vector2 point, IReadOnlyList<Vector2> shape, Rect boundingBox) {
			if(!IsInBoundingBox(point, boundingBox)) {
				return false;
			}
			return IsInPolygon(point, shape);
			int polygonLength = shape.Count, i=0;
			bool inside = false;
			// x, y for tested point.
			float pointX = point.x, pointY = point.y;
			// start / end point for the current polygon segment.
			float startX, startY, endX, endY;
			Vector2 endPoint = shape[polygonLength-1];           
			endX = endPoint.x; 
			endY = endPoint.y;
			while (i<polygonLength) {
				startX = endX;           startY = endY;
				endPoint = shape[i++];
				endX = endPoint.x;       endY = endPoint.y;
				//
				inside ^= ( endY > pointY ^ startY > pointY ) /* ? pointY inside [startY;endY] segment ? */
						&& /* if so, test if it is under the segment */
						( (pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY) ) ;
			}
			return inside;
		}

		public static bool IsInBoundingBox(Vector2 point, Rect boundingBox) {
			return point.x > boundingBox.x && point.x < boundingBox.x + boundingBox.width
				&& point.y > boundingBox.y && point.y < boundingBox.y + boundingBox.height;
		}
		
		public static Rect GetBoundingBox(IEnumerable<Vector2> shape) {
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

		public override string ToString() {
			var s = $"Obst: ({BoundingBox.x};{BoundingBox.y})..({BoundingBox.xMax};{BoundingBox.yMax}); {Effects}";
			return s;
		}

		public static bool IsConcave(IReadOnlyList<Vector2> points, Rect boundingBox) {
			if(points.Count <= 3)
				return true;

			for(int i = 0; i < points.Count - 2; i++) {
				var average = (points[i] + points[i + 2]) / 2;
				if(!ContainsPoint(average, points, boundingBox))
					return false;
			}
			return true;
		}

		//from https://stackoverflow.com/questions/39853481/is-point-inside-polygon
		//even with explanation there
		public static bool IsInPolygon(Vector2 testPoint, IReadOnlyList<Vector2> vertices)
		{
			if( vertices.Count < 3 ) 
				return false;
			bool isInPolygon = false;
			var lastVertex = vertices[vertices.Count - 1];
			foreach( var vertex in vertices)
			{
				if( IsBetween(testPoint.y, lastVertex.y, vertex.y ) )
				{
					double t = ( testPoint.y - lastVertex.y ) / ( vertex.y - lastVertex.y );
					double x = t * ( vertex.x - lastVertex.x ) + lastVertex.x;
					if( x >= testPoint.x ) 
						isInPolygon = !isInPolygon;
				}
				else
				{
					if( testPoint.y == lastVertex.y && testPoint.x < lastVertex.x && vertex.y > testPoint.y ) 
						isInPolygon = !isInPolygon;
					if( testPoint.y == vertex.y && testPoint.x < vertex.x && lastVertex.y > testPoint.y ) 
						isInPolygon = !isInPolygon;
					if(testPoint.y == lastVertex.y && testPoint.y == vertex.y && IsBetween(testPoint.x, vertex.x, lastVertex.y))
						return true;
				}

				lastVertex = vertex;
			}

			return isInPolygon;
		}

		public static bool IsBetween(double x, double a, double b )
		{
			return ( x - a ) * ( x - b ) < 0;
		}
	}

	public class ObstacleEffect {
		[SerializeField]
		public WalkObstacleEffect FriendlyWalkEffect;
		[SerializeField]
		public WalkObstacleEffect EnemyWalkEffect;
		[SerializeField]
		public VisionObstacleEffect FriendlyVisionEffect;
		[SerializeField]
		public VisionObstacleEffect EnemyVisionEffect;

		public ObstacleEffect(WalkObstacleEffect friendlyWalkEffect, WalkObstacleEffect enemyWalkEffect, 
			VisionObstacleEffect friendlyVisionEffect, VisionObstacleEffect enemyVisionEffect) {

			FriendlyWalkEffect = friendlyWalkEffect;
			EnemyWalkEffect = enemyWalkEffect;
			FriendlyVisionEffect = friendlyVisionEffect;
			EnemyVisionEffect = enemyVisionEffect;
		}
		public override string ToString() {
			string s = "OEff(";
			bool ch = false;
			string w = "W: ";
			if(FriendlyWalkEffect == WalkObstacleEffect.Unwalkable) {
				w += "F";
				ch = true;
			}
			if(EnemyWalkEffect == WalkObstacleEffect.Unwalkable) {
				w += "E";
				ch = true;
			}
			if(ch)
				s += w;
			ch = false;
			string v = "; V: ";
			if(FriendlyVisionEffect == VisionObstacleEffect.NonSeeThrough) {
				v += "F";
				ch = true;
			}
			if(EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough) {
				v += "E";
				ch = true;
			}
			if(ch)
				s += v;
			return s + ")";
		}

	}
}