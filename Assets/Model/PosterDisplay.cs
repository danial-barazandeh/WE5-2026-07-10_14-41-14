using UnityEngine;

[ExecuteAlways]
public class PosterDisplay : MonoBehaviour
{
    [Tooltip("Drag your poster image here!")]
    public Texture2D newPosterImage;

    void OnValidate()
    {
        ApplyImage();
    }

    void Start()
    {
        ApplyImage();
    }

    void ApplyImage()
    {
        // Don't do anything if no image is assigned
        if (newPosterImage == null) return;

        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        // Create a property block to override the material locally
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);

        // _BaseMap is for URP, _MainTex is for Standard Built-in Pipeline
        block.SetTexture("_BaseMap", newPosterImage); 
        block.SetTexture("_MainTex", newPosterImage); 

        // Apply the override to this specific poster
        rend.SetPropertyBlock(block);
    }
}