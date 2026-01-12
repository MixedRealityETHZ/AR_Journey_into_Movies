using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles in-app screenshot capture by temporarily hiding UI elements
/// and saving the rendered frame to the device gallery.
/// </summary>
public class PhotoController : MonoBehaviour
{
    [Header("UI")]
    public GameObject ControlPanel; 
    public void SaveScreenshot()
    {
        StartCoroutine(CaptureAndSave());
    }

    private IEnumerator CaptureAndSave()
    {
        // Hide UI controls so they are not captured in the screenshot
        ControlPanel.SetActive(false);

        // Wait until the end of frame to ensure the final rendered image
        yield return new WaitForEndOfFrame();

        // Create a Texture2D to store the screenshot
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        // Save the screenshot to the device gallery
        NativeGallery.SaveImageToGallery(
            tex,
            "ARJourney",  // Gallery album name
            "AR_screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
        );

        ControlPanel.SetActive(true);
        // Release temporary texture to free memory
        Destroy(tex);
    }

    
}
