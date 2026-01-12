using UnityEngine;
using UnityEngine.UI;
using ARJourneyIntoMovies.AR;  
using TMPro;

/// <summary>
/// UI controller for adjusting the spawned character's position
/// using three sliders mapped to camera-local X/Y/Z offsets.
/// </summary>
public class PersonPositionController : MonoBehaviour
{
    [Header("References")]
    public OverlayManager overlayManager;  
    public Slider sliderX;                 
    public Slider sliderY;                 
    public Slider sliderZ;                

    [Header("UI Text Displays")]
    public TMP_Text textX;                    
    public TMP_Text textY;                    
    public TMP_Text textZ;                     

    [Header("Range Settings")]
    public float rangeX = 10.0f;
    public float rangeY = 10.0f;
    public float rangeZ = 10.0f;

    private void Start()
    {
        if (sliderX != null)
            sliderX.onValueChanged.AddListener(OnSliderChanged);
        if (sliderY != null)
            sliderY.onValueChanged.AddListener(OnSliderChanged);
        if (sliderZ != null)
            sliderZ.onValueChanged.AddListener(OnSliderChanged);

        // Initialize UI once at startup
        UpdatePositionAndText();
    }

    private void OnDestroy()
    {
        if (sliderX != null)
            sliderX.onValueChanged.RemoveListener(OnSliderChanged);
        if (sliderY != null)
            sliderY.onValueChanged.RemoveListener(OnSliderChanged);
        if (sliderZ != null)
            sliderZ.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float _)
    {
        UpdatePositionAndText();
    }

    private void UpdatePositionAndText()
    {
        if (overlayManager == null)
            return;

        // Map normalized slider values to camera-local offsets (meters)
        float x = (sliderX != null ? sliderX.value : 0f) * -rangeX;
        float y = (sliderY != null ? sliderY.value : 0f) * -rangeY;
        float z = (sliderZ != null ? sliderZ.value : 0f) * rangeZ;

        // Update character position via OverlayManager
        overlayManager.UpdatePersonPosition(x, y, z);

        // Display values with two decimal precision
        if (textX != null)
            textX.text = $"X: {x:F2} m";
        if (textY != null)
            textY.text = $"Y: {y:F2} m";
        if (textZ != null)
            textZ.text = $"Z: {z:F2} m";
    }
}