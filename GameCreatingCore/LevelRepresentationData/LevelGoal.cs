using GameCreatingCore.LevelStateData;
using UnityEngine;

namespace GameCreatingCore.LevelRepresentationData
{

    [System.Serializable]
    public sealed class LevelGoal
    {
        [SerializeField]
        public Vector2 Position;
        [SerializeField]
        public float Radius;

        public LevelGoal(Vector2 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public override string ToString()
        {
            return $"Goal: {Position}; {Radius}";
        }

        public bool IsAchieved(LevelState levelState)
            => (levelState.playerState.Position - Position).sqrMagnitude < Radius * Radius;
    }
}