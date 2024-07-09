using System;

namespace GameCreatingCore.StaticSettings {
	[Serializable]
	public class StaticMovementSettings {
		/// <summary>
		/// The baseline speed of all characters. 
		/// </summary>
		[UnityEngine.SerializeField]
		public float Speed;
		
        /// <summary>
        /// Maximal radius of all characters considering all lines going through theoretical middle of the character.
        /// </summary>
		[UnityEngine.SerializeField]
        public float CharacterMaxRadius;

		public StaticMovementSettingsProcessed ToProcessed()
			=> new StaticMovementSettingsProcessed(CharacterMaxRadius);
	}

	
	public class StaticMovementSettingsProcessed {
		
        /// <summary>
        /// Maximal radius of all characters considering all lines going through theoretical middle of the character.
        /// </summary>
        public readonly float CharacterMaxRadius;

		internal StaticMovementSettingsProcessed(float characterMaxRadius) {
			this.CharacterMaxRadius = characterMaxRadius;
		}
	}
}
