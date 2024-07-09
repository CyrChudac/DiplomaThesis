using System.Collections.Generic;
using System.Linq;
using GameCreatingCore.GameActions;
using UnityEngine;

namespace GameCreatingCore.LevelStateData
{
    public class LevelState
    {
        public readonly IReadOnlyList<EnemyState> enemyStates;
        public readonly IReadOnlyList<bool> pickupableSkillsPicked;
        /// <summary>
        /// Those are the independent actions (like some projectile).
        /// </summary>
        public readonly IReadOnlyList<IGameAction> skillsInAction;

        public readonly PlayerState playerState;
        public LevelState(IReadOnlyList<EnemyState> enemyStates,
            PlayerState playerState, IReadOnlyList<IGameAction> skillsInAction,
            IReadOnlyList<bool> pickupableSkillsPicked)
        {
            this.enemyStates = enemyStates;
            this.playerState = playerState;
            this.skillsInAction = skillsInAction;
            this.pickupableSkillsPicked = pickupableSkillsPicked;
        }

        public LevelState DuplicateActions()
        {

            var ens = enemyStates
                .Select(x => x.Duplicate())
                .ToList();

            var player = playerState.Duplicate();

            var inAction = skillsInAction
                .Select(x => x.Duplicate())
                .ToList();

            return new LevelState(
                ens,
                player,
                inAction,
                pickupableSkillsPicked);
        }

        public LevelState Change(IReadOnlyList<EnemyState>? enemyStates = null,
            PlayerState? playerState = null,
            IReadOnlyList<IGameAction>? skillsInAction = null,
            IReadOnlyList<bool>? pickupablesPickedUp = null)
        {
            return new LevelState(
                enemyStates ?? this.enemyStates,
                playerState ?? this.playerState,
                skillsInAction ?? this.skillsInAction,
                pickupablesPickedUp ?? pickupableSkillsPicked);
        }

        public LevelState ChangePlayer(Vector2? position = null, float? rotation = null,
            IReadOnlyList<IActiveGameActionProvider>? availableSkills = null,
            IGameAction? performingAction = null, bool performingActionToNull = false)
        {
            return Change(playerState: playerState.Change(position, rotation, availableSkills, performingAction, performingActionToNull));
        }

        public LevelState ChangeEnemy(int index, float? rotation = null, Vector2? position = null,
            IGameAction? performingAction = null, bool performingActionToNull = false,
            bool? alive = null, bool? alerted = null, float? timeOfPlayerInView = null,
            IGameAction? currentPathAction = null, bool removePathAction = false,
            float? viewconeAlertLengthModifier = null, int? pathIndex = null, bool? isReturning = null)
        {

            var enems = enemyStates.ToList();
            enems[index] = enems[index].Change(rotation, position, performingAction, performingActionToNull, alive,
                alerted, timeOfPlayerInView, currentPathAction, removePathAction, viewconeAlertLengthModifier, pathIndex, isReturning);
            return Change(enemyStates: enems);
        }


        public override string ToString()
        {
            string toPick = pickupableSkillsPicked.Count > 0 ? "; P" + pickupableSkillsPicked.Where(s => !s).Count() : "";
            string playerAction = playerState.PerformingAction != null ? "!" : "";
            string enems = $"{enemyStates.Where(e => e.Alive).Count()}/{enemyStates.Count}";

            return $"{GetType().Name}: {playerState.Position}{playerAction}; E{enems}{toPick}";
        }
    }
}