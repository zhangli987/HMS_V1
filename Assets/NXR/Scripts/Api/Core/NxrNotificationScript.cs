using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Nxr.Internal
{
    [RequireComponent(typeof(MeshRenderer))]
    public class NxrNotificationScript : MonoBehaviour
    {
        private AndroidJavaObject javaObj = null;

        Material material;
        Texture defaultTexture;

        /// <summary>
        /// Preview the width and height of the texture. Be careful not to be too large, too large will affect the performance.
        /// </summary>
        public int PreTextureWidth = 1080;
        public int PreTextureHeight = 720;

        public enum CMD_ID
        {
            INIT = 1,
            CLICK = 2,
            HOVER = 3,
            COPY_TEXTURE = 4,
            GET_STATUS = 5,
            DESTROY = 6
        }

        // Use this for initialization
        void Start()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                AndroidJavaClass notification = new AndroidJavaClass("com.nibiru.lib.vr.unity.NibiruUnityNotification");
                javaObj = notification.CallStatic<AndroidJavaObject>("getInstance");
            }

            material = GetComponent<Renderer>().material;
            defaultTexture = material.mainTexture;

            InitTexture();


            material.mainTextureScale = new Vector2(1, -1);


        }

        private Texture2D mTexture = null;
        private bool updateMaterialTexture = false;
        // Initialize the video texture
        private void InitTexture()
        {
            // Create texture of size 0 that will be updated in the plugin (we allocate buffers in native code)
            mTexture = new Texture2D(PreTextureWidth, PreTextureHeight, TextureFormat.RGBA32, false);
            mTexture.filterMode = FilterMode.Bilinear;
            mTexture.wrapMode = TextureWrapMode.Repeat;
            int nativeTextureID = (int)mTexture.GetNativeTexturePtr();
            SendCmdToJava(CMD_ID.INIT, nativeTextureID + "," + PreTextureWidth + "," + PreTextureHeight);
        }



        void OnRenderObject()
        {
            int status = SendCmdToJava(CMD_ID.GET_STATUS, null);
            if (status >= 0)
            {
                int res = SendCmdToJava(CMD_ID.COPY_TEXTURE, null);
                if (res == 1)
                {
                    GL.InvalidateState();

                    if (!updateMaterialTexture)
                    {
                        material.mainTexture = mTexture;

                        updateMaterialTexture = true;
                    }
                }
            }
        }

        public int SendCmdToJava(CMD_ID cmdId, string parameter)
        {
            if (javaObj != null)
            {
                return javaObj.Call<int>("sendCmd", (int)cmdId, parameter);
            }
            else if (Application.isMobilePlatform)
            {
                Debug.Log("javaObj is null, " + cmdId + "," + parameter);

            }

            return -1;
        }

        void OnDestroy()
        {
            SendCmdToJava(CMD_ID.DESTROY, null);
            Debug.Log("NxrNotification.OnDestroy");
        }
    }
}