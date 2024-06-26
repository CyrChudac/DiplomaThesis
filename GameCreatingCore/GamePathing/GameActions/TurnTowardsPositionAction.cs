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

        private bool started = false;
        private bool shouldTurn = true;

        public LevelStateTimed CharacterActionPhase(LevelStateTimed input)
        {
            if (IsIndependentOfCharacter)
            {
                return input;
            }
            else
            {
                return TurnAndManage(input);
            }
        }

        public LevelStateTimed AutonomousActionPhase(LevelStateTimed input)
        {
            if (IsIndependentOfCharacter)
            {
                return TurnAndManage(input);
            }
            else
                return input;
        }

        private LevelStateTimed TurnAndManage(LevelStateTimed input) {
            if(!started) {
                started = true;
                shouldTurn = RemoveLowerPriorityTurning(input, out input);
            }
            if(!shouldTurn) {
                _done = true;
                return input;
            }
            var res = Turn(input);
            _done = res.Time > 0;
            return res;
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
            if(pos == position)
                return input;
            var angle = Vector2Utils.AngleTowards(pos, position);

            var side = ComputeTurnDirection(turnSide, currRotation, angle);

            var maxChange = Math.Min(movementSettings.TurningSpeed * input.Time, 360);

            float actualChange;

            if(side == TurnSideEnum.Anticlockwise) {
                if(currRotation + maxChange - 360 > angle) {
                    actualChange = (360 - currRotation) + angle;
                    currRotation = angle;
                } else if(angle > currRotation && angle < currRotation + maxChange) {
                    actualChange = angle - currRotation;
                    currRotation = angle;
                } else {
                    actualChange = maxChange;
                    currRotation += maxChange;
                    currRotation %= 360;
                }
            }else if (side == TurnSideEnum.Clockwise) {
                if(currRotation - maxChange + 360 < angle) {
                    actualChange = currRotation + (360 - angle);
                    currRotation = angle;
                } else if(angle < currRotation && angle > currRotation - maxChange) {
                    actualChange = currRotation - angle;
                    currRotation = angle;
                } else {
                    actualChange = maxChange;
                    currRotation -= maxChange;
                    if(currRotation < 0)
                        currRotation += 360;
                }
            } else {
                throw new NotImplementedException($"{nameof(TurnSideEnum)} with value '{side}' is not implemented.");
            }

            float leftoverTime = input.Time - actualChange / movementSettings.TurningSpeed;
            if(FloatEquality.Equals(leftoverTime, 0)) {
                leftoverTime = 0;
            }
            LevelState result;
            if(EnemyIndex.HasValue) {
                result = input.ChangeEnemy(EnemyIndex.Value, rotation: currRotation);
            } else {
                result = input.ChangePlayer(rotation: currRotation);
            }
            return new LevelStateTimed(result, leftoverTime);
        }
        
        private static TurnSideEnum ComputeTurnDirection(TurnSideEnum input, float currAngle, float desiredAngle) {
            if(input == TurnSideEnum.Clockwise || input == TurnSideEnum.Anticlockwise)
                return input;
            if(input == TurnSideEnum.ShortestPrefereClockwise || input == TurnSideEnum.ShortestPrefereAntiClockwise) {
                float c;
                if(currAngle >= desiredAngle) {
                    c = currAngle - desiredAngle;
                } else {
                    c = currAngle + 360 - desiredAngle;
                }
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

        /// <summary>
        /// Looks into the <paramref name="input"/> in the skillsInAction on the unit this should turn and removes all 
        /// instances of with lower priority than this. If any has higher, returns <paramref name="input"/> and false.
        /// </summary>
        private bool RemoveLowerPriorityTurning(LevelStateTimed input, out LevelStateTimed stateWithRemoved) {
            List<IGameAction> inAction = new List<IGameAction>();
            foreach(var p in input.skillsInAction) {
                if(p is TurnTowardsPositionAction) {
                    TurnTowardsPositionAction ttpa = (TurnTowardsPositionAction)p;
                    if(ttpa.Priority < Priority)
                        continue;
                    else {
                        stateWithRemoved = input;
                        return false;
                    }
                }
                inAction.Add(p);
            }
            stateWithRemoved = new LevelStateTimed(input.Change(skillsInAction: inAction), input.Time);
            return true;
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