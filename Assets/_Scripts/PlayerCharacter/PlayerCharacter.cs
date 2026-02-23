using Unity.Netcode;
using UnityEngine;

namespace _scripts.PlayerCharacter
{
    /// <summary>
    /// Contient toutes les refs de components du personnage
    /// </summary>
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("Scene References")] 
        public PlayerPhysics physics;
        public PlayerMovement movement;
        public PlayerCharacterInputs inputs;
        public PlayerStateMachine stateMachine;
        public NetworkObject networkObject;
    }
}
