using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.Windows;

namespace GameCreatingCore.GamePathing.GameActions
{
    public interface IGameAction
    {
        /// <summary>
        /// When true, the character can do another action even when this one is not done yet.
        /// </summary>
        bool IsIndependentOfCharacter { get; }

        /// <summary>
        /// Signifies whether this action has finished all of its execution.
        /// This can only be <b>True</b>, if the action was already run at least once.
        /// </summary>
        bool Done { get; }

        /// <summary>
        /// True if the action can be canceled.
        /// </summary>
        bool IsCancelable { get; }

        /// <summary>
        /// How many more seconds will the action force the character to use it.
        /// Only available if the action is already in use.
        /// </summary>
        float TimeUntilCancelable { get; }

        /// <summary>
        /// Which enemy is affected by this action? If null, it is the player.
        /// </summary>
        int? EnemyIndex { get; }

        /// <summary>
        /// If this action was already used, this way we reset it for next use.
        /// </summary>
        public void Reset();

        /// <summary>
        /// Player occupied by this action is performing it on a given <paramref name="input"/>.
        /// </summary>
        LevelStateTimed CharacterActionPhase(LevelStateTimed input);

        /// <summary>
        /// When the player is no longer occupied by the action, this a way how the action can still last.
        /// </summary>
        LevelStateTimed AutonomousActionPhase(LevelStateTimed input);

        /// <summary>
        /// Create an exact copy of this action also in the same state as this one.
        /// </summary>
        IGameAction Duplicate();
    }

	public enum GameActionReachType {
		Instant,
		RangedStraight,
		RangedCurved
	}

    public interface IWithInnerActions : IGameAction {
        
        /// <summary>
        /// In case the action is playing some inner actions, this function should return those.
        /// </summary>
        IEnumerable<IGameAction> GetInnerActions();
        

        /// <summary>
        /// In case the action is playing some inner actions, this function should return the currently played action.
        /// </summary>
        IGameAction? CurrentInnerAction { get; }
    }
}