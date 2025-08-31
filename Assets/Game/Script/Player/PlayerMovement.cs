using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private float _walkSpeed;
    [SerializeField]
    private float _walkSpeedMax;
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _crouchingSpeed = 2f;
    [SerializeField]
    private float _crouchingSpeedMax = 2f;
    [SerializeField]
    private float _onAirSpeed;
    [SerializeField]
    private float _walkSprintTransition;
    [SerializeField]
    private float _walkAccel;
    [SerializeField]
    private float _sprintAccel;
    [SerializeField]
    private float _crouchingAccel;
    [SerializeField]
    private float _jumpForce;

    [SerializeField]
    private float _airDrag;
    [SerializeField]
    private float _glideSpeed;
    [SerializeField]
    private Vector3 _glideRotationSpeed;
    [SerializeField]
    private float _glideMinRotattionX;
    [SerializeField]
    private float _glideMaxRotattionX;
    [SerializeField]
    private float _resetComboInterval;
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
    [SerializeField]
    private Transform _topClimbableAreaDetector;
    [SerializeField]
    private Transform _bottomClimbableAreaDetector;
    [SerializeField]
    private Transform _leftClimbableAreaDetector;
    [SerializeField]
    private Transform _rightClimbableAreaDetector;
    [SerializeField]
    private float _climbableAreaDistance;
    [SerializeField]
    private Transform _thirdCameraTransform;
    [SerializeField]
    private Transform _firstCameraTransform;
    [SerializeField]
    private CameraState _playerCameraPOV;
    [SerializeField]
    private Transform _hitDetector;
    [SerializeField]
    private LayerMask _hitLayer;
    [SerializeField]
    private float _hitDetectorRadius;
    [SerializeField]
    private PlayerAudioManager _playerAudio;




    private Vector3 _climbWallNormal;
    private Rigidbody _rigidBody;
    private Animator _animator;

    private float _rotationSmoothVelocity;

    private bool _isSprinting;
    private bool _hideCursor = true;

    private float _speed;

    private bool _isGrounded = false;
    // private float _climbWallDistance;
    private bool _isJump = false;
    private bool _topAbleToClimb = true;
    private bool _bottomAbleToClimb = true;
    private bool _rightAbleToClimb = true;
    private bool _leftAbleToClimb = true;
    private int _hitCnt = 0;
    private bool _isAttack = false;

    private PlayerStance _playerStance;
    private CapsuleCollider _playerCollider;
    private Coroutine _resetCombo;


    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _playerCollider = GetComponent<CapsuleCollider>();

        _speed = _walkSpeed;
        _playerStance = PlayerStance.Stand;

        HideAndLockCursor();
    }

    private void Start()
    {
        _input.OnSprintInput += Sprint;
        _input.OnMoveInput += Move;
        _input.OnJumpStartedInput += Jump;
        _input.OnClimbStartedInput += StartClimb;
        _input.OnCancelClimb += CancelClimbAndGlide;
        _input.OnCrouching += Crouching;
        _input.OnGliding += Gliding;
        _input.OnAttack += Attack;
        _input.OnHideShowCursor += LockAndUnlockCursor;
        _playerCameraPOV.OnChangePOV += ChangePOV;

    }

    private void Update()
    {
        CheckIsGround();
        // if (_playerStance == PlayerStance.Stand && _playerStance != PlayerStance.Climb) CheckStep();
        CheckUpDownLeftRightClimable();
        isGliding();

    }

    private void OnDestroy()
    {
        _input.OnSprintInput -= Sprint;
        _input.OnMoveInput -= Move;
        _input.OnJumpStartedInput -= Jump;
        _input.OnClimbStartedInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimbAndGlide;
        _input.OnCrouching -= Crouching;
        _input.OnGliding -= Gliding;
        _input.OnAttack -= Attack;
        _input.OnHideShowCursor -= LockAndUnlockCursor;
        _playerCameraPOV.OnChangePOV -= ChangePOV;

    }

    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LockAndUnlockCursor()
    {
        _hideCursor = !_hideCursor;
        Debug.Log(_hideCursor);
        if (_hideCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        bool isPlayerCrouching = _playerStance == PlayerStance.Crouch;
        bool isPlayerGliding = _playerStance == PlayerStance.Glide;
        Transform camera = _playerCameraPOV.CameraStatePOV == PlayerCameraPOV.TP ? _thirdCameraTransform : _firstCameraTransform;
        if ((isPlayerStanding || isPlayerCrouching) && !_isAttack)
        {
            if (_playerCameraPOV.CameraStatePOV == PlayerCameraPOV.TP)
            {
                if (axisDirection.magnitude >= 0.1)
                {
                    float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg + camera.eulerAngles.y;
                    float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                    movementDirection = Quaternion.Euler(0f, smoothAngle, 0) * Vector3.forward;
                    // transform.Translate(movementDirection * Time.deltaTime * 5f);
                    // _rigidBody.AddForce(movementDirection * _speed * Time.deltaTime, ForceMode.VelocityChange);
                    // Vector3 horizontalVelocity = new Vector3(_rigidBody.linearVelocity.x, 0, _rigidBody.linearVelocity.z);
                    // float maxHorizontalSpeed = _walkSpeedMax;
                    // bool isRunning = _isSprinting;


                    // if (horizontalVelocity.magnitude > maxHorizontalSpeed)
                    // {
                    //     horizontalVelocity = horizontalVelocity.normalized * maxHorizontalSpeed;
                    //     _rigidBody.linearVelocity = new Vector3(horizontalVelocity.x, _rigidBody.linearVelocity.y, horizontalVelocity.z);
                    // }
                    float maxSpeed = isPlayerCrouching ? _crouchingSpeedMax : _isSprinting ? _sprintSpeed : _walkSpeedMax;
                    Vector3 targetVelocity = movementDirection * maxSpeed;

                    Vector3 horizontalVelocity = new Vector3(_rigidBody.linearVelocity.x, 0, _rigidBody.linearVelocity.z);
                    float accel = _isSprinting ? _sprintSpeed : _walkAccel;
                    horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accel * Time.deltaTime);

                    _rigidBody.linearVelocity = new Vector3(horizontalVelocity.x, _rigidBody.linearVelocity.y, horizontalVelocity.z);
                    CheckStep();
                }
                else
                {
                    Vector3 horizontalVelocity = new Vector3(_rigidBody.linearVelocity.x, 0, _rigidBody.linearVelocity.z);
                    horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, 5f * Time.deltaTime);
                    _rigidBody.linearVelocity = new Vector3(horizontalVelocity.x, _rigidBody.linearVelocity.y, horizontalVelocity.z);
                }
            }
            else if (_playerCameraPOV.CameraStatePOV == PlayerCameraPOV.FP)
            {

                transform.rotation = Quaternion.Euler(0f, _firstCameraTransform.eulerAngles.y, 0f);
                Vector3 verticalDirection = axisDirection.y * transform.forward;
                Vector3 horizontalDirection = axisDirection.x * transform.right;
                movementDirection = verticalDirection + horizontalDirection;
                // transform.Translate(movementDirection * Time.deltaTime * 5f);

                // _rigidBody.AddForce(movementDirection * _speed * Time.deltaTime);
                float maxSpeed = isPlayerCrouching ? _crouchingSpeedMax : _isSprinting ? _sprintSpeed : _walkSpeedMax;
                Vector3 targetVelocity = movementDirection * maxSpeed;

                Vector3 horizontalVelocity = new Vector3(_rigidBody.linearVelocity.x, 0, _rigidBody.linearVelocity.z);
                float accel = _isSprinting ? _sprintSpeed : _walkAccel;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accel * Time.deltaTime);

                _rigidBody.linearVelocity = new Vector3(horizontalVelocity.x, _rigidBody.linearVelocity.y, horizontalVelocity.z);
                CheckStep();
            }

            float velocity = new Vector3(_rigidBody.linearVelocity.x, 0, _rigidBody.linearVelocity.z).magnitude;

            // ===================== Update Animator ==========================================
            _animator.SetFloat("Speed", velocity * axisDirection.magnitude);
            _animator.SetFloat("VelocityX", velocity * axisDirection.x);
            _animator.SetFloat("VelocityY", velocity * axisDirection.y);
            bool goingUpStep = _rigidBody.linearVelocity.y > 0.05f && velocity > 0.1f;
            _animator.SetBool("StepUp", goingUpStep);
        }

        else if (isPlayerClimbing)
        {
            if (axisDirection.magnitude > 0.1f)
            {
                movementDirection = new Vector3(axisDirection.x, axisDirection.y, 0f);

                Vector3 climbMovement = Vector3.ProjectOnPlane(transform.TransformDirection(movementDirection), _climbWallNormal);

                if (climbMovement.magnitude > 0.1f)
                {
                    climbMovement.Normalize();
                }

                if (!_topAbleToClimb && axisDirection.y > 0) climbMovement.y = 0;
                // if (!_bottomAbleToClimb && axisDirection.y < 0) climbMovement.y = 0;
                if (!_leftAbleToClimb && axisDirection.x < 0) climbMovement.x = 0;
                if (!_rightAbleToClimb && axisDirection.x > 0) climbMovement.x = 0;

                _animator.SetFloat("climbUp", climbMovement.y);
                _animator.SetFloat("climbRight", climbMovement.x);
                _rigidBody.MovePosition(transform.position + climbMovement * _climbSpeed * Time.deltaTime);

                // Vector3 horizontal = axisDirection.x * transform.right;
                // Vector3 vertical = axisDirection.y * transform.up;
                // movementDirection = horizontal + vertical;
                //  _rigidBody.AddForce(movementDirection * Time.deltaTime * _climbSpeed);

            }
            else
            {
                _animator.SetFloat("climbUp", 0);
                _animator.SetFloat("climbRight", 0);
            }
        }
        else if (isPlayerGliding)
        {
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += _glideRotationSpeed.x * axisDirection.x * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _glideMinRotattionX, _glideMaxRotattionX);
            rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
            rotationDegree.y += _glideRotationSpeed.y * axisDirection.x * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationDegree);
        }
    }

    private void Sprint(bool isSprint)
    {
        if (_playerStance == PlayerStance.Stand)
        {
            if (isSprint)
            {
                if (_speed < _sprintSpeed) _speed = _speed + _walkSprintTransition * Time.deltaTime;
                _isSprinting = true;
            }
            else
            {
                if (_speed > _walkSpeed) _speed = _speed - _walkSprintTransition * Time.deltaTime;
                _isSprinting = false;
            }
        }

    }

    private void Jump()
    {
        if (_isGrounded)
        {
            Vector3 jumpDirection = Vector3.up;
            _isJump = true;
            _animator.SetTrigger("Jump");
            _rigidBody.AddForce(jumpDirection * _jumpForce, ForceMode.Impulse);
            // _speed = _onAirSpeed;
        }
    }

    private void CheckIsGround()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);

        if (_isGrounded && !wasGrounded)
        {
            _isJump = false;
            _animator.SetBool("isGliding", false);
            if (_playerStance == PlayerStance.Glide)
            {
                _playerAudio.StopGlideSFX();
                _playerCameraPOV.StartClampedCamera(false, PlayerStance.Stand);
            }
            if (_playerStance != PlayerStance.Climb && _playerStance != PlayerStance.Crouch) _playerStance = PlayerStance.Stand;
        }


        _animator.SetBool("isGrounded", _isGrounded);

    }

    private void CheckStep()
    {
        // bool isHitLowerStep = Physics.Raycast(_groundDetector.position,
        // transform.forward, _stepCheckDistance);
        // bool isHitUpperStep = Physics.Raycast(_groundDetector.position + _upperStepOffset,
        // transform.forward, _stepCheckDistance);

        // if (isHitLowerStep && !isHitUpperStep) _rigidBody.AddForce(0, _stepForce, 0);

        if (Physics.Raycast(_groundDetector.position, transform.forward, out RaycastHit lowerHit, _stepCheckDistance))
        {
            // Make sure it's not our own collider
            if (lowerHit.collider != _rigidBody.GetComponent<Collider>())
            {
                bool isHitUpperStep = Physics.Raycast(
                    _groundDetector.position + _upperStepOffset,
                    transform.forward,
                    out RaycastHit upperHit,
                    _stepCheckDistance
                );

                // If wall at bottom but space above -> climb
                if (!isHitUpperStep)
                {
                    _rigidBody.AddForce(Vector3.up * _stepForce, ForceMode.VelocityChange);
                }
            }
        }
    }

    private void CheckUpDownLeftRightClimable()
    {
        bool isTopDetectorClimbableAreaHit = Physics.Raycast(_topClimbableAreaDetector.position, transform.forward,
        _climbableAreaDistance, _climbableLayer
        );

        bool isBottomDetectorClimbableAreaHit = Physics.Raycast(_bottomClimbableAreaDetector.position, transform.forward,
            _climbableAreaDistance, _climbableLayer
        );

        bool isLeftDetectorClimbableAreaHit = Physics.Raycast(_leftClimbableAreaDetector.position, transform.forward,
            _climbableAreaDistance, _climbableLayer
        );

        bool isRightDetectorClimbableAreaHit = Physics.Raycast(_rightClimbableAreaDetector.position, transform.forward,
            _climbableAreaDistance, _climbableLayer
        );

        _topAbleToClimb = isTopDetectorClimbableAreaHit;
        _bottomAbleToClimb = isBottomDetectorClimbableAreaHit;
        _leftAbleToClimb = isLeftDetectorClimbableAreaHit;
        _rightAbleToClimb = isRightDetectorClimbableAreaHit;


    }

    private void StartClimb()
    {
        bool isInFrontOfClimbableWall = Physics.Raycast(_climbDetector.position,
        transform.forward, out RaycastHit hit, _climbCheckDistance, _climbableLayer);
        bool isNotClimbing = _playerStance != PlayerStance.Climb;
        if (isInFrontOfClimbableWall && _topAbleToClimb && _leftAbleToClimb && _rightAbleToClimb && _bottomAbleToClimb && isNotClimbing)
        {
            if (_isGrounded || _isJump)
            {
                _climbWallNormal = hit.normal;
                Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
                Vector3 targetPosition = hit.point - offset;

                transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

                StartCoroutine(MoveToClimbPosition(targetPosition, 0.3f, () =>
                {
                    _playerStance = PlayerStance.Climb;
                    _playerCameraPOV.StartClampedCamera(true, _playerStance);
                    _rigidBody.useGravity = false;
                    _speed = _climbSpeed;
                    _playerCollider.center = Vector3.up * 1.3f;
                    _animator.SetBool("isClimbing", true);
                    _playerCameraPOV.SetTPFieldOfView(true);
                }));

            }
        }
    }

    private IEnumerator MoveToClimbPosition(Vector3 target, float duration, System.Action onComplete = null)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        onComplete?.Invoke();
    }

    private void CancelClimbAndGlide()
    {
        bool isPlayerStanceClimb = _playerStance == PlayerStance.Climb;
        bool isPlayerStanceGliding = _playerStance == PlayerStance.Glide;
        if (isPlayerStanceClimb)
        {
            _rigidBody.useGravity = true;
            _playerStance = PlayerStance.Stand;
            // transform.position -= transform.forward * .2f;
            _speed = _walkSpeed;
            _animator.SetBool("isClimbing", false);
            _playerCameraPOV.StartClampedCamera(false, _playerStance);
            _playerCameraPOV.SetTPFieldOfView(false);
            _playerCollider.center = Vector3.up * .9f;
        }
        else if (isPlayerStanceGliding)
        {
            _playerStance = PlayerStance.Stand;
            _rigidBody.useGravity = true;
            _playerCameraPOV.StartClampedCamera(false, _playerStance);
        }
    }

    private void Crouching()
    {
        bool isPlayerCrouching = _playerStance == PlayerStance.Crouch;
        _playerStance = isPlayerCrouching ? PlayerStance.Stand : PlayerStance.Crouch;
        isPlayerCrouching = _playerStance == PlayerStance.Crouch;

        _animator.SetBool("isCrouching", isPlayerCrouching);
        _speed = isPlayerCrouching ? _crouchingSpeed : _walkSpeed;
        _playerCollider.height = isPlayerCrouching ? 1.3f : 1.8f;
        _playerCollider.center = isPlayerCrouching ? Vector3.up * .66f : Vector3.up * .9f;
    }

    private void Gliding()
    {
        if (!_isGrounded)
        {
            bool isCurrentlyGliding = _playerStance == PlayerStance.Glide;
            _playerStance = isCurrentlyGliding ? PlayerStance.Stand : PlayerStance.Glide;

            bool isGliding = _playerStance == PlayerStance.Glide;
            _animator.SetBool("isGliding", isGliding);
            if (isGliding)
            {
                _playerCameraPOV.StartClampedCamera(true, _playerStance);
                _playerAudio.PlayGlideSFX();
            }
        }
    }

    private void isGliding()
    {
        bool isCurrentlyGliding = _playerStance == PlayerStance.Glide;
        if (isCurrentlyGliding)
        {
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 forceUp = transform.up * (lift + _airDrag);
            Vector3 forwardForce = transform.forward * _glideSpeed;
            Vector3 totalForce = forceUp + forwardForce;
            _rigidBody.AddForce(totalForce * Time.deltaTime);
        }
    }

    private void Attack()
    {
        if (!_isAttack && _playerStance == PlayerStance.Stand)
        {
            _isAttack = true;
            if (_hitCnt < 3)
            {
                _hitCnt++;
            }
            else
            {
                _hitCnt = 1;
            }
            _animator.SetTrigger("hit");
            _animator.SetInteger("combo", _hitCnt);

        }
    }

    private void EndAttack()
    {
        _isAttack = false;
        if (_resetCombo != null)
        {
            StopCoroutine(_resetCombo);
        }
        _resetCombo = StartCoroutine(ResetCombo());
    }

    private IEnumerator ResetCombo()
    {
        yield return new WaitForSeconds(_resetComboInterval);
        _hitCnt = 0;
    }

    private void Hit()
    {
        Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _hitLayer);
        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i].gameObject != null)
            {
                Destroy(hitObjects[i].gameObject);
            }
        }
    }

    private void WallJump()
    {
        bool isClimbing = _playerStance == PlayerStance.Climb;
        if (isClimbing)
        {
            CancelClimbAndGlide();
            _isJump = true;

        }
    }

    private void OnDrawGizmos()
    {
        if (_topClimbableAreaDetector != null)
            Gizmos.DrawRay(_topClimbableAreaDetector.position, transform.forward * _climbableAreaDistance);
        if (_bottomClimbableAreaDetector != null)
            Gizmos.DrawRay(_bottomClimbableAreaDetector.position, transform.forward * _climbableAreaDistance);
        if (_leftClimbableAreaDetector != null)
            Gizmos.DrawRay(_leftClimbableAreaDetector.position, transform.forward * _climbableAreaDistance);
        if (_rightClimbableAreaDetector != null)
            Gizmos.DrawRay(_rightClimbableAreaDetector.position, transform.forward * _climbableAreaDistance);
        if (_groundDetector != null)
        {
            if (Physics.Raycast(_groundDetector.position, transform.forward, out RaycastHit hit, _stepCheckDistance))
            {
                Gizmos.color = Color.green; // Hit detected
            }
            else
            {
                Gizmos.color = Color.red; // No hit
            }
            Gizmos.DrawRay(_groundDetector.position, transform.forward * _stepCheckDistance);

            // Upper ray
            if (Physics.Raycast(_groundDetector.position + _upperStepOffset, transform.forward, out RaycastHit upperHit, _stepCheckDistance))
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawRay(_groundDetector.position + _upperStepOffset, transform.forward * _stepCheckDistance);
        }
    }

    private void ChangePOV()
    {
        bool isThirdPerson = _playerCameraPOV.CameraStatePOV == PlayerCameraPOV.TP;
        _animator.SetBool("isThirdPerson", isThirdPerson);
    }

}
