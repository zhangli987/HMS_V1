using UnityEngine;
namespace Nxr.Internal
{
    public class CameraPreviewForMarker : MonoBehaviour
    {
        Material material;
        Texture defaultTexture;
        CameraPreviewHelper cameraPreviewHelper;

        /// <summary>
        /// Preview the width and height of the texture. Be careful not to be too large, too large will affect the performance.
        /// </summary>
        protected int PreTextureWidth = 640;//16:9
        protected int PreTextureHeight = 480;

        private bool EnablePreView = false;

        /// <summary>
        /// Returns the video player.
        /// </summary>
        public CameraPreviewHelper CameraPreviewHelper
        {
            get { return cameraPreviewHelper; }
        }

        int BackgroundLayer0 = 8;

        NibiruService nibiruService;
        // Use this for initialization
        void Start()
        {
            Debug.Log("Camera PreView Texture [" + PreTextureWidth + "," + PreTextureHeight + "]");
            nibiruService = NxrViewer.Instance.GetNibiruService();
            if (nibiruService != null)
            {
                cameraPreviewHelper = nibiruService.InitCameraPreviewHelper();
            }

            Renderer renderer = GetComponent<Renderer>();
            material = renderer == null ? null : renderer.material;
            defaultTexture = material != null ? material.mainTexture : null;
#if UNITY_ANDROID
            InitTexture();
#endif
        }

#if UNITY_ANDROID
        private bool inited = false;
        private bool textureIdSeted = false;
        private bool updateMaterialTexture = false;
        private Texture2D mTexture = null;
        // Initialize the video texture
        private void InitTexture()
        {
            // Create texture of size 0 that will be updated in the plugin (we allocate buffers in native code)
            mTexture = new Texture2D(PreTextureWidth, PreTextureHeight, TextureFormat.RGBA32, false);
            mTexture.filterMode = FilterMode.Bilinear;
            mTexture.wrapMode = TextureWrapMode.Clamp;

            inited = true;
        }

        private void SetTextureId()
        {
            int nativeTextureID = (int)mTexture.GetNativeTexturePtr();
            cameraPreviewHelper.SetTextureID(nativeTextureID, PreTextureWidth, PreTextureHeight);
            textureIdSeted = true;
        }

        private void Update()
        {
            if (!inited)
            {
                InitTexture();
            }
            else if (!textureIdSeted && cameraPreviewHelper != null)
            {
                SetTextureId();
            }
        }

        NxrPostRender postRenderObject;
        // Update is called once per frame
        void OnRenderObject()
        {
            if (!EnablePreView) return;

            bool succ = false;
            if (textureIdSeted && cameraPreviewHelper != null)
            {
                succ = cameraPreviewHelper.CopyTexture();
                // refresh unity gl state
                if (succ)
                {
                    GL.InvalidateState();
                }
            }

            bool updatePreViewTexture = EnablePreView && nibiruService != null && nibiruService.IsMarkerRecognizeRunning && inited;
            if (postRenderObject == null)
            {
                postRenderObject = GameObject.Find("NxrPostRender").GetComponent<NxrPostRender>();
            }
            else if(succ)
            {
                // defaultTexture;
                postRenderObject.PreviewTexture = updatePreViewTexture ? mTexture : null;
            }

        }

        void OnDestroy()
        {
            if (nibiruService != null)
            {
                nibiruService.StopCamereaPreView();
            }
            if (cameraPreviewHelper != null)
            {
                cameraPreviewHelper.Destroy();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                // 暂停相机
                OnDestroy();
            }
            else
            {
                textureIdSeted = false;
                updateMaterialTexture = false;
            }
        }
#endif
    }
}