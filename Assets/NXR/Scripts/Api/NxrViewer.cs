//  Copyright 2019 Nibiru. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using NibiruTask;
using NibiruAxis;
using UnityEngine.SceneManagement;

namespace Nxr.Internal
{
    /// <summary>
    /// 
    /// </summary>
    [AddComponentMenu("NXR/NxrViewer")]
    public class NxrViewer : MonoBehaviour
    {
        // base 2.1.0.xxxx release  
        public const string NXR_SDK_VERSION = "2.1.1.0_20201124";

        // 退出Kill Process
        public const bool IsAndroidKillProcess = true;

        // dtr or not 
        public static bool USE_DTR = true;

        private static int _texture_count = 6;

        // 绘制前事件标识
        public static int kPreRenderEvent = 1;

        // 头部角度限制范围
        private float[] headEulerAnglesRange = null;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("OnSceneLoaded->" + scene.name + " , Triggered=" + Triggered);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// The singleton instance of the NvrViewer class.
        public static NxrViewer Instance
        {
            get
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                USE_DTR = false;
                if (instance == null && !Application.isPlaying)
                {
                    Debug.Log("Create NxrViewer Instance !");
                    instance = FindObjectOfType<NxrViewer>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No NxrViewer instance found.  Ensure one exists in the scene, or call "
                                   + "NxrViewer.Create() at startup to generate one.\n"
                                   + "If one does exist but hasn't called Awake() yet, "
                                   + "then this error is due to order-of-initialization.\n"
                                   + "In that case, consider moving "
                                   + "your first reference to NxrViewer.Instance to a later point in time.\n"
                                   + "If exiting the scene, this indicates that the NxrViewer object has already "
                                   + "been destroyed.");
                }

                return instance;
            }
        }

        private static NxrViewer instance = null;
        public NxrEye[] eyes = new NxrEye[2];

        private byte[] winTypeName = new byte[]
            {110, 120, 114, 46, 78, 118, 114, 87, 105, 110, 66, 97, 115, 101}; //N/v/r/W/i/n/B/a/s/e

        /// Generate a NxrViewer instance. Takes no action if one already exists.
        public static void Create()
        {
            if (instance == null && FindObjectOfType<NxrViewer>() == null)
            {
                Debug.Log("Creating NxrViewerMain object");
                var go = new GameObject("NxrViewerMain", typeof(NxrViewer));
                go.transform.localPosition = Vector3.zero;
                // sdk will be set by Awake().
            }
        }

        /// The StereoController instance attached to the main camera, or null if there is none.
        /// @note Cached for performance.
        public NxrStereoController Controller
        {
            get
            {
                if (currentController == null)
                {
                    currentController = FindObjectOfType<NxrStereoController>();
                }

                return currentController;
            }
        }

        private NxrStereoController currentController;

        [SerializeField] public HMD_TYPE HmdType = HMD_TYPE.NONE;

        /// Whether to draw directly to the output window (_true_), or to an offscreen buffer
        /// first and then blit (_false_). If you wish to use Deferred Rendering or any
        /// Image Effects in stereo, turn this option off.  A common symptom that indicates
        /// you should do so is when one of the eyes is spread across the entire screen.
        [SerializeField] private bool openEffectRender = false;

        /// <summary>
        ///  false-Processing after closing，true-Processing after opening
        /// </summary>
        public bool OpenEffectRender
        {
            get { return openEffectRender; }
            set
            {
                if (value != openEffectRender)
                {
                    openEffectRender = value;
                }
            }
        }

        /// Determine whether the scene renders in stereo or mono.
        /// _True_ means to render in stereo, and _false_ means to render in mono.
        public bool SplitScreenModeEnabled
        {
            get { return splitScreenModeEnabled; }
            set
            {
                if (value != splitScreenModeEnabled && device != null)
                {
                    device.SetSplitScreenModeEnabled(value);
                }

                splitScreenModeEnabled = value;
            }
        }

        [SerializeField] private bool splitScreenModeEnabled = true;


        /// <summary>
        /// Get/Set Head Control 
        /// </summary>
        public HeadControl HeadControl
        {
            get { return headControlEnabled; }
            set
            {
                headControlEnabled = value;
                UpdateHeadControl();
            }
        }

        [SerializeField] private HeadControl headControlEnabled = HeadControl.GazeApplication;


        public float Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        [SerializeField] private float duration = 2;

        NxrReticle mNxrReticle;

        public NxrReticle GetNxrReticle()
        {
            InitNxrReticleScript();
            return mNxrReticle;
        }

        public void DismissReticle()
        {
            GetNxrReticle().Dismiss();
        }

        public void ShowReticle()
        {
            GetNxrReticle().Show();
        }

        private void InitNxrReticleScript()
        {
            if (mNxrReticle == null)
            {
                GameObject nxrReticleObject = GameObject.Find("NxrReticle");
                if (nxrReticleObject != null)
                {
                    mNxrReticle = nxrReticleObject.GetComponent<NxrReticle>();
                    if (mNxrReticle == null)
                    {
                        Debug.LogError("Not Find NxrReticle.cs From GameObject NxrReticle !!!");
                    }
                }
                else
                {
                    Debug.LogError("Not Find NxrReticle GameObject !!!");
                }
            }
        }

        /// <summary>
        /// Show head control
        /// </summary>
        public void ShowHeadControl()
        {
            InitNxrReticleScript();
            if (mNxrReticle != null)
            {
                mNxrReticle.HeadShow();
                Debug.Log("ShowHeadControl");
            }
        }

        /// <summary>
        /// Hide head control
        /// </summary>
        public void HideHeadControl()
        {
            InitNxrReticleScript();
            if (mNxrReticle != null)
            {
                mNxrReticle.HeadDismiss();
                Debug.Log("HideHeadControl");
            }
        }

        // App自行处理Trigger按键，SDK不处理。如果SDK处理默认当做确认键使用。
        /// <summary>
        ///  Is app handle trigger event
        /// </summary>
        public bool IsAppHandleTriggerEvent
        {
            get { return appHandleTriggerEvent; }
            set { appHandleTriggerEvent = value; }
        }

        [SerializeField] private bool appHandleTriggerEvent = false;

        public bool TrackerPosition
        {
            get { return trackerPosition; }
            set { trackerPosition = value; }
        }

        /// <summary>
        /// Whether to use third-party displacement data.
        /// </summary>
        public bool UseThirdPartyPosition
        {
            get { return useThirdPartyPosition; }
            set { useThirdPartyPosition = value; }
        }

        [SerializeField] private bool useThirdPartyPosition = false;

#if UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
        [SerializeField]
        private FrameRate targetFrameRate = FrameRate.FPS_60;
        public FrameRate TargetFrameRate
        {
            get
            {
                return targetFrameRate;
            }
            set
            {
                if (value != targetFrameRate)
                {
                    targetFrameRate = value;
                }
            }
        }

