using System;
using _Scripts.StateMachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace _scripts.PlayerCharacter.StateMachine.States
{
    [Serializable]
    public class Pst_Jumping : Pst_Airborne
    {
        private float _stateEnteredTime;

        [SerializeField] private float _jumpDuration; 
        [Space(8)]
        [SerializeField] private float _initialImpulseStrength; 
        [Range(0,1)][SerializeField] private float _jumpDirectionResponsivness = .35f; 
        [Space(8)]
        [SerializeField] private float _thrustPower; 
        //[SerializeField] private AnimationCurve _thrustStrengthOverTime; 
        
        private bool _hasEntered;
        
        public override StateBase<PlayerCharacter> FindNextState(PlayerCharacter ctx)
        {
            if (!ctx.inputs.IsHoldingJumpKey || _hasEntered && Time.time - _stateEnteredTime > _jumpDuration )
                return Sm.s_Falling;
            return base.FindNextState(ctx);
        }

        protected override void OnEntered(PlayerCharacter ctx)
        {
            _hasEntered = true;
            _stateEnteredTime = Time.time;

            //reset falling speed
            ctx.physics.SetVelocity(new Vector3(ctx.physics.Velocity.x, Mathf.Max( ctx.physics.Velocity.y,0), ctx.physics.Velocity.z));
            
            //align velocity with input direction
            Vector3 movementInputws = transform.TransformDirection(Vector3.ClampMagnitude(new Vector3(ctx.inputs.movementInput.Value.x, 0, ctx.inputs.movementInput.Value.y), 1));
            ctx.physics.SetVelocity(
                Vector3.Lerp(ctx.physics.Velocity, movementInputws * ctx.physics.Velocity.magnitude,_jumpDirectionResponsivness));
            
            //jump
            ctx.physics.AddImpulse(Vector3.up * _initialImpulseStrength);
            
            base.OnEntered(ctx);
        }

        protected override void OnExited(PlayerCharacter ctx)
        {
            _hasEntered = false;
            base.OnExited(ctx);
        }

        public override void Behave(PlayerCharacter ctx, UpdatePoint updatePoint)
        {
            ApplyAirControls(ctx);

            float alpha =  (Time.time-_stateEnteredTime) / _jumpDuration;
            ctx.physics.AddForce(Vector3.up * (_thrustPower * (1f-alpha)));
            //ctx.physics.AddForce(Vector3.up * (_thrustPower * _thrustStrengthOverTime.Evaluate(alpha)));
            
            base.Behave(ctx, updatePoint);
        }
    }
}