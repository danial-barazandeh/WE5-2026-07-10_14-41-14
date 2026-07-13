using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ClipSwitcher : MonoBehaviour
{
    [SerializeField] AudioClip clipA;
    [SerializeField] AudioClip clipB;
    [SerializeField] AudioSource audioSource;        
    [SerializeField] AvaturnVisemeDriver visemeDriver; 

    public bool _useB = false;

    bool _lastUseB = false; // only for debug

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_useB != _lastUseB)   
        {
            _lastUseB = _useB;
            Trigger(_useB);       
        }
    }

    public void Trigger(bool play)
    {
        if (!play) return;

        _useB = !_useB;
        AudioClip next = _useB ? clipB : clipA;
        if (next == null)
        {
            Debug.LogWarning("Missing clip");
            return;
        }

        if (visemeDriver != null) visemeDriver.ResetVisemes();

        audioSource.Stop();
        audioSource.clip = next;
        audioSource.Play();
    }
}