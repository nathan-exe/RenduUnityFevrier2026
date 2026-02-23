using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.StateMachine
{
    public class StateMachine<T> : MonoBehaviour
    {
        private const int MaxPassThroughDepth = 10;

        /// <summary>
        /// stateBase oldState, StateBase newState
        /// </summary>
        public event Action<StateBase<T>,StateBase<T>> OnStateChanged; 
    
        [SerializeField] public T context;
        protected T ctx => context;
        [HideInInspector] public StateBase<T> currentState { get; private set; }
        public string CurrentStateName => currentState?.ToString() ?? "null";
        
        [HideInInspector] public StateBase<T> StartState { get; set; }

        //== Updates ==
        protected virtual void Update()
        {
            Assert.IsNotNull(currentState, "This state machine has no active state !");
            if ((currentState._updatePoint & ~UpdatePoint.Update) != 0)
                currentState.Update(context,UpdatePoint.Update);
        }

        void LateUpdate()
        {
            Assert.IsNotNull(currentState, "This state machine has no active state !");
            if ((currentState._updatePoint & ~UpdatePoint.LateUpdate) != 0) 
                currentState.Update(context,UpdatePoint.LateUpdate);
        }
        
        public virtual void FixedUpdate()
        {
            Assert.IsNotNull(currentState, "This state machine has no active state !");
            if ((currentState._updatePoint & ~UpdatePoint.FixedUpdate) != 0)
                currentState.Update(context,UpdatePoint.FixedUpdate);
        }

        // == transitions ==
        public void TransitionTo(StateBase<T> nextState,int passThroughDepth = 0)
        {
            Assert.IsTrue(passThroughDepth<MaxPassThroughDepth,"Max PassThroughDepth Reached : the states transition conditions may be leading to an infinite loop.");
            
            //si le state est en "passthrough", il check ses transitions avant même de rentrer dedans.
            if (nextState._passThrough)
            {
                StateBase<T> newState = nextState.FindNextState(ctx);
                if (newState != nextState)
                {
                    //Debug.Log("Passed through state : " + nextState.GetType().ToString());
                    TransitionTo(newState,passThroughDepth+1);
                    return;
                }
            }

            StateBase<T> oldState = currentState;
            
            Debug.Log("Transtitioning" + (currentState != null?" from : " + currentState.GetType().ToString()  : "" )+ " to : " + nextState.GetType().ToString());

            //exit previous state
            if (currentState != null) 
                try { currentState.Exit(context); }
                catch (Exception e) { Debug.LogException(e); }
            else
                StartState = nextState;
            
            //enter next state
            currentState = nextState;
            try{ currentState.Enter(context);}
            catch (Exception e) { Debug.LogException(e);}
            
            //notifier
            try{OnStateChanged?.Invoke(oldState,currentState);}
            catch (Exception e) { Debug.LogException(e);}
        }

    }

    [Flags]
    public enum UpdatePoint
    {
        Never = 0, 
        Update = 1,
        LateUpdate = 2,
        FixedUpdate = 4
    }
    
    #if UNITY_EDITOR

    [CustomEditor(typeof(StateMachine<>),true)]
    public class StateMachineEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(5);
            // Unity est giga nul pour gérer les types génériques :,)
            switch (target)
            {
                case PlayerStateMachine machine :
                    GUILayout.Box("Current state : "+ machine.CurrentStateName);
                    break;
                default : break;
            }
            
        }
    }
    
    #endif
}