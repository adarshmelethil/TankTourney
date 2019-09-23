using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenshotHandler : MonoBehaviour
{

    private Camera myCamera;
    private bool takeScreenshotOnNextFrame;

    public string screenshotName;
    public KeyCode triggerKey = KeyCode.M;
    public bool isMain = false;

    // Start is called before the first frame update
    void Awake()
    {
        myCamera = gameObject.GetComponent<Camera>();
        if (!isMain) myCamera.enabled = false;

    }

    private void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log(triggerKey);
            TakeScreenshot(Screen.width, Screen.height);
        }
    }
    public void SetScreenshotName(string n)
    {
        screenshotName = n;
    }
    // Update is called once per frame
    void OnPostRender()
    {
        if (takeScreenshotOnNextFrame)
        {
            takeScreenshotOnNextFrame = false;

            RenderTexture renderTexture = myCamera.targetTexture;

            Texture2D renderResult = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            Rect rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
            renderResult.ReadPixels(rect, 0, 0);

            byte[] byteResult = renderResult.EncodeToPNG();

            string directorName = System.IO.Path.Combine(Application.dataPath, "screenshots", screenshotName);
            System.IO.Directory.CreateDirectory(directorName);
            var fileNameFormat = System.IO.Path.Combine(directorName, "{0}.png");

            var numOfFiles = System.IO.Directory.GetFiles(directorName).Length;
            
            System.IO.File.WriteAllBytes(System.String.Format(fileNameFormat, numOfFiles), byteResult);
            Debug.Log($"Saved {System.String.Format(fileNameFormat, numOfFiles)}");

            RenderTexture.ReleaseTemporary(renderTexture);
            myCamera.targetTexture = null;

            if (!isMain) myCamera.enabled = false;
        }
    }

    private void TakeScreenshot(int width, int height)
    {
        myCamera.targetTexture = RenderTexture.GetTemporary(width, height, 16);
        takeScreenshotOnNextFrame = true;
        myCamera.enabled = true;
    }
}
