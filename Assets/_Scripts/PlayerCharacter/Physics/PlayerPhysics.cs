using System;
using System.Diagnostics;
using _Scripts.Extensions;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// cette classe permet de gérer facilement la physique des objets et les collisions. c'est un peu comme un rigidbody amélioré.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerPhysics : MonoBehaviour
{
    [field: SerializeField] private Vector3 velocity;

    public Vector3 Velocity
    {
        get => velocity;
        protected set => velocity = value;
    }

    public Vector3 Position => _rb.position;
    public event Action OnBounce;

    private Vector3 _sumOfForces;

    [Header("Settings")] //suggestion : mettre ça dans un scriptable object -> mieux 
    [SerializeField] private float _gravityStrength;
    [SerializeField] private byte _maxCollisionSteps = 5;
    [SerializeField] [Tooltip("0 -> glisse, 1 -> rebondit parfaitement")] [Range(0, 1)] public float bounciness = 0;
    [SerializeField] private bool autoUpdate = false;
    [SerializeField] private LayerMask _layerMask;
    
    //scene references
    [Header("Scene References")]
    [SerializeField] private SphereCollider col;
    
    private Rigidbody _rb;
    public Rigidbody Rb => _rb;
    
    public Vector3 Acceleration {get; private set;}
    Vector3 _lastVelocity;

    private void Awake()
    {
        TryGetComponent(out _rb);
        if(col == null) TryGetComponent(out col);
    }

    private void FixedUpdate()
    {
        if(autoUpdate) UpdatePhysics();
    }
    
    public void UpdatePhysics()
    {
        if (!enabled) return;
        
        //gravity
        AddForce(Vector3.down * _gravityStrength);
        
        //Apply forces
        Velocity += _sumOfForces * Time.deltaTime;
        _sumOfForces = Vector3.zero;

        //check for collisions
        CheckForCollisions();

        //Apply Velocity
        Acceleration = Velocity - _lastVelocity;
        _rb.MovePosition(_rb.position + Velocity * Time.deltaTime);
        _lastVelocity = Velocity;
    }
    
//collisions
    void CheckForCollisions() 
    {
        int i = 0;

        float velocityMagnitude = Velocity.magnitude;
        bool hasBounced = false;
        
        RaycastHit hit;
        Vector3 rayDirection = Velocity / velocityMagnitude;
        bool raycast = false;
        if(velocityMagnitude > 0)
            while (
                (Physics.SphereCast(
                    Position, col.radius,
                    rayDirection,
                    out hit,
                    velocityMagnitude * Time.deltaTime,
                    _layerMask) 
                || (raycast = Physics.Raycast(
                    Position,
                    rayDirection,
                    out hit,
                    velocityMagnitude * Time.deltaTime + col.radius,
                    _layerMask)))
                && i < _maxCollisionSteps)
            {
                //Vector3 toHit = hit.point - _rb.position;
                if (true|| Vector2.Dot(hit.normal, Velocity) <= 0)
                {
                    hasBounced |= bounciness > 0;

                    float DistanceToImpactPoint = hit.distance- (raycast? 0.5f : .001f);

                    Vector3 travelVector = Velocity * Time.deltaTime;
                    Vector3 TravelToImpactPoint = (rayDirection * DistanceToImpactPoint);

                    Vector3 remainingTravel = travelVector - TravelToImpactPoint;
                    Vector3 RemainingProjectedTravel = Vector3.ProjectOnPlane(remainingTravel,hit.normal);//(remainingTravel) - (1f + bounciness) * Vector3.Dot(remainingTravel, hit.normal) * hit.normal;
                    Velocity = (TravelToImpactPoint + RemainingProjectedTravel) / Time.deltaTime;
                    velocityMagnitude = Velocity.magnitude;
                    rayDirection = Velocity / velocityMagnitude;
                    //todo :snap position too
                }

                i++;
            }
        
        if(hasBounced) OnBounce?.Invoke();
        
        if (i == _maxCollisionSteps-1)
        {
            Debug.LogWarning( gameObject.name+ " reached max collision steps",this);
        }
    }

//public methods
    
    public bool ComputeIsGrounded()
    {
        if (Physics.SphereCast(
                Position,
                col.radius
                ,Vector3.down,
                out RaycastHit _
                ,.02f
                , _layerMask) )
            return true;
        
        if (Physics.Raycast(
                Position,
                Vector3.down,
                out RaycastHit hit,
                0.02f + col.radius,
                _layerMask))
                return true;
        
        return false;
    }
    
    public void SmoothDampToward(Vector2 target, float smoothTime)
    {
        Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }
    
    public void MoveToward(Vector3 target, float maxVelocity,float acceleration)
    {
        Vector3 offset = target - Position;
        Vector3 wishVel = Vector2.ClampMagnitude(offset.normalized * maxVelocity,offset.magnitude/Time.fixedDeltaTime);
        Velocity = Velocity.MoveToward(wishVel, acceleration * Time.deltaTime);
    }
    
    public void AddForce(Vector3 force)
    {
        _sumOfForces += force;
    }

    public void AddImpulse(Vector3 impulse)
    {
        _sumOfForces += impulse / Time.fixedDeltaTime;
    }

    public void SetVelocity(Vector3 newVelocity)
    {
        Velocity = newVelocity;
    }

    public void SetPosition(Vector3 newPosition)
    {
        _rb.position = newPosition;
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration, int quality = 3)
    {
        quality = Mathf.Clamp(quality, 1, 10);

        int segments = quality << 2;
        int subdivisions = quality << 3;
        int halfSegments = segments >> 1;
        float strideAngle = 360F / subdivisions;
        float segmentStride = 180F / segments;

        Vector3 first;
        Vector3 next;
        for (int i = 0; i < segments; i++)
        {
            first = (Vector3.forward * radius);
            first = Quaternion.AngleAxis(segmentStride * (i - halfSegments), Vector3.right) * first;

            for (int j = 0; j < subdivisions; j++)
            {
                next = Quaternion.AngleAxis(strideAngle, Vector3.up) * first;
                UnityEngine.Debug.DrawLine(first + center, next + center, color, duration);
                first = next;
            }
        }

        Vector3 axis;
        for (int i = 0; i < segments; i++)
        {
            first = (Vector3.forward * radius);
            first = Quaternion.AngleAxis(segmentStride * (i - halfSegments), Vector3.up) * first;
            axis = Quaternion.AngleAxis(90F, Vector3.up) * first;

            for (int j = 0; j < subdivisions; j++)
            {
                next = Quaternion.AngleAxis(strideAngle, axis) * first;
                UnityEngine.Debug.DrawLine(first + center, next + center, color, duration);
                first = next;
            }
        }
    }
    
}