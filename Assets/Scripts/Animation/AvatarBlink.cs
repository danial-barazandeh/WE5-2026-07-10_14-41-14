using System.Collections;
using UnityEngine;

public class AvatarBlink : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer[] meshes;      
    [SerializeField] string blinkLeft = "eyeBlinkLeft";
    [SerializeField] string blinkRight = "eyeBlinkRight";
    [SerializeField] Vector2 intervalRange = new Vector2(2.5f, 6f);
    [SerializeField] float blinkDuration = 0.12f;

    int[] _idxL, _idxR;
    float _timer, _nextBlink;

    void Start()
    {
        _idxL = new int[meshes.Length];
        _idxR = new int[meshes.Length];
        for (int m = 0; m < meshes.Length; m++)
        {
            if (meshes[m] == null) { _idxL[m] = _idxR[m] = -1; continue; }
            var mesh = meshes[m].sharedMesh;
            _idxL[m] = mesh.GetBlendShapeIndex(blinkLeft);
            _idxR[m] = mesh.GetBlendShapeIndex(blinkRight);
        }
        _nextBlink = Random.Range(intervalRange.x, intervalRange.y);
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _nextBlink)
        {
            _timer = 0f;
            _nextBlink = Random.Range(intervalRange.x, intervalRange.y);
            StartCoroutine(DoBlink());
        }
    }

    IEnumerator DoBlink()
    {
        float half = blinkDuration * 0.5f;
        yield return Ramp(0f, 100f, half);   // close
        yield return Ramp(100f, 0f, half);   // open
    }

    IEnumerator Ramp(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetWeight(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetWeight(to);
    }

    void SetWeight(float w)
    {
        for (int m = 0; m < meshes.Length; m++)
        {
            if (_idxL[m] >= 0) meshes[m].SetBlendShapeWeight(_idxL[m], w);
            if (_idxR[m] >= 0) meshes[m].SetBlendShapeWeight(_idxR[m], w);
        }
    }
}