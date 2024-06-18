using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameCreatingCore.GamePathing.GameActions;
using UnityEngine;

namespace GameCreatingCore.GamePathing
{
    public class LevelState {
		public readonly IReadOnlyList<EnemyState> enemyStates;
		public readonly IReadOnlyList<bool> pickupableSkillsPicked;
		/// <summary>
		/// Those are the independent actions (like some projectile).
		/// </summary>
		public readonly IReadOnlyList<IGameAction> skillsInAction;

		public readonly PlayerState playerState;
		public LevelState(IReadOnlyList<EnemyState> enemyStates,
			PlayerState playerState, IReadOnlyList<IGameAction> skillsInAction,
			IReadOnlyList<bool> pickupableSkillsPicked) {
			this.enemyStates = enemyStates;
			this.playerState = playerState;
			this.skillsInAction = skillsInAction;
			this.pickupableSkillsPicked = pickupableSkillsPicked;
		}

		public LevelState Change(IReadOnlyList<EnemyState>? enemyStates = null,
			PlayerState? playerState = null,
			IReadOnlyList<IGameAction>? skillsInAction = null,
			IReadOnlyList<bool>? pickupablesPickedUp = null) {

			return new LevelState(
				enemyStates ?? this.enemyStates,
				playerState ?? this.playerState,
				skillsInAction ?? this.skillsInAction,
				pickupablesPickedUp ?? this.pickupableSkillsPicked);
		}

		public LevelState ChangePlayer(Vector2? position = null, float? rotation = null, 
			IReadOnlyList<IActiveGameActionProvider>? availableSkills = null,
			IGameAction? performingAction = null) {
			return Change(playerState: playerState.Change(position, rotation, availableSkills, performingAction));
		}
		public LevelState ChangeEnemy(int index, float? rotation = null, Vector2? position = null, 
			IGameAction? performingAction = null,
			bool? alive = null, bool? alerted = null, float? timeOfPlayerInView = null,
			float? viewconeAlertLengthModifier = null, int? pathIndex = null, bool? isReturning = null) {

			var enems = enemyStates.ToList();
			enems[index] = enems[index].Change(rotation, position, performingAction, alive,
				alerted, timeOfPlayerInView, viewconeAlertLengthModifier, pathIndex, isReturning);
			return Change(enemyStates: enems);
		}

		
		public static LevelState GetInitialLevelState(LevelRepresentation levelRepresentation) {

			List<EnemyState> enemies = levelRepresentation.Enemies
				.Select(e => new EnemyState(
					position: e.Position, 
					rotation: e.Rotation,
					performingAction: null,
					alive: true,
					alerted: false,
					viewconeAlertLengthModifier: 0, 
					timeOfPlayerInView: 0, 
					pathIndex: (e.Path != null && e.Path.Commands.Count > 0) ? (int?)0 : null))
				.ToList();

			return new LevelState(
				enemyStates: enemies,
				playerState: new PlayerState(levelRepresentation.FriendlyStartPos, 0, 
					levelRepresentation.SkillsStartingWith, null),
				skillsInAction: new List<IGameAction>(),
				Enumerable.Repeat<bool>(false, levelRepresentation.SkillsToPickup.Count).ToList()
				);
		}
	}

	public abstract class CharacterState {
		public readonly Vector2 Position;
		public readonly float Rotation;
		/// <summary>
		/// Those are the last actions and they were not cancellable, so finish them first and then proceed.
		/// </summary>
		public readonly IGameAction? PerformingAction;

		protected CharacterState(Vector2 position, float rotation, IGameAction? performingAction) {
			Position = position;
			Rotation = rotation;
			PerformingAction = performingAction;
		}
	}

	public class PlayerState : CharacterState {
		/// <summary>
		/// What skills can the player use.
		/// </summary>
		public readonly IReadOnlyList<IActiveGameActionProvider> AvailableSkills;

		public PlayerState(Vector2 position, float rotation, 
			IReadOnlyList<IActiveGameActionProvider> availableSkills,
			IGameAction? performingAction) 
			:base(position, rotation, performingAction){
			AvailableSkills = availableSkills;
		}

		public PlayerState Change(Vector2? position = null, float? rotation = null, 
			IReadOnlyList<IActiveGameActionProvider>? availableSkills = null,
			IGameAction? performingAction = null) {

			return new PlayerState(
				position ?? this.Position,
				rotation ?? this.Rotation,
				availableSkills ?? this.AvailableSkills,
				performingAction ?? this.PerformingAction);
		}
	}

	public class EnemyState : CharacterState {
		public int? PathIndex { get; }

		/// <summary>
		/// True when going through the path commands backwards.
		/// </summary>
		public bool IsReturning { get; }

		public bool Alive { get; }
		public bool Alerted { get; }

		public float TimeOfPlayerInView { get; }

		/// <summary>
		/// How much is the viewcone length towards the alerted length. (0 = NormalLengh; 1 = AlertedLength)
		/// </summary>
		public float ViewconeAlertLengthModifier { get; }

		public EnemyState(Vector2 position, float rotation, IGameAction? performingAction, bool alive, bool alerted, 
			float viewconeAlertLengthModifier, float timeOfPlayerInView, int? pathIndex = null, bool isReturning = false) 
			:base(position, rotation, performingAction) {

			PathIndex = pathIndex;
			Alive = alive;
			Alerted = alerted;
			ViewconeAlertLengthModifier = viewconeAlertLengthModifier;
			IsReturning = isReturning;
			TimeOfPlayerInView = timeOfPlayerInView;
		}

		public EnemyState Change(float? rotation = null, Vector2? position = null, 
			IGameAction? performingAction = null,
			bool? alive = null, bool? alerted = null, float? timeOfPlayerInView = null,
			float? viewconeAlertLengthModifier = null, int? pathIndex = null, bool? isReturning = null) {
			return new EnemyState(
				position ?? this.Position,
				rotation ?? this.Rotation,
				performingAction ?? this.PerformingAction,
				alive ?? this.Alive,
				alerted ?? this.Alerted,
				viewconeAlertLengthModifier ?? this.ViewconeAlertLengthModifier,
				timeOfPlayerInView ?? this.TimeOfPlayerInView,
				pathIndex ?? this.PathIndex,
				isReturning ?? this.IsReturning);
		}
	}
	
	public class LevelStateTimed : LevelState {
		/// <summary>
		/// When performing actions over level state, we need to know how much time we have. 
		/// This represents the remaning time.
		/// </summary>
		public float Time;

		public LevelStateTimed(LevelState state, float time) 
			:base(state.enemyStates, state.playerState, state.skillsInAction, state.pickupableSkillsPicked) {

			Time = time;
		}
	}

	public class CharacterIndex {
		public int Index { get; }
		public CharacterType CharacterType { get; }
	}

	public enum CharacterType {
		Enemy,
		Player
	}
}
