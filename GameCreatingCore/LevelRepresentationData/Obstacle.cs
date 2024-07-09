using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System;

namespace GameCreatingCore.LevelRepresentationData
{
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
			foreach( var vertex in vertices)
			{
				if(IsBetween(testPoint.y, lastVertex.y, vertex.y ))
				{
					float t = ( testPoint.y - lastVertex.y ) / ( vertex.y - lastVertex.y );
					float x = t * ( vertex.x - lastVertex.x ) + lastVertex.x;
					if(!FloatEquality.LessOrEqual(x, testPoint.x)) {
						isInPolygon = !isInPolygon;
					} else if(FloatEquality.AreEqual(x, testPoint.x))
						return boundaryIsIn;
				}
				else 
				{
					if(FloatEquality.AreEqual(testPoint.y, lastVertex.y)){
						if(FloatEquality.AreEqual(testPoint.x, lastVertex.x))
							return boundaryIsIn;
						if(FloatEquality.AreEqual(testPoint.y, vertex.y) 
							&& IsBetween(testPoint.x, vertex.x, lastVertex.x))

							return boundaryIsIn;
						if(testPoint.x < lastVertex.x && !FloatEquality.LessOrEqual(vertex.y, testPoint.y)) {
							isInPolygon = !isInPolygon;
						}
					} 
						
					if(FloatEquality.AreEqual(testPoint.y, vertex.y) && testPoint.x < vertex.x
						&& !FloatEquality.LessOrEqual(lastVertex.y, testPoint.y)) {
						isInPolygon = !isInPolygon;
					}
				} 

				lastVertex = vertex;
			}

			return isInPolygon;
		}

		public static bool IsBetween(float x, float a, float b )
		{
			return ((!FloatEquality.MoreOrEqual(x, a)) &&
				(!FloatEquality.LessOrEqual(x, b))) 
				||
				((!FloatEquality.MoreOrEqual(x, b)) &&
				(!FloatEquality.LessOrEqual(x, a)));
		}
	}
}