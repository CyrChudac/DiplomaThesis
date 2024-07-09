using System.Collections.Generic;
using GameCreatingCore.LevelRepresentationData;

namespace GameCreatingCore.GameActions
{

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