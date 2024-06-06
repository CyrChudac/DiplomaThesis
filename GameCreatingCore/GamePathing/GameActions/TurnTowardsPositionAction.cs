using GameCreatingCore.GamePathing.NavGraphs.Viewcones;
using GameCreatingCore.StaticSettings;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.Windows;

namespace GameCreatingCore.GamePathing.GameActions
{

    public class TurnTowardsPositionAction : IGameAction
    {
        public int? EnemyIndex { get; }
        readonly MovementSettingsProcessed movementSettings;
        readonly Vector2 position;
        readonly TurnSideEnum turnSide; //has to be reversed for pathing backwards
        public bool IsIndependentOfCharacter { get; }
        public int Priority { get; }

        public TurnTowardsPositionAction(int? enemyIndex, MovementSettingsProcessed movementSettings,
            bool isIndependentOfCharacter, int priority, Vector2 position, TurnSideEnum turnSide)
        {

            this.EnemyIndex = enemyIndex;
            this.movementSettings = movementSettings;
            IsIndependentOfCharacter = isIndependentOfCharacter;
            Priority = priority;
            this.position = position;
            this.turnSide = turnSide;
        }

        private bool _done = false;
        public bool Done => _done;

        public bool IsCancelable => true;

        public LevelStateTimed CharacterActionPhase(LevelStateTimed input)
        {
            if (IsIndependentOfCharacter)
            {
                return input;
            }
            else
            {
                var res = Turn(input);
                _done = res.Time > 0;
                return res;
            }
        }

        public LevelStateTimed AutonomousActionPhase(LevelStateTimed input)
        {
            if (IsIndependentOfCharacter)
            {
                var res = Turn(input);
                _done = res.Time > 0;
                return res;
            }
            else
                return input;
        }

        private LevelStateTimed Turn(LevelStateTimed input)
        {
            Vector2 pos;
            float currRotation;
            if(EnemyIndex.HasValue) {
                pos = input.enemyStates[EnemyIndex.Value].Position;
                currRotation = input.enemyStates[EnemyIndex.Value].Rotation;
            } else {
                pos = input.playerState.Position;
                currRotation = input.playerState.Rotation;
            }
            var angle = AngleTowards(pos, position);

            var side = ComputeTurnDirection(turnSide, currRotation, angle);

            var angleChange = movementSettings.TurningSpeed * input.Time;

            float actualChange;

            if(side == TurnSideEnum.Anticlockwise) {
                currRotation = angle;
                if(currRotation + angleChange - 360 > angle)
                    actualChange = (360 - currRotation) + angle;
                else if(angle > currRotation && angle < currRotation + angleChange) {
                    actualChange = angle - currRotation;
                } else {
                    actualChange = angleChange;
                    currRotation += angleChange;
                    currRotation %= 360;
                }
            }else if (side == TurnSideEnum.Clockwise) {
                currRotation = angle;
                if(currRotation - angleChange + 360 < angle)
                    actualChange = currRotation + (360 - angle);
                else if(angle < currRotation && angle > currRotation - angleChange) {
                    actualChange = currRotation - angle;
                } else {
                    actualChange = angleChange;
                    currRotation -= angleChange;
                    while(currRotation < 0)
                        currRotation += 360;
                }
            } else {
                throw new NotImplementedException($"{nameof(TurnSideEnum)} with value '{side}' is not implemented.");
            }

            float leftoverTime = input.Time - actualChange / movementSettings.TurningSpeed;
            LevelState result;
            if(EnemyIndex.HasValue) {
                var enems = input.enemyStates.ToList();
                enems[EnemyIndex.Value] = enems[EnemyIndex.Value].Change(rotation: currRotation);
                result = input.Change(enemyStates: enems);
            } else {
                result = input.ChangePlayer(rotation: currRotation);
            }
            return new LevelStateTimed(result, leftoverTime);
        }
        
        private static TurnSideEnum ComputeTurnDirection(TurnSideEnum input, float currAngle, float desiredAngle) {
            if(input == TurnSideEnum.Clockwise || input == TurnSideEnum.Anticlockwise)
                return input;
            if(input == TurnSideEnum.ShortestPrefereClockwise || input != TurnSideEnum.ShortestPrefereAntiClockwise) {
                var c = Math.Max(currAngle - desiredAngle, 360 - (desiredAngle - currAngle));
                var a = 360 - c;
                if(a > c)
                    return TurnSideEnum.Clockwise;
                if(a < c)
                    return TurnSideEnum.Anticlockwise;
                if(input == TurnSideEnum.ShortestPrefereClockwise)
                    return TurnSideEnum.Clockwise;
                return TurnSideEnum.Anticlockwise;
            }
            throw new NotImplementedException($"{nameof(TurnSideEnum)} with value '{input}' is not implemented.");
        }

        //should be computed always, the character could be moving during turning which can change the desired angle
        /// <returns>Angle in degrees <paramref name="from"/> -> <paramref name="towards"/> 
        /// compared to base rotation 0.</returns>
        private static float AngleTowards(Vector2 from, Vector2 towards) {
            return 0; //TODO: compute angle
        }

        /// <summary>
        /// Looks into the <paramref name="input"/> in the skillsInAction on the unit this should turn and removes all 
        /// instances of with lower priority than this. If any has higher, returns <paramref name="input"/> and false.
        /// </summary>
        private (LevelStateTimed, bool) RemoveLowerPriorityTurning(LevelStateTimed input) {
            List<IGameAction> inAction = new List<IGameAction>();
            foreach(var p in input.skillsInAction) {
                if(p is TurnTowardsPositionAction) {
                    TurnTowardsPositionAction ttpa = (TurnTowardsPositionAction)p;
                    if(ttpa.Priority <= Priority)
                        continue;
                    else {
                        return (input, false);
                    }
                }
                inAction.Add(p);
            }
            return (new LevelStateTimed(input.Change(skillsInAction: inAction), input.Time), true);
        }
    }

    public enum TurnSideEnum {
        ShortestPrefereClockwise,
        ShortestPrefereAntiClockwise,
        Clockwise,
        Anticlockwise
    }

    public static class TurnSideEnum_Extensions {
        public static TurnSideEnum Opposite(this TurnSideEnum side) {
			switch(side) {
				case TurnSideEnum.ShortestPrefereClockwise:
					return TurnSideEnum.ShortestPrefereAntiClockwise;
				case TurnSideEnum.ShortestPrefereAntiClockwise:
					return TurnSideEnum.ShortestPrefereClockwise;
				case TurnSideEnum.Clockwise:
					return TurnSideEnum.Anticlockwise;
				case TurnSideEnum.Anticlockwise:
					return TurnSideEnum.Clockwise;
                default:
                    throw new NotImplementedException($"{nameof(Opposite)} for " +
                        $"{nameof(TurnSideEnum)} with value {side} not implemented.");
			}
		}
	}
}