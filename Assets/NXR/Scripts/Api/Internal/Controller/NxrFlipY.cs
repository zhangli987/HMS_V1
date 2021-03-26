using UnityEngine;
namespace Nxr.Internal
{
    public class NxrFlipY : MonoBehaviour
{
    public Material _flipMat; 
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, _flipMat);
    }
}
}