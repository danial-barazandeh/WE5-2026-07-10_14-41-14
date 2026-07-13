using System.Collections;
using UnityEngine;
using UnityEngine.Events;


public class AvatarInteractionManager : MonoBehaviour
{
    enum State { Idle, PlayingFirst, WaitingForYes, PlayingSecond, Done }

    [Header("Audio")]
    [SerializeField] AudioClip firstClip;
    [SerializeField] AudioClip secondClip;
    [SerializeField] AudioSource audioSource;       // same GameObject as OVRLipSyncContext
    [SerializeField] AvaturnVisemeDriver visemeDriver;

    [Header("Distance trigger")]
    [SerializeField] Transform avatarPoint;         // reference point on the avatar (head/chest)
    [SerializeField] Camera targetCamera;
    [SerializeField] float triggerDistance = 2f;    // fires when the camera is AT LEAST this far

    [Header("Debug")]
    [SerializeField] bool debugYesWithKey = true;   // press Y to simulate hearing "yes"

    // Hook this to activate your speech recognizer (fires when the avatar stops talking)
    public UnityEvent onStartListeningForYes;

    State _state = State.Idle;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (targetCamera == null) targetCamera = Camera.main;
    }

    void Update()
    {
        if (_state == State.Idle && targetCamera != null && avatarPoint != null)
        {
            float d = Vector3.Distance(targetCamera.transform.position, avatarPoint.position);
            if (d <= triggerDistance)               // flip to <= for "within 2m"
                Debug.LogWarning("Achieved distance");
                StartCoroutine(RunSequence());
        }

        if (debugYesWithKey && _state == State.WaitingForYes && Input.GetKeyDown(KeyCode.Y))
            NotifyYesHeard();
    }

    IEnumerator RunSequence()
    {
        _state = State.PlayingFirst;
        yield return PlayClip(firstClip);

        _state = State.WaitingForYes;
        onStartListeningForYes?.Invoke();           
    }

    // Called by your speech recognizer when it detects "yes"
    public void NotifyYesHeard()
    {
        if (_state != State.WaitingForYes) return;
        StartCoroutine(PlaySecondThenDone());
    }

    IEnumerator PlaySecondThenDone()
    {
        _state = State.PlayingSecond;
        yield return PlayClip(secondClip);
        _state = State.Done;
    }

    IEnumerator PlayClip(AudioClip clip)
    {
        if (clip == null) yield break;
        if (visemeDriver != null) visemeDriver.ResetVisemes();

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();

        yield return null;                         
        yield return new WaitWhile(() => audioSource.isPlaying);

        if (visemeDriver != null) visemeDriver.ResetVisemes();
    }
}