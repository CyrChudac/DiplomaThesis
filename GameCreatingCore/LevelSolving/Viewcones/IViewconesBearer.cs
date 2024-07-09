using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GameCreatingCore.LevelStateData;

namespace GameCreatingCore.LevelSolving.Viewcones
{

    public interface IViewconesBearer
    {
        List<List<Vector2>> GetRawViewcones(LevelState state);
    }
}
