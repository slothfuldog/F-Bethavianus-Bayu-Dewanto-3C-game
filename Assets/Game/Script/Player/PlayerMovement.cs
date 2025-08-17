using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private float _walkSpeed;
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _walkSprintTransition;
    [SerializeField]
    private float _jumpForce;

    [SerializeField]
    private InputManager _input;

    [SerializeField]
    private float _rotationSmoothTime = 0.1f;

    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private float _detectorRadius;
    [SerializeField]
    private LayerMask _groundLayer;

    [SerializeField]
    private Vector3 _upperStepOffset;
    [SerializeField]
    private float _stepCheckDistance;
    [SerializeField]
    private float _stepForce;
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private float _climbSpeed;
    [SerializeField]
    private float _climbCheckDistance;
    [SerializeField]
    private LayerMask _climbableLayer;
    [SerializeField]
    private Vector3 _climbOffset;
    private Vector3 _climbWallNormal;
    private Rigidbody _rigidBody;

    private float _rotationSmoothVelocity;

    private float _speed;

    private bool _isGrounded = false;
    private float _climbWallDistance;

    private PlayerStance _playerStance;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _speed = _walkSpeed;
        _playerStance = PlayerStance.Stand;
    }

    private void Start()
    {
        _input.OnSprintInput += Sprint;
        _input.OnMoveInput += Move;
        _input.OnJumpStartedInput += Jump;
        _input.OnClimbStartedInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;

    }

    private void Update()
    {
        CheckIsGround();
        CheckStep();
    }

    private void OnDestroy()
    {
        _input.OnSprintInput -= Sprint;
        _input.OnMoveInput -= Move;
        _input.OnJumpStartedInput -= Jump;
        _input.OnClimbStartedInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimb;

    }

    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        if (isPlayerStanding)
        {
            if (axisDirection.magnitude >= 0.1)
            {
                float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                movementDirection = Quaternion.Euler(0f, smoothAngle, 0) * Vector3.forward;
                // transform.Translate(movementDirection * Time.deltaTime * 5f);
                _rigidBody.AddForce(movementDirection * _speed * Time.deltaTime);
                // Debug.Log(_speed);
            }
        }
        else if (isPlayerClimbing)
        {
            movementDirection = new Vector3(axisDirection.x, axisDirection.y, 0f);

            Vector3 planarMovement = Vector3.ProjectOnPlane(transform.TransformDirection(movementDirection), _climbWallNormal);

            if (planarMovement.magnitude > 1f)
            {
                planarMovement.Normalize();
            }

            _rigidBody.MovePosition(transform.position + planarMovement * _climbSpeed * Time.deltaTime);
        }
    }

    private void Sprint(bool isSprint)
    {
        if (_playerStance == PlayerStance.Stand)
        {
            if (isSprint)
            {
                if (_speed < _sprintSpeed) _speed = _speed + _walkSprintTransition * Time.deltaTime;

            }
            else
            {
                if (_speed > _walkSpeed) _speed = _speed - _walkSprintTransition * Time.deltaTime;
            }
        }

    }

    private void Jump()
    {
        if (_isGrounded)
        {
            Vector3 jumpDirection = Vector3.up;
            _rigidBody.AddForce(jumpDirection * _jumpForce, ForceMode.Impulse);
        }
    }

    private void CheckIsGround()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
    }

    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(_groundDetector.position,
        transform.forward, _stepCheckDistance);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position + _upperStepOffset,
        transform.forward, _stepCheckDistance);

        if (isHitLowerStep && !isHitUpperStep) _rigidBody.AddForce(0, _stepForce, 0);
    }

    private void StartClimb()
    {
        bool isInFrontOfClimbableWall = Physics.Raycast(_climbDetector.position,
        transform.forward, out RaycastHit hit, _climbCheckDistance, _climbableLayer);
        bool isNotClimbing = _playerStance != PlayerStance.Climb;
        if (isInFrontOfClimbableWall && isNotClimbing && _isGrounded)
        {
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            Vector3 targetPosition = hit.point - offset;

            _climbWallDistance = Vector3.Distance(transform.position, hit.point);
            _climbWallNormal = hit.normal;

            transform.rotation = Quaternion.LookRotation(-hit.normal);

            StartCoroutine(MoveToClimbPosition(targetPosition, 0.3f, () =>
            {
                _playerStance = PlayerStance.Climb;
                _rigidBody.useGravity = false; 
                _speed = _climbSpeed;
            }));
            }

    }
    
    private IEnumerator MoveToClimbPosition(Vector3 target, float duration, System.Action onComplete = null)
    {
        Vector3 start = transform.position;
        float elapsed = 1f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        onComplete?.Invoke();
    }

    private void CancelClimb()
    {
        bool isPlayerStanceClimb = _playerStance == PlayerStance.Climb;
        if (isPlayerStanceClimb)
        {
            _rigidBody.useGravity = true;
            _playerStance = PlayerStance.Stand;
            transform.position -= transform.forward * .2f;
            _speed = _walkSpeed;
        }
    }
    
}
