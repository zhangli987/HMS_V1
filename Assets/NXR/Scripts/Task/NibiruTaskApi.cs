using Nxr.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NibiruTask
{
    // 注意调用接口，需要使用RunOnUIThread因为unity脚本都是在渲染线程运行的
    /// <summary>
    ///  
    /// </summary>
    public class NibiruTaskApi
    {
        public static string version = "V1.0.1_20180112";
        private const string NibiruSDKClassName = "com.nibiru.lib.vr.NibiruVR";
        private const string ServiceClassName = "com.nibiru.service.NibiruService";

        private const int DeviceID = 1;

        protected static AndroidJavaObject androidActivity;
        protected static AndroidJavaClass nibiruSDKClass;
        // NibiruOSService
        protected static AndroidJavaObject nibiruOsServiceObject;

        // 声明回调函数原型，即函数委托了  
        public delegate void onSelectionResult(AndroidJavaObject task);

        public static onSelectionResult selectionCallback = null;   // 此处相当于定义函数指针了  
        public static void setSelectionCallback(onSelectionResult callback)
        {
            selectionCallback = callback;
        }

        //声明电量回调函数原型
        public delegate void onPowerChange(double task);

        public static onPowerChange powerChangeListener = null;   // 此处相当于定义函数指针了  
        public static NibiruPowerListener nibiruPowerListener;
        /// <summary>
        /// Add event listening of power change.
        /// </summary>
        /// <param name="listener"></param>
        public static void addOnPowerChangeListener(onPowerChange listener)
        {
            if(listener != null)
            {
                powerChangeListener = listener;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            nibiruPowerListener = new NibiruPowerListener();
#endif
            if (nibiruOsServiceObject != null)
            {
                nibiruOsServiceObject.Call("registerPowerChangeListener", nibiruPowerListener);
            }
        }

        /// <summary>
        /// Remove event listening of power change.
        /// </summary>
        /// <param name="listener"></param>
        public static void removeOnPowerChangeListener(onPowerChange listener)
        {
            powerChangeListener = null;
            if (nibiruOsServiceObject != null && nibiruPowerListener != null)
            {
                nibiruOsServiceObject.Call("unregisterPowerChangeListener", nibiruPowerListener);
            }
               

        }

        // 声明设置信息获取服务绑定回调函数原型  
        public delegate void onServerApiReady(bool isReady);

        public static onServerApiReady serverApiReady = null;   // 此处相当于定义函数指针了  
        public static IServerApiReadyListener serverApiReadyListener;
        public static void addOnServerApiReadyCallback(onServerApiReady callback)
        {
            serverApiReady = callback;
            // UI线程执行
            //RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
            //{
            //    serverApiReadyListener = new IServerApiReadyListener();
            //    if (nibiruOsServiceObject != null) nibiruOsServiceObject.Call("addServerApiReadyListener", serverApiReadyListener);
            //}
            //));
        }
        public static void removeOnServerApiReadyCallback(onServerApiReady callback)
        {
            serverApiReady = null;
            // if (nibiruOsServiceObject != null) nibiruOsServiceObject.Call("removeServerApiReadyListener", serverApiReadyListener);
        }

        // 声明休眠时间服务绑定回调函数原型  
        public delegate void onSysSleepApiReady(bool isReady);

        public static onSysSleepApiReady sysSleepApiReady = null;   // 此处相当于定义函数指针了  
        public static ISysSleepApiReadyListener sysSleepApiReadyListener;
        public static void addOnSysSleepApiReadyCallback(onSysSleepApiReady callback)
        {
            sysSleepApiReady = callback;

            //RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
            //{
            //    sysSleepApiReadyListener = new ISysSleepApiReadyListener();
            //    if (nibiruOsServiceObject != null) nibiruOsServiceObject.Call("addSysSleepApiReadyListener", sysSleepApiReadyListener);
            //}
            //));
        }
        public static void removeOnSysSleepApiReadyCallback(onSysSleepApiReady callback)
        {
            sysSleepApiReady = null;
            // if (nibiruOsServiceObject != null) nibiruOsServiceObject.Call("removeSysSleepApiReadyListener", sysSleepApiReadyListener);
        }

        public delegate void onDeviceConnectState(int state, CDevice device);

        public static onDeviceConnectState deviceConnectState = null;   // 此处相当于定义函数指针了  
        public static void setOnDeviceListener(onDeviceConnectState listener)
        {
            deviceConnectState = listener;
            ControllerAndroid.setOnDeviceListener(new OnDeviceListener());
        }

        public static void Init()
        {
            if (nibiruOsServiceObject != null) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("-------NibiruTaskLib-------Version-------" + version);
            ConnectToActivity();
            nibiruSDKClass = GetClass(NibiruSDKClassName);
            nibiruOsServiceObject = nibiruSDKClass.CallStatic<AndroidJavaObject>("getNibiruOSService", androidActivity);

            ControllerAndroid.onStart();
#endif
        }

        /// <summary>
        /// Check whether the plugin such as Record and Marker is declared.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsPluginDeclared(PLUGIN_ID id)
        {
            if (nibiruSDKClass == null)
            {
                Init();
            }

            if (nibiruSDKClass == null) return false;
            return nibiruSDKClass.CallStatic<bool>("isPluginDeclared", (int)id);
        }

        /// <summary>
        /// Check whether the plugin such as Record and Marker is supported.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsPluginSupported(PLUGIN_ID id)
        {
            if (nibiruSDKClass == null)
            {
                Init();
            }
            if (nibiruSDKClass == null) return false;
            return nibiruSDKClass.CallStatic<bool>("isPluginSupported", (int)id);
        }

        /// <summary>
        /// Launch app by package name
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        public static bool LaunchAppByPkgName(string pkgName)
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject != null)
            {
                return nibiruOsServiceObject.Call<bool>("launchAppByPkgName", pkgName);
            }

            return false;
        }

        /// <summary>
        /// Get device's id
        /// </summary>
        /// <returns></returns>
        public static string GetDeviceId()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject != null)
            {
                return nibiruOsServiceObject.Call<string>("getDeviceId");
            }
            return "null";
        }

        /// <summary>
        /// Get device's mac address
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddress()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject != null)
            {
                return nibiruOsServiceObject.Call<string>("getMacAddress");
            }
            return "null";
        }

        /// <summary>
        /// Get device's bluetooth status
        /// </summary>
        /// <returns></returns>
        public static int GetBluetoothStatus()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruSDKClass != null)
            {
                return nibiruSDKClass.CallStatic<int>("getBluetoothStatus");
            }
            return 0;
        }

        /// <summary>
        /// Get device's network status
        /// </summary>
        /// <returns></returns>
        public static int GetNetworkStatus()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruSDKClass != null)
            {
                return nibiruSDKClass.CallStatic<int>("getNetworkStatus");
            }
            return 0;
        }

        public static void Destroy()
        {
            ControllerAndroid.onStop();
        }

        /// <summary>
        /// Quick interface.Open peripheral driver
        /// </summary>
        public static void OpenDeviceDriver()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.DEVICE_DRIVER);
            StartNibiruTask(task);
        }
        //打开视频播放器 参数详细说明参考Video类
        /// <summary>
        /// Open system's VideoPlayer to play video
        /// </summary>
        /// <param name="path"></param>
        /// <param name="loop"></param>
        /// <param name="decode"></param>
        /// <param name="mode"></param>
        /// <param name="type"></param>
        public static void OpenVideoPlayer(string path, int loop = Video.VIDEO_KEY_LOOP_ON, int decode = Video.VIDEO_PARAMETERS_DECODE_HARDWARE, int mode = Video.VIDEO_PARAMETERS_MODE_NORMAL, int type = Video.VIDEO_PARAMETERS_TYPE_2D)
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.VIDEO_PLAY);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_CONTROL, Video.VIDEO_CONTROL_START);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_LOOP, loop);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_PARAMETERS_DECODE, decode);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_PARAMETERS_MODE, mode);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_PARAMETERS_TYPE, type);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_PATH, path);
            StartNibiruTask(task);
        }
        //暂时视频播放，一般是通过服务对当前运行的播放器操作
        public static void PauseVideoPlayer()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.VIDEO_PLAY);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_CONTROL, Video.VIDEO_CONTROL_PAUSE);
            StartNibiruTask(task);
        }
        //恢复视频播放
        public static void ResumeVideoPlayer()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.VIDEO_PLAY);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_CONTROL, Video.VIDEO_CONTROL_RESUME);
            StartNibiruTask(task);
        }
        //关闭视频播放器
        public static void CloseVideoPlayer()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.VIDEO_PLAY);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_CONTROL, Video.VIDEO_CONTROL_CLOSE);
            StartNibiruTask(task);
        }
        //播放器快进快退
        public static void VideoPlayerSeekto(long time)
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.VIDEO_PLAY);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_CONTROL, Video.VIDEO_CONTROL_SEEKTO);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_SEEKTO_TIME, time);
            StartNibiruTask(task);
        }
        //控制播放条显示
        public static void VideoPlayerShowORHideController(string state)
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.VIDEO_PLAY);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_CONTROL, Video.VIDEO_KEY_CONTROLLER);
            AddNibiruTaskParameter(task, Video.VIDEO_KEY_SEEKTO_TIME, state);
            StartNibiruTask(task);
        }
        //打开文件管理器 以路径的方式
        /// <summary>
        /// Open file explorer by path
        /// </summary>
        /// <param name="path"></param>
        public static void OpenFileExplorer(string path)
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.OPEN_FILE);
            AddNibiruTaskParameter(task, File.OPEN_FILE_KEY_PATH, path);
            StartNibiruTask(task);
        }
        //打开文件管理器 以文件类型的格式
        /// <summary>
        /// Open file explorer by file type
        /// </summary>
        /// <param name="type"></param>
        public static void OpenFileExplorer(int type)
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.OPEN_FILE);
            AddNibiruTaskParameter(task, File.OPEN_FILE_KEY_TYPE, type);
            StartNibiruTask(task);
        }

        //打开图库
        /// <summary>
        /// Open image gallery
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        public static void OpenImageGallery(string path, int type = Gallery.SHOW_IMAGE_KEY_2D)
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.SHOW_IMAGE);
            AddNibiruTaskParameter(task, Gallery.SHOW_IMAGE_KEY_PATH, path);
            AddNibiruTaskParameter(task, Gallery.SHOW_IMAGE_KEY_TYPE, type);
            StartNibiruTask(task);
        }

        //打开设置WIFI
        /// <summary>
        /// Open settings wifi scene
        /// </summary>
        public static void OpenSettingsWifi()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.SETTINGS);
            AddNibiruTaskParameter(task, Setting.SETTINGS_KEY_TYPE, Setting.SETTINGS_TYPE_WIFI);
            StartNibiruTask(task);
        }

        //打开设置蓝牙
        /// <summary>
        /// Open settings bluetooth scene
        /// </summary>
        public static void OpenSettingsBluetooth()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.SETTINGS);
            AddNibiruTaskParameter(task, Setting.SETTINGS_KEY_TYPE, Setting.SETTINGS_TYPE_BLUETOOTH);
            StartNibiruTask(task);
        }

        //打开设置系统界面
        /// <summary>
        /// Open settings system scene
        /// </summary>
        public static void OpenSettingsSystem()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.SETTINGS);
            AddNibiruTaskParameter(task, Setting.SETTINGS_KEY_TYPE, Setting.SETTINGS_TYPE_SYSTEM);
            StartNibiruTask(task);
        }

        //打开设置通用界面
        /// <summary>
        /// Open settings g eneral scene
        /// </summary>
        public static void OpenSettingsGeneral()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.SETTINGS);
            AddNibiruTaskParameter(task, Setting.SETTINGS_KEY_TYPE, Setting.SETTINGS_TYPE_GENERAL);
            StartNibiruTask(task);
        }

        //打开设置主界面
        /// <summary>
        /// Open settings main scene
        /// </summary>
        public static void OpenSettingsMain()
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.SETTINGS);
            AddNibiruTaskParameter(task, Setting.SETTINGS_KEY_TYPE, Setting.SETTINGS_TYPE_MAIN);
            StartNibiruTask(task);
        }

        //打开浏览器
        /// <summary>
        /// Open browser explorer
        /// </summary>
        /// <param name="url"></param>
        /// <param name="actionBarState"></param>
        public static void OpenBrowerExplorer(string url, string actionBarState = Brower.EXPLORER_KEY_ACTIONBAR_SHOW)
        {
            AndroidJavaObject task = NibiruTaskApi.GetNibiruTask(TASK_ACTION.EXPLORER);
            AddNibiruTaskParameter(task, Brower.EXPLORER_KEY_URL, url);
            AddNibiruTaskParameter(task, Brower.EXPLORER_KEY_ACTIONBAR, actionBarState);
            StartNibiruTask(task);
        }

        //获取文件路径
        /// <summary>
        /// Get file's path
        /// </summary>
        /// <param name="basePath"></param>
        public static void GetFilePath(string basePath)
        {
            Debug.Log("GetFilePath:" + basePath);
            AndroidJavaObject task = NibiruTaskApi.GetNibiruSelectionTask(SELECTION_TASK_ACTION.FILE);
            AddNibiruTaskParameter(task, File.OPEN_FILE_KEY_PATH, basePath);
            StartNibiruTask(task);
        }

        public static void StartNibiruTask(AndroidJavaObject task)
        {
            if (task == null) return;
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            // UI线程执行
            RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
            {
                nibiruOsServiceObject.Call<bool>("startNibiruTask", task);
            }
            ));
        }

        public static AndroidJavaObject GetNibiruTask(TASK_ACTION action)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Create("com.nibiru.service.NibiruTask", (int)action);
