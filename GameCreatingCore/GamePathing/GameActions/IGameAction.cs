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

        bool Done { get; }

        /// <summary>
        /// True if the action can be canceled.
        /// </summary>
        bool IsCancelable { get; }

        /// <summary>
        /// Which enemy is affected by this action? If null, it is the player.
        /// </summary>
        int? EnemyIndex { get; }


        /// <summary>
        /// Player occupied by this action is performing it on a given <paramref name="input"/>.
        /// </summary>
        LevelStateTimed CharacterActionPhase(LevelStateTimed input);

        /// <summary>
        /// When the player is no longer occupied by the action, this a way how the action can still last.
        /// </summary>
        LevelStateTimed AutonomousActionPhase(LevelStateTimed input);
    }

	public enum GameActionReachType {
		Instant,
		RangedStraight,
		RangedCurved
	}
}