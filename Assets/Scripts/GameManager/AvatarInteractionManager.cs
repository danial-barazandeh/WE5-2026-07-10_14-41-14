using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AvatarData
{
    public string gender;
    public string tatoo;
}

public class AvatarInteractionManager : MonoBehaviour
{
    enum State { Setup, Idle, PlayingFirst, WaitingForYes, PlayingSecond, ShowingPanel }

    [Header("Avatars (roots - one at a time, random order)")]
    [SerializeField] Transform[] avatars;            // each is the root of an avatar prefab
    [SerializeField] bool loop = true;               // reshuffle and start over when all are done
    [SerializeField] bool isListenerUsed = false;

    [Header("Audio clips")]
    [SerializeField] AudioClip firstClip;
    [SerializeField] AudioClip secondClip;

    [Header("Interaction")]
    [SerializeField] VoiceActivityListener voiceListener;

    [Header("Distance trigger")]
    [SerializeField] Camera targetCamera;
    [SerializeField] float triggerDistance = 2f;     // fires when the camera is within this distance (XZ)
    [SerializeField] bool isDistanceUsed = false;

    [Header("Camera reset")]
    [SerializeField] Transform cameraRig;            // XR Origin / rig root (NAO a camera)
    [SerializeField] float yawOffset = 180f;        
    [SerializeField] bool resetOnStart = true;

    [Header("Animation")]
    [SerializeField] string boolParam = "isAskingToOpen";

    [Header("Debug (keyboard - will become VR input later)")]
    [SerializeField] bool debugYesWithKey = true;
    [SerializeField] KeyCode advanceKey = KeyCode.N;

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


    Vector3 _startPos;
    float _startYaw;

    bool isExperimentReadytoStart = false;

    // --- public state queries for external input (debug / VR) ---
    public bool IsIdle => _state == State.Idle;
    public bool IsWaitingForYes => _state == State.WaitingForYes;
    public bool IsShowingPanel => _state == State.ShowingPanel;

    public Vector3 currentAvatarPos;
    public float restartTime = 3f;

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

        if (cameraRig == null && targetCamera != null)
            cameraRig = targetCamera.transform.parent;

        if (cameraRig != null)
        {
            _startPos = cameraRig.position;
            _startYaw = cameraRig.eulerAngles.y;
        }
        else
        {
            Debug.LogError("[Manager] cameraRig nao atribuido - ResetCamera nao vai funcionar");
        }
    }

    void Start()
    {
        BeginNextAvatar();


       // if (resetOnStart) StartCoroutine(ResetCameraNextFrame());
    }

    IEnumerator ResetCameraNextFrame()
    {
        yield return new WaitForEndOfFrame();
        ResetCamera();
    }

    void Update()
    {
        // Distance trigger against the CURRENT avatar (XZ plane only)
        if (_state == State.Idle && _current != null && targetCamera != null)
        {
            Vector3 avatarPos = _current.position; avatarPos.y = 0f;
            currentAvatarPos = avatarPos;

            if (isDistanceUsed)
            {
                Vector3 camPos = targetCamera.transform.position; camPos.y = 0f;
                if (Vector3.Distance(camPos, avatarPos) <= triggerDistance)
                    StartCoroutine(RunSequence());
            }
            else
            {
                if (isExperimentReadytoStart) StartCoroutine(RunSequence());
            }
        }
    }

    public void StartExperimentWithUI()
    {
        isExperimentReadytoStart = true;
    }

    // ---------- sequence ----------

    IEnumerator RunSequence()
    {

        _state = State.PlayingFirst;

        yield return new WaitForSeconds(restartTime);
        yield return PlayClip(firstClip);

        _state = State.WaitingForYes;
        if (isListenerUsed)
        {
            if (voiceListener != null) voiceListener.StartListening();
            else Debug.LogError("[Manager] voiceListener not assigned");
        }
    }

    public void NotifyYesHeard()
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

    // ---------- questions panel ----------

    void ShowQuestionsPanel()
    {
        _state = State.ShowingPanel;
        Debug.Log($"[Manager] Questions panel for '{_current.name}'");
        onShowQuestionsPanel?.Invoke();
    }

    // ---------- advance / restart ----------

    public void AdvanceToNextAvatar()
    {
        onHideQuestionsPanel?.Invoke();
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

    // ---------- camera reset ----------


    static float YawOf(Transform t)
    {
        Vector3 f = t.forward;
        f.y = 0f;

        if (f.sqrMagnitude < 1e-4f)   
        {
            f = -t.up;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-4f) return 0f;
        }

        return Quaternion.LookRotation(f.normalized, Vector3.up).eulerAngles.y;
    }

    public void ResetCamera()
    {
        if (cameraRig == null || targetCamera == null) return;

        Transform cam = targetCamera.transform;

        float targetYaw = _startYaw + yawOffset;
        float delta = Mathf.DeltaAngle(YawOf(cam), targetYaw);
        cameraRig.RotateAround(cam.position, Vector3.up, delta);


        Vector3 offset = _startPos - cam.position;
        offset.y = 0f;
        cameraRig.position += offset;
    }
}