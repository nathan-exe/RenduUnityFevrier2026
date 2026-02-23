using System;
using _Scripts.StateMachine;
using UnityEngine;

namespace _scripts.PlayerCharacter.StateMachine.States
{
    [Serializable]
    public class Pst_Falling : Pst_Airborne
    {
        [Header("Coyote jump")]
        [SerializeField] private float _coyoteJumpDuration;
        
        private float _stateEnteredTime;
        private bool _hasEntered;
        
        public override StateBase<PlayerCharacter> FindNextState(PlayerCharacter ctx)
        {
            //coyote Jump
            if (_hasEntered && ctx.physics.Velocity.y < 0 && Time.time-_stateEnteredTime < _coyoteJumpDuration && ctx.inputs.TryConsumeJumpKeyPress())
                return Sm.s_Jumping;
            
            //ground
            if (ctx.physics.Velocity.y<=0 && ctx.physics.ComputeIsGrounded())
                return Sm.s_Idle;
            
            return base.FindNextState(ctx);
        }

        protected override void OnEntered(PlayerCharacter ctx)
        {
            base.OnEntered(ctx);
            _stateEnteredTime = Time.time;
            _hasEntered = true;
        }
        
        protected override void OnExited(PlayerCharacter ctx)
        {
            _hasEntered = false;
            base.OnExited(ctx);
        }
        
        public override void Behave(PlayerCharacter ctx, UpdatePoint updatePoint)
        {
            ApplyAirControls(ctx);
        }

        
    }
}