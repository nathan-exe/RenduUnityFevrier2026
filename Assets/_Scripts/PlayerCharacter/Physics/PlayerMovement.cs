using _Scripts.Extensions;
using _scripts.PlayerCharacter;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PlayerCharacter _playerCharacter;

    public void ApplyFriction(float friction, bool xzOnly)
    {
        Move(Vector3.zero,friction,xzOnly);
    }
    
    public void Move(Vector3 targetVelocity,float acceleration, bool xzOnly)
    {
        Vector3 currentVelocity = _playerCharacter.physics.Velocity;
        Vector3 velocity = currentVelocity;
        
        if (xzOnly) {
            velocity.y = 0;
            targetVelocity.y = 0; }
        velocity = velocity.MoveToward(targetVelocity, acceleration);
        if (xzOnly) velocity.y = currentVelocity.y;
        //Debug.Log("old velocity : "+currentVelocity+", new velocity: "+velocity);
        _playerCharacter.physics.SetVelocity(velocity);
    }

    
}
