using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nxr.Internal
{
    /// <summary>
    /// 
    /// </summary>
    public class NibiruService
    {
        private const string NibiruSDKClassName = "com.nibiru.lib.vr.NibiruVR";
        private const string ServiceClassName = "com.nibiru.service.NibiruService";
        protected AndroidJavaObject androidActivity;
        protected AndroidJavaClass nibiruSDKClass;
        protected AndroidJavaObject nibiruOsServiceObject;
        protected AndroidJavaObject nibiruSensorServiceObject;
        protected AndroidJavaObject nibiruVoiceServiceObject;
        protected AndroidJavaObject nibiruGestureServiceObject;
        protected AndroidJavaObject nibiruVRServiceObject;
        protected AndroidJavaObject nibiruCameraServiceObject;
        protected AndroidJavaObject nibiruMarkerServiceObject;

        protected CameraPreviewHelper cameraPreviewHelper;
        protected AndroidJavaObject cameraDeviceObject;
        protected AndroidJavaObject audioManager;

        public int HMDCameraId;
        public int ControllerCameraId;

        private bool isCameraPreviewing = false;

        private string systemDevice = "";

        public void Init()
        {
#if UNITY_ANDROID
            try
            {
                using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    androidActivity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    audioManager = androidActivity.Call<AndroidJavaObject>("getSystemService",
                        new AndroidJavaObject("java.lang.String", "audio"));
                }
            }
            catch (AndroidJavaException e)
            {
                androidActivity = null;
                Debug.LogError("Exception while connecting to the Activity: " + e);
                return;
            }

            nibiruSDKClass = BaseAndroidDevice.GetClass(NibiruSDKClassName);

            // 
            // systemDevice = nibiruSDKClass.CallStatic<string>("getSystemProperty", "ro.product.device", "");

            nibiruOsServiceObject = nibiruSDKClass.CallStatic<AndroidJavaObject>("getNibiruOSService", androidActivity);
            nibiruSensorServiceObject =
                nibiruSDKClass.CallStatic<AndroidJavaObject>("getNibiruSensorService", androidActivity);
            nibiruVoiceServiceObject =
                nibiruSDKClass.CallStatic<AndroidJavaObject>("getNibiruVoiceService", androidActivity);
            nibiruGestureServiceObject =
                nibiruSDKClass.CallStatic<AndroidJavaObject>("getNibiruGestureService", androidActivity);
            nibiruVRServiceObject = nibiruSDKClass.CallStatic<AndroidJavaObject>("getUsingNibiruVRServiceGL");

            nibiruCameraServiceObject =
                nibiruSDKClass.CallStatic<AndroidJavaObject>("getNibiruCameraService", androidActivity);
            nibiruMarkerServiceObject =
                nibiruSDKClass.CallStatic<AndroidJavaObject>("getNibiruMarkerService", androidActivity);

            HMDCameraId = nibiruCameraServiceObject.Call<int>("getHMDCameraId");
            ControllerCameraId = nibiruCameraServiceObject.Call<int>("getControllerCameraId");

            UpdateVoiceLanguage();
            // Debug.Log("nibiruOsServiceObject is "+ nibiruOsServiceObject.Call<AndroidJavaObject>("getClass").Call<string>("getName"));
            // Debug.Log("nibiruSensorServiceObject is " + nibiruSensorServiceObject.Call<AndroidJavaObject>("getClass").Call<string>("getName"));

            NibiruTask.NibiruTaskApi.Init();

            IsCaptureEnabled = -1;

            // 默认触发请求权限：
            RequsetPermission(new string[]
            {
                NxrGlobal.Permission.CAMERA,
                NxrGlobal.Permission.WRITE_EXTERNAL_STORAGE,
                NxrGlobal.Permission.READ_EXTERNAL_STORAGE,
                NxrGlobal.Permission.ACCESS_NETWORK_STATE,
                NxrGlobal.Permission.ACCESS_COARSE_LOCATION,
                NxrGlobal.Permission.BLUETOOTH,
                NxrGlobal.Permission.BLUETOOTH_ADMIN,
                NxrGlobal.Permission.INTERNET,
                NxrGlobal.Permission.GET_TASKS,
            });
#endif
        }

        public static int NKEY_SYS_HANDLE = 0;
        public static int NKEY_APP_HANDLE = 1;

        /// <summary>
        /// Handle N key event
        /// 0=system handle 
        /// 1=app handle
        /// </summary>
        /// <param name="mode"></param>
        public void RegHandleNKey(int mode)
        {
            if (nibiruVRServiceObject != null)
            {
                RunOnUIThread(androidActivity,
                    new AndroidJavaRunnable(() => { nibiruVRServiceObject.Call("regHandleNKey", mode); }));
            }
            else
            {
                Debug.LogError("regHandleNKey failed, nibiruVRServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Enable fps statistics
        /// </summary>
        /// <param name="isEnabled"></param>
        public void SetEnableFPS(bool isEnabled)
        {
            if (nibiruVRServiceObject != null)
            {
                nibiruVRServiceObject.Call("setEnableFPS", isEnabled);
            }
            else
            {
                Debug.LogError("SetEnableFPS failed, nibiruVRServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Get fps : 0=app,1=dtr
        /// </summary>
        /// <returns></returns>
        public float[] GetFPS()
        {
            if (nibiruVRServiceObject != null)
            {
                return nibiruVRServiceObject.Call<float[]>("getFPS");
            }
            else
            {
                Debug.LogError("SetEnableFPS failed, nibiruVRServiceObject is null !!!");
            }

            return new float[] {-1, -1};
        }

        /// <summary>
        /// Register virtual mouse service
        /// </summary>
        /// <param name="serviceStatus"></param>
        public void RegisterVirtualMouseService(OnVirtualMouseServiceStatus serviceStatus)
        {
            if (nibiruOsServiceObject != null)
            {
                nibiruOsServiceObject.Call("registerVirtualMouseManagerService",
                    new NibiruVirtualMouseServiceListener(serviceStatus));
            }
            else
            {
                Debug.LogError("RegisterVirtualMouseService failed, nibiruOsServiceObject is null !!!");
            }
        }

        /// <summary>
        /// UnRegister virtual mouse service
        /// </summary>
        public void UnRegisterVirtualMouseService()
        {
            if (nibiruOsServiceObject != null)
            {
                nibiruOsServiceObject.Call("unRegisterVirtaulMouseManagerService");
            }
            else
            {
                Debug.LogError("UnRegisterVirtualMouseService failed, nibiruOsServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Set enable virtual mouse
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public bool SetEnableVirtualMouse(bool enabled)
        {
            if (nibiruOsServiceObject != null)
            {
                return nibiruOsServiceObject.Call<bool>("setEnableVirtualMouse", enabled);
            }
            else
            {
                Debug.LogError("SetEnableVirtualMouse failed, nibiruOsServiceObject is null !!!");
                return false;
            }
        }

        public CameraPreviewHelper InitCameraPreviewHelper()
        {
            cameraPreviewHelper = new CameraPreviewHelper();
            return cameraPreviewHelper;
        }

        public CameraPreviewHelper CameraPreviewHelper
        {
            get { return cameraPreviewHelper; }
        }

        /// <summary>
        /// Get camera status async
        /// </summary>
        public void GetCameraStatus()
        {
            int cameraId = HMDCameraId;
            if (nibiruCameraServiceObject != null)
            {
                nibiruCameraServiceObject.Call("getCameraStatus", cameraId, new CameraStatusCallback());
            }
            else
            {
                Debug.LogError("GetCameraStatus failed, because nibiruCameraServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Open camera
        /// </summary>
        /// <returns> NibiruCameraDevice</returns>
        private AndroidJavaObject OpenCamera()
        {
            int cameraId = HMDCameraId;
            if (nibiruCameraServiceObject != null && cameraDeviceObject == null)
            {
                cameraDeviceObject = nibiruCameraServiceObject.Call<AndroidJavaObject>("openCamera", cameraId);
                return cameraDeviceObject;
            }
            else if (cameraDeviceObject != null)
            {
                return cameraDeviceObject;
            }
            else
            {
                Debug.LogError("OpenCamera failed, because nibiruCameraServiceObject is null !!!");
                return null;
            }
        }

        /// <summary>
        /// Get current cameraId
        /// </summary>
        /// <returns></returns>
        public CAMERA_ID GetCurrentCameraId()
        {
            return (CAMERA_ID) HMDCameraId;
        }

        /// <summary>
        /// Start camera preView
        /// </summary>
        public void StartCameraPreView()
        {
            StartCameraPreView(false);
        }

        /// <summary>
        /// Start camera preView
        /// </summary>
        /// <param name="triggerFocus">trigger focus</param>
        public void StartCameraPreView(bool triggerFocus)
        {
            OpenCamera();
            AndroidJavaObject surfaceTextureObject = cameraPreviewHelper.GetSurfaceTexture();
            cameraDeviceObject.Call<bool>("startPreviewWithBestSize", surfaceTextureObject);
            if (triggerFocus)
            {
                DoCameraAutoFocus();
            }

            isCameraPreviewing = true;
        }

        /// <summary>
        /// Stop camera preView
        /// </summary>
        public void StopCamereaPreView()
        {
            if (nibiruCameraServiceObject != null)
            {
                isCameraPreviewing = false;
                nibiruCameraServiceObject.Call("stopPreview");
                cameraDeviceObject = null;
            }
            else
            {
                Debug.LogError("StopCamereaPreView failed, because nibiruCameraServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Determine whether the camera is in preview. 
        /// </summary>
        /// <returns></returns>
        public bool IsCameraPreviewing()
        {
            return isCameraPreviewing;
        }

        public void SetCameraPreviewing(bool enabled)
        {
            isCameraPreviewing = true;
        }

        public void DoCameraAutoFocus()
        {
            if (cameraDeviceObject != null)
            {
                cameraDeviceObject.Call("doAutoFocus");
            }
            else
            {
                Debug.LogError("DoCameraAutoFocus failed, because cameraDeviceObject is null !!!");
            }
        }

        public void EnableVoiceService(bool enabled)
        {
            if (nibiruVoiceServiceObject != null)
            {
                nibiruVoiceServiceObject.Call("setEnableVoice", enabled);
            }
            else
            {
                Debug.LogError("EnableVoiceService failed, because nibiruVoiceServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Start voice recording
        /// </summary>
        public void StartVoiceRecording()
        {
            if (nibiruVoiceServiceObject != null)
            {
                nibiruVoiceServiceObject.Call("startRecording");
            }
            else
            {
                Debug.LogError("StartVoiceRecording failed, because nibiruVoiceServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Stop voice recording
        /// </summary>
        public void StopVoiceRecording()
        {
            if (nibiruVoiceServiceObject != null)
            {
                nibiruVoiceServiceObject.Call("stopRecording");
            }
            else
            {
                Debug.LogError("StopVoiceRecording failed, because nibiruVoiceServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Cancel voice recognizer
        /// </summary>
        public void CancelVoiceRecognizer()
        {
            if (nibiruVoiceServiceObject != null)
            {
                nibiruVoiceServiceObject.Call("cancelRecognizer");
            }
            else
            {
                Debug.LogError("CancelVoiceRecognizer failed, because nibiruVoiceServiceObject is null !!!");
            }
        }

        public bool IsSupportVoice()
        {
            if (nibiruVoiceServiceObject != null)
            {
                return nibiruVoiceServiceObject.Call<bool>("isMicrophoneVoice");
            }
            else
            {
                Debug.LogError("IsSupportVoice failed, because nibiruVoiceServiceObject is null !!!");
            }

            return false;
        }

        public bool IsSupport6DOF()
        {
            if (nibiruVRServiceObject != null)
            {
                return nibiruVRServiceObject.Call<bool>("isSupport6Dof");
            }
            else
            {
                Debug.LogError("IsSupport6DOF failed, because nibiruVRServiceObject is null !!!");
            }

            return false;
        }

        public bool IsSupportGesture()
        {
            if (nibiruGestureServiceObject != null)
            {
                return nibiruGestureServiceObject.Call<bool>("isCameraGesture");
            }
            else
            {
                Debug.LogError("isSupportGesture failed, because nibiruGestureServiceObject is null !!!");
            }

            return false;
        }

        public void UpdateVoiceLanguage()
        {
            if (nibiruVoiceServiceObject != null)
            {
                nibiruVoiceServiceObject.Call("setVoicePID", (int) NxrGlobal.voiceLanguage);
            }
            else
            {
                Debug.LogError("UpdateVoiceLanguage failed, because nibiruVoiceServiceObject is null !!!");
            }
        }

        /// <summary>
        /// Control camera status of gesture service：false-Turn off camera occupation，true-Turn on camera occupation
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableGestureService(bool enabled)
        {
            if (nibiruGestureServiceObject != null)
            {
                nibiruGestureServiceObject.Call("setEnableGesture", enabled);
            }
            else
            {
                Debug.LogError("EnableGestureService failed, because nibiruGestureServiceObject is null !!!");
            }
        }

        public bool IsCameraGesture()
        {
            if (nibiruGestureServiceObject != null)
            {
                return nibiruGestureServiceObject.Call<bool>("isCameraGesture");
            }

            return false;
        }

        public delegate void OnSensorDataChanged(NibiruSensorEvent sensorEvent);

        /// <summary>
        /// The callback when sensor data changes. 
        /// </summary>
        public static OnSensorDataChanged OnSensorDataChangedHandler;

        class NibiruSensorDataListenerCallback : AndroidJavaProxy
        {
            public NibiruSensorDataListenerCallback() : base(
                "com.nibiru.service.NibiruSensorService$INibiruSensorDataListener")
            {
            }

            public void onSensorDataChanged(AndroidJavaObject sensorEventObject)
            {
                float x = sensorEventObject.Get<float>("x");
                float y = sensorEventObject.Get<float>("y");
                float z = sensorEventObject.Get<float>("z");
                long timestamp = sensorEventObject.Get<long>("timestamp");
                AndroidJavaObject locationObject = sensorEventObject.Get<AndroidJavaObject>("sensorLocation");
                AndroidJavaObject typeObject = sensorEventObject.Get<AndroidJavaObject>("sensorType");
                SENSOR_LOCATION sensorLocation = (SENSOR_LOCATION) locationObject.Call<int>("ordinal");
                SENSOR_TYPE sensorType = (SENSOR_TYPE) typeObject.Call<int>("ordinal");

                NibiruSensorEvent sensorEvent = new NibiruSensorEvent(x, y, z, timestamp, sensorType, sensorLocation);
                // sensorEvent.printLog();

                // 用Loom的方法在Unity主线程中调用Text组件
                Loom.QueueOnMainThread((param) =>
                {
                    if (OnSensorDataChangedHandler != null)
                    {
                        OnSensorDataChangedHandler((NibiruSensorEvent) param);
                    }
                }, sensorEvent);
            }
        }


        private NibiruSensorDataListenerCallback nibiruSensorDataListenerCallback;

        public void RegisterSensorListener(SENSOR_TYPE type, SENSOR_LOCATION location)
        {
            if (nibiruSensorServiceObject != null)
            {
                if (nibiruSensorDataListenerCallback == null)
                {
                    nibiruSensorDataListenerCallback = new NibiruSensorDataListenerCallback();
                }

                // UI线程执行
                RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
                    {
                        AndroidJavaClass locationClass =
                            BaseAndroidDevice.GetClass("com.nibiru.service.NibiruSensorService$SENSOR_LOCATION");
                        AndroidJavaObject locationObj =
                            locationClass.CallStatic<AndroidJavaObject>("valueOf", location.ToString());

                        AndroidJavaClass typeClass =
                            BaseAndroidDevice.GetClass("com.nibiru.service.NibiruSensorService$SENSOR_TYPE");
                        AndroidJavaObject typeObj = typeClass.CallStatic<AndroidJavaObject>("valueOf", type.ToString());

                        nibiruSensorServiceObject.Call<bool>("registerSensorListener", typeObj, locationObj,
                            nibiruSensorDataListenerCallback);
                        Debug.Log("registerSensorListener=" + type.ToString() + "," + location.ToString());
                    }
                ));
            }
            else
            {
                Debug.LogError("RegisterControllerSensor failed, nibiruSensorServiceObject is null !");
            }
        }

        public void UnRegisterSensorListener()
        {
            if (nibiruSensorServiceObject != null)
            {
                // UI线程执行
                RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
                    {
                        nibiruSensorServiceObject.Call("unregisterSensorListenerAll");
                    }
                ));
            }
            else
            {
                Debug.LogError("UnRegisterSensorListener failed, nibiruSensorServiceObject is null !");
            }
        }

        //4.1 获取屏幕亮度值：
        /// <summary>
        /// Get system's brightness value
        /// </summary>
        /// <returns></returns>
        public int GetBrightnessValue()
        {
            int BrightnessValue = 0;
#if UNITY_ANDROID
            BaseAndroidDevice.CallObjectMethod<int>(ref BrightnessValue, nibiruOsServiceObject, "getBrightnessValue");
#endif
            return BrightnessValue;
        }

        //4.2 调节屏幕亮度：
        /// <summary>
        /// Set system's brightness value
        /// </summary>
        /// <returns></returns>
        public void SetBrightnessValue(int value)
        {
            if (nibiruOsServiceObject == null) return;
#if UNITY_ANDROID
            RunOnUIThread(androidActivity,
                new AndroidJavaRunnable(() =>
                {
                    BaseAndroidDevice.CallObjectMethod(nibiruOsServiceObject, "setBrightnessValue", value, 200.01f);
                }));
#endif
        }

        //4.3 获取当前2D/3D显示模式：
        /// <summary>
        /// Get display mode 2d/3d
        /// </summary>
        /// <returns></returns>
        public DISPLAY_MODE GetDisplayMode()
        {
            if (nibiruOsServiceObject == null) return DISPLAY_MODE.MODE_2D;
            AndroidJavaObject androidObject = nibiruOsServiceObject.Call<AndroidJavaObject>("getDisplayMode");
            int mode = androidObject.Call<int>("ordinal");
            return (DISPLAY_MODE) mode;
        }

        //4.4 切换2D/3D显示模式:
        /// <summary>
        /// Set display mode 2d/3d
        /// </summary>
        /// <param name="displayMode"></param>
        public void SetDisplayMode(DISPLAY_MODE displayMode)
        {
            if (nibiruOsServiceObject != null)
            {
                RunOnUIThread(androidActivity,
                    new AndroidJavaRunnable(() =>
                    {
                        nibiruOsServiceObject.Call("setDisplayMode", (int) displayMode);
                    }));
            }
        }

        // 渠道ID
        /// <summary>
        /// Get system's channel code
        /// </summary>
        /// <returns></returns>
        public string GetChannelCode()
        {
            if (nibiruOsServiceObject == null) return "NULL";
            return nibiruOsServiceObject.Call<string>("getChannelCode");
        }

        // 型号
        /// <summary>
        /// Get device's model
        /// </summary>
        /// <returns></returns>
        public string GetModel()
        {
            if (nibiruOsServiceObject == null) return "NULL";
            return nibiruOsServiceObject.Call<string>("getModel");
        }

        // 系统OS版本
        /// <summary>
        /// Get system's os version
        /// </summary>
        /// <returns></returns>
        public string GetOSVersion()
        {
            if (nibiruOsServiceObject == null) return "NULL";
            return nibiruOsServiceObject.Call<string>("getOSVersion");
        }

        // 系统OS版本号
        /// <summary>
        /// Get system's service version
        /// </summary>
        /// <returns></returns>
        public int GetOSVersionCode()
        {
            if (nibiruOsServiceObject == null) return -1;
            return nibiruOsServiceObject.Call<int>("getOSVersionCode");
        }

        // 系统服务版本
        /// <summary>
        /// Get system's service version code
        /// </summary>
        /// <returns></returns>
        public string GetServiceVersionCode()
        {
            if (nibiruOsServiceObject == null) return "NULL";
            return nibiruOsServiceObject.Call<string>("getServiceVersionCode");
        }

        // 获取厂家软件版本：（对应驱动板软件版本号）
        /// <summary>
        /// Get system's vendor SW version
        /// </summary>
        /// <returns></returns>
        public string GetVendorSWVersion()
        {
            if (nibiruOsServiceObject == null) return "NULL";
            return nibiruOsServiceObject.Call<string>("getVendorSWVersion");
        }

        // 控制touchpad是否显示 value为true表示显示，false表示不显示 
        /// <summary>
        /// Control whether touchpad is displayed. true-display false-not display
        /// </summary>
        /// <param name="isEnable"></param>
        public void SetEnableTouchCursor(bool isEnable)
        {
            RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
            {
                if (nibiruOsServiceObject != null)
                {
                    nibiruOsServiceObject.Call("setEnableTouchCursor", isEnable);
                }
            }));
        }

        /// <summary>
        /// Get the value of the distance sensor.
        /// </summary>
        /// <returns></returns>
        public int GetProximityValue()
        {
            if (nibiruSensorServiceObject == null) return -1;
            return nibiruSensorServiceObject.Call<int>("getProximityValue");
        }

        /// <summary>
        /// Get the value of light perception.
        /// </summary>
        /// <returns></returns>
        public int GetLightValue()
        {
            if (nibiruSensorServiceObject == null) return -1;
            return nibiruSensorServiceObject.Call<int>("getLightValue");
        }

        // UI线程中运行
        public void RunOnUIThread(AndroidJavaObject activityObj, AndroidJavaRunnable r)
        {
            activityObj.Call("runOnUiThread", r);
        }

        public delegate void CameraIdle();

        public delegate void CameraBusy();

        /// <summary>
        /// The callback when Camera is idle.
        /// </summary>
        public static CameraIdle OnCameraIdle;

        /// <summary>
        /// The callback when Camera is busy.
        /// </summary>
        public static CameraBusy OnCameraBusy;

        public delegate void OnRecorderSuccess();

        public delegate void OnRecorderFailed();

        public static OnRecorderSuccess OnRecorderSuccessHandler;
        public static OnRecorderFailed OnRecorderFailedHandler;

        class CameraStatusCallback : AndroidJavaProxy
        {
            public CameraStatusCallback() : base("com.nibiru.lib.vr.listener.NVRCameraStatusListener")
            {
            }

            public void cameraBusy()
            {
                // 从Android UI线程回调过来的，加入到Unity主线程处理
                // NxrViewer.Instance.TriggerCameraStatus(1);
                Loom.QueueOnMainThread((param) =>
                {
                    if (OnCameraBusy != null)
                    {
                        OnCameraBusy();
                    }
                }, 1);
                Debug.Log("cameraBusy");
            }

            public void cameraIdle()
            {
                // 从Android UI线程回调过来的，加入到Unity主线程处理
                // NxrViewer.Instance.TriggerCameraStatus(0);
                Loom.QueueOnMainThread((param) =>
                {
                    if (OnCameraIdle != null)
                    {
                        OnCameraIdle();
                    }
                }, 0);
                Debug.Log("cameraIdle");
            }
        }

        class CaptureCallback : AndroidJavaProxy
        {
            public CaptureCallback() : base("com.nibiru.lib.vr.listener.NVRVideoCaptureListener")
            {
            }

            public void onSuccess()
            {
                // 从Android UI线程回调过来的，加入到Unity主线程处理
                // NxrViewer.Instance.TriggerCaptureStatus(1);
                Loom.QueueOnMainThread((param) =>
                {
                    if (OnRecorderSuccessHandler != null)
                    {
                        OnRecorderSuccessHandler();
                    }
                }, 1);
            }

            public void onFailed()
            {
                // 从Android UI线程回调过来的，加入到Unity主线程处理
                // NxrViewer.Instance.TriggerCaptureStatus(0);
                Loom.QueueOnMainThread((param) =>
                {
                    if (OnRecorderFailedHandler != null)
                    {
                        OnRecorderFailedHandler();
                    }
                }, 0);
            }
        }

        public int IsCaptureEnabled { set; get; }

        public static int BIT_RATE = 4000000;

        /// <summary>
        /// Start capture
        /// </summary>
        /// <param name="path"></param>
        public void StartCapture(string path)
        {
            StartCapture(path, -1);
        }

        /// <summary>
        ///  Start capture
        /// </summary>
        /// <param name="path"></param>
        /// <param name="seconds"></param>
        public void StartCapture(string path, int seconds)
        {
            StartCapture(path, BIT_RATE, seconds);
        }

        private static int videoSize = (int) VIDEO_SIZE.V720P;

        // private static int captureCameraId = (int)CAMERA_ID.FRONT;
        /// <summary>
        ///  Start capture
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bitRate"></param>
        /// <param name="seconds"></param>
        public void StartCapture(string path, int bitRate, int seconds)
        {
            IsCaptureEnabled = 1;
            nibiruSDKClass.CallStatic("startCaptureForUnity", new CaptureCallback(), path, bitRate, seconds, videoSize,
                HMDCameraId);
        }

        public static void SetCaptureVideoSize(VIDEO_SIZE video_Size)
        {
            videoSize = (int) video_Size;
        }

        /// <summary>
        /// Stop capture
        /// </summary>
        public void StopCapture()
        {
            nibiruSDKClass.CallStatic("stopCaptureForUnity");
            IsCaptureEnabled = 0;
        }

        public bool CaptureDrawFrame(int textureId, int frameId)
        {
            if (IsCaptureEnabled <= -3)
            {
                return false;
            }
            else if (IsCaptureEnabled <= 0 && IsCaptureEnabled >= -2)
            {
                // 在stop后，多执行3次，用于内部处理stop的逻辑。
                IsCaptureEnabled--;
            }

            return nibiruSDKClass.CallStatic<bool>("onDrawFrameForUnity", textureId, frameId);
        }

        private const int STREAM_VOICE_CALL = 0;
        private const int STREAM_SYSTEM = 1;
        private const int STREAM_RING = 2;
        private const int STREAM_MUSIC = 3;
        private const int STREAM_ALARM = 4;
        private const int STREAM_NOTIFICATION = 5;
        private const string currentVolume = "getStreamVolume"; //当前音量
        private const string maxVolume = "getStreamMaxVolume"; //最大音量

        public int GetVolumeValue()
        {
            if (audioManager == null) return 0;
            return audioManager.Call<int>(currentVolume, STREAM_MUSIC);
        }

        public int GetMaxVolume()
        {
            if (audioManager == null) return 1;
            return audioManager.Call<int>(maxVolume, STREAM_MUSIC);
        }

        public void EnabledMarkerAutoFocus(bool enabled)
        {
            if (nibiruMarkerServiceObject == null)
            {
                Debug.LogError("nibiruMarkerServiceObject is null");
            }
            else if (isMarkerRecognizeRunning)
            {
                nibiruMarkerServiceObject.Call(enabled ? "doAutoFocus" : "stopAutoFocus");
            }
        }

        /// <summary>
        /// Set marker recognize cameraId
        /// </summary>
        /// <param name="cameraID"></param>
        private void SetMarkerRecognizeCameraId(int cameraID)
        {
            if (nibiruMarkerServiceObject == null)
            {
                Debug.LogError("nibiruMarkerServiceObject is null");
            }
            else
            {
                nibiruMarkerServiceObject.Call("setCameraId", cameraID);
            }
        }

        private bool isMarkerRecognizeRunning;

        public bool IsMarkerRecognizeRunning
        {
            get { return isMarkerRecognizeRunning; }
            set { isMarkerRecognizeRunning = value; }
        }

        /// <summary>
        /// Start marker recognize
        /// </summary>
        public void StartMarkerRecognize()
        {
            if (nibiruMarkerServiceObject == null)
            {
                Debug.LogError("nibiruMarkerServiceObject is null");
            }
            else if (!isMarkerRecognizeRunning)
            {
                // 默认使用前置相机
                SetMarkerRecognizeCameraId(HMDCameraId);
                // 焦距，具体不同机器可以需要微调 16 , 640 * 480
                nibiruMarkerServiceObject.Call("setCameraZoom", NxrGlobal.GetMarkerCameraZoom());
                nibiruMarkerServiceObject.Call("setPreviewSize", 640, 480);
                nibiruMarkerServiceObject.Call("startMarkerRecognize");
                isMarkerRecognizeRunning = true;
            }
        }

        /// <summary>
        /// Stop marker recognize
        /// </summary>
        public void StopMarkerRecognize()
        {
            if (nibiruMarkerServiceObject == null)
            {
                Debug.LogError("nibiruMarkerServiceObject is null");
            }
            else if (isMarkerRecognizeRunning)
            {
                nibiruMarkerServiceObject.Call("stopMarkerRecognize");
                isMarkerRecognizeRunning = false;
            }
        }

        /// <summary>
        /// Get the ViewMatrix of Marker.
        /// </summary>
        /// <returns></returns>
        public float[] GetMarkerViewMatrix()
        {
            if (nibiruMarkerServiceObject == null)
            {
                Debug.LogError("nibiruMarkerServiceObject is null");
                return null;
            }
            else
            {
                float[] result = nibiruMarkerServiceObject.Call<float[]>("getMarkerViewMatrix");
                if (result == null || result.Length == 0) return null;
                // 全是0
                if (IsAllZero(result)) return null;
                return result;
            }
        }

        public static bool IsAllZero(float[] array)
        {
            for (int i = 0, l = array.Length; i < l; i++)
            {
                if (array[i] != 0) return false;
            }

            return true;
        }

        public float[] GetMarkerViewMatrix(int eyeType)
        {
            if (nibiruMarkerServiceObject == null)
            {
                Debug.LogError("nibiruMarkerServiceObject is null");
                return null;
            }
            else
            {
                float[] result = nibiruMarkerServiceObject.Call<float[]>("getMarkerViewMatrix", eyeType);
                if (result == null || result.Length == 0) return null;
                // 全是0
                if (IsAllZero(result)) return null;
                return result;
            }
        }

        public float[] GetMarkerProjectionMatrix()
        {
            if (nibiruMarkerServiceObject == null)
            {
                Debug.LogError("nibiruMarkerServiceObject is null");
                return null;
            }
            else
            {
                float[] projArr = nibiruMarkerServiceObject.Call<float[]>("getProjection");
                if (projArr == null || projArr.Length == 0)
                    return null;
                return projArr;
            }
        }

        public string GetMarkerDetectStatus()
        {
            if (nibiruMarkerServiceObject == null)
            {
                Debug.LogError("GetMarkerDetectStatus failed, nibiruMarkerServiceObject is null");
                return "-1";
            }

            string res = nibiruMarkerServiceObject.Call<string>("getParameters", "p_detect_status");
            return res == null ? "-1" : res;
        }

        public delegate void OnVirtualMouseServiceStatus(bool succ);

        public class NibiruVirtualMouseServiceListener : AndroidJavaProxy
        {
            OnVirtualMouseServiceStatus _OnVirtualMouseServiceStatus;

            public NibiruVirtualMouseServiceListener(OnVirtualMouseServiceStatus onVirtualMouseServiceStatus) : base(
                "com.nibiru.service.NibiruVirtualMouseManager$VirtualMouseServiceListener")
            {
                _OnVirtualMouseServiceStatus = onVirtualMouseServiceStatus;
            }

            public void onServiceRegisterResult(bool succ)
            {
                if (_OnVirtualMouseServiceStatus != null)
                {
                    _OnVirtualMouseServiceStatus(succ);
                }
            }
        }

        public void PauseGestureService()
        {
            if (nibiruGestureServiceObject != null)
            {
                nibiruGestureServiceObject.Call("onPause");
            }
            else
            {
                Debug.LogError("onPause failed, because nibiruGestureServiceObject is null !!!");
            }
        }

        public void ResumeGestureService()
        {
            if (nibiruGestureServiceObject != null)
            {
                nibiruGestureServiceObject.Call("onResume");
            }
            else
            {
                Debug.LogError("onResume failed, because nibiruGestureServiceObject is null !!!");
            }
        }

        private AndroidJavaObject javaArrayFromCS(string[] values)
        {
            AndroidJavaClass arrayClass = new AndroidJavaClass("java.lang.reflect.Array");
            AndroidJavaObject arrayObject = arrayClass.CallStatic<AndroidJavaObject>("newInstance",
                new AndroidJavaClass("java.lang.String"), values.Count());
            for (int i = 0; i < values.Count(); ++i)
            {
                arrayClass.CallStatic("set", arrayObject, i, new AndroidJavaObject("java.lang.String", values[i]));
            }

            return arrayObject;
        }

        /// <summary>
        /// Request permission
        /// </summary>
        /// <param name="names">NxrGlobal.Permission</param>
        public void RequsetPermission(string[] names)
        {
            if (nibiruOsServiceObject != null)
            {
                nibiruOsServiceObject.Call("requestPermission", javaArrayFromCS(names));
            }
        }

        /// <summary>
        /// Get QCOM of product device.
        /// </summary>
        /// <returns></returns>
        public QCOMProductDevice GetQCOMProductDevice()
        {
            if ("msm8996".Equals(systemDevice))
            {
                return QCOMProductDevice.QCOM_820;
            }
            else if ("msm8998".Equals(systemDevice))
            {
                return QCOMProductDevice.QCOM_835;
            }
            else if ("sdm710".Equals(systemDevice))
            {
                return QCOMProductDevice.QCOM_XR1;
            }
            else if ("sdm845".Equals(systemDevice))
            {
                return QCOMProductDevice.QCOM_845;
            }

            return QCOMProductDevice.QCOM_UNKNOW;
        }
    }
}