#else
            return null;
#endif
        }

        public static AndroidJavaObject GetNibiruSelectionTask(SELECTION_TASK_ACTION action)
        {
            AndroidJavaObject task = Create("com.nibiru.service.NibiruSelectionTask", (int)action);
            task.Call("setCallback", new NibiruSelectionCallback());
            return task;
        }

        public static void AddNibiruTaskParameter(AndroidJavaObject task, string key, string value)
        {
            if (task == null) return;
            task.Call("addParameter", key, value);
        }

        public static void AddNibiruTaskParameter(AndroidJavaObject task, string key, int value)
        {
            if (task == null) return;
            task.Call("addParameter", key, value);
        }

        public static void AddNibiruTaskParameter(AndroidJavaObject task, string key, long value)
        {
            if (task == null) return;
            task.Call("addParameter", key, value);
        }
        //获取文件管理器返回文件路径
        public static string GetResultValueFromSelectionTask(AndroidJavaObject selectionTask)
        {
            //GetResultCodeFromSelectionTask(selectionTask);
            return GetResultPathFromSelectionTask(selectionTask);
        }
        //获取文件管理器返回Code
        public static int GetResultCodeFromSelectionTask(AndroidJavaObject selectionTask)
        {
            // AndroidJavaObject hashMapObject = selectionTask.Call<AndroidJavaObject>("getResultValue");
            AndroidJavaObject SelectionResult = selectionTask.Call<AndroidJavaObject>("getResultCode");
            return SelectionResult.Call<int>("ordinal");
        }
        //获取文件管理器返回Path
        public static string GetResultPathFromSelectionTask(AndroidJavaObject selectionTask)
        {
            AndroidJavaObject hashMapObject = selectionTask.Call<AndroidJavaObject>("getResultValue");
            return hashMapObject.Call<string>("get", File.FILE_KEY_SELECTION_RESULT);
        }

        /// <summary>
        ///  Get device's channel code
        /// </summary>
        /// <returns></returns>
        public static string GetChannelCode()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return "null";
            }
            return nibiruOsServiceObject == null ? "null" : nibiruOsServiceObject.Call<string>("getChannelCode");
        }

        /// <summary>
        /// Get devices's model
        /// </summary>
        /// <returns></returns>
        public static string GetModel()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return "null";
            }
            return nibiruOsServiceObject == null ? "null" : nibiruOsServiceObject.Call<string>("getModel");
        }

        /// <summary>
        /// Get devices's os version
        /// </summary>
        /// <returns></returns>
        public static string GetOSVersion()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return "null";
            }
            return nibiruOsServiceObject == null ? "null" : nibiruOsServiceObject.Call<string>("getOSVersion");
        }

        /// <summary>
        /// Get devices's os version code
        /// </summary>
        /// <returns></returns>
        public static int GetOSVersionCode()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return -1;
            }
            return nibiruOsServiceObject == null ? -1 : nibiruOsServiceObject.Call<int>("getOSVersionCode");
        }
 
        /// <summary>
        /// Get device's service version code
        /// </summary>
        /// <returns></returns>
        public static string GetServiceVersionCode()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }

            if (nibiruOsServiceObject == null)
            {
                return "null";
            }
            return nibiruOsServiceObject == null ? "null" : nibiruOsServiceObject.Call<string>("getServiceVersionCode");
        }

        /// <summary>
        /// Get vendor sw version 
        /// </summary>
        /// <returns></returns>
        public static string GetVendorSWVersion()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return "null";
            }
            return nibiruOsServiceObject == null ? "null" : nibiruOsServiceObject.Call<string>("getVendorSWVersion");
        }
    
        /// <summary>
        ///  Get brightness value
        /// </summary>
        /// <returns></returns>
        public static int GetBrightnessValue()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return 0;
            }
            return nibiruOsServiceObject == null ? 0 : nibiruOsServiceObject.Call<int>("getBrightnessValue");
        }

        /// <summary>
        /// Set brightness value
        /// </summary>
        /// <param name="value"></param>
        public static void SetBrightnessValue(int value)
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            nibiruOsServiceObject.Call("setBrightnessValue", value);
        }

        //4.3 获取当前2D/3D显示模式：
        public static DISPLAY_MODE GetDisplayMode()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null) return DISPLAY_MODE.MODE_2D;
            AndroidJavaObject androidObject = nibiruOsServiceObject.Call<AndroidJavaObject>("getDisplayMode");
            int mode = androidObject.Call<int>("ordinal");
            return (DISPLAY_MODE)mode;
        }

        //4.4 切换2D/3D显示模式:
        /// <summary>
        /// Set display mode
        /// </summary>
        /// <param name="displayMode"></param>
        public static void SetDisplayMode(DISPLAY_MODE displayMode)
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject != null)
            {
                RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
                {
                    nibiruOsServiceObject.Call("setDisplayMode", (int)displayMode);
                }));
            }
        }

        /// <summary>
        /// Get system sleep time
        /// </summary>
        /// <returns></returns>
        public static int GetSysSleepTime()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return 0;
            }
            return nibiruOsServiceObject.Call<int>("getSysSleepTime");
        }

