using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraState : MonoBehaviour
{
    public Action OnChangePOV;
    public PlayerCameraPOV CameraStatePOV;
    [SerializeField]
    private InputManager _input;
    [SerializeField]
    public CinemachineCamera ThirdPersonCamera;
    [SerializeField]
    public CinemachineCamera FirstPersonCamera;
    private CinemachinePanTilt _cinemachinePanTilt;
    [SerializeField]
    private Transform _playerTransform;
    private PlayerStance _playerStance;


    private void Awake()
    {
        FirstPersonCamera.gameObject.SetActive(false);
        _cinemachinePanTilt = FirstPersonCamera.GetComponent<CinemachinePanTilt>();
        _cinemachinePanTilt.PanAxis.Range = new Vector3(-180, 180);
        _playerStance = PlayerStance.Stand;
        CameraStatePOV = PlayerCameraPOV.TP;
        // FirstPersonCamera.Priority = 0;
        // ThirdPersonCamera.Priority = 10;
    }
    private void Start()
    {
        _input.OnChangePOV += ChangePOV;
    }

    private void Update()
    {
    }

    private void OnDestroy()
    {
        _input.OnChangePOV -= ChangePOV;
    }

    public void StartClampedCamera(bool isClamped, PlayerStance playerStance)
    {
        _playerStance = playerStance;
        if (isClamped)
        {
            if (CameraStatePOV == PlayerCameraPOV.FP && playerStance == PlayerStance.Climb && playerStance == PlayerStance.Glide)
            {
                _cinemachinePanTilt.PanAxis.Range = new Vector3(-45, 45);
                _cinemachinePanTilt.PanAxis.Wrap = false;
            }
        }
        else
        {
            if (CameraStatePOV == PlayerCameraPOV.FP)
            {
                _cinemachinePanTilt.PanAxis.Range = new Vector3(-180, 180);
                _cinemachinePanTilt.PanAxis.Wrap = true;
            }
        }
    }

    private void ChangePOV()
    {

        if (CameraStatePOV == PlayerCameraPOV.TP)
        {
            CameraStatePOV = PlayerCameraPOV.FP;
            FirstPersonCamera.gameObject.SetActive(true);
            ThirdPersonCamera.gameObject.SetActive(false);

            // FirstPersonCamera.Priority = 10;
            // ThirdPersonCamera.Priority = 0;

            StartCoroutine(SmoothPOVTransition(
            FirstPersonCamera,
            Camera.main.transform.position,
            Camera.main.transform.rotation
        ));

            StartClampedCamera(true, _playerStance);
        }
        else if (CameraStatePOV == PlayerCameraPOV.FP)
        {
            CameraStatePOV = PlayerCameraPOV.TP;
            FirstPersonCamera.gameObject.SetActive(false);
            ThirdPersonCamera.gameObject.SetActive(true);
            // FirstPersonCamera.Priority = 0;
            // ThirdPersonCamera.Priority = 10;
            StartClampedCamera(false, _playerStance);
        }
        OnChangePOV?.Invoke();
    }
    private IEnumerator SmoothPOVTransition(CinemachineCamera cam, Vector3 targetPos, Quaternion targetRot)
    {
        float duration = 0.25f;
        float t = 0f;

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            Quaternion rot = Quaternion.Slerp(startRot, targetRot, t);

            cam.ForceCameraPosition(pos, rot);

            yield return null;
        }
    }

    public void SetTPFieldOfView(bool isClimbing)
    {
        if (CameraStatePOV != PlayerCameraPOV.TP) return;

        float targetFOV = isClimbing ? 70f : 40f;

        StartCoroutine(SmoothFOVTransition(targetFOV, 0.5f));
    }

    private IEnumerator SmoothFOVTransition(float targetFOV, float duration)
    {
        float startFOV = ThirdPersonCamera.Lens.FieldOfView;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            ThirdPersonCamera.Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, time / duration);
            yield return null;
        }

        ThirdPersonCamera.Lens.FieldOfView = targetFOV;
    }
}

