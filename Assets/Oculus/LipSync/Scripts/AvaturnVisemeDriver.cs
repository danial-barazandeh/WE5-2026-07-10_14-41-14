using UnityEngine;

[RequireComponent(typeof(OVRLipSyncContext))]
public class AvaturnVisemeDriver : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer[] meshes;   
    [SerializeField] float smoothing = 0.7f;    
    [SerializeField] float scale = 100f;
    
    static readonly string[] Names = { "viseme_sil", "viseme_PP", "viseme_FF", "viseme_TH", "viseme_DD", "viseme_kk", "viseme_CH", "viseme_SS", "viseme_nn", "viseme_RR", "viseme_aa", "viseme_E", "viseme_I", "viseme_O", "viseme_U" };
    OVRLipSyncContext _context; 
    int[][] _idx;         
    float[] _current;    

    void Start()
    {
        _context = GetComponent<OVRLipSyncContext>(); 
        _current = new float[Names.Length]; 
        _idx = new int[meshes.Length][];
        for (int m = 0; m < meshes.Length; m++)
        {
            _idx[m] = new int[Names.Length];
            if (meshes[m] == null) 
            { 
                Debug.LogWarning($"Mesh slot {m} is empty"); 
                for (int i = 0; i < Names.Length; i++) _idx[m][i] = -1; 
                continue; 
            }
            var mesh = meshes[m].sharedMesh; 
            for (int i = 0; i < Names.Length; i++) 
            { 
                _idx[m][i] = mesh.GetBlendShapeIndex(Names[i]); 
                if (_idx[m][i] < 0) Debug.LogWarning($"Blendshape not found in {meshes[m].name}: {Names[i]}"); 
            }
        }
    }
    void Update()
    {
        var frame = _context.GetCurrentPhonemeFrame(); 
        if (frame == null) 
            return;
        for (int i = 0; i < Names.Length; i++)
        {
            float target = frame.Visemes[i] * scale; 
            _current[i] = Mathf.Lerp(_current[i], target, 1f - smoothing);
            for (int m = 0; m < meshes.Length; m++) 
            { 
                if (_idx[m] == null) 
                    continue; 
                if (_idx[m][i] < 0) 
                    continue; 
                meshes[m].SetBlendShapeWeight(_idx[m][i], _current[i]); 
            }
        }
    }

    public void ResetVisemes()
    {
        for (int i = 0; i < _current.Length; i++)
        {
            _current[i] = 0f;
            for (int m = 0; m < meshes.Length; m++)
            {
                if (_idx[m] == null) continue;
                if (_idx[m][i] < 0) continue;
                meshes[m].SetBlendShapeWeight(_idx[m][i], 0f);
            }
        }
    }
}