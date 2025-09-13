
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputManager _input;
    [SerializeField]
    private CinemachineCamera _cinemachineCameraTPS;
    [SerializeField]
    private Transform _thirdCameraTransform;
    [SerializeField]
    private Transform _firstCameraTransform;
    [SerializeField]
    private CameraState _playerCameraPOV;
    [SerializeField]
    private float _walkingMovementSpeed, _runningMovementSpeed, _crouchMovementSpeed;
    [SerializeField]
    private float _jumpForce = 8f, _gravity = -0.25f, _nonJumpDrag = -3f;
    [SerializeField]
    private LayerMask _groundLayer;
    [SerializeField]
    private Animator _animator;
    [SerializeField]
    private PlayerAudioManager _playerAudio;
    [SerializeField]
    private Transform _hitDetector;
    [SerializeField]
    private LayerMask _hitLayer;
    [SerializeField]
    private float _hitDetectorRadius;
    [SerializeField]
    private float _resetComboInterval;
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private float _climbSpeed, _climbCheckDistance, _climbableAreaDistance;
    [SerializeField]
    private LayerMask _climbableLayer;
    [SerializeField]
    private Vector3 _climbOffset;
    [SerializeField]
    private Transform _topClimbableAreaDetector, _bottomClimbableAreaDetector,
    _leftClimbableAreaDetector, _rightClimbableAreaDetector;
    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private float _stepCheckDistance;
    [SerializeField]
    private float stepHeight = 0.3f;
    [SerializeField]
    private float stepCheckDistance = 0.5f;
    [SerializeField]
    private float stepSmooth = 0.1f;

    [SerializeField]
    private Transform stepRayUpper;
    [SerializeField]
    float _airDrag, _glideSpeed, _glideMaxRotattionX, _glideMinRotattionX;
    [SerializeField]
    private Vector3 _glideRotationSpeed;
    [SerializeField]
    private LayerMask _stairsLayer;
    [SerializeField]
    private LayerMask _retryLayer;
    [SerializeField]
    private LayerMask _interactedLayer;
    private Vector3 _startingPosition;
    private Vector3 _checkPoint;
    public CharacterController CharacterController;
    private PlayerStance _playerStance;
    private Vector3 _climbWallNormal;
    private Vector3 rotationDegree = Vector3.zero;

    private GameObject _lastGroundObject;
    private float _rotationSmoothTime = 0.1f;
    private float _rotationSmoothVelocity;
    private float _speed;
    private bool _isSprint;
    private float _verticalVelocity;
    private bool _isGround = true;
    private bool _isJump = false;
    private float _lastGroundTime;
    private bool _isAttack = false;
    private int _hitCnt = 0;
    private Coroutine _resetCombo;

    private bool _topAbleToClimb, _bottomAbleToClimb, _leftAbleToClimb, _rightAbleToClimb;


    void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        _speed = _walkingMovementSpeed;
        _playerStance = PlayerStance.Stand;
        _startingPosition = transform.position;
        _checkPoint = transform.position;
        HideAndLockCursor();
    }
    void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpStartedInput += Jump;
        _input.OnCrouching += Crouching;
        _input.OnAttack += Attack;
        _input.OnClimbStartedInput += StartClimb;
        _input.OnCancelClimb += CancelClimbAndGlide;
        _input.OnChangePOV += ChangePOV;
        _input.OnGliding += Gliding;
        _input.OnInteract += Interact;
    }

    void Update()
    {
        bool wasGrounded = _isGround;
        _isGround = CharacterController.isGrounded;
        if (_isGround)
        {
            _lastGroundTime = Time.time;
            _isJump = false;
            _animator.SetBool("isJump", false);
            if (_playerStance == PlayerStance.Glide)
            {
                _playerAudio.StopGlideSFX();
                _animator.SetBool("isGliding", false);
                _playerCameraPOV.StartClampedCamera(false, PlayerStance.Stand);
            }
            if (_playerStance != PlayerStance.Climb && _playerStance != PlayerStance.Crouch) _playerStance = PlayerStance.Stand;
        }
        if (!wasGrounded && !_isGround)
        {
            if (_playerStance == PlayerStance.Crouch)
            {
                _animator.SetBool("isCrouching", false);
                _playerStance = PlayerStance.Stand;
            }
        }
        if (Time.time - _lastGroundTime <= _jumpForce)
            _animator.SetBool("isGrounded", _isGround);
        if (_playerStance == PlayerStance.Stand && !_isJump)
        {
            _verticalVelocity = _gravity + _nonJumpDrag + Time.deltaTime;
            CharacterController.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
        }
        bool isStair = Physics.CheckSphere(_groundDetector.position, 0.5f, _stairsLayer) ||
                   Physics.Raycast(_groundDetector.position, transform.forward, stepCheckDistance, _stairsLayer);

        _animator.SetBool("onStairs", isStair);
        // if (_playerStance == PlayerStance.Stand || _playerStance == PlayerStance.Crouch) CheckStep();
        HandleGlide();
        TrackGround();
        CheckVoid();
        CheckRetryGround();

        CheckUpDownLeftRightClimable();

    }

    void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpStartedInput += Jump;
        _input.OnCrouching -= Crouching;
        _input.OnAttack -= Attack;
        _input.OnClimbStartedInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimbAndGlide;
        _input.OnChangePOV -= ChangePOV;
        _input.OnGliding -= Gliding;
        _input.OnInteract -= Interact;
    }

    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void TrackGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(_groundDetector.position, Vector3.down, out hit, 2f, _groundLayer))
        {
            _lastGroundObject = hit.collider.gameObject;

            Vector3 center = hit.collider.bounds.center;

            float topY = hit.collider.bounds.max.y;
            Vector3 topCenter = new Vector3(center.x, topY, center.z);

            _checkPoint = topCenter;
        }
    }

    void CheckVoid()
    {
        if (transform.position.y < -100f)
        {
            if (_lastGroundObject != null)
            {
                transform.position = _checkPoint + Vector3.up * 1f;
            }
        }
    }
    void CheckRetryGround()
    {
        bool retry = Physics.CheckSphere(_groundDetector.position, 0.1f, _retryLayer);
        if (_lastGroundObject != null && retry)
        {
            transform.position = _checkPoint + Vector3.up * 1f;
        }
    }

    void Interact()
    {
        Collider[] interactionObject = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _interactedLayer);
        if (interactionObject.Length == 0) return;

        Collider nearest = interactionObject[0];
        float minDist = Vector3.Distance(_hitDetector.position, nearest.transform.position);

        CheckFinishedObject(nearest.gameObject.name);
    }

    void CheckFinishedObject(string name)
    {
        if (name == "FinishStatue")
        {
            CharacterController.enabled = false;
            transform.position = _startingPosition + Vector3.up * 1f;
            CharacterController.enabled = true;
        }
    }

    void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;
        if (_playerStance == PlayerStance.Stand || _playerStance == PlayerStance.Crouch)
        {
            if (_playerCameraPOV.CameraStatePOV == PlayerCameraPOV.TP)
            {
                if (axisDirection.magnitude >= 0.1 && !_isAttack)
                {
                    Transform camera = _thirdCameraTransform;
                    float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg + camera.eulerAngles.y;
                    float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                    movementDirection = Quaternion.Euler(0f, smoothAngle, 0) * Vector3.forward;
                    CharacterController.Move(movementDirection * _speed * Time.deltaTime);
                    CheckStep();
                }
            }
            else if (_playerCameraPOV.CameraStatePOV == PlayerCameraPOV.FP)
            {
                if (!_isAttack && axisDirection.magnitude >= 0.1)
                {
                    transform.rotation = Quaternion.Euler(0f, _firstCameraTransform.eulerAngles.y, 0f);
                    Vector3 verticalDirection = axisDirection.y * transform.forward;
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    movementDirection = verticalDirection + horizontalDirection;
                    CharacterController.Move(movementDirection * _speed * Time.deltaTime);
                    CheckStep();
                }
            }
            _verticalVelocity = _verticalVelocity + _gravity + Time.deltaTime;
            CharacterController.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);

            _animator.SetFloat("Speed", _speed * axisDirection.magnitude);
            _animator.SetFloat("VelocityX", _speed * axisDirection.x);
            _animator.SetFloat("VelocityY", _speed * axisDirection.y);
        }
        else if (_playerStance == PlayerStance.Climb)
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
                if (!_bottomAbleToClimb && axisDirection.y < 0) climbMovement.y = 0;
                if (!_leftAbleToClimb && axisDirection.x < 0) climbMovement.x = 0;
                if (!_rightAbleToClimb && axisDirection.x > 0) climbMovement.x = 0;

                _animator.SetFloat("climbUp", climbMovement.y);
                _animator.SetFloat("climbRight", -climbMovement.x);
                CharacterController.Move(climbMovement * _climbSpeed * Time.deltaTime);
            }
            else
            {
                _animator.SetFloat("climbUp", 0);
                _animator.SetFloat("climbRight", 0);
            }
        }
        else if (_playerStance == PlayerStance.Glide)
        {
            rotationDegree.x += _glideRotationSpeed.x * axisDirection.y * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _glideMinRotattionX, _glideMaxRotattionX);
            rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
            rotationDegree.y += _glideRotationSpeed.y * axisDirection.x * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationDegree);
        }
    }

    void Sprint(bool isSprint)
    {
        if (_playerStance == PlayerStance.Stand && !_isAttack)
        {
            if (isSprint)
            {
                if (_speed < _runningMovementSpeed) _speed = _speed + _walkingMovementSpeed * Time.deltaTime;
                _isSprint = true;
            }
            else
            {
                if (_speed > _walkingMovementSpeed) _speed = _speed - _walkingMovementSpeed * Time.deltaTime;
                _isSprint = false;
            }
        }
    }

    void Jump()
    {
        if (CharacterController.isGrounded && _playerStance == PlayerStance.Stand && !_isAttack)
        {
            _isJump = true;
            _verticalVelocity = _jumpForce;
            _animator.SetTrigger("isJump");
        }
    }

    void Crouching()
    {
        Vector3 checkerUpPosition = transform.position + (transform.up * 1.4f);
        bool isCantStand = Physics.Raycast(checkerUpPosition, transform.up, 0.25f, _groundLayer);
        _playerStance = _playerStance == PlayerStance.Crouch && !isCantStand ? PlayerStance.Stand : PlayerStance.Crouch;
        bool isPlayerCrouching = _playerStance == PlayerStance.Crouch;
        if (!isCantStand)
        {
            _speed = isPlayerCrouching ? _crouchMovementSpeed : _walkingMovementSpeed;
            CharacterController.center = isPlayerCrouching ? new Vector3(0, .7f, 0f) : new Vector3(0, .9f, 0f);
            CharacterController.height = isPlayerCrouching ? 1.3f : 1.8f;
            _animator.SetBool("isCrouching", isPlayerCrouching);
        }
    }

    void StartClimb()
    {
        bool isInFrontOfClimbableWall = Physics.Raycast(_climbDetector.position,
        transform.forward, out RaycastHit hit, _climbCheckDistance, _climbableLayer);
        bool isNotClimbing = _playerStance != PlayerStance.Climb;
        if (isInFrontOfClimbableWall && _topAbleToClimb && _leftAbleToClimb && _rightAbleToClimb && _bottomAbleToClimb && isNotClimbing)
        {
            if (_isGround || _isJump)
            {

                _climbWallNormal = hit.normal;
                Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
                Vector3 targetPosition = hit.point - offset;

                transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

                _animator.SetBool("isClimbing", true);
                StartCoroutine(MoveToClimbPosition(targetPosition, 0.3f, () =>
                {

                    _playerStance = PlayerStance.Climb;
                    _playerCameraPOV.StartClampedCamera(true, _playerStance);
                    _speed = _climbSpeed;
                    CharacterController.center = Vector3.up * 1.3f;

                    _playerCameraPOV.SetTPFieldOfView(true);
                }));

            }
        }
    }



    void Attack()
    {
        if (_isGround && !_isAttack && _playerStance == PlayerStance.Stand)
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
    void OnHit()
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

    void OnEndAttack()
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

    void CheckUpDownLeftRightClimable()
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

    void CancelClimbAndGlide()
    {
        bool isPlayerStanceClimb = _playerStance == PlayerStance.Climb;
        bool isPlayerStanceGliding = _playerStance == PlayerStance.Glide;
        if (isPlayerStanceClimb)
        {

            _playerStance = PlayerStance.Stand;
            // transform.position -= transform.forward * .2f;
            _speed = _walkingMovementSpeed;
            _animator.SetBool("isClimbing", false);
            _playerCameraPOV.StartClampedCamera(false, _playerStance);
            _playerCameraPOV.SetTPFieldOfView(false);
            CharacterController.center = Vector3.up * .9f;
            _verticalVelocity = _jumpForce;
        }
        else if (isPlayerStanceGliding)
        {
            _playerStance = PlayerStance.Stand;
            _playerAudio.StopGlideSFX();
            _animator.SetBool("isGliding", false);
            _playerCameraPOV.StartClampedCamera(false, _playerStance);
            _verticalVelocity = _jumpForce;
        }
    }

    private void ChangePOV()
    {
        bool isThirdPerson = _playerCameraPOV.CameraStatePOV == PlayerCameraPOV.TP;
        _animator.SetBool("isThirdPerson", isThirdPerson);
    }

    void Gliding()
    {
        if (_playerStance != PlayerStance.Glide && !_isGround)
        {
            rotationDegree = transform.rotation.eulerAngles;
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
    void HandleGlide()
    {
        bool isCurrentlyGliding = _playerStance == PlayerStance.Glide;
        if (isCurrentlyGliding)
        {
            Vector3 forwardMovement = new Vector3(transform.forward.x, 0, transform.forward.z).normalized * _glideSpeed;

            _verticalVelocity = _gravity + _nonJumpDrag + Time.deltaTime;

            Vector3 glideMovement = forwardMovement + Vector3.up * _verticalVelocity;

            CharacterController.Move(glideMovement * Time.deltaTime);
        }
    }
    void CheckStep()
    {
        RaycastHit hitLower;
        RaycastHit hitDown;
        if (Physics.Raycast(_groundDetector.position, transform.forward, out hitLower, stepCheckDistance))
        {
            RaycastHit hitUpper;
            if (!Physics.Raycast(stepRayUpper.position, transform.forward, out hitUpper, stepCheckDistance))
            {
                CharacterController.Move(Vector3.up * stepSmooth);
            }
        }
        else if (!Physics.Raycast(_groundDetector.position, Vector3.down, out hitDown, stepHeight + 0.1f) && !_isJump)
        {
            CharacterController.Move(Vector3.down * stepSmooth);

        }

    }
}