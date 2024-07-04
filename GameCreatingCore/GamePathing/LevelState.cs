using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameCreatingCore.GamePathing.GameActions;
using GameCreatingCore.GamePathing.NavGraphs;
using GameCreatingCore.StaticSettings;
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
		
		public LevelState DuplicateActions() {
			
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
			IReadOnlyList<bool>? pickupablesPickedUp = null) {
			return new LevelState(
				enemyStates ?? this.enemyStates,
				playerState ?? this.playerState,
				skillsInAction ?? this.skillsInAction,
				pickupablesPickedUp ?? this.pickupableSkillsPicked);
		}

		public LevelState ChangePlayer(Vector2? position = null, float? rotation = null, 
			IReadOnlyList<IActiveGameActionProvider>? availableSkills = null,
			IGameAction? performingAction = null, bool performingActionToNull = false) {
			return Change(playerState: playerState.Change(position, rotation, availableSkills, performingAction, performingActionToNull));
		}

		public LevelState ChangeEnemy(int index, float? rotation = null, Vector2? position = null, 
			IGameAction? performingAction = null, bool performingActionToNull = false,
			bool? alive = null, bool? alerted = null, float? timeOfPlayerInView = null,
			IGameAction? currentPathAction = null, bool removePathAction = false,
			float? viewconeAlertLengthModifier = null, int? pathIndex = null, bool? isReturning = null) {

			var enems = enemyStates.ToList();
			enems[index] = enems[index].Change(rotation, position, performingAction, performingActionToNull, alive,
				alerted, timeOfPlayerInView, currentPathAction, removePathAction, viewconeAlertLengthModifier, pathIndex, isReturning);
			return Change(enemyStates: enems);
		}

		
		public static LevelState GetInitialLevelState(LevelRepresentation levelRepresentation,
			StaticGameRepresentation staticGameRepresentation, StaticNavGraph staticNavGraph) {

			List<EnemyState> enemies = levelRepresentation.Enemies
				.Select((e, i) => new EnemyState(
					position: e.Position, 
					rotation: e.Rotation,
					performingAction: null,
					alive: true,
					alerted: false,
					viewconeAlertLengthModifier: 0,
					timeOfPlayerInView: 0,
					currentPathAction: (e.Path != null && e.Path.Commands.Count > 0) ? e.Path!.Commands[0]
						.GetAction(i, e.Position, staticGameRepresentation, staticNavGraph, levelRepresentation, false) : null,
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
		public override string ToString() {
			string toPick = pickupableSkillsPicked.Count > 0 ? ("; P" + pickupableSkillsPicked.Where(s => !s).Count()) : "";
			string playerAction = playerState.PerformingAction != null ? "!" : "";
			string enems = $"{enemyStates.Where(e => e.Alive).Count()}/{enemyStates.Count}";

			return $"{this.GetType().Name}: {playerState.Position}{playerAction}; E{enems}{toPick}";
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

	public sealed class PlayerState : CharacterState {
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
			IGameAction? performingAction = null, bool performingActionToNull = false) {

			return new PlayerState(
				position ?? this.Position,
				rotation ?? this.Rotation,
				availableSkills ?? this.AvailableSkills,
				performingActionToNull ? null : performingAction ?? this.PerformingAction);
		}

		public override string ToString() {
			return $"{nameof(PlayerState)}: {Position}; {Rotation}";
		}

		public PlayerState Duplicate() {
			return new PlayerState(Position, Rotation, AvailableSkills, PerformingAction?.Duplicate());
		}
	}

	public sealed class EnemyState : CharacterState {
		public int? PathIndex { get; }

		/// <summary>
		/// True when going through the path commands backwards.
		/// </summary>
		public bool IsReturning { get; }

		public bool Alive { get; }
		public bool Alerted { get; }

		public float TimeOfPlayerInView { get; }

		public IGameAction? CurrentPathAction { get; }

		/// <summary>
		/// How much is the viewcone length towards the alerted length. (0 = NormalLengh; 1 = AlertedLength)
		/// </summary>
		public float ViewconeAlertLengthModifier { get; }

		public EnemyState(Vector2 position, float rotation, IGameAction? performingAction, bool alive, bool alerted, 
			float viewconeAlertLengthModifier, float timeOfPlayerInView, IGameAction? currentPathAction,
			int? pathIndex = null, bool isReturning = false) 
			:base(position, rotation, performingAction) {

			PathIndex = pathIndex;
			Alive = alive;
			Alerted = alerted;
			ViewconeAlertLengthModifier = viewconeAlertLengthModifier;
			IsReturning = isReturning;
			TimeOfPlayerInView = timeOfPlayerInView;
			CurrentPathAction = currentPathAction;
		}

		public EnemyState Change(float? rotation = null, Vector2? position = null, 
			IGameAction? performingAction = null, bool performingActionToNull = false,
			bool? alive = null, bool? alerted = null, float? timeOfPlayerInView = null,
			IGameAction? currentPathAction = null, bool removePathAction = false,
			float? viewconeAlertLengthModifier = null, int? pathIndex = null, bool? isReturning = null) {
			return new EnemyState(
				position ?? this.Position,
				rotation ?? this.Rotation,
				performingActionToNull ? null : performingAction ?? this.PerformingAction,
				alive ?? this.Alive,
				alerted ?? this.Alerted,
				viewconeAlertLengthModifier ?? this.ViewconeAlertLengthModifier,
				timeOfPlayerInView ?? this.TimeOfPlayerInView,
				removePathAction ? null : currentPathAction ?? this.CurrentPathAction,
				pathIndex ?? this.PathIndex,
				isReturning ?? this.IsReturning);
		}

		public override int GetHashCode() {
			return (int)((Alive ? 1 : 0)
				+ (Rotation * 2)
				+ (Position.GetHashCode() * 721));
		}
		public override string ToString() {
			if(!Alive) {
				return $"{nameof(EnemyState)}: Dead";
			}
			return $"{nameof(EnemyState)}: {Position}; {Rotation}";
		}

		public EnemyState Duplicate() {
			return new EnemyState(
				Position,
				Rotation,
				PerformingAction?.Duplicate(),
				Alive,
				Alerted,
				ViewconeAlertLengthModifier,
				TimeOfPlayerInView,
				CurrentPathAction?.Duplicate(),
				PathIndex,
				IsReturning);
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
		public override string ToString() {
			return base.ToString() + $"; T:{Time}";
		}
	}
}
