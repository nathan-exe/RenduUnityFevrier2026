using Chat;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// cette classe contient les inputs du joueur et est utilisée dans les states du personnage.
/// </summary>
//todo : new input system
public class PlayerCharacterInputs : NetworkBehaviour
{
    [HideInInspector] public NetworkVariable<Vector2> movementInput = new(Vector2.zero, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    [HideInInspector] public NetworkVariable<Vector2> aimInput = new(Vector2.zero, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    [HideInInspector] public NetworkVariable<bool> isHoldingRunKey { get; private set; } = new(false, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    [Header("Settings")]
    [SerializeField] private float _aimSmoothingTime = .1f;
    [SerializeField] private float _jumpBufferingDuration = .2f;

    private Vector2 aimVel;

    private bool _paused = false;
    
    public bool TryConsumeJumpKeyPress()
    {
        bool wasBuffered = _jumpKeyBuffered;
        _jumpKeyBuffered = false;
        return wasBuffered;
    }
    
    private float _lastJumpKeyPressTime;
    private bool _jumpKeyBuffered;
    public bool IsHoldingJumpKey { get; private set; }
    
    void Update()
    {
        if (!IsOwner) return;
        
        //chat
        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Escape))
        {
            GameChat.Instance.Hide();
            _paused = false;
        }
        
        if (_paused) return;
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            GameChat.Instance.Show();
            Clear();
            _paused = true;
        }

        //movement
        movementInput.Value = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical")).normalized;
        
        //aim
        aimInput.Value = Vector2.SmoothDamp(
            aimInput.Value,
            new Vector2(Input.mousePositionDelta.x/(float)Screen.height,-Input.mousePositionDelta.y/(float)Screen.height),
            ref aimVel,
            _aimSmoothingTime);

        //jump
        IsHoldingJumpKey = Input.GetKey(KeyCode.Space);
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _jumpKeyBuffered = true;
            _lastJumpKeyPressTime = Time.time;
        }
        _jumpKeyBuffered &= Time.time - _lastJumpKeyPressTime <= _jumpBufferingDuration && IsHoldingJumpKey;
        
        //run
        isHoldingRunKey.Value = Input.GetKey(KeyCode.LeftShift);
        
        
    }

    public void Clear()
    {
        movementInput.Value = Vector2.zero;
        aimInput.Value = Vector2.zero;
        IsHoldingJumpKey = false;
        _jumpKeyBuffered = false;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerCharacterInputs))]
public class PlayerCharacterInputsEditor : Editor
{
    override public void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        PlayerCharacterInputs i = target as PlayerCharacterInputs;
        GUILayout.Label($"Movement Input : {i.movementInput.Value.x}, {i.movementInput.Value.y}");
        GUILayout.Label($"Aim Input : {i.aimInput.Value.x}, {i.aimInput.Value.y}");
    }
}
#endif