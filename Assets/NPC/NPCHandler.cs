using UnityEngine;

public class NPCHandler : MonoBehaviour
{
    public enum GenderType
    {
        Male,
        Female
    }
    [SerializeField] private GenderType NPCGender;


    public enum TattooCategory
    {
        None,
        Violent,
        Tribal,
        Natural,
    }
    [SerializeField] private TattooCategory NPCTattooCategory;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
