using System;
using _Scripts.StateMachine;
using UnityEngine;

namespace _scripts.PlayerCharacter.StateMachine.States
{
    [Serializable]
    public class Pst_Walking : Pst_Grounded
    {
        [Header("Movement Settings")]
        [SerializeField] public float _walkSpeed;
        [SerializeField] private float _acceleration;
        [SerializeField] private float _brakeStrength = 2;
        
        public override StateBase<global::_scripts.PlayerCharacter.PlayerCharacter> FindNextState(global::_scripts.PlayerCharacter.PlayerCharacter ctx)
        {
            return base.FindNextState(ctx); 
        }

        public override void Behave(global::_scripts.PlayerCharacter.PlayerCharacter ctx, UpdatePoint updatePoint)
        {
            //compute target velocity
            Vector3 targetVelocity = 
                ctx.inputs.movementInput.Value.x*transform.right 
                + ctx.inputs.movementInput.Value.y * transform.forward; 
            targetVelocity *= _walkSpeed;
            
            //compute acceleration
            float accelerationScale = Vector3.Dot(ctx.physics.Velocity.normalized, targetVelocity.normalized);
            accelerationScale = Mathf.Abs(accelerationScale) * accelerationScale<0 ? _brakeStrength:1;
            
            //apply movement
            ctx.movement.Move(targetVelocity,_acceleration * accelerationScale * Time.deltaTime,true);
        }
        
    }
}