using System;
using System.Dynamic;
using _scripts.PlayerCharacter;
using _scripts.PlayerCharacter.StateMachine.States;
using _Scripts.StateMachine;
using UnityEngine;


public class PlayerStateMachine : StateMachine<PlayerCharacter>
{
    
    [Space(20)]
    public Pst_Idle s_Idle;
    public Pst_Walking s_Walking;
    public Pst_Falling s_Falling;
    public Pst_Jumping s_Jumping;
    public Pst_Dead s_dead;
    //public Pst_Running s_running;
    
    void SetUpStates()
    {
        s_Idle ??= new();
        s_Walking ??= new();
        s_Falling ??= new();
        s_Jumping ??= new();
        s_dead ??= new();
        //s_running ??= new();

        s_Idle.SetUp(this);
        s_Walking.SetUp(this);
        s_Falling.SetUp(this);
        s_Jumping.SetUp(this);
        s_dead.SetUp(this);
        //s_running.SetUp(this);
    }

    private void Awake()
    {
        SetUpStates();
    }

    private void Start()
    {
        TransitionTo(s_Idle); //le joueur commence en idle
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        //update physics after current state was updated
        context.physics.UpdatePhysics();
    }
}

