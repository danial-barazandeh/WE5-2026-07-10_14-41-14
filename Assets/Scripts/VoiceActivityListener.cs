using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class VoiceActivityListener : MonoBehaviour
{
    [SerializeField] AvatarInteractionManager manager;

    [Header("Detection")]
    [SerializeField] float loudnessThreshold = 0.02f;  // calibrate to your environment (0..1)
    [SerializeField] float requiredDuration = 0.15f;    // seconds above threshold to count as speech
    [SerializeField] float ignoreInitial = 0.3f;        // ignore the mic's initial pop when starting
    [SerializeField] int sampleWindow = 1024;           // samples read per frame

    [Header("Debug")]
    [SerializeField] bool verbose = true;               // log start/stop of listening

    string _device;
    AudioClip _micClip;
    int _lastPos;
    float _loudTime;
    float _startTime;
    bool _listening;
    bool _pendingStart;      // waiting for the permission dialog to resolve
    float[] _buffer;

    void Awake()
    {
        // Ask for the mic permission as early as possible, so devices are populated later.
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
    }

    // Hook this to the manager after the first clip ends
    public void StartListening()
    {
        if (_listening)
        {
            if (verbose) Debug.Log("[VoiceActivity] StartListening ignored: already listening");
            return;
        }

#if UNITY_ANDROID
        // If not granted yet, request and defer the actual start until it is.
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            if (verbose) Debug.Log("[VoiceActivity] Mic permission not granted — requesting and deferring");
            Permission.RequestUserPermission(Permission.Microphone);
            _pendingStart = true;
            return;
        }
#endif
        BeginMic();
    }

    void Update()
    {
        // If we were waiting on the permission dialog, start once it's granted.
        if (_pendingStart)
        {
#if UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                _pendingStart = false;
                BeginMic();
            }
#else
            _pendingStart = false;
            BeginMic();
#endif
            return;
        }

        if (!_listening || _micClip == null) return;

        // Skip the first moments (pop when the mic turns on)
        if (Time.time - _startTime < ignoreInitial) return;

        int pos = Microphone.GetPosition(_device);
        if (pos < 0 || pos == _lastPos) return;

        float loudness = GetLoudness(pos);
        _lastPos = pos;

        Debug.Log($"[VoiceActivity] loudness={loudness:F6}");

        if (loudness >= loudnessThreshold)
        {
            _loudTime += Time.deltaTime;
            if (_loudTime >= requiredDuration)
                OnUserResponded();
        }
        else
        {
            _loudTime = 0f; // reset if it dropped below (ignore isolated noise)
        }
    }

    void BeginMic()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[VoiceActivity] No microphone detected even after permission. " +
                           "Check RECORD_AUDIO in the generated manifest.");
            return;
        }

        _device = null; // default mic
        _micClip = Microphone.Start(_device, true, 1, 16000);
        if (_micClip == null)
        {
            Debug.LogError("[VoiceActivity] Microphone.Start returned null.");
            return;
        }

        _buffer = new float[sampleWindow];
        _lastPos = 0;
        _loudTime = 0f;
        _startTime = Time.time;
        _listening = true;

        if (verbose)
            Debug.Log($"[VoiceActivity] Started listening (mic: '{Microphone.devices[0]}', threshold: {loudnessThreshold})");
    }

    public void StopListening() => StopListening("manual");

    void StopListening(string reason)
    {
        if (!_listening) return;
        Microphone.End(_device);
        _listening = false;
        _micClip = null;
        if (verbose) Debug.Log($"[VoiceActivity] Stopped listening (reason: {reason})");
    }

    float GetLoudness(int currentPos)
    {
        int start = currentPos - sampleWindow;
        if (start < 0) start += _micClip.samples; // wrap around the circular buffer

        _micClip.GetData(_buffer, start);

        float sum = 0f;
        for (int i = 0; i < _buffer.Length; i++)
            sum += _buffer[i] * _buffer[i];

        return Mathf.Sqrt(sum / _buffer.Length); // RMS
    }

    void OnUserResponded()
    {
        if (verbose) Debug.Log("[VoiceActivity] User response detected");
        StopListening("user responded");

        if (manager != null) manager.NotifyYesHeard();
        else Debug.LogError("[VoiceActivity] manager not assigned");
    }
}