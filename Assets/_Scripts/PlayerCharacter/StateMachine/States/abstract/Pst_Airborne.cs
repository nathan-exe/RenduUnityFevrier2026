using _Scripts.StateMachine;
using UnityEngine;

namespace _scripts.PlayerCharacter.StateMachine.States
{
    public abstract class Pst_Airborne : Pst_Alive
    {
        [Header("Movement Settings")]
        [SerializeField] private float _airSpeed;
        [SerializeField] private float _acceleration;

        public override StateBase<PlayerCharacter> FindNextState(PlayerCharacter ctx)
        {
            return base.FindNextState(ctx);
        }

        protected void ApplyAirControls(PlayerCharacter ctx)
        {
            //compute target velocity
            Vector3 targetVelocity = 
                ctx.inputs.movementInput.Value.x*transform.right 
                + ctx.inputs.movementInput.Value.y * transform.forward; 
            targetVelocity *= _airSpeed;
            
            //apply movement
            ctx.movement.Move(targetVelocity,_acceleration * Time.deltaTime,true);
        }
    }
}