using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.StaticSettings
{
    public class StaticGameRepresentation
    {
        private IDictionary<EnemyType, EnemySettings> _dict;
        public StaticGameRepresentation(
            IDictionary<EnemyType, EnemySettings> dict, 
            int gameDiff, 
            StaticMovementSettings movementSettings,
            PlayerSettings playerSettings)
        {
            _dict = dict;
            GameDifficulty = gameDiff;
            this.movementSettings = movementSettings;
            this.playerSettings = playerSettings;
        }


        public EnemySettingsProcessed GetEnemySettings(EnemyType enemy)
            => _dict[enemy].ToProcessed(movementSettings);

        private PlayerSettings playerSettings { get; }
        public PlayerSettingsProcessed PlayerSettings => playerSettings.ToProcessed(movementSettings);

        public int GameDifficulty { get; }

        private StaticMovementSettings movementSettings;
    }
}