#if NIBIRU_VR
        //以下获取设置参数功能onServerApiReady回调绑定服务之后调用
        //获取当前主题 对应Android 自定义类型 ThemeApiData 
        //局部变量：
        //      private String themeName;主题名称
        //      private String themeSign;主题标志
        //      private String themeIcon;主题icon
        //获取方法： 
        //    public String getThemeName()
        //    public String getThemeSign()
        //    public String getThemeIcon()
        /// <summary>
        /// Get current system's theme
        /// </summary>
        /// <returns></returns>
        public static ThemeApiData GetCurrentTheme()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            IntPtr tptr = nibiruOsServiceObject.Call<IntPtr>("getCurrentTheme");
            if (tptr == IntPtr.Zero) return null;
            AndroidJavaObject theme = nibiruOsServiceObject.Call<AndroidJavaObject>("getCurrentTheme");
            if (theme == null) return null;
            return new ThemeApiData(theme.Call<string>("getThemeName"), theme.Call<string>("getThemeSign"), theme.Call<string>("getThemeIcon"));
        }

        /// <summary>
        /// Get system's themeList
        /// </summary>
        /// <returns></returns>
        public static List<ThemeApiData> GetThemeList()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            List<ThemeApiData> themeList = new List<ThemeApiData>();
            AndroidJavaObject themes = nibiruOsServiceObject.Call<AndroidJavaObject>("getThemeList");
            if (themes != null)
            {
                int themeListSize = themes.Call<int>("size");
                for (int i = 0; i < themeListSize; i++)
                {
                    AndroidJavaObject theme = themes.Call<AndroidJavaObject>("get", i);
                    themeList.Add(new ThemeApiData(theme.Call<string>("getThemeName"), theme.Call<string>("getThemeSign"), theme.Call<string>("getThemeIcon")));
                }
            }

            return themeList;
        }
