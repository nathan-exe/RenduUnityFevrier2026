using System;
using _Scripts.StateMachine;
using UnityEngine;

namespace _scripts.PlayerCharacter.StateMachine.States
{
    [Serializable]
    public class Pst_Idle : Pst_Grounded
    {
        [Header("Movement Settings")]
        [SerializeField] float _groundFriction;
        public override StateBase<PlayerCharacter> FindNextState(PlayerCharacter ctx)
        {
            if (ctx.inputs.movementInput.Value != Vector2.zero)
                return Sm.s_Walking;
            return base.FindNextState(ctx);
        }

        public override void Behave(PlayerCharacter ctx, UpdatePoint updatePoint)
        {
            ctx.movement.ApplyFriction(_groundFriction * Time.deltaTime,true);
        }
    }
}