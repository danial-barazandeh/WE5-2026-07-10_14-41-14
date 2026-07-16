using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

public class AvatarData
{
    public string gender;
    public string tatoo;
}

public class AvatarInteractionManager : MonoBehaviour
{
    enum State { Setup, Idle, PlayingFirst, WaitingForYes, PlayingSecond, ShowingPanel }

    [Header("Avatars (roots � one at a time, random order)")]
    [SerializeField] Transform[] avatars;            // each is the root of an avatar prefab
    [SerializeField] bool loop = true;               // reshuffle and start over when all are done
    [SerializeField] bool isListenerUsed = false;

    [Header("Audio clips")]
    [SerializeField] AudioClip firstClip;            // shared across avatars; see notes for per-avatar
    [SerializeField] AudioClip secondClip;

    [Header("Interaction")]
    [SerializeField] VoiceActivityListener voiceListener;

    [Header("Distance trigger")]
    [SerializeField] Camera targetCamera;
    [SerializeField] float triggerDistance = 2f;     // fires when the camera is within this distance (XZ)
    [SerializeField] bool isDistanceUsed = false;

    [Header("Camera reset")]
    [SerializeField] Transform cameraRig;            // XR rig / player root to reset (see notes for VR)

    [Header("Animation")]
    [SerializeField] string boolParam = "isAskingToOpen";

    [Header("Debug (keyboard � will become VR input later)")]
    [SerializeField] bool debugYesWithKey = true;    // Y = simulate hearing "yes"
    [SerializeField] KeyCode advanceKey = KeyCode.N; // N = advance past the questions panel

    [Header("Events")]
    public UnityEvent onShowQuestionsPanel;
    public UnityEvent onHideQuestionsPanel;

    State _state = State.Setup;
    int[] _order;                                    // shuffled indices into 'avatars'
    int _orderPos;

    // Resolved components of the CURRENT avatar
    Transform _current;
    AudioSource _audioSource;
    AvaturnVisemeDriver _visemeDriver;
    Animator _animator;

    Transform _resetTarget;
    Vector3 _initialCamPos;
    Quaternion _initialCamRot;

    bool isExperimentReadytoStart=false;



    // --- public state queries for external input (debug / VR) ---
    public bool IsIdle => _state == State.Idle;
    public bool IsWaitingForYes => _state == State.WaitingForYes;
    public bool IsShowingPanel => _state == State.ShowingPanel;


    public Vector3 currentAvatarPos; 