#endif

        /// <summary>
        /// Get current system's timezone
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentTimezone()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if(nibiruOsServiceObject == null)
            {
                return "null";
            }
            return nibiruOsServiceObject.Call<string>("getCurrentTimezone");
        }

        /// <summary>
        /// Get current system's language list
        /// </summary>
        /// <returns></returns>
        public static List<string> GetLanguageList()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            List<string> languageList = new List<string>();
            AndroidJavaObject languages = nibiruOsServiceObject.Call<AndroidJavaObject>("getLanguageList");
            if (languages != null)
            {
                int size = languages.Call<int>("size");
                for (int i = 0; i < size; i++)
                {
                    languageList.Add(languages.Call<string>("get", i));
                }
            }
            return languageList;

        }

        /// <summary>
        /// Get current system's language
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentLanguage()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }

            if (nibiruOsServiceObject == null) return "null";
            return nibiruOsServiceObject.Call<string>("getCurrentLanguage");
        }

        /// <summary>
        /// Get system's vr version
        /// </summary>
        /// <returns></returns>
        public static string GetVRVersion()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return "null";
            }
            return nibiruOsServiceObject.Call<string>("getVRVersion");
        }

        /// <summary>
        /// Get device's name
        /// </summary>
        /// <returns></returns>
        public static string GetDeviceName()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            if (nibiruOsServiceObject == null)
            {
                return "null";
            }
            return nibiruOsServiceObject.Call<string>("getDeviceName");
        }

        /// <summary>
        /// Open Wps
        /// </summary>
        public static void OpenWps()
        {
            if (nibiruOsServiceObject == null)
            {
                Init();
            }
            nibiruOsServiceObject.Call("openWps");
        }


        //获取外设相关信息
        //获取按键状态
        //返回值：keystate 一维数组，数组下标即为按键键值，对应的值则为按键状态
        //所有按键键值定义请参考CKeyEvent中的静态常量定义
        //例子：int action = keystate[CKeyEvent.KEYCODE_DPAD_UP];
        //action表示KEYCODE_DPAD_UP 的状态，如果为CKeyEvent.ACTION_DOWN表 示按下状态，否则为弹起状态。

        public static int[] GetKeyAction(int deviceId = DeviceID)
        {
            return ControllerAndroid.getKeyAction(deviceId);
        }

        //获取支持的外设设备名称
        public static List<string> getCSupportDevices()
        {
            List<string> deviceList = new List<string>();
            AndroidJavaObject devices = ControllerAndroid.getCSupportDevices();
            if (devices != null)
            {
                int size = devices.Call<int>("size");
                for (int i = 0; i < size; i++)
                {
                    deviceList.Add(devices.Call<string>("get", i));
                }
            }
            return deviceList;
        }
        //获取自行车数据
        public static CBikeEvent GetCBikeEvent(int deviceId = DeviceID)
        {
            AndroidJavaObject bikeEvent = ControllerAndroid.getCBikeEvent(deviceId);
            if (bikeEvent == null)
                return null;
            return new CBikeEvent(bikeEvent.Call<int>("getDeviceId"), bikeEvent.Call<int>("getAngle"), bikeEvent.Call<long>("getEventTime"));
        }
        //获取Sensor数据
        public static CSensorEvent GetCSensorEvent(int type, int deviceId = DeviceID)
        {
            AndroidJavaObject sensorEvent = ControllerAndroid.getCSensorEvent(type, deviceId);
            if (sensorEvent == null)
                return null;
            return new CSensorEvent(sensorEvent.Call<int>("getType"), sensorEvent.Call<int>("getDeviceId"), sensorEvent.Call<long>("getEventTime"), sensorEvent.Call<float[]>("getValues"));
        }
        //连接设备
        public static void Connect(AndroidJavaObject bluetoothDevice)
        {
            ControllerAndroid.connect(bluetoothDevice);
        }
        //断开设备连接
        public static void Disconnect(AndroidJavaObject bluetoothDevice)
        {
            ControllerAndroid.disconnect(bluetoothDevice);
        }
        //获取当前连接设备列表
        public static List<CDevice> GetCDevices()
        {
            List<CDevice> deviceList = new List<CDevice>();
            AndroidJavaObject devices = ControllerAndroid.getCDevices();
            if (devices != null)
            {
                int size = devices.Call<int>("size");
                for (int i = 0; i < size; i++)
                {
                    AndroidJavaObject device = devices.Call<AndroidJavaObject>("get", i);
                    if (device != null)
                    {
                        AndroidJavaObject usbDevice = device.Call<AndroidJavaObject>("getUdevice");
                        AndroidJavaObject bluetoothDevice = device.Call<AndroidJavaObject>("getBdevice");
                        if (usbDevice != null)
                        {
                            deviceList.Add(new CDevice(usbDevice, device.Call<bool>("isQuat"), device.Call<int>("getType")));
                        }
                        else if (bluetoothDevice != null)
                        {
                            deviceList.Add(new CDevice(bluetoothDevice, device.Call<bool>("isQuat"), device.Call<int>("getType"), device.Call<int>("getMode")));
                        }
                        else
                        {
                            deviceList.Add(new CDevice(device.Call<string>("getName"), device.Call<bool>("isQuat"), device.Call<int>("getType"), device.Call<int>("getMode")));
                        }
                    }
                }
            }
            return deviceList;
        }
        //获取左右手模式 0:right, 1:left
        public static int GetHandMode()
        {
            return ControllerAndroid.getHandMode();
        }

        //获取遥感数据
        //返回值4维数组
        //遥感分为左摇杆(x轴) ，左摇杆(y轴)，右摇杆（x轴），右摇杆（y轴）
        //分别对应x轴，y轴，z轴，rz轴， 遥感取值范围为-1~1,遥感在中间时为0
        public static float[] GetMotion(int deviceId = DeviceID)
        {
            return ControllerAndroid.getMotion(deviceId);
        }
        //获取四元素数据 返回值为四元素数据4维数组，对应关系为
        //x = quat[0] y = quat[1] z = quat[2] w = quat[3] 四元素包含 x, y, z, w 四个轴
        public static float[] GetQuat(int deviceId = DeviceID)
        {
            return ControllerAndroid.getQuat(deviceId);
        }
        //获取脑电波状态
        //返回值 3维数组 为脑电波数据，对应关系为
        //poorSignal = brain[0] attention = brain[1] mediation = brain[1]
        //脑电波包含集中度，放松度及信号量
        //信号量 poorSignal:int 类型，0 连接正常，200 为未连接（可能佩戴不正确），其他非0 数字表示信号较弱
        //集中度 attention:int 类型，0~100，数值越高集中度越高
        //放松度 mediation:int 类型，0~100，数值越高放松度越高
        public static int[] GetBrain(int deviceId = DeviceID)
        {
            return ControllerAndroid.getBrain(deviceId);
        }
        //获取眨眼数据 返回值int 类型，0~100，数值越大眨眼力度越大
        public static int GetBlink(int deviceId = DeviceID)
        {
            return ControllerAndroid.getBlink(deviceId);
        }
        //获取手势识别状态 返回值int 类型，具体定义在 CGestureEvent 静态常量中，例如：CGestureEvent.GESTURE_SLIP_UP
        public static int GetGesture(int deviceId = DeviceID)
        {
            return ControllerAndroid.getGesture(deviceId);
        }
        //获取 Touch 事件数据 返回值4维数组，对应关系为 x = touch[2] , y = touch[3]
        public static float[] GetTouch(int deviceId = DeviceID)
        {
            return ControllerAndroid.getTouch(deviceId);
        }
        //获取所有连接设备 deviceId
        public static int[] GetDeviceIds()
        {
            return ControllerAndroid.getDeviceIds();
        }
        //判断当前是否有支持四元素的设备连接
        public static bool IsQuatConn()
        {
            return ControllerAndroid.isQuatConn();
        }
        //获取当前连接设备类型 返回值int 型数据，具体参考 CDevice 中静态常量，如CDevice.DEVICE_NINE_GAMEPAD
        public static int GetDeviceType()
        {
            return ControllerAndroid.getDeviceType();
        }

        //设置遥感转键值或者 Touch 转键值,参数值motionType 在 CMotionType 类中定义,如 CMotionType.REPORT_KEY
        public static void SetMotionType(int motionType)
        {
            ControllerAndroid.setMotionType(motionType);
        }
        /// <summary>
        ///  Unity->Android
        /// </summary>

        protected static void ConnectToActivity()
        {
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
            }
        }

        public static AndroidJavaClass GetClass(string className)
        {
            try
            {
                return new AndroidJavaClass(className);
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception getting class " + className + ": " + e);
                return null;
            }
        }

        public static AndroidJavaObject Create(string className, params object[] args)
        {
            try
            {
                return new AndroidJavaObject(className, args);
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception creating object " + className + ": " + e);
                return null;
            }
        }

        public static bool CallStaticMethod(AndroidJavaObject jo, string name, params object[] args)
        {
            if (jo == null)
            {
                Debug.LogError("Object is null when calling static method " + name);
                return false;
            }
            try
            {
                jo.CallStatic(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception calling static method " + name + ": " + e);
                return false;
            }
        }

        public static bool CallObjectMethod(AndroidJavaObject jo, string name, params object[] args)
        {
            if (jo == null)
            {
                Debug.LogError("Object is null when calling method " + name);
                return false;
            }
            try
            {
                jo.Call(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception calling method " + name + ": " + e);
                return false;
            }
        }

        public static bool CallStaticMethod<T>(ref T result, AndroidJavaObject jo, string name,
                                                  params object[] args)
        {
            if (jo == null)
            {
                Debug.LogError("Object is null when calling static method " + name);
                return false;
            }
            try
            {
                result = jo.CallStatic<T>(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception calling static method " + name + ": " + e);
                return false;
            }
        }

        // UI线程中运行
        public static void RunOnUIThread(AndroidJavaObject activityObj, AndroidJavaRunnable r)
        {
            activityObj.Call("runOnUiThread", r);
        }

    }

    /// <summary>
    /// The interface of Callback.
    /// </summary>
    public class NibiruSelectionCallback : AndroidJavaProxy
    {
        // 回调接口
        public NibiruSelectionCallback() : base("com.nibiru.service.NibiruSelectionTask$NibiruSelectionCallback")
        {

        }

        public void onSelectionResult(AndroidJavaObject task)
        {
            Debug.Log("onSelectionResult");
            if (NibiruTaskApi.selectionCallback != null)
            {
                NibiruTaskApi.selectionCallback(task);
            }
            //NibiruTaskApi.GetResultValueFromSelectionTask(task);
        }

    }

    public class NibiruPowerListener : AndroidJavaProxy
    {
        public NibiruPowerListener() : base("com.nibiru.service.NibiruOSService$INibiruPowerChangeListener")
        {

        }

        public void onPowerChanged(double power)
        {
            Debug.Log("onPowerChange:" + power);
            if (NibiruTaskApi.powerChangeListener != null)
            {
                NibiruTaskApi.powerChangeListener(power);
            }
        }

#if UNITY_2017_4_OR_NEWER
#else
        public bool equals(AndroidJavaObject obj)
        {
            Debug.Log("onEqual:" + base.Equals(obj));
            //return base.Equals(obj);
            return true;
        }
#endif
    }

    public class IServerApiReadyListener : AndroidJavaProxy
    {
        public IServerApiReadyListener() : base("com.nibiru.service.NibiruOSService$IServerApiReadyListener")
        {

        }

        public void onServerApiReady(bool isReady)
        {
            Debug.Log("onServerApiReady:" + isReady);
            if (NibiruTaskApi.serverApiReady != null)
            {
                NibiruTaskApi.serverApiReady(isReady);
            }
        }

    }

    public class ISysSleepApiReadyListener : AndroidJavaProxy
    {
        public ISysSleepApiReadyListener() : base("com.nibiru.service.NibiruOSService$ISysSleepApiReadyListener")
        {

        }

        public void onSysSleepApiReady(bool isReady)
        {
            Debug.Log("onSysSleepApiReady:" + isReady);
            if (NibiruTaskApi.sysSleepApiReady != null)
            {
                NibiruTaskApi.sysSleepApiReady(isReady);
            }
        }

    }

    //连接状态变化回调 state值参考啊ConnectState类
    public class OnDeviceListener : AndroidJavaProxy
    {
        public OnDeviceListener() : base("ruiyue.controller.sdk.IIControllerService$OnDeviceListener")
        {

        }

        public void onDeviceConnectState(int state, AndroidJavaObject device)
        {
            Debug.Log("NibiruTaskApi.onDeviceConnectState:" + state + "(0=connect,1=disconnect)");
            if (!InteractionManager.IsInteractionSDKEnabled())
            {
                NxrSDKApi.Instance.ExecuteControllerStatusChangeEvent(InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT, state == 0, false);
            }
            CDevice cDevice = null;
            if (device != null)
            {
                IntPtr usbDevicePtr = device.Call<IntPtr>("getUdevice");
                IntPtr bluetoothDevicePtr = device.Call<IntPtr>("getBdevice");
                AndroidJavaObject usbDevice = usbDevicePtr == IntPtr.Zero ? null : device.Call<AndroidJavaObject>("getUdevice");
                AndroidJavaObject bluetoothDevice = bluetoothDevicePtr == IntPtr.Zero ? null : device.Call<AndroidJavaObject>("getBdevice");
                if (usbDevice != null)
                {
                    cDevice = new CDevice(usbDevice, device.Call<bool>("isQuat"), device.Call<int>("getType"));
                }
                else if (bluetoothDevice != null)
                {
                    cDevice = new CDevice(bluetoothDevice, device.Call<bool>("isQuat"), device.Call<int>("getType"), device.Call<int>("getMode"));
                }
                else
                {
                    cDevice = new CDevice(device.Call<string>("getName"), device.Call<bool>("isQuat"), device.Call<int>("getType"), device.Call<int>("getMode"));
                }
            }
            if (NibiruTaskApi.deviceConnectState != null)
            {
                NibiruTaskApi.deviceConnectState(state, cDevice);
            }
        }
    }
}
