using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.LevelRepresentationData
{
    public class ObstacleEffect
    {
        [SerializeField]
        public WalkObstacleEffect FriendlyWalkEffect;
        [SerializeField]
        public WalkObstacleEffect EnemyWalkEffect;
        [SerializeField]
        public VisionObstacleEffect FriendlyVisionEffect;
        [SerializeField]
        public VisionObstacleEffect EnemyVisionEffect;

        public ObstacleEffect(WalkObstacleEffect friendlyWalkEffect, WalkObstacleEffect enemyWalkEffect,
            VisionObstacleEffect friendlyVisionEffect, VisionObstacleEffect enemyVisionEffect)
        {

            FriendlyWalkEffect = friendlyWalkEffect;
            EnemyWalkEffect = enemyWalkEffect;
            FriendlyVisionEffect = friendlyVisionEffect;
            EnemyVisionEffect = enemyVisionEffect;
        }
        public override string ToString()
        {
            string s = "OEff(";
            bool ch = false;
            string w = "W: ";
            if (FriendlyWalkEffect == WalkObstacleEffect.Unwalkable)
            {
                w += "F";
                ch = true;
            }
            if (EnemyWalkEffect == WalkObstacleEffect.Unwalkable)
            {
                w += "E";
                ch = true;
            }
            if (ch)
                s += w;
            ch = false;
            string v = "; V: ";
            if (FriendlyVisionEffect == VisionObstacleEffect.NonSeeThrough)
            {
                v += "F";
                ch = true;
            }
            if (EnemyVisionEffect == VisionObstacleEffect.NonSeeThrough)
            {
                v += "E";
                ch = true;
            }
            if (ch)
                s += v;
            return s + ")";
        }

    }
}
