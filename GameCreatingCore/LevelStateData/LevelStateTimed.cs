namespace GameCreatingCore.LevelStateData
{

    public class LevelStateTimed : LevelState
    {
        /// <summary>
        /// When performing actions over level state, we need to know how much time we have. 
        /// This represents the remaning time.
        /// </summary>
        public float Time;

        public LevelStateTimed(LevelState state, float time)
            : base(state.enemyStates, state.playerState, state.skillsInAction, state.pickupableSkillsPicked)
        {

            Time = time;
        }
        public override string ToString()
        {
            return base.ToString() + $"; T:{Time}";
        }
    }
}