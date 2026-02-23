using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.StateMachine
{
    public abstract class StateBase<T>
    {
    
        [Header(("State Logic"))]
        public UpdatePoint _updatePoint = UpdatePoint.Never;
        public bool _passThrough = true;
        
        [HideInInspector] protected StateMachine<T> stateMachine { get; set ; }
    
        //notifiers
        public event Action EventOnEntered;
        public event Action EventOnExited;
        
        public virtual void SetUp(StateMachine<T> sm)
        {
            this.stateMachine = sm;
        }

        public void Update(T ctx,UpdatePoint updatePoint) 
        {
            StateBase<T> newState = FindNextState(ctx);
            if (newState != this)
            {
                TransitionTo(newState);
            }
            else
            {
                Behave(ctx,updatePoint);
            }
        }
    
        private void TransitionTo(StateBase<T> nextState)
        {
            //Debug.Log("About to transition to some new state");
        
            //error check
            Assert.IsTrue(stateMachine.currentState == this,
                "this state ("+GetType().ToString() + ") isn't currently active and cannot transition to next state : " + nextState.GetType().ToString());

            stateMachine.TransitionTo(nextState);
        }

    
        // == abstract methods ==
    
        /// <summary>
        /// is always called before the state behaves,
        /// and before entering the state if passThrough is set to true.
        /// should return "base.FindNextState(ctx)" or "this" by default.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public abstract StateBase<T> FindNextState(T ctx); //should return "this" by default
        
        public void Enter(T ctx)
        {
            OnEntered(ctx);
            try
            {
                EventOnEntered?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

        }
        public void Exit(T ctx)
        {
            OnExited(ctx);

            try
            {
                EventOnExited?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
        }
        
        protected virtual  void OnEntered(T ctx){}
        protected virtual  void OnExited(T ctx){}
        public virtual void Behave(T ctx, UpdatePoint updatePoint){}

        protected void Print(string msg)
        {
            Debug.Log(msg,stateMachine);
        }
    }
}


