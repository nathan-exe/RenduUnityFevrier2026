using System;
using _scripts.PlayerCharacter;
using _Scripts.StateMachine;
using UnityEngine;

public abstract class PlayerState : StateBase<PlayerCharacter>
{
    public Transform transform => Sm.context.transform; 
    
    [HideInInspector] protected PlayerStateMachine Sm { get; private set ; }
        
    public override void SetUp(StateMachine<PlayerCharacter> sm)
    {
        Sm = (PlayerStateMachine)sm;
        base.stateMachine = sm;
    }
        
    protected PlayerState()
    {
        _updatePoint = UpdatePoint.Update;
    }

    public override StateBase<PlayerCharacter> FindNextState(PlayerCharacter ctx)
    {
        return this;
    }
    // protected void ApplyGroundFriction(DynamicObject physics,float friction)
    // {
    //     Vector3 vel = physics.velocity.MoveToward(Vector3.zero,  friction * Time.deltaTime);
    //     physics.SetVelocity(vel);
    // }
}
