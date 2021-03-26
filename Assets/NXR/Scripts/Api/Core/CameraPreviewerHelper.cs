using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Nxr.Internal
{
    public class CameraPreviewHelper
    {
        public int PreTextureWidth = 640;//16:9
        public int PreTextureHeight = 360;

        public CameraPreviewHelper()
        {
            GetJavaObject();
        }

        IEnumerator WaitPreviewTextureIdSeted()
        {
            Debug.Log("WaitPreviewTextureIdSeted begin...");
            //   -1=not init, 0=init, 1=set texture id, 2=running, 3=destroy
            yield return new WaitUntil(() => GetStatus() >= 1);
            Debug.Log("WaitPreviewTextureIdSeted end...");
        }

        /// <summary>
        /// Get preview's status
        /// -1=not init, 0=init, 1=set texture id, 2=running, 3=destroy
        /// </summary>
        /// <returns></returns>
        public int GetStatus()
        {
            return GetJavaObject().Call<int>("getStatus");
        }

        /// <summary>
        /// Take picture
        /// </summary>
        /// <param name="screenShotCallback"></param>
        /// <param name="filePath"></param>
        public void TakePicture(ScreenShotCallback screenShotCallback, string filePath)
        {
            GetJavaObject().Call("doTakePicture", screenShotCallback ,filePath);
        }

        /// <summary>
        /// Set the native texture object that the video frames will be copied to
        /// </summary>
        public void SetTextureID(int textureID, int width, int height)
        {
            PreTextureWidth = width;
            PreTextureHeight = height;
            GetJavaObject().Call<bool>("setTextureId", textureID, width, height);
        }

        public bool CopyTexture()
        {
            return GetJavaObject().Call<bool>("copyTexture");
        }

        public bool CopyTexture(bool flipX)
        {
            return GetJavaObject().Call<bool>("copyTexture", flipX);
        }

        public void Destroy()
        {
            GetJavaObject().Call("destroy");
        }

        public int GetOESTextureId()
        {
            return GetJavaObject().Call<int>("getOESTextureId");
        }

        public AndroidJavaObject GetSurfaceTexture()
        {
            return GetJavaObject().Call<AndroidJavaObject>("getSurfaceTexture");
        }

#if UNITY_ANDROID

        private AndroidJavaObject javaObj = null;

        public AndroidJavaObject GetJavaObject()
        {
            if (javaObj == null)
            {
                javaObj = new AndroidJavaObject("com.nibiru.lib.vr.NibiruUnityHelper");
            }

            return javaObj;
        }
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN
        private AndroidJavaObject GetJavaObject() { return null;}
#endif

        public delegate void OnScreenShot(bool isSuccess);
        public class ScreenShotCallback : AndroidJavaProxy
        {
            private OnScreenShot mOnScreenShot;
            public ScreenShotCallback(OnScreenShot onScreenShot) : base("com.nibiru.lib.vr.listener.NVRScreenShotListener")
            {
                mOnScreenShot = onScreenShot;
            }

            public void onSuccess()
            {

                // 用Loom的方法在Unity主线程中调用Text组件
                Loom.QueueOnMainThread((param) =>
                {
                    if (mOnScreenShot != null)
                    {
                        mOnScreenShot((bool)param);
                    }
                },true);

            }

            public void onFailed()
            { 
                // 用Loom的方法在Unity主线程中调用Text组件
                Loom.QueueOnMainThread((param) =>
                {
                    if (mOnScreenShot != null)
                    {
                        mOnScreenShot((bool)param);
                    }
                }, false);

            }

        }
    }
}