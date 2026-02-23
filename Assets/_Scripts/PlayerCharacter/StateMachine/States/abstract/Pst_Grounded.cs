using System;
using _Scripts.StateMachine;
using UnityEngine;

namespace _scripts.PlayerCharacter.StateMachine.States
{
    public abstract class Pst_Grounded : Pst_Alive
    {
        public override StateBase<PlayerCharacter> FindNextState(global::_scripts.PlayerCharacter.PlayerCharacter ctx)
        {
            StateBase<PlayerCharacter> nextState = base.FindNextState(ctx);
            if (nextState != this) return nextState;
            
            //fall if in air
            if (!ctx.physics.ComputeIsGrounded())
                return Sm.s_Falling;
            
            //Jump
            if (ctx.inputs.TryConsumeJumpKeyPress())
                return Sm.s_Jumping;
            
            //idle if no input
            if (ctx.inputs.movementInput.Value == Vector2.zero)
                return Sm.s_Idle;
            
            return Sm.s_Walking;
            
        }
    }
}