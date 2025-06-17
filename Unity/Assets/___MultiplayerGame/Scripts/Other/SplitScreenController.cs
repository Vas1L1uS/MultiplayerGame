using UnityEngine;

public class SplitScreenController : MonoBehaviour
{
    public Camera[] cameras;
    public enum CameraMode { TwoCameras, FourCameras }
    public CameraMode cameraMode = CameraMode.TwoCameras;

    public enum SplitMode2 { Horizontal, Vertical }
    public SplitMode2 splitMode2 = SplitMode2.Horizontal;

    public enum SplitMode4 { Grid2x2, TopBottom, LeftRight }
    public SplitMode4 splitMode4 = SplitMode4.Grid2x2;

    void Start()
    {
        UpdateCameraViewports();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            cameraMode = CameraMode.TwoCameras;
            UpdateCameraViewports();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            cameraMode = CameraMode.FourCameras;
            UpdateCameraViewports();
        }

        if (Input.GetKeyDown(KeyCode.H) && cameraMode == CameraMode.TwoCameras)
        {
            splitMode2 = SplitMode2.Horizontal;
            UpdateCameraViewports();
        }
        if (Input.GetKeyDown(KeyCode.V) && cameraMode == CameraMode.TwoCameras)
        {
            splitMode2 = SplitMode2.Vertical;
            UpdateCameraViewports();
        }

        if (Input.GetKeyDown(KeyCode.G) && cameraMode == CameraMode.FourCameras)
        {
            splitMode4 = SplitMode4.Grid2x2;
            UpdateCameraViewports();
        }
        if (Input.GetKeyDown(KeyCode.T) && cameraMode == CameraMode.FourCameras)
        {
            splitMode4 = SplitMode4.TopBottom;
            UpdateCameraViewports();
        }
        if (Input.GetKeyDown(KeyCode.L) && cameraMode == CameraMode.FourCameras)
        {
            splitMode4 = SplitMode4.LeftRight;
            UpdateCameraViewports();
        }
    }

    void UpdateCameraViewports()
    {
        foreach (var cam in cameras)
        {
            cam.gameObject.SetActive(false);
        }

        switch (cameraMode)
        {
            case CameraMode.TwoCameras:
                cameras[0].gameObject.SetActive(true);
                cameras[1].gameObject.SetActive(true);

                if (splitMode2 == SplitMode2.Horizontal)
                {
                    cameras[0].rect = new Rect(0f, 0f, 0.5f, 1f);
                    cameras[1].rect = new Rect(0.5f, 0f, 0.5f, 1f);
                }
                else
                {
                    // ¬ертикальное разделение (верхн€€ и нижн€€ половины)
                    cameras[0].rect = new Rect(0f, 0.5f, 1f, 0.5f);
                    cameras[1].rect = new Rect(0f, 0f, 1f, 0.5f);
                }
                break;

            case CameraMode.FourCameras:
                cameras[0].gameObject.SetActive(true);
                cameras[1].gameObject.SetActive(true);
                cameras[2].gameObject.SetActive(true);
                cameras[3].gameObject.SetActive(true);

                switch (splitMode4)
                {
                    case SplitMode4.Grid2x2:
                        cameras[0].rect = new Rect(0f, 0.5f, 0.5f, 0.5f);
                        cameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                        cameras[2].rect = new Rect(0f, 0f, 0.5f, 0.5f);
                        cameras[3].rect = new Rect(0.5f, 0f, 0.5f, 0.5f);
                        break;

                    case SplitMode4.TopBottom:
                        cameras[0].rect = new Rect(0f, 0.5f, 0.5f, 0.5f);
                        cameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);     
                        cameras[2].rect = new Rect(0f, 0f, 0.5f, 0.5f);        
                        cameras[3].rect = new Rect(0.5f, 0f, 0.5f, 0.5f);       
                        break;

                    case SplitMode4.LeftRight:
                        cameras[0].rect = new Rect(0f, 0f, 0.5f, 1f);      
                        cameras[1].rect = new Rect(0f, 0f, 0.5f, 1f);       
                        cameras[2].rect = new Rect(0.5f, 0f, 0.5f, 1f);    
                        cameras[3].rect = new Rect(0.5f, 0f, 0.5f, 1f);       
                        break;
                }
                break;
        }
    }
}