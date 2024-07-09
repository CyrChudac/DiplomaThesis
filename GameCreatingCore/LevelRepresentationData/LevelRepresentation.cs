using UnityEngine;
using System.Collections.Generic;
using GameCreatingCore.GameActions;

namespace GameCreatingCore.LevelRepresentationData
{

    [System.Serializable]
    public sealed class LevelRepresentation
    {
        [SerializeField]
        public List<Obstacle> Obstacles;
        /// <summary>
        /// The Outer walls of the environment.
        /// </summary>
        [SerializeField]
        public Obstacle OuterObstacle;
        [SerializeField]
        public List<Enemy> Enemies;
        [SerializeField]
        public List<PickupableActionProvider> SkillsToPickup;
        [SerializeField]
        public List<IActiveGameActionProvider> SkillsStartingWith;
        [SerializeField]
        public Vector2 FriendlyStartPos;
        [SerializeField]
        public LevelGoal Goal;

        public LevelRepresentation(List<Obstacle> obstacles, Obstacle outerObstacle, List<Enemy> enemies,
            List<PickupableActionProvider> skillsToPickup,
            List<IActiveGameActionProvider> skillsStartingWith, Vector2 friendlyStartPos, LevelGoal goal)
        {
            Obstacles = obstacles;
            OuterObstacle = outerObstacle;
            Enemies = enemies;
            SkillsToPickup = skillsToPickup;
            SkillsStartingWith = skillsStartingWith;
            FriendlyStartPos = friendlyStartPos;
            Goal = goal;
        }

        public override string ToString()
        {
            return $"Level: o-{Obstacles.Count}; e-{Enemies.Count}";
        }
    }
}