using System;
using _scripts.PlayerCharacter;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// la gestion de la rotation de la camera
/// </summary>
public class CharacterAiming : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] PlayerCharacter _character;
    [SerializeField] Transform _cameraRoot;
    [SerializeField] Rigidbody _rigidbody;
    
    [Header("parameters")]
    [SerializeField] float _sensitivity;

    private float angle = 0;
    
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        _cameraRoot.Rotate(_sensitivity * Time.deltaTime* _character.inputs.aimInput.Value.y * Vector3.right,Space.Self);
            angle = Mathf.Clamp((angle + _sensitivity * Time.deltaTime * _character.inputs.aimInput.Value.y), -90,90);
        _cameraRoot.transform.localEulerAngles = angle * Vector3.right;
    }

    void FixedUpdate()
    {
        _rigidbody.MoveRotation(_rigidbody.rotation * quaternion.RotateY( _sensitivity * Time.deltaTime * _character.inputs.aimInput.Value.x * Mathf.Deg2Rad));
    }
}
