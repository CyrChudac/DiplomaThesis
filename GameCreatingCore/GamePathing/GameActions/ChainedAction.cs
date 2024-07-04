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
    /// <summary>
    /// Chains multiple actions of the same character together into one action.
    /// </summary>
    public class ChainedAction : IWithInnerActions
    {
        private readonly IReadOnlyList<IGameAction> _actions;
        int _index = 0;
        bool _started = false;
        public ChainedAction(IReadOnlyList<IGameAction> actions)
        {
            _actions = actions;
            bool first = true;
            int? enemyIndex = null;
            foreach(var a in actions) {
                if(first) {
                    first = false;
                    enemyIndex = a.EnemyIndex;
                }
                if(enemyIndex != a.EnemyIndex) {
                    throw new NotSupportedException("All actions in chained action have to have the same enemy index!");
                }
            }
            EnemyIndex = enemyIndex;
        }
        public bool IsIndependentOfCharacter => false; //_actions[_index].IsIndependentOfCharacter;

        //we need to check if started, because if _actions.Count == 0, then it would be always done
        public bool Done => _started && _index == _actions.Count;

        //ChainedAction adds noncancelable action into the state, so we do not need to make this uncancellable as well
        public bool IsCancelable => true;
        public float TimeUntilCancelable => 0;

		public int? EnemyIndex { get; }
        
		public IGameAction? CurrentInnerAction => _actions[_index];

        public LevelStateTimed CharacterActionPhase(LevelStateTimed input)
        {
            _started = true;
            var result = input;
            var skillsToPerformAdd = new List<IGameAction>();
            for (; _index != _actions.Count; _index++) 
                //this does not take into acount the performing skill, and it doesnt have to, since all
                //of the actions have the same enemy index
            {
                if(_actions[_index].Done) {
                    continue; 
                    //this is only necessary because noncancelable actions remain here even when game simulater finishes them
                }
                if(_actions[_index].IsIndependentOfCharacter) {
                    skillsToPerformAdd.Add(new StartAfterAction(_actions[_index], input.Time - result.Time, false));
                } else {
                    result = _actions[_index].CharacterActionPhase(result);
                    var lt = result.Time;
                    if(FloatEquality.LessOrEqual(lt, 0)) {
                        if(!_actions[_index].IsCancelable)
                        //strange, what should be set as performing action? 'this' or _actions[_index]?
                        {
                            if(_actions[_index].EnemyIndex.HasValue) {
                                result = new LevelStateTimed(
                                    result.ChangeEnemy(_actions[_index].EnemyIndex!.Value, 
                                        performingAction: _actions[_index]),
                                    lt);
                            } else {
                                result = new LevelStateTimed(
                                    result.ChangePlayer(performingAction: _actions[_index]), lt);
                            }
                        }
                        break;
                    } else {
                        if(_actions[_index].IsIndependentOfCharacter) {
                            skillsToPerformAdd.Add(_actions[_index]);
                        }
                    }
                }
            }
            skillsToPerformAdd.AddRange(result.skillsInAction); //order of skills in action should be arbitrary
            return new LevelStateTimed(
                result.Change(skillsInAction: skillsToPerformAdd),
                result.Time);
        }

		public void Reset() { 
            _index = 0;
            _started = false;
            foreach(var a in _actions) {
                a.Reset();
            }
		}
        
		public IEnumerable<IGameAction> GetInnerActions() {
			return _actions;
		}

        public LevelStateTimed AutonomousActionPhase(LevelStateTimed input)
        {
            throw new Exception("This should never happen.");
        }
        
		public override string ToString() {
			var nameChars = nameof(ChainedAction).Where(c => c.ToString() == c.ToString().ToUpper());
			var name = string.Concat(nameChars);
            string text = string.Empty;
            for(int i = 0; i < _actions.Count; i++) {
                if(i == 3) {
                    text += "...";
                    break;
                }
                text += _actions[i].ToString();
                if(i < _actions.Count - 1)
                    text += " |";
            }
			return $"{name}({_actions.Count}): {{{text}}}";
		}

		public IGameAction Duplicate() {
            var actions = _actions
                .Select(a => a.Duplicate())
                .ToList();
            var result = new ChainedAction(actions);
            result._index = _index;
            return result;
		}
	}
}