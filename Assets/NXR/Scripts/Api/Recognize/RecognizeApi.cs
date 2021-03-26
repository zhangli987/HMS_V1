using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nxr.Internal
{
    public class Recognition
    {
        public string id;
        public string title;
        public float confidence;
        public Rect location;
        public int frameWidth;
        public int frameHeight;

        public void PrintInfo()
        {
            Debug.Log("Recognition :  id=" + id + ",title=" + title + ",confidence=" + confidence + ",location." + location.ToString()
                + ", width="+frameWidth+", height="+frameHeight);
        }
    }

    public class RecoginizeApi
    {
        public enum ErrorType
        {
            CAMERA_BUSY, MODEL_LOAD_ERROR, NOT_DECLARE_OBJECT_RECOGINIZE_PLUGIN_ID, NOT_SUPPORT_OBJECT_RECOGINIZE_PLUGIN_ID,
            SURFACETEXTURE_IS_NULL, BITMAP_CONFIG_ERROR
        }

        string managerClassName = "com.nibiru.tensorflow.core.NibiruTensorManager";
        string unityHelperClassName = "com.nibiru.lib.vr.NibiruUnityHelper";
        private static readonly object locker = new object();

        private RecoginizeApi()
        {
            destroyed = false;
        }

        private static RecoginizeApi _current;
        public static RecoginizeApi Instance
        {
            get
            {
                lock (locker)
                {
                    if (_current == null)
                    {
                        _current = new RecoginizeApi();
                    }
                    return _current;
                }
            }
        }

        private AndroidJavaObject androidActivity;
        private AndroidJavaClass nibiruTensorManagerClass;
        private AndroidJavaObject nibiruTensorManager;
        public void Init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                androidActivity = player.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }
        catch (AndroidJavaException e)
        {
            androidActivity = null;
            Debug.LogError("Exception while connecting to the Activity: " + e);
            return;
        }

        if(androidActivity != null)
        {
            AndroidJavaObject contextObject = androidActivity.Call<AndroidJavaObject>("getApplicationContext");
            nibiruTensorManagerClass = new AndroidJavaClass(managerClassName);
            nibiruTensorManager = nibiruTensorManagerClass.CallStatic<AndroidJavaObject>("getInstance");
            nibiruTensorManager.Call("init", contextObject, new NibiruVerifyListener(VerifyStatusCallback));
        }
#endif
            Debug.Log("RecognizeApi init succ.");
        }

        public void StartRecognize(OnRecognizeSuccess succ, OnRecognizeFailed failed)
        {
            if (nibiruTensorManager != null)
            {
                AndroidJavaClass unityHelperClass = new AndroidJavaClass(unityHelperClassName);
                AndroidJavaObject unityHelperObject = unityHelperClass.CallStatic<AndroidJavaObject>("getInstance");
                if (unityHelperObject == null)
                {
                    Debug.LogError("StartRecognize failed , UnityHelper.getInstance is null !!!");
                    return;
                }
                int status = unityHelperObject.Call<int>("getStatus");
                if (status < 1)
                {
                    Debug.LogError("StartRecognize failed , Must be after UnityHelper create SurfaceTexture !!! " + status);
                    return;
                }

                AndroidJavaObject surfaceTextureObject = unityHelperObject.Call<AndroidJavaObject>("getSurfaceTexture");


                nibiruTensorManager.Call("start", surfaceTextureObject, new NibiruRecognizeCallback(this, succ, failed));
                stoped = false;
                destroyed = false;

                NibiruService nibiruService = NxrViewer.Instance.GetNibiruService();
                if(nibiruService != null)
                {
                    nibiruService.SetCameraPreviewing(true);
                }
                Debug.Log("RecognizeApi start succ.");
            }
        }

        private bool stoped;
        public void StopRecognize()
        {
            if (nibiruTensorManager != null)
            {
                stoped = true;
                nibiruTensorManager.Call("stop");
                NibiruService nibiruService = NxrViewer.Instance.GetNibiruService();
                if (nibiruService != null)
                {
                    nibiruService.SetCameraPreviewing(false);
                }
            }
        }

        private bool destroyed = false;
        public void OnDestroy()
        {
            if (!destroyed && nibiruTensorManager != null)
            {
                destroyed = true;
                nibiruTensorManager.Call("destroy");
            }
        }

        public bool IsRunning()
        {
            return !stoped && !destroyed;
        }


        void VerifyStatusCallback(int status)
        {
            if (status != 0)
            {
                Debug.LogError("VerifyStatusCallback=" + status);
            } else
            {
                Debug.Log("Verify Success !!!");
            }
        }


        public delegate void OnNVRVerifyListener(int status);
        public class NibiruVerifyListener : AndroidJavaProxy
        {
            OnNVRVerifyListener _OnNVRVerifyListener;
            public NibiruVerifyListener(OnNVRVerifyListener onNVRVerifyListener) : base("com.nibiru.lib.vr.listener.NVRVerifyListener")
            {
                _OnNVRVerifyListener = onNVRVerifyListener;
            }

            public void onVerifySuccess()
            {
                if (_OnNVRVerifyListener != null)
                {
                    _OnNVRVerifyListener(0);
                }
            }

            public void onVerifyFailed(int status)
            {
                if (_OnNVRVerifyListener != null)
                {
                    _OnNVRVerifyListener(-1);
                }
            }

        }

        public delegate void OnRecognizeFailed(string message);
        public delegate void OnRecognizeSuccess(List<Recognition> recognitionData);
        public class NibiruRecognizeCallback : AndroidJavaProxy
        {
            RecoginizeApi _RecognizeApi;
            OnRecognizeSuccess _OnRecognizeSuccess;
            OnRecognizeFailed _OnRecognizeFailed;
            public NibiruRecognizeCallback(RecoginizeApi recognizeApi, OnRecognizeSuccess succ, OnRecognizeFailed failed) : base("com.nibiru.tensorflow.core.NibiruTensorCallback")
            {
                _RecognizeApi = recognizeApi;
                _OnRecognizeSuccess = succ;
                _OnRecognizeFailed = failed;
            }

            //List<Classifier.Recognition> mappedRecognitions
            public void onRecoginizeSuccess(AndroidJavaObject list)
            {
                if (!_RecognizeApi.IsRunning()) return;

                int size = list.Call<int>("size");
                List<Recognition> dataList = new List<Recognition>(size);
                for (int i = 0; i < size; i++)
                {
                    Recognition rec = new Recognition();
                    AndroidJavaObject item = list.Call<AndroidJavaObject>("get", i);
                    rec.id = item.Call<string>("getId");
                    rec.title = item.Call<string>("getTitle");
                    AndroidJavaObject confidenceObject = item.Call<AndroidJavaObject>("getConfidence");
                    rec.confidence = ((int) (confidenceObject.Call<float>("floatValue") * 100)) / 100.00f;
                    AndroidJavaObject locationObject = item.Call<AndroidJavaObject>("getLocation");
                    float cx = locationObject.Call<float>("centerX");
                    float cy = locationObject.Call<float>("centerY");
                    float width = locationObject.Call<float>("width");
                    float height = locationObject.Call<float>("height");
                    rec.frameWidth = item.Call<int>("getPreviewWidth");
                    rec.frameHeight = item.Call<int>("getPreviewHeight");
                    Rect rect = new Rect(cx - width / 2, cy - height / 2, width, height);
                    rec.location = rect;

                    // LOG
                    // rec.PrintInfo();

                    dataList.Add(rec);

                    Loom.QueueOnMainThread((param) =>
                    {
                        if (_OnRecognizeSuccess != null)
                        {
                            _OnRecognizeSuccess((List<Recognition>)param);
                        }
                    }, dataList);
                }
            }

            public void onRecognizeFailed(AndroidJavaObject errorType, string message)
            {
                Debug.LogError("onRecognizeFailed." + message);
                Loom.QueueOnMainThread((param) =>
                {
                    if (_OnRecognizeFailed != null)
                    {
                        _OnRecognizeFailed((string) param);
                    }
                }, message);
            }

        }

    }
}