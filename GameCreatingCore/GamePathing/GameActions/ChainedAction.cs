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

    public class ChainedAction : IGameAction
    {
        private readonly IReadOnlyList<IGameAction> _actions;
        int _index = 0;
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
        }
        public bool IsIndependentOfCharacter => false; //_actions[_index].IsIndependentOfCharacter;

        public bool Done => _index == _actions.Count;

        public bool IsCancelable => _actions[_index].IsCancelable; //not sure about this, think it through

        public float LeftoverPlayerTime => throw new NotImplementedException();
		public int? EnemyIndex => _actions[_index].EnemyIndex;

        public LevelStateTimed CharacterActionPhase(LevelStateTimed input)
        {
            var result = input;
            var skillsToPerform = input.skillsInAction.ToList();
            while (true) 
                //this does not take into acount the performing skill, and it doesnt have to, since all
                //of the actions have the same enemy index
            {
                if (_index == _actions.Count)
                    break;
                if (_actions[_index].IsIndependentOfCharacter)
                {
                    skillsToPerform.Add(new StartAfterAction(_actions[_index], input.Time - result.Time));
                    continue;
                }
                result = _actions[_index].CharacterActionPhase(result);
                var lt = result.Time;
                if (lt <= 0){
                    if (!_actions[_index].IsCancelable) 
                        //strange, what should be set as performing action? 'this' or _actions[_index]?
                    {
                        if(_actions[_index].EnemyIndex.HasValue) {
                            result = new LevelStateTimed(
                                result.ChangeEnemy(_actions[_index].EnemyIndex!.Value, performingAction: _actions[_index]),
                                lt);
                        } else {
                            result = new LevelStateTimed(
                                result.ChangePlayer(performingAction: _actions[_index]), lt);
                        }
                    }
                    break;
                }
                else
                {
                    if (_actions[_index].IsIndependentOfCharacter)
                    {
                        skillsToPerform.Add(_actions[_index]);
                    }
                }
                _index++;
            }
            return new LevelStateTimed(
                result.Change(skillsInAction: skillsToPerform),
                result.Time);
        }

        public LevelStateTimed AutonomousActionPhase(LevelStateTimed input)
        {
            throw new NotImplementedException();
        }

	}
}