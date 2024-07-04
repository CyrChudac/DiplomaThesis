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

		public bool ContainsPoint(Vector2 point, bool isBoundaryInside)
			=> ContainsPoint(point, Shape, BoundingBox, isBoundaryInside);

		public static bool ContainsPoint(Vector2 point, IReadOnlyList<Vector2> shape, bool isBoundaryInside)
			=> ContainsPoint(point, shape, GetBoundingBox(shape), isBoundaryInside);

		// from https://codereview.stackexchange.com/questions/108857/point-inside-polygon-check
		public static bool ContainsPoint(Vector2 point, IReadOnlyList<Vector2> shape, Rect boundingBox, bool isBoundaryInside) {
			if(!IsInBoundingBox(point, boundingBox)) {
				return false;
			}
			return IsInPolygon(point, shape, isBoundaryInside);
		}

		private static bool IsInBoundingBox(Vector2 point, Rect boundingBox) {
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
				if(!ContainsPoint(average, points, boundingBox, true))
					return false;
			}
			return true;
		}

		//from https://stackoverflow.com/questions/39853481/is-point-inside-polygon
		//even with explanation there
		public static bool IsInPolygon(Vector2 testPoint, IReadOnlyList<Vector2> vertices, bool boundaryIsIn)
		{
			if( vertices.Count < 3 ) 
				return false;
			bool isInPolygon = false;
			var lastVertex = vertices[vertices.Count - 1];
			bool lastSame = false;
			bool lastLeft = false;
			foreach( var vertex in vertices)
			{
				if((!lastSame) && IsBetween(testPoint.y, lastVertex.y, vertex.y ))
				{
					float t = ( testPoint.y - lastVertex.y ) / ( vertex.y - lastVertex.y );
					float x = t * ( vertex.x - lastVertex.x ) + lastVertex.x;
					if(x > testPoint.x) 
						isInPolygon = !isInPolygon;
					else if(FloatEquality.AreEqual(x, testPoint.x))
						return boundaryIsIn;
					lastLeft = true;
				}
				else if(!lastLeft)
				{
					if(FloatEquality.AreEqual(testPoint.y, lastVertex.y) && 
						FloatEquality.AreEqual(testPoint.y, vertex.y) && 
						IsBetween(testPoint.x, vertex.x, lastVertex.x))
						return boundaryIsIn;
					if(FloatEquality.AreEqual(testPoint.y, vertex.y) && testPoint.x < vertex.x
						&& !FloatEquality.LessOrEqual(lastVertex.y, testPoint.y)) {
						isInPolygon = !isInPolygon;
						lastSame = true;
					}
					if((!lastSame) && FloatEquality.AreEqual(testPoint.y, lastVertex.y)
						&& testPoint.x < lastVertex.x && vertex.y > testPoint.y) {
						isInPolygon = !isInPolygon;
						lastSame = true;
					}
					else
						lastSame = false;
				} else {
					lastSame = false;
					lastLeft = false;
				}

				lastVertex = vertex;
			}

			return isInPolygon;
		}

		public static bool IsBetween(float x, float a, float b )
		{
			return !FloatEquality.MoreOrEqual(( x - a ) * ( x - b ), 0);
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