#endif

        // 纹理抗锯齿
        [SerializeField] public TextureMSAA textureMsaa = TextureMSAA.MSAA_2X;

        public TextureMSAA TextureMSAA
        {
            get { return textureMsaa; }
            set
            {
                if (value != textureMsaa)
                {
                    textureMsaa = value;
                }
            }
        }

        [SerializeField] private bool trackerPosition = true;

        [Serializable]
        public class NxrSettings
        {
            [Tooltip("Change Timewarp Status")] public int timewarpEnabled = -1; //-1=not set,0=close,1=open
            [Tooltip("Change Sync Frame Status")] public bool syncFrameEnabled = false;
        }

        [SerializeField] public NxrSettings settings = new NxrViewer.NxrSettings();

        // Remote Debug
        [SerializeField] private bool remoteDebug = false;

        public bool RemoteDebug
        {
            get { return remoteDebug; }
            set
            {
                if (value != remoteDebug)
                {
                    remoteDebug = value;
                }
            }
        }

        [SerializeField] private bool remoteController = false;

        public bool RemoteController
        {
            get { return remoteController; }
            set
            {
                if (value != remoteController)
                {
                    remoteController = value;
                }
            }
        }

        // FPS
        [SerializeField] private bool showFPS = false;

        public bool ShowFPS
        {
            get { return showFPS; }
            set
            {
                if (value != showFPS)
                {
                    showFPS = value;
                }
            }
        }

        // 纹理质量
        [SerializeField] public TextureQuality textureQuality = TextureQuality.Better;

        public TextureQuality TextureQuality
        {
            get { return textureQuality; }
            set
            {
                if (value != textureQuality)
                {
                    textureQuality = value;
                }
            }
        }

        [SerializeField] private bool requestLock = false;

        /// <summary>
        ///  Lock head tracker.
        /// </summary>
        public bool LockHeadTracker
        {
            get { return requestLock; }
            set
            {
                if (value != requestLock)
                {
                    requestLock = value;
                }
            }
        }

        public bool InitialRecenter { get; set; }

        /// <summary>
        /// Lock head posture.
        /// </summary>
        public void RequestLock()
        {
            if (device != null)
            {
                device.NLockTracker();
            }
        }

        /// <summary>
        /// Unlock head posture.
        /// </summary>
        public void RequestUnLock()
        {
            if (device != null)
            {
                device.NUnLockTracker();
            }
        }

        private bool IsNativeGazeShow = false;

        /// <summary>
        ///  Change Gaze's size/color/status
        /// </summary>
        /// <param name="tag"></param>
        public void GazeApi(GazeTag tag)
        {
            GazeApi(tag, "");
        }


        public void TurnOff()
        {
            device.TurnOff();
        }

        public void Reboot()
        {
            device.Reboot();
        }


        /// <summary>
        ///  The parameter of Param behind GazeTag.Show and GazeTag.Hide is passed "".
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="param"></param>
        public void GazeApi(GazeTag tag, string param)
        {
            if (device != null)
            {
                bool rslt = device.GazeApi(tag, param);
                if (tag == GazeTag.Show)
                {
                    bool useDFT = USE_DTR && !NxrGlobal.supportDtr;
                    IsNativeGazeShow = useDFT ? true : rslt;
                }
                else if (tag == GazeTag.Hide)
                {
                    IsNativeGazeShow = false;
                }
            }
        }

        /// <summary>
        /// Switch to controller mode
        /// </summary>
        /// <param name="enabled"></param>
        public void SwitchControllerMode(bool enabled)
        {
            // Debug.LogError("SwitchControllerMode:" + enabled);
            if (enabled)
            {
                HeadControl = HeadControl.Controller;
            }
            else
            {
                // 
                HeadControl = HeadControl.GazeApplication;
            }
        }

        /// <summary>
        /// Switch application reticle mode
        /// true-force using reticle
        /// false-use system reticle
        /// </summary>
        /// <param name="enabled"></param>
        private void SwitchApplicationReticle(bool enabled)
        {
            InitNxrReticleScript();

            bool IsControllerMode = HeadControl == HeadControl.Controller;

            if (enabled)
            {
                if (mNxrReticle != null) mNxrReticle.Show();
                GazeInputModule.gazePointer = mNxrReticle;
            }
            else if (!enabled && (!NxrGlobal.isVR9Platform || IsControllerMode))
            {
                if (mNxrReticle != null)
                {
                    mNxrReticle.Dismiss();
                }

                GazeInputModule.gazePointer = null;
            }

            if (enabled)
            {
                GazeApi(GazeTag.Hide);
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN

        /// The screen size to emulate when testing in the Unity Editor.
        public NxrProfile.ScreenSizes ScreenSize
        {
            get { return screenSize; }
            set
            {
                if (value != screenSize)
                {
                    screenSize = value;
                    if (device != null)
                    {
                        device.UpdateScreenData();
                    }
                }
            }
        }

        [SerializeField] private NxrProfile.ScreenSizes screenSize = NxrProfile.ScreenSizes.Nexus5;

        /// The viewer type to emulate when testing in the Unity Editor.
        public NxrProfile.ViewerTypes ViewerType
        {
            get { return viewerType; }
            set
            {
                if (value != viewerType)
                {
                    viewerType = value;
                    if (device != null)
                    {
                        device.UpdateScreenData();
                    }
                }
            }
        }

        [SerializeField] private NxrProfile.ViewerTypes viewerType = NxrProfile.ViewerTypes.CardboardMay2015;
#endif

        // The AR device that will be providing input data.
        private static BaseARDevice device;

        public RenderTexture GetStereoScreen(int eye)
        {
            // Don't need it except for distortion correction.
            if (!splitScreenModeEnabled || NxrGlobal.isVR9Platform)
            {
                return null;
            }

            if (eyeStereoScreens[0] == null)
            {
                // 初始化6个纹理
                InitEyeStereoScreens();
            }

            if (Application.isEditor || (NxrViewer.USE_DTR && !NxrGlobal.supportDtr))
            {
                // DFT or Editor
                return eyeStereoScreens[0];
            }

            // 获取对应索引的纹理
            return eyeStereoScreens[eye + _current_texture_index];
        }

        // 初始创建6个纹理，左右各3个 【左右左右左右】
        public RenderTexture[] eyeStereoScreens = new RenderTexture[_texture_count];

        private void InitEyeStereoScreens()
        {
            InitEyeStereoScreens(-1, -1);
        }

        // 初始化
        private void InitEyeStereoScreens(int width, int height)
        {
            RealeaseEyeStereoScreens();

#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_WIN
            bool useDFT = NxrViewer.USE_DTR && !NxrGlobal.supportDtr;
            if (!USE_DTR || useDFT || IsWinPlatform)
            {
                // 编辑器模式 or 不支持DTR的DFT模式 只生成1个纹理
                RenderTexture rendetTexture = device.CreateStereoScreen(width, height);
                if (!rendetTexture.IsCreated())
                {
                    rendetTexture.Create();
                }

                int tid = (int) rendetTexture.GetNativeTexturePtr();
                for (int i = 0; i < _texture_count; i++)
                {
                    eyeStereoScreens[i] = rendetTexture;
                    _texture_ids[i] = tid;
                }
            }
            else
            {
                for (int i = 0; i < _texture_count; i++)
                {
                    eyeStereoScreens[i] = device.CreateStereoScreen(width, height);
                    eyeStereoScreens[i].Create();
                    _texture_ids[i] = (int) eyeStereoScreens[i].GetNativeTexturePtr();
                }
            }
#endif
        }

        // 释放所有纹理
        private void RealeaseEyeStereoScreens()
        {
            for (int i = 0; i < _texture_count; i++)
            {
                if (eyeStereoScreens[i] != null)
                {
                    eyeStereoScreens[i].Release();
                    eyeStereoScreens[i] = null;
                    _texture_ids[i] = 0;
                }
            }

            Resources.UnloadUnusedAssets();
            Debug.Log("RealeaseEyeStereoScreens");
        }

        /// Describes the current device, including phone screen.
        public NxrProfile Profile
        {
            get { return device.Profile; }
        }

        /// Distinguish the stereo eyes.
        public enum Eye
        {
            Left,

            /// The left eye
            Right,

            /// The right eye
            Center /// The "center" eye (unused)
        }

        /// When retrieving the #Projection and #Viewport properties, specifies
        /// whether you want the values as seen through the viewer's lenses (`Distorted`) or
        /// as if no lenses were present (`Undistorted`).
        public enum Distortion
        {
            Distorted,

            /// Viewing through the lenses
            Undistorted /// No lenses
        }

        /// The transformation of head from origin in the tracking system.
        public Pose3D HeadPose
        {
            get { return device.GetHeadPose(); }
        }

        /// The projection matrix for a given eye.
        /// This matrix is an off-axis perspective projection with near and far
        /// clipping planes of 1m and 1000m, respectively.  The NxrEye script
        /// takes care of adjusting the matrix for its particular camera.
        public Matrix4x4 Projection(Eye eye, Distortion distortion = Distortion.Distorted)
        {
            return device.GetProjection(eye, distortion);
        }

        /// The screen space viewport that the camera for the specified eye should render into.
        /// In the _Distorted_ case, this will be either the left or right half of the `StereoScreen`
        /// render texture.  In the _Undistorted_ case, it refers to the actual rectangle on the
        /// screen that the eye can see.
        public Rect Viewport(Eye eye, Distortion distortion = Distortion.Distorted)
        {
            return device.GetViewport(eye, distortion);
        }

        private void InitDevice()
        {
            if (device != null)
            {
                device.Destroy();
            }

            // 根据当前运行场景获取对应的设备对象
            device = BaseARDevice.GetDevice();
            device.Init();

            device.SetSplitScreenModeEnabled(splitScreenModeEnabled);
            // 更新界面数据
            device.UpdateScreenData();

            GazeApi(GazeTag.Show);
            GazeApi(GazeTag.Set_Size, ((int) GazeSize.Original).ToString());
        }

        // Windows Editor/Player
        public bool IsWinPlatform { get; set; }

        NxrInput nxrInput;

        /// @note Each scene load causes an OnDestroy of the current SDK, followed
        /// by and Awake of a new one.  That should not cause the underlying native
        /// code to hiccup.  Exception: developer may call Application.DontDestroyOnLoad
        /// on the SDK if they want it to survive across scene loads.
        void Awake()
        {
            Debug.Log("NxrViewer Awake");
            SettingsAssetConfig asset;
#if UNITY_EDITOR
            asset = NxrSDKApi.Instance.GetSettingsAssetConfig();
#else
            asset = Resources.Load<SettingsAssetConfig>("Config/SettingsAssetConfig");
#endif
            sixDofMode = asset.mSixDofMode;
            sleepTimeoutMode = asset.mSleepTimeoutMode;
            headControlEnabled = asset.mHeadControl;
            textureQuality = asset.mTextureQuality;
            textureMsaa = asset.mTextureMSAA;
            InitialRecenter = true;
            nxrInput = new NxrInput();
            IsWinPlatform = false;
            Debug.Log("SettingsAssetConfig:" + asset.mSixDofMode + "--" + asset.mSleepTimeoutMode + "--" +
                      asset.mHeadControl + "--" + asset.mTextureQuality + "--" + asset.mTextureMSAA);
#if UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
            IsWinPlatform = true;
#endif
            if (instance == null)
            {
                instance = this;

                Loom.Initialize();

                if (Application.isMobilePlatform)
                {
                    QualitySettings.antiAliasing = 0;
                    Application.runInBackground = false;
                    Input.gyro.enabled = false;
                    Debug.Log("SleepTimeout:" + SleepMode.ToString());
                    if (SleepMode == SleepTimeoutMode.NEVER_SLEEP)
                    {
                        // Disable screen dimming
                        Screen.sleepTimeout = SleepTimeout.NeverSleep;
                    }
                    else
                    {
                        Screen.sleepTimeout = SleepTimeout.SystemSetting;
                    }
                }
            }

            if (instance != this)
            {
                Debug.LogError("There must be only one NxrViewer object in a scene.");
                DestroyImmediate(this);
                return;
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            // 编辑器PASS
            nxr.NibiruXR.instance.Init(gameObject);
#endif

            InitDevice();
            if (!IsWinPlatform && !NxrGlobal.supportDtr && !NxrGlobal.isVR9Platform)
            {
                // 录屏功能需要使用2个脚本 [VR9不需要]
                // 非DTR需要
                AddPrePostRenderStages();
            }

            Debug.Log("Is Windows Platform : " + IsWinPlatform + ", ScreenInfo : " + Screen.width + "*" +
                      Screen.height + ", AntiAliasing : " + QualitySettings.antiAliasing);
#if UNITY_ANDROID
            // 在unity20172.0f3版本使用-1相当于half vsync，所以此处设置为90fps。目前机器最高就是90
            int targetFrameRate = Application.platform == RuntimePlatform.Android
                ? ((int) NxrGlobal.refreshRate > 0 ? (int) NxrGlobal.refreshRate : 90)
                : 60;
            if (NxrGlobal.isVR9Platform)
            {
                // 参考全志官方
                targetFrameRate = 60;
                textureMsaa = TextureMSAA.NONE;
            }

            Application.targetFrameRate = targetFrameRate;
#endif
            if (Application.platform != RuntimePlatform.Android)
            {
                NxrSDKApi.Instance.IsInXRMode = true;
            }

            // not use dtr //
            if (!NxrGlobal.supportDtr || NxrGlobal.isVR9Platform)
            {
                QualitySettings.vSyncCount = 1;
                if (NxrGlobal.offaxisDistortionEnabled)
                {
                    Application.targetFrameRate = Application.platform == RuntimePlatform.Android
                        ? (int) NxrGlobal.refreshRate
                        : -1;
                    Debug.Log("offaxisDistortionEnabled : Setting frame rate to " + Application.targetFrameRate);
                }
            }
            else
            {
                // we sync in the TimeWarp, so we don't want unity syncing elsewhere
                QualitySettings.vSyncCount = 0;
            }
#if UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
            if (IsWinPlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = (int)targetFrameRate;
                // don't require companion window focus
                Application.runInBackground = true;
                QualitySettings.maxQueuedFrames = -1;
                QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, (int)TextureMSAA);
            }
#endif

#if UNITY_ANDROID && UNITY_EDITOR
            //GraphicsDeviceType[] graphicsDeviceType = UnityEditor.PlayerSettings.GetGraphicsAPIs(UnityEditor.BuildTarget.Android);
            // Debug.Log("GraphicsDeviceType------->" + graphicsDeviceType[0].ToString());
            //if (graphicsDeviceType[0] != GraphicsDeviceType.OpenGLES2)
            //{
            //    string title = "Incompatible graphics API detected!";
            //    string message = "Please set graphics API to \"OpenGL ES 2.0\" and rebuild, or Some Api may not work as expected .";
            // UnityEditor.EditorUtility.DisplayDialog(title, message, "OK");
            // Debug.LogError(title + " " + message);
            //}

#if UNITY_EDITOR
            string defineSymbols =
                UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Android);
            if (defineSymbols == null || !defineSymbols.Contains("NIBIRU_"))
            {
                string title = "Hmd Type Not Configed!";
                string message = "Please Choose Hmd Type in Menu \nNibiruXR->XR Settings .";
                UnityEditor.EditorUtility.DisplayDialog(title, message, "OK");
                Debug.LogError(title + " " + message);
            }
#endif
#endif
            device.AndroidLog("Welcome to use Unity NXR SDK , current SDK VERSION is " + NXR_SDK_VERSION + ", j " +
                              NxrGlobal.jarVersion
                              + ", s " + NxrGlobal.soVersion + ", u " + Application.unityVersion + ", fps " +
                              Application.targetFrameRate + ", vsync "
                              + QualitySettings.vSyncCount + ", hmd " + HmdType + ", antiAliasing : " +
                              QualitySettings.antiAliasing
                              + ", SixDofMode : " + sixDofMode.ToString());

            AddStereoControllerToCameras();
        }

        public delegate void NibiruConfigCallback(NxrInstantNativeApi.Nibiru_Config cfg);

        NibiruConfigCallback _nvrConfigCallback;

        void Start()
        {
            //lx changed for 5.4：在Awake中执行，不然在NxrArmModel调用之前Head还没创建
            //AddStereoControllerToCameras();

            if (IsWinPlatform)
            {
                // 初始化光学参数
                _nvrConfigCallback += OnNibiruConfigCallback;
                NxrInstantNativeApi.SetNibiruConfigCallback(_nvrConfigCallback);


#if NIBIRU_DEBUG
                NvrInstantNativeApi.Inited = false;
                Debug.Log("NvrInstantNativeApi.Init.Not Called.");
#else
                int _textureWidth = 1920, _textureHeight = 1080;
                NxrInstantNativeApi.NvrInitParams param;
                param.renderWidth = _textureWidth;
                param.renderHeight = _textureHeight;
                param.bitRate = 30;
                NxrInstantNativeApi.Inited = NxrInstantNativeApi.Init(param);
                Debug.Log("NxrInstantNativeApi.Init.Called.");

                NxrInstantNativeApi.GetVersionInfo(ref NxrInstantNativeApi.nativeApiVersion,
                    ref NxrInstantNativeApi.driverVersion);
                Debug.Log("NxrInstantNativeApi.Version.Api." + NxrInstantNativeApi.nativeApiVersion + ",Driver." +
                          NxrInstantNativeApi.driverVersion);

                if (NxrInstantNativeApi.nativeApiVersion >= 2000)
                {
                    NxrInstantNativeApi.GetTextureResolution(ref _textureWidth, ref _textureHeight);
                    if (NxrInstantNativeApi.driverVersion >= 2002)
                    {
                        UInt32 rateData = NxrInstantNativeApi.GetRefreshRate();
                        Debug.Log("-------------rateData--------" + rateData);
                        if (rateData >= 60)
                        {
                            Application.targetFrameRate = (int) rateData;
                        }
                    }
                }

                if (eyeStereoScreens[0] == null && !NxrGlobal.isVR9Platform)
                {
                    // 初始化6个纹理
                    InitEyeStereoScreens(_textureWidth, _textureHeight);
                }

                if (eyeStereoScreens[0] != null)
                {
                    // 设置Texturew Native Ptr
                    NxrInstantNativeApi.SetFrameTexture(eyeStereoScreens[0].GetNativeTexturePtr());
                    Debug.Log("NxrInstantNativeApi.SetFrameTexture." + eyeStereoScreens[0].GetNativeTexturePtr());
                }
#endif

                Debug.Log("NxrInstantNativeApi.Init. Size " + param.renderWidth + "*" + param.renderHeight + ", Bit " +
                          param.bitRate + ", Inited " + NxrInstantNativeApi.Inited);
            }
            else
            {
                if (eyeStereoScreens[0] == null && !NxrGlobal.isVR9Platform)
                {
                    // 初始化6个纹理
                    InitEyeStereoScreens();
                    device.SetTextureSizeNative(eyeStereoScreens[0].width, eyeStereoScreens[1].height);
                }
            }

            if (ShowFPS)
            {
                Transform[] father;
                father = GetComponentsInChildren<Transform>(true);
                GameObject FPS = null;
                foreach (Transform child in father)
                {
                    if (child.gameObject.name == "NxrFPS")
                    {
                        FPS = child.gameObject;
                        break;
                    }
                }

                if (FPS != null)
                {
                    FPS.SetActive(ShowFPS);
                }
                else
                {
                    GameObject fpsGo = Instantiate(Resources.Load("Prefabs/NxrFPS")) as GameObject;
#if UNITY_ANDROID && !UNITY_EDITOR
                    fpsGo.GetComponent<NxrFPS>().enabled = false;
                    fpsGo.AddComponent<FpsStatistics>();
#else
                    fpsGo.GetComponent<NxrFPS>().enabled = true;
#endif
                }
            }

            UpdateHeadControl();

            NxrSDKApi.Instance.SixDofControllerPrimaryDeviceType = device.GetSixDofControllerPrimaryDeviceType();
            // 6dof双手柄
            gameObject.AddComponent<NxrControllerManager>();
        }

        public void UpdateHeadControl()
        {
            // Debug.LogError("UpdateHeadControl=" + HeadControl.ToString());
            // 已经设置强制使用Unity白点，不做处理
            switch (HeadControl)
            {
                case HeadControl.GazeApplication:
                    SwitchApplicationReticle(true);
                    GetNxrReticle().HeadDismiss();
                    break;
                case HeadControl.GazeSystem:
                    SwitchApplicationReticle(false);
                    GetNxrReticle().HeadDismiss();
                    GazeApi(GazeTag.Show);
                    break;
                case HeadControl.Hover:
                    GetNxrReticle().HeadShow();
                    SwitchApplicationReticle(true);
                    break;
                case HeadControl.Controller:
                    SwitchApplicationReticle(false);
                    GetNxrReticle().HeadDismiss();
                    GazeApi(GazeTag.Hide);
                    break;
            }
        }

        private NxrHead head;

        /// <summary>
        ///  Get the NxrHead
        /// </summary>
        /// <returns></returns>
        public NxrHead GetHead()
        {
            if (head == null && Controller != null)
            {
                head = Controller.Head;
            }

            if (head == null)
            {
                head = FindObjectOfType<NxrHead>();
            }

            return head;
        }

        /// <summary>
        /// The displacement data of Third-party. 
        /// </summary>
        /// <param name="position"></param>
        public void Update3rdPartyPosition(Vector3 position)
        {
            NxrHead mHead = GetHead();
            if (mHead != null)
            {
                mHead.Update3rdPartyPosition(position);
            }
        }

        private void OnNibiruConfigCallback(NxrInstantNativeApi.Nibiru_Config cfg)
        {
            Loom.QueueOnMainThread((param) =>
            {
                device.Profile.viewer.lenses.separation = cfg.ipd;

                //-0.03048, 0.03, -0.03402, 0.03402
                device.ComputeEyesForWin(NxrViewer.Eye.Left, cfg.near, 1000, cfg.eyeFrustumParams[0],
                    cfg.eyeFrustumParams[3], cfg.eyeFrustumParams[1], cfg.eyeFrustumParams[2]);
                //-0.03, 0.03048, -0.03402, 0.03402
                device.ComputeEyesForWin(NxrViewer.Eye.Right, cfg.near, 1000, cfg.eyeFrustumParams[4],
                    cfg.eyeFrustumParams[7], cfg.eyeFrustumParams[5], cfg.eyeFrustumParams[6]);

                // 手柄类型
                NxrControllerHelper.InitController((int) cfg.controllerType);
                device.profileChanged = true;
                Debug.Log("OnNibiruConfigCallback Config : Ipd " + cfg.ipd + ", Near " + cfg.near +
                          ", FrustumLeft(LRBT) " + cfg.eyeFrustumParams[0] + ", " + cfg.eyeFrustumParams[1] + ","
                          + cfg.eyeFrustumParams[2] + ", " + cfg.eyeFrustumParams[3] + ", ControllerType " +
                          cfg.controllerType);

                TrackerPosition = true;
                {
                    NxrHead head = GetHead();
                    if (head != null)
                    {
                        head.SetTrackPosition(TrackerPosition);
                    }
                }

                NxrButtonEvent mNvrButtonEvent = GameObject.FindObjectOfType<NxrButtonEvent>();
                if (mNvrButtonEvent != null)
                {
                    mNvrButtonEvent.RefreshTrackedDevices();
                }

                device.profileChanged = true;
            }, cfg);
        }

        private int baseTryTimes = 1;

        private void Update()
        {
            UpdateState();

            if (!NxrGlobal.isVR9Platform)
            {
                UpdateEyeTexture();
            }


            if (GazeInputModule.gazePointer != null)
            {
                GazeInputModule.gazePointer.UpdateStatus();
            }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (baseTryTimes < 5 && gameObject.GetComponent(System.Text.Encoding.Default.GetString(winTypeName)) == null)
            {
                baseTryTimes++;
                nxr.NibiruXR.instance.Init(gameObject);
                Debug.Log("Run NibiruXR Init in Update(), " + baseTryTimes);
            }
#endif
            if (nxrInput != null)
            {
                nxrInput.Process();
            }
        }

        public BaseARDevice GetDevice()
        {
            return device;
        }

        /// <summary>
        /// Print log using android logcat.
        /// </summary>
        /// <param name="msg"></param>
        public void AndroidLog(string msg)
        {
            if (device != null)
            {
                device.AndroidLog(msg);
            }
            else
            {
                Debug.Log(msg);
            }
        }

        public void UpdateHeadPose()
        {
            if (device != null && NxrSDKApi.Instance.IsInXRMode)
                device.UpdateState();
        }

        public void UpdateEyeTexture()
        {
            // 更新左右眼目标纹理
            if (USE_DTR && NxrGlobal.supportDtr)
            {
                // 更换纹理索引
                SwapBuffers();

                NxrEye[] eyes = NxrViewer.Instance.eyes;
                for (int i = 0; i < 2; i++)
                {
                    NxrEye eye = eyes[i];
                    if (eye != null)
                    {
                        eye.UpdateTargetTexture();
                    }
                }
            }
        }

        void AddPrePostRenderStages()
        {
            var preRender = FindObjectOfType<NxrPreRender>();
            if (preRender == null)
            {
                var go = new GameObject("PreRender", typeof(NxrPreRender));
                go.SendMessage("Reset");
                go.transform.parent = transform;
                Debug.Log("Add NxrPreRender");
            }

            var postRender = FindObjectOfType<NxrPostRender>();
            if (postRender == null)
            {
                var go = new GameObject("PostRender", typeof(NxrPostRender));
                go.SendMessage("Reset");
                go.transform.parent = transform;
                Debug.Log("Add NxrPostRender");
            }
        }

        /// Whether the viewer's trigger was pulled. True for exactly one complete frame
        /// after each pull
        public bool Triggered { get; set; }

        public bool ProfileChanged { get; private set; }

        // Only call device.UpdateState() once per frame.
        private int updatedToFrame = 0;

        /// Reads the latest tracking data from the phone.  This must be
        /// called before accessing any of the poses and matrices above.
        ///
        /// Multiple invocations per frame are OK:  Subsequent calls merely yield the
        /// cached results of the first call.  To minimize latency, it should be first
        /// called later in the frame (for example, in `LateUpdate`) if possible.
        public void UpdateState()
        {
            if (updatedToFrame != Time.frameCount)
            {
                updatedToFrame = Time.frameCount;
                DispatchEvents();
                if (NeedUpdateNearFar && device != null && device.nibiruVRServiceId != 0)
                {
                    float far = GetCameraFar();
                    float mNear = 0.0305f;
                    if (NxrGlobal.fovNear > -1)
                    {
                        mNear = NxrGlobal.fovNear;
                    }

                    device.SetCameraNearFar(mNear, far);
                    Instance.NeedUpdateNearFar = false;

                    for (int i = 0; i < 2; i++)
                    {
                        NxrEye eye = eyes[i];
                        if (eye != null)
                        {
                            if (eye.cam.farClipPlane < NxrGlobal.fovFar)
                            {
                                eye.cam.farClipPlane = NxrGlobal.fovFar;
                            }
                        }
                    }
                }
            }
        }

        // /// <summary>
        // /// Analog all-in-one button： W-Up A-Left S-Down D-Right Space-Return Return-Confirm
        // /// </summary>
        // private void SimulateHeadSetKeyEvent()
        // {
        //     if (!Application.isEditor && !(Application.platform == RuntimePlatform.WindowsPlayer))
        //     {
        //         // 编辑器 or win
        //         return;
        //     }
        //
        //     if (Input.GetKeyDown(KeyCode.W))
        //     {
        //         TriggerKeyEvent(KeyCode.UpArrow, false);
        //     }
        //     else if (Input.GetKeyUp(KeyCode.W))
        //     {
        //         TriggerKeyEvent(KeyCode.UpArrow, true);
        //     }
        //     else if (Input.GetKeyDown(KeyCode.S))
        //     {
        //         TriggerKeyEvent(KeyCode.DownArrow, false);
        //     }
        //     else if (Input.GetKeyUp(KeyCode.S))
        //     {
        //         TriggerKeyEvent(KeyCode.DownArrow, true);
        //     }
        //     else if (Input.GetKeyDown(KeyCode.A))
        //     {
        //         TriggerKeyEvent(KeyCode.LeftArrow, false);
        //     }
        //     else if (Input.GetKeyUp(KeyCode.A))
        //     {
        //         TriggerKeyEvent(KeyCode.LeftArrow, true);
        //     }
        //     else if (Input.GetKeyDown(KeyCode.D))
        //     {
        //         TriggerKeyEvent(KeyCode.RightArrow, false);
        //     }
        //     else if (Input.GetKeyUp(KeyCode.D))
        //     {
        //         TriggerKeyEvent(KeyCode.RightArrow, true);
        //     }
        //     else if (Input.GetKeyDown(KeyCode.Space))
        //     {
        //         TriggerKeyEvent(KeyCode.Escape, false);
        //     }
        //     else if (Input.GetKeyUp(KeyCode.Space))
        //     {
        //         TriggerKeyEvent(KeyCode.Escape, true);
        //     }
        //     else if (Input.GetKeyDown(KeyCode.Return))
        //     {
        //         TriggerKeyEvent(KeyCode.JoystickButton0, false);
        //     }
        //     else if (Input.GetKeyUp(KeyCode.Return))
        //     {
        //         TriggerKeyEvent(KeyCode.JoystickButton0, true);
        //     }
        //
        // }

        // List<INxrButtonListener> btnLiseners = null;
        List<INxrJoystickListener> joystickListeners = null;

        int[] lastKeyAction;

        private void DispatchEvents()
        {
            // Update flags first by copying from device and other inputs.
            if (device == null) return;
            // if (Input.GetMouseButton(0) && !Triggered)
            // {
            //     Triggered = Input.GetMouseButtonDown(0);
            // }
            ProfileChanged = device.profileChanged;
            if (device.profileChanged)
            {
                if (NxrOverrideSettings.OnProfileChangedEvent != null) NxrOverrideSettings.OnProfileChangedEvent();
                device.profileChanged = false;
            }

            // 模拟一体机按键
            // SimulateHeadSetKeyEvent();

            bool IsHasController = false;
            if (Application.platform == RuntimePlatform.Android)
            {
                // 3dof or nolo
                IsHasController = (NxrPlayerCtrl.Instance.IsQuatConn() || ControllerAndroid.IsNoloConn());
            }
            else if (IsWinPlatform)
            {
                IsHasController = NxrControllerHelper.Is3DofControllerConnected &&
                                  NxrPlayerCtrl.Instance.GamepadEnabled;
            }

            if (IsHasController)
            {
                int[] KeyAction = null;
                if (InteractionManager.IsControllerConnected())
                {
                    KeyAction = InteractionManager.GetKeyAction();
                }
                else
                {
                    KeyAction = NibiruTaskApi.GetKeyAction();
                }

                // if (KeyAction[CKeyEvent.KEYCODE_DPAD_CENTER] == 0)
                // {
                //     Triggered = true;
                // }

                if (lastKeyAction == null)
                {
                    lastKeyAction = KeyAction;
                }

                for (int i = 0; i < CKeyEvent.KeyCodeIds.Length; i++)
                {
                    int keyCode = CKeyEvent.KeyCodeIds[i];

                    if (KeyAction[keyCode] == 0 && lastKeyAction[keyCode] == 1)
                    {
                        // down
                        // TriggerKeyEventForController(keyCode, false);
                    }

                    if (KeyAction[keyCode] == 1 && lastKeyAction[keyCode] == 0)
                    {
                        // up
                        // TriggerKeyEventForController(keyCode, true);
                    }
                }

                lastKeyAction = KeyAction;
            }

            //   
            // 手柄上下左右兼容处理
            float leftKeyHor = Input.GetAxis("5th axis");
            float leftKeyVer = Input.GetAxis("6th axis");

            if (leftKeyHor == 1)
            {
                // 左
                // TriggerKeyEvent(KeyCode.LeftArrow);
                TriggerJoystickEvent(16, 0);
            }
            else if (leftKeyHor == -1)
            {
                // 右
                // TriggerKeyEvent(KeyCode.RightArrow);
                TriggerJoystickEvent(17, 0);
            }

            if (leftKeyVer == -1)
            {
                // 上
                // TriggerKeyEvent(KeyCode.UpArrow);
                TriggerJoystickEvent(14, 0);
            }
            else if (leftKeyVer == 1)
            {
                // 下
                // TriggerKeyEvent(KeyCode.DownArrow);
                TriggerJoystickEvent(15, 0);
            }

            // 左摇杆
            float leftStickHor = Input.GetAxis("joystick_Horizontal");
            float leftStickVer = Input.GetAxis("joystick_Vertical");
            if (leftStickHor != 0)
            {
                TriggerJoystickEvent(10, leftStickHor);
            }

            if (leftStickVer != 0)
            {
                TriggerJoystickEvent(11, leftStickVer);
            }

            // 右摇杆
            float rightStickHor = Input.GetAxis("3th axis");
            float rightStickVer = Input.GetAxis("4th axis");
            if (rightStickHor != 0)
            {
                TriggerJoystickEvent(12, rightStickHor);
            }

            if (rightStickVer != 0)
            {
                TriggerJoystickEvent(13, rightStickVer);
            }
            // 

            if (Application.platform == RuntimePlatform.Android && Input.anyKeyDown)
            {
                bool triggerDirectKey = false;
                Event e = Event.current;
                if (e != null && e.isKey)
                {
                    KeyCode currentKey = e.keyCode;
                    Debug.Log("Current Key is : " + currentKey.ToString());
                    // if ((int)currentKey == 10 || currentKey == KeyCode.LeftArrow || currentKey == KeyCode.RightArrow || currentKey == KeyCode.UpArrow || currentKey == KeyCode.DownArrow || currentKey == KeyCode.Escape
                    // || currentKey == KeyCode.JoystickButton0)
                    // {
                    // if ((int)currentKey == 10 || currentKey == KeyCode.JoystickButton0)
                    // {
                    //     // ok 键
                    //     Triggered = true;
                    // }
                    triggerDirectKey = currentKey == KeyCode.LeftArrow || currentKey == KeyCode.RightArrow ||
                                       currentKey == KeyCode.UpArrow || currentKey == KeyCode.DownArrow;
                    // TriggerKeyEvent(currentKey);
                    // }

                    // 手柄  
                    if (currentKey == KeyCode.LeftShift)
                    {
                        TriggerJoystickEvent(0, 0);
                    }
                    else if (currentKey == KeyCode.LeftAlt)
                    {
                        TriggerJoystickEvent(1, 0);
                    }
                    else if (currentKey == KeyCode.RightShift)
                    {
                        TriggerJoystickEvent(2, 0);
                    }
                    else if (currentKey == KeyCode.RightAlt)
                    {
                        TriggerJoystickEvent(3, 0);
                    }
                    else if (currentKey == KeyCode.Pause)
                    {
                        TriggerJoystickEvent(4, 0);
                    }
                    else if (currentKey == KeyCode.Return)
                    {
                        TriggerJoystickEvent(5, 0);
                    }
                    else if (currentKey == KeyCode.JoystickButton2)
                    {
                        TriggerJoystickEvent(6, 0);
                    }
                    else if (currentKey == KeyCode.JoystickButton3)
                    {
                        TriggerJoystickEvent(7, 0);
                    }
                    else if (currentKey == KeyCode.JoystickButton1)
                    {
                        TriggerJoystickEvent(8, 0);
                    }
                    else if (currentKey == KeyCode.JoystickButton0)
                    {
                        TriggerJoystickEvent(9, 0);
                    }
                    else if (currentKey == KeyCode.JoystickButton8)
                    {
                        TriggerJoystickEvent(18, 0);
                    }
                    else if (currentKey == KeyCode.JoystickButton9)
                    {
                        TriggerJoystickEvent(19, 0);
                    }
                }
            }

            // up 事件
            // if (Application.platform == RuntimePlatform.Android &&
            //     (Input.GetKeyUp(KeyCode.JoystickButton0) || Input.GetKeyUp((KeyCode)10)))
            // {
            //     TriggerKeyEvent(KeyCode.JoystickButton0, true);
            // }

            if (Application.platform == RuntimePlatform.Android && Event.current != null &&
                (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow)
                                                   || Input.GetKeyUp(KeyCode.DownArrow) ||
                                                   Input.GetKeyUp(KeyCode.UpArrow)
                                                   || Input.GetKeyUp(KeyCode.Escape)))
            {
                Debug.Log("KeyUp===>" + Event.current.keyCode.ToString());
                if (NibiruRemindBox.Instance.Showing())
                {
                    NibiruRemindBox.Instance.ReleaseDestory();
                }

                if (NibiruShutDownBox.Instance.Showing())
                {
                    NibiruShutDownBox.Instance.ReleaseDestory();
                }

                // else
                // {
                //     TriggerKeyEvent(Event.current.keyCode, true);
                // }
            }
        }

        // private void TriggerKeyEvent(KeyCode currentKey)
        // {
        //     TriggerKeyEvent(currentKey, false);
        // }

        // private void TriggerKeyEvent(KeyCode currentKey, bool isKeyUp)
        // {
        //     if (btnLiseners == null || btnLiseners.Count == 0)
        //     {
        //         List<GameObject> allObject = GetAllObjectsInScene();
        //         btnLiseners = new List<INxrButtonListener>();
        //         foreach (GameObject obj in allObject)
        //         {
        //             Component[] comps = obj.GetComponents(typeof(INxrButtonListener));
        //             if (comps != null)
        //             {
        //                 INxrButtonListener[] listeners = new INxrButtonListener[comps.Length];
        //
        //                 for (int p = 0; p < comps.Length; p++)
        //                 {
        //                     listeners[p] = (INxrButtonListener)comps[p];
        //                 }
        //                 // 获取所有挂载了INxrButtonListener的物体
        //                 NotifyBtnPressed(listeners, currentKey, isKeyUp);
        //                 foreach (Component cp in comps)
        //                 {
        //                     btnLiseners.Add((INxrButtonListener)cp);
        //                 }
        //             }
        //         }
        //     }
        //     else
        //     {
        //         NotifyBtnPressed(btnLiseners.ToArray(), currentKey, isKeyUp);
        //     }
        // }

        // private void NotifyBtnPressed(INxrButtonListener[] comps, KeyCode currentKey, bool isKeyUp)
        // {
        //     if (comps == null) return;
        //     for (int i = 0, length = comps.Length; i < length; i++)
        //     {
        //         INxrButtonListener btnListener = (INxrButtonListener)comps[i];
        //         if (btnListener == null) continue;
        //         if (currentKey == KeyCode.LeftArrow)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftLeft(); } else { btnListener.OnPressLeft(); }
        //         }
        //         else if (currentKey == KeyCode.RightArrow)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftRight(); } else { btnListener.OnPressRight(); }
        //         }
        //         else if (currentKey == KeyCode.UpArrow)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftUp(); } else { btnListener.OnPressUp(); }
        //         }
        //         else if (currentKey == KeyCode.DownArrow)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftDown(); } else { btnListener.OnPressDown(); }
        //         }
        //         else if (currentKey == KeyCode.Escape)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftBack(); } else { btnListener.OnPressBack(); }
        //         }
        //         else if (currentKey == KeyCode.JoystickButton0 || (int)currentKey == 10)
        //         {
        //             btnListener.OnPressEnter(isKeyUp);
        //         }
        //         else if (currentKey == KeyCode.Joystick5Button18)
        //         {
        //             // 音量加
        //             btnListener.OnPressVolumnUp();
        //         }
        //         else if (currentKey == KeyCode.Joystick5Button19)
        //         {
        //             // 音量减
        //             btnListener.OnPressVolumnDown();
        //         }
        //         else if (currentKey == KeyCode.Joystick6Button1)
        //         {
        //             // Left
        //             if (isKeyUp)
        //             {
        //                 btnListener.OnLiftFuctionKey(FunctionKeyCode.NF1);
        //             }
        //             else
        //             {
        //                 btnListener.OnFuctionKeyDown(FunctionKeyCode.NF1);
        //             }
        //         }
        //         else if (currentKey == KeyCode.Joystick6Button2)
        //         {
        //             // Right
        //             if (isKeyUp)
        //             {
        //                 btnListener.OnLiftFuctionKey(FunctionKeyCode.NF2);
        //             }
        //             else
        //             {
        //                 btnListener.OnFuctionKeyDown(FunctionKeyCode.NF2);
        //             }
        //         }
        //     }
        //
        // }

        // private void TriggerKeyEventForController(int keycode, bool isKeyUp)
        // {
        //     if(NibiruRemindBox.Instance.Showing() && keycode == CKeyEvent.KEYCODE_BUTTON_APP && isKeyUp)
        //     {
        //         NibiruRemindBox.Instance.ReleaseDestory();
        //         return;
        //     }
        //     
        //     if (btnLiseners == null || btnLiseners.Count == 0)
        //     {
        //         List<GameObject> allObject = GetAllObjectsInScene();
        //         btnLiseners = new List<INxrButtonListener>();
        //         foreach (GameObject obj in allObject)
        //         {
        //             Component[] comps = obj.GetComponents(typeof(INxrButtonListener));
        //             if (comps != null)
        //             {
        //                 INxrButtonListener[] listeners = new INxrButtonListener[comps.Length];
        //
        //                 for (int p = 0; p < comps.Length; p++)
        //                 {
        //                     listeners[p] = (INxrButtonListener)comps[p];
        //                 }
        //                 // 获取所有挂载了INvrButtonListener的物体
        //                 NotifyBtnPressedForController(listeners, keycode, isKeyUp);
        //                 foreach (Component cp in comps)
        //                 {
        //                     btnLiseners.Add((INxrButtonListener)cp);
        //                 }
        //             }
        //         }
        //     }
        //     else
        //     {
        //         NotifyBtnPressedForController(btnLiseners.ToArray(), keycode, isKeyUp);
        //     }
        // }

        // private void NotifyBtnPressedForController(INxrButtonListener[] comps, int keycode, bool isKeyUp)
        // {
        //     if (comps == null) return;
        //     for (int i = 0, length = comps.Length; i < length; i++)
        //     {
        //         INxrButtonListener btnListener = comps[i];
        //         if (btnListener == null) continue;
        //         if (keycode == CKeyEvent.KEYCODE_DPAD_LEFT)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftLeft(); } else { btnListener.OnPressLeft(); }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_DPAD_RIGHT)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftRight(); } else { btnListener.OnPressRight(); }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_DPAD_UP)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftUp(); } else { btnListener.OnPressUp(); }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_DPAD_DOWN)
        //         {
        //             if (isKeyUp) { btnListener.OnLiftDown(); } else { btnListener.OnPressDown(); }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_BUTTON_APP)
        //         {
        //             // MENU
        //             if (isKeyUp)
        //             {
        //                 btnListener.OnLiftFuctionKey(FunctionKeyCode.MENU);
        //             }
        //             else
        //             {
        //                 btnListener.OnFuctionKeyDown(FunctionKeyCode.MENU);
        //             }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_DPAD_CENTER)
        //         {
        //             // TOUCH PAD CLICK
        //             if (isKeyUp)
        //             {
        //                 btnListener.OnLiftFuctionKey(FunctionKeyCode.TOUCHPAD);
        //             }
        //             else
        //             {
        //                 btnListener.OnFuctionKeyDown(FunctionKeyCode.TOUCHPAD);
        //             }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_VOLUME_UP)
        //         {
        //             // 音量加
        //             btnListener.OnPressVolumnUp();
        //             if (isKeyUp)
        //             {
        //                 btnListener.OnLiftFuctionKey(FunctionKeyCode.VOLUMN_UP);
        //             }
        //             else
        //             {
        //                 btnListener.OnFuctionKeyDown(FunctionKeyCode.VOLUMN_UP);
        //             }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_VOLUME_DOWN)
        //         {
        //             // 音量减
        //             btnListener.OnPressVolumnDown();
        //             if (isKeyUp)
        //             {
        //                 btnListener.OnLiftFuctionKey(FunctionKeyCode.VOLUMN_DOWN);
        //             }
        //             else
        //             {
        //                 btnListener.OnFuctionKeyDown(FunctionKeyCode.VOLUMN_DOWN);
        //             }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_BUTTON_R1)
        //         {
        //             // trigger
        //             if (isKeyUp)
        //             {
        //                 btnListener.OnLiftFuctionKey(FunctionKeyCode.TRIGGER);
        //             }
        //             else
        //             {
        //                 btnListener.OnFuctionKeyDown(FunctionKeyCode.TRIGGER);
        //             }
        //         }
        //         else if (keycode == CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH)
        //         {
        //             // touch pad
        //             if (isKeyUp)
        //             {
        //                 btnListener.OnLiftFuctionKey(FunctionKeyCode.TOUCHPAD_TOUCH);
        //             }
        //             else
        //             {
        //                 btnListener.OnFuctionKeyDown(FunctionKeyCode.TOUCHPAD_TOUCH);
        //             }
        //         }
        //     }
        //
        // }


        /// <summary>
        /// Resets the tracker so that the user's current direction becomes forward.
        /// </summary>
        public void Recenter()
        {
            device.Recenter();
            if (GetHead() != null)
            {
                GetHead().ResetInitEulerYAngle();
            }
        }

        /// Add a StereoController to any camera that does not have a Render Texture (meaning it is
        /// rendering to the screen).
        public static void AddStereoControllerToCameras()
        {
            for (int i = 0; i < Camera.allCameras.Length; i++)
            {
                Camera camera = Camera.allCameras[i];
                Debug.Log("Check Camera : " + camera.name);
                if (
                    (camera.tag == "MainCamera" || camera.tag == "NibiruCamera")
                    && camera.targetTexture == null &&
                    camera.GetComponent<NxrStereoController>() == null &&
                    camera.GetComponent<NxrEye>() == null &&
                    camera.GetComponent<NxrPreRender>() == null &&
                    camera.GetComponent<NxrPostRender>() == null)
                {
                    camera.gameObject.AddComponent<NxrStereoController>();
                }
            }
        }

        void OnEnable()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            // This can happen if you edit code while the editor is in Play mode.
            if (device == null)
            {
                InitDevice();
            }
