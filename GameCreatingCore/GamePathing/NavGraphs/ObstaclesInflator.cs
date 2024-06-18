using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs {
	public static class ObstaclesInflator {

		public static LevelRepresentation InflateAllInLevel(LevelRepresentation levelRepresentation, 
			StaticMovementSettingsProcessed movSet) {
			return new LevelRepresentation(
				levelRepresentation.Obstacles
					.Select(o => InflateNormal(o, movSet.CharacterMaxRadius))
					.ToList(),
				InflateOuterObst(levelRepresentation.OuterObstacle, movSet.CharacterMaxRadius),
				levelRepresentation.Enemies,
				levelRepresentation.SkillsToPickup,
				levelRepresentation.SkillsStartingWith,
				levelRepresentation.FriendlyStartPos,
				levelRepresentation.Goal);
		}

		//we only increase the obstacle by radius/2 => so 2 obstacles next to each other determine,
		//if the character fits.
		public static Obstacle InflateNormal(Obstacle obstacle, float maxCharacterRadius)
			=> Inflate(obstacle, maxCharacterRadius/2);

		public static Obstacle InflateOuterObst(Obstacle obstacle, float maxCharacterRadius) 
			=> Inflate(obstacle, -maxCharacterRadius / 2);

		private static Obstacle Inflate(Obstacle obstacle, float inflationSize) {
			//we basically increase the length of each edge by inflationSize
			//and then compute the midpoint between the enlarged edges ends
			//and then we double it (repeat that for every point)
			/*         ____
			          /\  /
			   ______/__\/
			  /     /
			 /obst./
			/_____/
			
			*/

			//however double average = average of doubles, so we can simply
			//double the inflationSize to achieve the same.
			inflationSize *= 2;
			List<Vector2> offsets = new List<Vector2>(obstacle.Shape.Count);
			for(int i = 0; i < obstacle.Shape.Count; i++) {
				var next = (i + 1) % obstacle.Shape.Count;
				offsets.Add(obstacle.Shape[i] - obstacle.Shape[next]);
			}

			List<Vector2> shape = new List<Vector2>(obstacle.Shape.Count);
			
			for(int i = 0; i < obstacle.Shape.Count; i++) {
				var next = (i + 1) % obstacle.Shape.Count;
				var prev = Mod((i - 1), obstacle.Shape.Count);
				var ch = offsets[prev].magnitude / inflationSize;
				var p1 = obstacle.Shape[prev] + offsets[prev].normalized * inflationSize * (1 + ch);
				ch = offsets[i].magnitude / inflationSize;
				var p2 = obstacle.Shape[next] + offsets[i].normalized * inflationSize * -1 * (1 + ch);
				shape.Add((p1 + p2) / 2); 
			}

			return new Obstacle(shape, obstacle.Effects);

		}

		private static int Mod(int x, int m) {
			return (x%m + m)%m;
		}
	}
}