    public AvatarData GetAvatarData()
    {
        if (_current == null)
        {
            Debug.LogWarning("[Manager] GetAvatarData called with no active avatar");
            return null;
        }

        string fullName = _current.name;
        int i = fullName.IndexOf('_');

        AvatarData avatar = new AvatarData();
        avatar.gender = i >= 0 ? fullName.Substring(0, i) : fullName;
        avatar.tatoo = i >= 0 ? fullName.Substring(i + 1) : "";
        return avatar;
    }


    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        _resetTarget = cameraRig != null ? cameraRig
                     : (targetCamera != null ? targetCamera.transform : null);
        if (_resetTarget != null)
        {
            _initialCamPos = _resetTarget.position;
            _initialCamRot = _resetTarget.rotation;
        }
    }

    void Start()
    {
        BeginNextAvatar();
    }

    void Update()
    {
        // Distance trigger against the CURRENT avatar (XZ plane only)
        if (_state == State.Idle && _current != null && targetCamera != null)
        {
            if (isDistanceUsed)
            {
                Vector3 camPos = targetCamera.transform.position; camPos.y = 0f;
                Vector3 avatarPos = _current.position; avatarPos.y = 0f;
                currentAvatarPos = avatarPos; 
                if (Vector3.Distance(camPos, avatarPos) <= triggerDistance)
                    StartCoroutine(RunSequence());
            }
            else
            {
                Vector3 avatarPos = _current.position; avatarPos.y = 0f;
                currentAvatarPos = avatarPos; 
                if (isExperimentReadytoStart) StartCoroutine(RunSequence());
            }
                
        }

        if (debugYesWithKey && _state == State.WaitingForYes && Input.GetKeyDown(KeyCode.Y))
            NotifyYesHeard();

        if (_state == State.ShowingPanel && Input.GetKeyDown(advanceKey))
            AdvanceToNextAvatar();
    }

    public void StartExperimentWithUI()
    {
        isExperimentReadytoStart=true;
    }

    // ---------- sequence ----------

    IEnumerator RunSequence()
    {
        yield return new WaitForSeconds(1.0f);
        _state = State.PlayingFirst;
        yield return PlayClip(firstClip);

        _state = State.WaitingForYes;
        if (isListenerUsed)
        {
            if (voiceListener != null)
            {
                voiceListener.StartListening();
            }
            else Debug.LogError("[Manager] voiceListener not assigned");
        }
    }


    public void NotifyYesHeard()  // call this method to confirm the person has the key
    {
        if (_state != State.WaitingForYes) return;
        StartCoroutine(PlaySecondThenPanel());
    }

    IEnumerator PlaySecondThenPanel()
    {
        _state = State.PlayingSecond;

        if (_animator != null) _animator.SetBool(boolParam, true);
        yield return PlayClip(secondClip);
        if (_animator != null) _animator.SetBool(boolParam, false);

        ShowQuestionsPanel();
    }

    IEnumerator PlayClip(AudioClip clip)
    {
        if (clip == null || _audioSource == null) yield break;

        if (_visemeDriver != null) _visemeDriver.ResetVisemes();
        _audioSource.Stop();
        _audioSource.clip = clip;
        _audioSource.Play();

        yield return null;
        yield return new WaitWhile(() => _audioSource.isPlaying);

        if (_visemeDriver != null) _visemeDriver.ResetVisemes();
    }

    // ---------- questions panel (placeholder for later) ----------

    void ShowQuestionsPanel()
    {
        _state = State.ShowingPanel;
        Debug.Log($"[Manager] Questions panel for '{_current.name}' � press '{advanceKey}' to continue");
        onShowQuestionsPanel?.Invoke();
        // TODO: build/populate the actual questions panel here.
    }

    // ---------- advance / restart ----------

    public void AdvanceToNextAvatar() 
    {
        onHideQuestionsPanel?.Invoke();
        ResetCamera();
        BeginNextAvatar();
    }

    void BeginNextAvatar()
    {
        _state = State.Setup;

        if (_order == null || _orderPos >= _order.Length)
        {
            if (_order != null && !loop)
            {
                Debug.Log("[Manager] All avatars done.");
                ShowOnlyCurrent(-1);
                _current = null;
                return; 
            }
            BuildShuffledOrder();
        }

        int idx = _order[_orderPos++];
        ShowOnlyCurrent(idx);
        BindCurrent(avatars[idx]);

        if (_animator != null) _animator.SetBool(boolParam, false);

        _state = State.Idle;
        Debug.Log($"[Manager] Now active: {_current.name}");
    }

    // Resolve the components from the active avatar's root
    void BindCurrent(Transform root)
    {
        _current = root;
        _audioSource = root.GetComponent<AudioSource>();
        _visemeDriver = root.GetComponent<AvaturnVisemeDriver>();
        _animator = root.GetComponent<Animator>();

        if (_audioSource == null)
            Debug.LogError($"[Manager] '{root.name}' has no AudioSource on its root");
        if (_animator == null)
            Debug.LogWarning($"[Manager] '{root.name}' has no Animator on its root");
    }

    void BuildShuffledOrder()
    {
        _order = new int[avatars.Length];
        for (int i = 0; i < _order.Length; i++) _order[i] = i;

        for (int i = _order.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_order[i], _order[j]) = (_order[j], _order[i]);
        }
        _orderPos = 0;
    }

    void ShowOnlyCurrent(int idx)
    {
        for (int i = 0; i < avatars.Length; i++)
            if (avatars[i] != null)
                avatars[i].gameObject.SetActive(i == idx);
    }

    void ResetCamera()
    {
        if (_resetTarget != null)
        {
            _resetTarget.position = _initialCamPos;
            _resetTarget.rotation = _initialCamRot;
        }
    }
}