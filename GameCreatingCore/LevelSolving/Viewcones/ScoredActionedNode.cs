using GameCreatingCore.GameActions;
using System.Collections.Generic;
using UnityEngine;
using GameCreatingCore.GamePathing;

namespace GameCreatingCore.LevelSolving.Viewcones {

    public class ScoredActionedNode : Node
    {
        public IGameAction? NodeAction { get; }

        /// <summary>
        /// If this node is inside a view of an enemy, how far from the enemy is it.
        /// </summary>
        public float? DistanceFromEnemy { get; }

        /// <summary>
        /// If this node is inside a view of an enemy, this is its index.
        /// </summary>
        public int? EnemyIndex { get; }

        /// <summary>
        /// How far is this node from the game goal.
        /// </summary>
        public ScoreHolder? GoalScore { get; }

        /// <summary>
        /// How far is the node from place where we can use a skill.
        /// </summary>
        public ScoreHolder? SkillUseScore { get; }

        /// <summary>
        /// How far is this node from place where we can pick up a new skill.
        /// </summary>
        public ScoreHolder? SkillPickupScore { get; }

        public ScoredActionedNode(Vector2 position, IGameAction? action, ScoreHolder? goalScore, 
            ScoreHolder? skillUseScore, ScoreHolder? skillPickupScore, float? distanceFromEnemy, int? enemyIndex) 
            : base(position)
        {
            NodeAction = action;
            GoalScore = goalScore;
            SkillUseScore = skillUseScore;
            SkillPickupScore = skillPickupScore;
            DistanceFromEnemy = distanceFromEnemy;
            EnemyIndex = enemyIndex;
        }

        public override string ToString()
        {
            return $"{nameof(ScoredActionedNode)}: {Position}; {GoalScore?.Score}; {NodeAction?.GetType()}";
        }
    }
    public class ScoreHolder {
        public int? Previous { get; }
        public float Score { get; }

		public ScoreHolder(int? previous, float score) {
			Previous = previous;
			Score = score;
		}

        public ScoredActionedNode? GetPrevious(IReadOnlyList<ScoredActionedNode> vertices) {
            return Previous.HasValue ? vertices[Previous.Value] : null;
        }

		public override string ToString() {
			return $"{nameof(ScoreHolder)}: S-{((int)(Score * 100)) / 100f}; P-{Previous}";
		}
	}
}
