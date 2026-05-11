using UnityEngine;

public class CameraDepth : MonoBehaviour
{
    void Start()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}
