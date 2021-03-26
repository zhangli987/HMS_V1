using UnityEngine;
namespace Nxr.Internal
{
    public class CameraPreviewScript : MonoBehaviour
    {
        Material material;
        Texture defaultTexture;
        CameraPreviewHelper cameraPreviewHelper;

        /// <summary>
        /// Preview the width and height of the texture. Be careful not to be too large, too large will affect the performance.
        /// </summary>
        public int PreTextureWidth = 640;//16:9
        public int PreTextureHeight = 360;

        /// <summary>
        /// Auto focus,perform one focus every two seconds.
        /// </summary>
        public bool AutoCameraFocus = false;

        private bool isCamersFocusRunning = false;

        /// <summary>
        /// Returns the video player.
        /// </summary>
        public CameraPreviewHelper CameraPreviewHelper
        {
            get { return cameraPreviewHelper; }
        }

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

            material = GetComponent<Renderer>().material;
            defaultTexture = material.mainTexture;
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
            mTexture.wrapMode = TextureWrapMode.Repeat;
        }

        public bool IsTextureUpdated()
        {
            return textureIdSeted;
        }

        private bool showCameraTexture;
        public void DismissCameraTexture()
        {
            material.color = new Color(material.color.r, material.color.g, material.color.b, 0);
        }

        public void ShowCameraTexture()
        {
            material.color = new Color(material.color.r, material.color.g, material.color.b, 1);
        }


        private void Update()
        {
            if (!inited)
            {
                InitTexture();
                inited = true;
            }
            else if (!textureIdSeted && cameraPreviewHelper != null)
            {
                int nativeTextureID = (int)mTexture.GetNativeTexturePtr();
                cameraPreviewHelper.SetTextureID(nativeTextureID, PreTextureWidth, PreTextureHeight);
                textureIdSeted = true;
            }
        }

        // Update is called once per frame
        void OnRenderObject()
        {
            if (cameraPreviewHelper != null)
            {
                bool succ = cameraPreviewHelper.CopyTexture();
                // refresh unity gl state
                if (succ)
                {
                    GL.InvalidateState();
                }

                // recoginize
                if(succ && !nibiruService.IsCameraPreviewing())
                {
                    // camera preview stopped !!!
                    Debug.LogError("IsCameraPreviewing false!!!");
                    succ = false;
                }

                if (succ && !updateMaterialTexture)
                {
                    material.mainTexture = mTexture;

                    updateMaterialTexture = true;
                }
                else if (!succ)
                {
                    material.mainTexture = defaultTexture;
                    // material.mainTextureScale = new Vector2(1, 1);
                    updateMaterialTexture = false;
                }


                //if (succ && nibiruService.GetCurrentCameraId() == CAMERA_ID.BACK)
                //{
                // material.mainTextureScale = new Vector2(-1, 1);
                //}

                if (AutoCameraFocus && succ && !isCamersFocusRunning)
                {
                    StartAutoCameraFocus();
                }

                if (textureIdSeted && !succ && isCamersFocusRunning)
                {
                    StoptAutoCameraFocus();
                }

            }
        }

        void StartAutoCameraFocus()
        {
            isCamersFocusRunning = true;
            InvokeRepeating("DoCameraFocus", 0, 2);
        }

        void StoptAutoCameraFocus()
        {
            isCamersFocusRunning = false;
            CancelInvoke("DoCameraFocus");
        }

        private void DoCameraFocus()
        {
            if (cameraPreviewHelper != null && nibiruService != null)
            {
                nibiruService.DoCameraAutoFocus();
            }
        }

        void OnDestroy()
        {
            if (isCamersFocusRunning)
            {
                StoptAutoCameraFocus();
            }

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
#elif UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
       public bool IsTextureUpdated() { return false; }
#endif


    }
}