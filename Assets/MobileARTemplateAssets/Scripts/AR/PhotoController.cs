using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoController : MonoBehaviour
{
    public void SaveScreenshot()
    {
        StartCoroutine(CaptureAndSave());
    }

    private IEnumerator CaptureAndSave()
    {
        // 等待渲染结束，确保没有空白
        yield return new WaitForEndOfFrame();

        // 创建截图 Texture2D
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        // 保存到相册
        NativeGallery.SaveImageToGallery(
            tex,
            "ARJourney",  // 相册文件夹名
            "AR_screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
        );

        // 释放内存
        Destroy(tex);
    }

    
}
