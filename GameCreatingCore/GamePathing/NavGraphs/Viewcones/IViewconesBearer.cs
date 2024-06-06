using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs.Viewcones
{
    public interface IViewconesBearer
    {
        List<List<Vector2>> GetRawViewcones(LevelState state);
    }
}
