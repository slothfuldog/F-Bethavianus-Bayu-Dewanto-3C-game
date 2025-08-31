
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource _footStepSFX;
    [SerializeField]
    private AudioSource _landingSFX;
    [SerializeField]
    private AudioSource _glideSFX;
    [SerializeField]
    private AudioSource _punchSFX;

    private void PlayFootstepSFX()
    {
        _footStepSFX.volume = Random.Range(0.8f, 1f);
        _footStepSFX.pitch = Random.Range(.8f, 1.5f);
        _footStepSFX.Play();
    }
    private void PlayFootstepOnCrouchSFX()
    {
        _footStepSFX.volume = Random.Range(0.2f, .4f);
        _footStepSFX.pitch = Random.Range(.3f, .5f);
        _footStepSFX.Play();
    }
    private void PlayLandingSFX()
    {
        _landingSFX.Play();
    }
    public void PlayGlideSFX()
    {
        _glideSFX.Play();
    }

    public void StopGlideSFX()
    {
        _glideSFX.Stop();
    }
    private void PlayPunchSFX()
    {
        _footStepSFX.volume = Random.Range(0.8f, 1f);
        _footStepSFX.pitch = Random.Range(.8f, 1.5f);
        _punchSFX.Play();
    }

}