#endif
            device.OnPause(false);
            if (!NxrGlobal.isVR9Platform)
            {
                StartCoroutine("EndOfFrame");
            }
        }

        void OnDisable()
        {
            device.OnPause(true);
            Debug.Log("NxrViewer->OnDisable");
            StopCoroutine("EndOfFrame");
        }

        private Coroutine onResume = null;

        void OnPause()
        {
            onResume = null;
            device.OnApplicationPause(true);
        }

        IEnumerator OnResume()
        {
            yield return new WaitForSeconds(1.0f);
            // resume
            if (!NxrGlobal.isVR9Platform && NxrGlobal.supportDtr)
            {
                InitNxrReticleScript();
                UpdateHeadControl();
            }

            device.OnApplicationPause(false);
        }

        public void SetPause(bool pause)
        {
            if (pause)
            {
                OnPause();
            }
            else if (onResume == null)
            {
                onResume = StartCoroutine(OnResume());
            }
        }

        void OnApplicationPause(bool pause)
        {
            Debug.Log("NxrViewer->OnApplicationPause," + pause + ", hasEnterXRMode=" + NxrSDKApi.Instance.IsInXRMode);
            SetPause(pause);
        }

        void OnApplicationFocus(bool focus)
        {
            Debug.Log("NxrViewer->OnApplicationFocus," + focus);
            device.OnFocus(focus);
        }

        void OnApplicationQuit()
        {
            if (GetNibiruService() != null && GetNibiruService().IsMarkerRecognizeRunning)
            {
                GetNibiruService().StopMarkerRecognize();
            }

            StopAllCoroutines();
            device.OnApplicationQuit();

            if (NxrOverrideSettings.OnApplicationQuitEvent != null)
            {
                NxrOverrideSettings.OnApplicationQuitEvent();
            }

            Debug.Log("NxrViewer->OnApplicationQuit");

#if UNITY_ANDROID && !UNITY_EDITOR
			if(IsAndroidKillProcess) 
            {
                 NxrSDKApi.Instance.Destroy();
                 Debug.Log("NxrViewer->OnApplicationQuit.KillProcess");
                 System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
#endif
        }

        /// <summary>
        ///  Quit app
        /// </summary>
        public void AppQuit()
        {
            device.AppQuit();
        }

        void OnDestroy()
        {
            if (IsWinPlatform)
            {
                if (NxrInstantNativeApi.Inited)
                {
                    _nvrConfigCallback -= OnNibiruConfigCallback;
                    NxrInstantNativeApi.Inited = false;
                    NxrInstantNativeApi.Cleanup();
                    NxrControllerHelper.Reset();
                }
            }

            // if (btnLiseners != null)
            // {
            //     btnLiseners.Clear();
            // }
            // btnLiseners = null;

            this.SplitScreenModeEnabled = false;
            InteractionManager.Reset();
            if (device != null)
            {
                device.Destroy();
            }

            if (instance == this)
            {
                instance = null;
            }

            Debug.Log("NxrViewer->OnDestroy");
        }

        // 处理来自Android的调用 
        public void ResetHeadTrackerFromAndroid()
        {
            if (instance != null && device != null)
            {
                Recenter();
            }

            NibiruRemindBox.Instance.ReleaseDestory();
            NibiruShutDownBox.Instance.ReleaseDestory();
        }

        void OnVolumnUp()
        {
            // TriggerKeyEvent(KeyCode.Joystick5Button18);
            if (nxrInput != null)
            {
                nxrInput.OnChangeKeyEvent(CKeyEvent.KEYCODE_VOLUME_DOWN, CKeyEvent.ACTION_DOWN);
            }
        }

        void OnVolumnDown()
        {
            // TriggerKeyEvent(KeyCode.Joystick5Button19);
            if (nxrInput != null)
            {
                nxrInput.OnChangeKeyEvent(CKeyEvent.KEYCODE_VOLUME_UP, CKeyEvent.ACTION_DOWN);
            }
        }

        void OnKeyDown(string keyCode)
        {
            Debug.Log("OnKeyDown=" + keyCode);
            if (keyCode == NxrGlobal.KeyEvent_KEYCODE_ALT_LEFT)
            {
                // TriggerKeyEvent(KeyCode.Joystick6Button1, false);
                if (nxrInput != null)
                {
                    nxrInput.OnChangeKeyEvent(CKeyEvent.KEYCODE_NF_1, CKeyEvent.ACTION_DOWN);
                }
            }
            else if (keyCode == NxrGlobal.KeyEvent_KEYCODE_MEDIA_RECORD)
            {
                // TriggerKeyEvent(KeyCode.Joystick6Button2, false);
                if (nxrInput != null)
                {
                    nxrInput.OnChangeKeyEvent(CKeyEvent.KEYCODE_NF_2, CKeyEvent.ACTION_DOWN);
                }
            }
        }

        void OnKeyUp(string keyCode)
        {
            Debug.Log("OnKeyUp=" + keyCode);
            if (keyCode == NxrGlobal.KeyEvent_KEYCODE_ALT_LEFT)
            {
                // TriggerKeyEvent(KeyCode.Joystick6Button1, true);
                if (nxrInput != null)
                {
                    nxrInput.OnChangeKeyEvent(CKeyEvent.KEYCODE_NF_1, CKeyEvent.ACTION_UP);
                }
            }
            else if (keyCode == NxrGlobal.KeyEvent_KEYCODE_MEDIA_RECORD)
            {
                // TriggerKeyEvent(KeyCode.Joystick6Button2, true);
                if (nxrInput != null)
                {
                    nxrInput.OnChangeKeyEvent(CKeyEvent.KEYCODE_NF_2, CKeyEvent.ACTION_UP);
                }
            }
        }

        void OnActivityPause()
        {
            Debug.Log("OnActivityPause");
        }

        void OnActivityResume()
        {
            Debug.Log("OnActivityResume");
        }

        /// <summary>
        ///  Set system split mode : 1=split by system，0=split by app
        /// </summary>
        public void SetSystemSplitMode(int flag)
        {
            device.NSetSystemSplitMode(flag);
        }

        private int[] _texture_ids = new int[_texture_count];
        private int _current_texture_index, _next_texture_index;

        public bool SwapBuffers()
        {
            bool ret = true;
            for (int i = 0; i < _texture_count; i++)
            {
                if (!eyeStereoScreens[i].IsCreated())
                {
                    eyeStereoScreens[i].Create();
                    _texture_ids[i] = (int) eyeStereoScreens[i].GetNativeTexturePtr();
                    ret = false;
                }
            }

            _current_texture_index = _next_texture_index;
            _next_texture_index = (_next_texture_index + 2) % _texture_count;
            return ret;
        }

        public int GetEyeTextureId(int eye)
        {
            return _texture_ids[_current_texture_index + (int) eye];
        }

        public int GetTimeWarpViewNum()
        {
            return device.GetTimewarpViewNumber();
        }

        public List<GameObject> GetAllObjectsInScene()
        {
            GameObject[] pAllObjects = (GameObject[]) Resources.FindObjectsOfTypeAll(typeof(GameObject));
            List<GameObject> pReturn = new List<GameObject>();
            foreach (GameObject pObject in pAllObjects)
            {
                if (pObject == null || !pObject.activeInHierarchy || pObject.hideFlags == HideFlags.NotEditable ||
                    pObject.hideFlags == HideFlags.HideAndDontSave)
                {
                    continue;
                }

                pReturn.Add(pObject);
            }

            return pReturn;
        }

        public Texture2D createTexture2D(RenderTexture renderTexture)
        {
            int width = renderTexture.width;
            int height = renderTexture.height;
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture2D.Apply();
            return texture2D;
        }

        private int frameCount = 0;

        private void EndOfFrameCore()
        {
            if (USE_DTR && (!NxrSDKApi.Instance.IsInXRMode && frameCount < 3))
            {
                frameCount++;
                Debug.Log("EndOfFrame->hasEnterRMode " + "" + NxrSDKApi.Instance.IsInXRMode + " or frameCount " +
                          frameCount);
                // Call GL.clear before Enter VRMode to avoid unexpected graph breaking.
                GL.Clear(false, true, Color.black);
            }
            else
            {
                frameCount++;
                if (USE_DTR && NxrGlobal.supportDtr)
                {
                    if (settings.timewarpEnabled >= 0 && frameCount > 0 && frameCount < 10)
                    {
                        device.SetTimeWarpEnable(false);
                    }

                    if (NxrGlobal.DEBUG_LOG_ENABLED) Debug.Log("EndOfFrame.TimeWarp[" + frameCount + "]");
                    // Debug.Log("TimeWrap." + frameCount);
                }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
                // Submit
                if (NxrInstantNativeApi.Inited)
                {
                    GL.IssuePluginEvent(NxrInstantNativeApi.GetRenderEventFunc(),
                        (int) NxrInstantNativeApi.RenderEvent.SubmitFrame);
                }
#endif
            }

            bool IsHeadPoseUpdated = device.IsHeadPoseUpdated();
            if (USE_DTR && NxrGlobal.supportDtr && IsHeadPoseUpdated)
                NxrPluginEvent.IssueWithData(NibiruRenderEventType.TimeWarp, NxrViewer.Instance.GetTimeWarpViewNum());
        }

        IEnumerator EndOfFrame()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                EndOfFrameCore();
            }
        }

        public int GetFrameId()
        {
            return frameCount;
        }


        private void TriggerJoystickEvent(int index, float axisValue)
        {
            if (joystickListeners == null)
            {
                List<GameObject> allObject = GetAllObjectsInScene();
                joystickListeners = new List<INxrJoystickListener>();
                foreach (GameObject obj in allObject)
                {
                    Component[] joystickcomps = obj.GetComponents(typeof(INxrJoystickListener));

                    if (joystickcomps != null)
                    {
                        INxrJoystickListener[] listeners = new INxrJoystickListener[joystickcomps.Length];

                        for (int p = 0; p < joystickcomps.Length; p++)
                        {
                            listeners[p] = (INxrJoystickListener) joystickcomps[p];
                        }

                        // INibiruJoystickListener
                        notifyJoystickPressed(listeners, index, axisValue);
                        foreach (Component cp in joystickcomps)
                        {
                            joystickListeners.Add((INxrJoystickListener) cp);
                        }
                    }
                }
            }
            else
            {
                notifyJoystickPressed(joystickListeners.ToArray(), index, axisValue);
            }
        }

        private void notifyJoystickPressed(INxrJoystickListener[] comps, int index, float axisValue)
        {
            if (comps == null) return;
            for (int i = 0; i < comps.Length; i++)
            {
                INxrJoystickListener joystickListener = (INxrJoystickListener) comps[i];
                if (joystickListener == null) continue;
                switch (index)
                {
                    case 0:
                        // l1
                        joystickListener.OnPressL1();
                        break;
                    case 1:
                        // l2
                        joystickListener.OnPressL2();
                        break;
                    case 2:
                        // r1
                        joystickListener.OnPressR1();
                        break;
                    case 3:
                        // r2
                        joystickListener.OnPressR2();
                        break;
                    case 4:
                        // select
                        joystickListener.OnPressSelect();
                        break;
                    case 5:
                        // start
                        joystickListener.OnPressStart();
                        break;
                    case 6:
                        // x
                        joystickListener.OnPressX();
                        break;
                    case 7:
                        // y
                        joystickListener.OnPressY();
                        break;
                    case 8:
                        // a
                        joystickListener.OnPressA();
                        break;
                    case 9:
                        // b
                        joystickListener.OnPressB();
                        break;
                    case 10:
                        // leftstickx
                        joystickListener.OnLeftStickX(axisValue);
                        break;
                    case 11:
                        // leftsticky
                        joystickListener.OnLeftStickY(axisValue);
                        break;
                    case 12:
                        // rightstickx
                        joystickListener.OnRightStickX(axisValue);
                        break;
                    case 13:
                        // rightsticky
                        joystickListener.OnRightStickY(axisValue);
                        break;
                    case 14:
                        // dpad-up
                        joystickListener.OnPressDpadUp();
                        break;
                    case 15:
                        // dpad-down
                        joystickListener.OnPressDpadDown();
                        break;
                    case 16:
                        // dpad-left
                        //joystickListener.OnPressDpadLeft();
                        joystickListener.OnPressDpadRight();
                        break;
                    case 17:
                        // dpad-right
                        //joystickListener.OnPressDpadRight();
                        joystickListener.OnPressDpadLeft();
                        break;
                    case 18:
                        joystickListener.OnLeftStickDown();
                        break;
                    case 19:
                        joystickListener.OnRightStickDown();
                        break;
                }
            }
        }

        private float mFar = -1;
        private bool needUpdateNearFar = false;

        /// <summary>
        ///  Update the left and right camera's farClipPlane
        /// </summary>
        /// <param name="far"></param>
        public void UpateCameraFar(float far)
        {
            mFar = far;
            needUpdateNearFar = true;
            NxrGlobal.fovFar = far;
            if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // 编辑器及时生效
                Camera.main.farClipPlane = far;
            }
        }

        public float GetCameraFar()
        {
            return mFar;
        }

        public bool NeedUpdateNearFar
        {
            get { return needUpdateNearFar; }
            set
            {
                if (value != needUpdateNearFar)
                {
                    needUpdateNearFar = value;
                }
            }
        }


        private float oldFov = -1;

        private Matrix4x4[] eyeOriginalProjection = null;

        /// <summary>
        ///  Update eye camera projection
        /// </summary>
        /// <param name="eye"></param>
        public void UpdateEyeCameraProjection(Eye eye)
        {
            if (oldFov != -1 && eye == Eye.Right)
            {
                UpdateCameraFov(oldFov);
            }

            if (!Application.isEditor && device != null && eye == Eye.Right)
            {
                if (mFar > 0)
                {
                    float mNear = 0.0305f;
                    if (NxrGlobal.fovNear > -1)
                    {
                        mNear = NxrGlobal.fovNear;
                    }
                    // Debug.Log("new near : " + mNear + "," + NxrGlobal.fovNear+ ",new far : " + mFar + "," + NxrGlobal.fovFar);

                    // 更新camera  near far
                    float fovLeft = mNear * Mathf.Tan(-Profile.viewer.maxFOV.outer * Mathf.Deg2Rad);
                    float fovTop = mNear * Mathf.Tan(Profile.viewer.maxFOV.upper * Mathf.Deg2Rad);
                    float fovRight = mNear * Mathf.Tan(Profile.viewer.maxFOV.inner * Mathf.Deg2Rad);
                    float fovBottom = mNear * Mathf.Tan(-Profile.viewer.maxFOV.lower * Mathf.Deg2Rad);

                    //Debug.Log("fov : " +fovLeft+","+fovRight+","+fovTop+","+fovBottom);

                    Matrix4x4 eyeProjection =
                        BaseARDevice.MakeProjection(fovLeft, fovTop, fovRight, fovBottom, mNear, mFar);
                    for (int i = 0; i < 2; i++)
                    {
                        NxrEye mEye = eyes[i];
                        if (mEye != null)
                        {
                            mEye.cam.projectionMatrix = eyeProjection;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  Reset the left and right camera's fov to default.
        /// </summary>
        public void ResetCameraFov()
        {
            for (int i = 0; i < 2; i++)
            {
                if (eyeOriginalProjection == null || eyeOriginalProjection[i] == null) return;
                NxrEye eye = eyes[i];
                if (eye != null)
                {
                    eye.cam.projectionMatrix = eyeOriginalProjection[i];
                }
            }

            oldFov = -1;
        }

        /// <summary>
        ///  Change the left and right camera's fov
        ///  fov range [40~90]
        /// </summary>
        /// <param name="fov"></param>
        public void UpdateCameraFov(float fov)
        {
            if (fov > 90) fov = 90;
            if (fov < 5) fov = 5;
            // cache左右眼透视矩阵
            if (eyeOriginalProjection == null && eyes[0] != null && eyes[1] != null)
            {
                eyeOriginalProjection = new Matrix4x4[2];
                eyeOriginalProjection[0] = eyes[0].cam.projectionMatrix;
                eyeOriginalProjection[1] = eyes[1].cam.projectionMatrix;
            }

            oldFov = fov;
            float near = NxrGlobal.fovNear > 0 ? NxrGlobal.fovNear : 0.0305f;
            float far = NxrGlobal.fovFar > 0 ? NxrGlobal.fovFar : 2000;
            far = far > 100 ? far : 2000;
            float fovLeft = near * Mathf.Tan(-fov * Mathf.Deg2Rad);
            float fovTop = near * Mathf.Tan(fov * Mathf.Deg2Rad);
            float fovRight = near * Mathf.Tan(fov * Mathf.Deg2Rad);
            float fovBottom = near * Mathf.Tan(-fov * Mathf.Deg2Rad);
            Matrix4x4 eyeProjection = BaseARDevice.MakeProjection(fovLeft, fovTop, fovRight, fovBottom, near, far);
            if (device != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    NxrEye eye = eyes[i];
                    if (eye != null)
                    {
                        eye.cam.projectionMatrix = eyeProjection;
                    }
                }
            }
        }


        private float displacementCoefficient = 1.0f;

        public float DisplacementCoefficient
        {
            get { return displacementCoefficient; }
            set { displacementCoefficient = value; }
        }


        /// <summary>
        ///  Set horizontal angle range :mid is 0，left is less than 0，right is greater than 0
        /// </summary>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        public void SetHorizontalAngleRange(float minRange, float maxRange)
        {
            if (headEulerAnglesRange == null)
            {
                headEulerAnglesRange = new float[] {0, 360, 0, 360};
            }

            headEulerAnglesRange[0] = minRange + 360;
            headEulerAnglesRange[1] = maxRange;
        }

        /// <summary>
        ///  Set vertical angle range :mid is 0，up is less than 0，down is greater than 0
        /// </summary>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        public void SetVerticalAngleRange(float minRange, float maxRange)
        {
            if (headEulerAnglesRange == null)
            {
                headEulerAnglesRange = new float[] {0, 360, 0, 360};
            }

            headEulerAnglesRange[2] = minRange + 360;
            headEulerAnglesRange[3] = maxRange;
        }

        /// <summary>
        /// Remove head angle limit
        /// </summary>
        public void RemoveAngleLimit()
        {
            headEulerAnglesRange = null;
        }

        public float[] GetHeadEulerAnglesRange()
        {
            return headEulerAnglesRange;
        }

        /// <summary>
        /// open system app video player 
        /// </summary>
        /// <param name="path">video path</param>
        /// <param name="type2D3D">0=2d,1=3d mode</param>
        /// <param name="mode">0=normal,1=360,2=180,3=fullmode video type</param>
        /// <param name="decode">0=hardware,1=software decode mode</param>
        public void OpenVideoPlayer(string path, int type2D3D, int mode, int decode)
        {
            device.ShowVideoPlayer(path, type2D3D, mode, decode);
        }

        /// <summary>
        /// Get android SD Card's path（/storage/emulated/0）
        /// </summary>
        /// <returns>exp: /storage/emulated/0</returns>
        public string GetStoragePath()
        {
            return device.GetStoragePath();
        }

        /// <summary>
        /// Set the screen is always on.
        /// </summary>
        /// <param name="keep"></param>
        public void SetIsKeepScreenOn(bool keep)
        {
            device.SetIsKeepScreenOn(keep);
        }

        private float defaultIpd = -1;
        private float userIpd = -1;

        /// <summary>
        /// Change eye's ipd
        /// </summary>
        /// <param name="ipd">0.064</param>
        public void SetIpd(float ipd)
        {
            if (defaultIpd < 0)
            {
                defaultIpd = GetIpd();
            }

            Debug.Log(" Ipd : D." + defaultIpd + "/N." + ipd);
            NxrGlobal.dftProfileParams[0] = ipd; //0.063f;
            userIpd = ipd;
            device.SetIpd(ipd);
            device.UpdateScreenData();
        }

        /// <summary>
        /// Reset ipd's change
        /// </summary>
        public void ResetIpd()
        {
            if (defaultIpd < 0) return;
            SetIpd(defaultIpd);
        }

        /// <summary>
        /// Get eye's ipd
        /// </summary>
        /// <returns></returns>
        public float GetIpd()
        {
            if (userIpd > 0) return userIpd;

            return eyes[0] == null ? 0.060f : 2 * Math.Abs(eyes[0].GetComponent<Camera>().transform.localPosition.x);
        }

        public float GetUseIpd()
        {
            return userIpd;
        }

        public void ShowSystemMenuUI()
        {
            if (device != null)
            {
                //Player = new UnityPlayer();
                AndroidJavaClass Player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                //Activity = Player.currentActivity;
                AndroidJavaObject Activity = Player.GetStatic<AndroidJavaObject>("currentActivity");
                //PackageManager = Activity.getPackageManager();
                AndroidJavaObject PackageNameObj = Activity.Call<AndroidJavaObject>("getPackageName");
                string packageName = PackageNameObj.Call<string>("toString");
                Debug.Log("show_nvr_menu->" + packageName);
                device.SetSystemParameters("show_nvr_menu", packageName);
            }
        }

        public delegate void ServiceReadyUpdatedDelegate(SERVICE_TYPE serviceType, bool isConnectedSucc);

        public delegate void OnSixDofPosition(float x, float y, float z);

        public delegate void OnMarkerLoadStatus(MARKER_LOAD_STATUS status);

        /// <summary>
        /// Get the service connection status (after the service is connected, you can check the plug-in support status)
        /// </summary>
        public static ServiceReadyUpdatedDelegate serviceReadyUpdatedDelegate;

        /// <summary>
        /// hmd 6dof position' data callback
        /// </summary>
        public static OnSixDofPosition onSixDofPosition;

        public static OnMarkerLoadStatus onMarkerLoadStatus;

        /// <summary>
        /// 
        /// Call after script's Awake
        /// </summary>
        /// <returns></returns>
        public NibiruService GetNibiruService()
        {
            return device.GetNibiruService();
        }

        private GameObject eventReceiverVoice;
        private GameObject eventReceiverGesture;

        public void RegisterVoiceListener(GameObject listenerObj)
        {
            eventReceiverVoice = listenerObj;
        }

        public void RegisterGestureListener(GameObject listenerObj)
        {
            eventReceiverGesture = listenerObj;
        }

        void onHandleAndroidMsg(string msgContent)
        {
            // msgId_msgContent
            string[] msgArr = msgContent.Split('_');
            int msgId = int.Parse(msgArr[0]);
            string msgData = msgArr[1];

            // Debug.Log((eventReceiverVoice == null) + "-------->onHandleAndroidMsg=" + msgContent);
            if (msgId == (int) MSG_ID.MSG_onServiceReady)
            {
                if (serviceReadyUpdatedDelegate != null)
                {
                    SERVICE_TYPE serviceType = (SERVICE_TYPE) int.Parse(msgData);
                    bool connected = int.Parse(msgArr[2]) == 1;
                    serviceReadyUpdatedDelegate(serviceType, connected);
                }

                return;
            }

            if (msgId == (int) MSG_ID.MSG_verifyFailed)
            {
                NxrGlobal.verifyStatus = (VERIFY_STATUS) int.Parse(msgData);
                Debug.Log("verify failed");
            }
            else if (msgId == (int) MSG_ID.MSG_verifySucc)
            {
                NxrGlobal.verifyStatus = VERIFY_STATUS.SUCC;
                Debug.Log("verify succ");
            }
            else if (msgId == (int) MSG_ID.MSG_onKeyStoreException)
            {
                NxrGlobal.verifyStatus = VERIFY_STATUS.HEAD_ERROR;
                Debug.Log("verify keystore exception");
            }
            else if (msgId == (int) MSG_ID.MSG_onHeadPosition && onSixDofPosition != null)
            {
                //[-0.26371408, -0.108221784, -0.05875514]  [0] 
                string[] posStr = msgData.Substring(1, msgData.Length - 2).Split(',');
                float x = (float) Math.Round(float.Parse(posStr[0]), 2);
                float y = (float) Math.Round(float.Parse(posStr[1]), 2);
                float z = (float) Math.Round(float.Parse(posStr[2]), 2);
                // 用Loom的方法在Unity主线程中调用Text组件
                if (onSixDofPosition != null)
                {
                    onSixDofPosition(x, y, z);
                }
            }
            else if ((MSG_ID) msgId == MSG_ID.MSG_onMarkerLoadStatus && onMarkerLoadStatus != null)
            {
                if (onMarkerLoadStatus != null)
                {
                    onMarkerLoadStatus((MARKER_LOAD_STATUS) int.Parse(msgData));
                }
            }

            if ((eventReceiverGesture != null || eventReceiverVoice != null) &&
                NxrGlobal.verifyStatus == VERIFY_STATUS.SUCC)
            {
                object msgObj = null;
                if ((MSG_ID) msgId == MSG_ID.MSG_onGestureEvent)
                {
                    msgObj = (GESTURE_ID) int.Parse(msgData);
                    if (eventReceiverGesture != null)
                        eventReceiverGesture.BroadcastMessage(NxrGlobal.GetMethodNameById((MSG_ID) msgId), msgObj,
                            SendMessageOptions.DontRequireReceiver);
                }
                else if ((MSG_ID) msgId == MSG_ID.MSG_onVoiceVolume ||
                         (MSG_ID) msgId == MSG_ID.MSG_onVoiceFinishError ||
                         (MSG_ID) msgId == MSG_ID.MSG_onVoiceFinishResult)
                {
                    msgObj = msgData;
                    if ((MSG_ID) msgId == MSG_ID.MSG_onVoiceFinishError && msgArr.Length > 1)
                    {
                        // msgId_errorcode_errormsg
                        msgObj = msgArr[2];
                    }

                    if (eventReceiverVoice != null)
                        eventReceiverVoice.BroadcastMessage(NxrGlobal.GetMethodNameById((MSG_ID) msgId), msgObj,
                            SendMessageOptions.DontRequireReceiver);
                }
            }

            if ((MSG_ID) msgId == MSG_ID.MSG_onServerApiReady)
            {
                Loom.QueueOnMainThread((param) =>
                {
                    bool isReady = int.Parse((string) param) == 1;
                    if (NibiruTaskApi.serverApiReady != null)
                    {
                        NibiruTaskApi.serverApiReady(isReady);
                    }
                }, msgData);
            }
            else if ((MSG_ID) msgId == MSG_ID.MSG_onSysSleepApiReady)
            {
                Loom.QueueOnMainThread((param) =>
                {
                    bool isReady = int.Parse((string) param) == 1;
                    if (NibiruTaskApi.sysSleepApiReady != null)
                    {
                        NibiruTaskApi.sysSleepApiReady(isReady);
                    }
                }, msgData);
            }
            else if ((MSG_ID) msgId == MSG_ID.MSG_onInteractionDeviceConnectEvent)
            {
                InteractionManager.OnDeviceConnectState(msgContent);
            }
            else if ((MSG_ID) msgId == MSG_ID.MSG_onInteractionKeyEvent)
            {
                if (!TouchScreenKeyboard.visible)
                {
                    InteractionManager.OnCKeyEvent(msgContent);
                }
                else
                {
                    Triggered = false;
                }
            }
            else if ((MSG_ID) msgId == MSG_ID.MSG_onInteractionTouchEvent)
            {
                InteractionManager.OnCTouchEvent(msgContent);
            }
        }

        /// <summary>
        ///  Speech Recognition：Chinese or English
        /// </summary>
        public VOICE_LANGUAGE VoiceLanguage
        {
            get { return NxrGlobal.voiceLanguage; }
            set
            {
                if (value != NxrGlobal.voiceLanguage)
                {
                    NxrGlobal.voiceLanguage = value;
                    NibiruService nibiruService = device.GetNibiruService();
                    if (nibiruService != null)
                    {
                        nibiruService.UpdateVoiceLanguage();
                    }
                }
            }
        }

        [SerializeField] public SleepTimeoutMode sleepTimeoutMode = SleepTimeoutMode.NEVER_SLEEP;

        [SerializeField] public ControllerSupportMode controllerSupportMode = ControllerSupportMode.NONE;

        public SleepTimeoutMode SleepMode
        {
            get { return sleepTimeoutMode; }
            set
            {
                if (value != sleepTimeoutMode)
                {
                    sleepTimeoutMode = value;
                }
            }
        }

        [SerializeField] private SixDofMode sixDofMode = SixDofMode.Head_3Dof_Ctrl_6Dof;

        public SixDofMode SixDofMode
        {
            get { return sixDofMode; }
            set
            {
                if (value != sixDofMode)
                {
                    sixDofMode = value;
                }
            }
        }

        /// <summary>
        /// Get main camera
        /// </summary>
        /// <returns></returns>
        public Camera GetMainCamera()
        {
            return Controller.cam;
        }

        /// <summary>
        /// Get left eye camera
        /// </summary>
        /// <returns></returns>
        public Camera GetLeftEyeCamera()
        {
            return Controller.Eyes[(int) Eye.Left].cam;
        }

        /// <summary>
        /// Get right eye camera
        /// </summary>
        /// <returns></returns>
        public Camera GetRightEyeCamera()
        {
            return Controller.Eyes[(int) Eye.Right].cam;
        }

        /// <summary>
        /// Get main camera's quaternion
        /// </summary>
        /// <returns></returns>
        public Quaternion GetCameraQuaternion()
        {
            return GetHead().transform.rotation;
        }

        /// <summary>
        /// Get controller's quaternion
        /// </summary>
        /// <returns></returns>
        public Quaternion GetControllerQuaternion()
        {
            if (IsControllerConnect())
            {
                return NxrPlayerCtrl.Instance.mTransform.localRotation;
            }

            return Quaternion.identity;
        }

        /// <summary>
        /// Set controller's active status
        /// </summary>
        /// <param name="isActive"></param>
        public void SetControllerActive(bool isActive)
        {
            NxrPlayerCtrl.Instance.ChangeControllerDisplay(isActive);
        }

        /// <summary>
        /// Is main camera locked
        /// </summary>
        /// <returns></returns>
        public bool IsCameraLocked()
        {
            return !NxrViewer.Instance.GetHead().IsTrackRotation();
        }

        /// <summary>
        /// Is controller connect
        /// </summary>
        /// <returns></returns>
        public bool IsControllerConnect()
        {
            return InteractionManager.IsControllerConnected();
        }

        /// <summary>
        /// Lock main camera's rotation
        /// </summary>
        /// <param name="isLock"></param>
        public void LockCamera(bool isLock)
        {
            NxrHead head = NxrViewer.Instance.GetHead();
            head.SetTrackRotation(!isLock);
        }

        /// <summary>
        /// Get 3dof controller's ray start point
        /// </summary>
        /// <returns></returns>
        public Transform GetRayStartPoint()
        {
            return NxrPlayerCtrl.Instance.GetRayStartPoint();
        }

        /// <summary>
        /// Get 3dof controller's ray end point
        /// </summary>
        /// <returns></returns>
        public Transform GetRayEndPoint()
        {
            return NxrPlayerCtrl.Instance.GetRayEndPoint();
        }
    }
}