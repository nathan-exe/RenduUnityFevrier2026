using _scripts.PlayerCharacter;
using UnityEngine;

public class PlayerCameraBehaviour : MonoBehaviour
{
    [SerializeField] PlayerCharacter _playerCharacter;
    [SerializeField] private Camera _cam;
    private Vector3 _vel;

    [SerializeField] private Vector2 FOVRange;

    [SerializeField] private float _fovSmoothTime;
    [SerializeField] private float _power = 3;

    private float _fovVel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        float targetFOV = Mathf.Lerp(FOVRange.x, FOVRange.y,
            Mathf.Pow(_playerCharacter.physics.Velocity.magnitude / _playerCharacter.stateMachine.s_Walking._walkSpeed, _power));
        _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView,targetFOV,ref _fovVel,_fovSmoothTime);
    }
}
