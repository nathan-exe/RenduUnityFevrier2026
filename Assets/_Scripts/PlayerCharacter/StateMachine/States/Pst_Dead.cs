using _Scripts.StateMachine;

namespace _scripts.PlayerCharacter.StateMachine.States
{
    public class Pst_Dead : PlayerState
    {
        public override StateBase<PlayerCharacter> FindNextState(global::_scripts.PlayerCharacter.PlayerCharacter ctx)
        {
            return this;
        }
        
    }
}