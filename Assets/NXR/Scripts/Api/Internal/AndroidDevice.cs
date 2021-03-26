// Copyright 2016 Nibiru. All rights reserved.
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
#if UNITY_ANDROID

using UnityEngine;

/// @cond
namespace Nxr.Internal
{
    public class AndroidDevice : NxrDevice
    {
        // 
        private const string ActivityListenerClass =
            "com.nibiru.lib.xr.unity.NibiruVRUnityService";

        // sdk-class
        private const string NibiruVRClass = "com.nibiru.lib.vr.NibiruVR";

        private static AndroidJavaObject activityListener, nibiruVR;

        AndroidJavaObject nibiruVRService = null;

        public override void Init()
        {
            SetApplicationState();

            ConnectToActivity();
            base.Init();
        }

        protected override void ConnectToActivity()
        {
            base.ConnectToActivity();
            if (androidActivity != null && activityListener == null)
            {
                activityListener = Create(ActivityListenerClass);
            }
            if (androidActivity != null && nibiruVR == null)
            {
                nibiruVR = Create(NibiruVRClass);
            }
        }

        public override void TurnOff()
        {
            CallStaticMethod(activityListener, "shutdownBroadcast");
        }

        public override void Reboot()
        {
            CallStaticMethod(activityListener, "rebootBroadcast");
        }

        public override long CreateNibiruVRService()
        {
            string hmdType = "NONE";
            CallStaticMethod(ref hmdType, nibiruVR, "getMetaData", androidActivity, "HMD_TYPE");
            if (hmdType != null)
            {
                NxrViewer.Instance.HmdType = hmdType.Equals("AR") ? HMD_TYPE.AR : (hmdType.Equals("VR") ? HMD_TYPE.VR : HMD_TYPE.NONE);
            }

            string initParams = "";
            long pointer = 0;
            CallStaticMethod(ref initParams, nibiruVR, "initNibiruVRServiceForUnity", androidActivity);
            // -1207076736_0_1_1_1_20.0_20.0
            Debug.Log("initParams is " + initParams + ",hmdType is " + hmdType);
            string[] data = initParams.Split('_');
            pointer = long.Parse(data[0]);
            NxrGlobal.supportDtr = (int.Parse(data[1]) == 1 ? true : false);
            NxrGlobal.distortionEnabled = (int.Parse(data[2]) == 1 ? true : false);
            NxrGlobal.useNvrSo = (int.Parse(data[3]) == 1 ? true : false);
            if (data.Length >= 5)
            {
                NxrGlobal.offaxisDistortionEnabled = (int.Parse(data[4]) == 1 ? true : false);
            }
            // 6dof
            if (NxrViewer.Instance.TrackerPosition)
            {
                CallStaticMethod(nibiruVR, "setTrackingModeForUnity", (int)TRACKING_MODE.POSITION);
            }


            int meshSizeX = -1;
            if (data.Length >= 6)
            {
                meshSizeX = (int)float.Parse(data[5]);
            }

            int meshSizeY = -1;
            if (data.Length >= 7)
            {
                meshSizeY = (int)float.Parse(data[6]);
            }

            if (data.Length >= 8)
            {
                float fps = float.Parse(data[7]);
                // 防止从系统获取的刷新率出现异常，此处保证最低为60
                NxrGlobal.refreshRate = Mathf.Max(60, fps > 0 ? fps : 0);
            }

            if (meshSizeX > 0 && meshSizeY > 0)
            {
                NxrGlobal.meshSize = new int[] { meshSizeX, meshSizeY };
            }

            string channelCode = "";
            CallStaticMethod<string>(ref channelCode, nibiruVR, "getChannelCode");
            NxrGlobal.channelCode = channelCode;

            // 系统支持
            int[] allVersion = new int[] { -1, -1, -1, -1 };
            CallStaticMethod(ref allVersion, nibiruVR, "getVersionForUnity");
            NxrGlobal.soVersion = allVersion[0];
            NxrGlobal.jarVersion = allVersion[1];
            NxrGlobal.platPerformanceLevel = allVersion[2];
            NxrGlobal.platformID = allVersion[3];
            NxrSDKApi.Instance.IsSptMultiThreadedRendering = NxrGlobal.soVersion >= 414;
            NxrGlobal.isVR9Platform = NxrGlobal.platformID == (int)PLATFORM.PLATFORM_VR9;
            if (NxrGlobal.isVR9Platform)
            {
                NxrGlobal.distortionEnabled = false;
                NxrGlobal.supportDtr = true;
                NxrViewer.Instance.SwitchControllerMode(false);
            }

            if(!NxrSDKApi.Instance.IsSptMultiThreadedRendering && SystemInfo.graphicsMultiThreaded)
            {
                AndroidLog("*****Warning******\n\n System Does Not Support Unity MultiThreadedRendering !!! \n\n*****Warning******");
                AndroidLog("Support Unity MultiThreadedRendering Need V2 Version >=414, Currently Is " + NxrGlobal.soVersion + " !!!");
            }

            Debug.Log("AndDev->Service : [pointer]=" + pointer + ", [dtrSpt] =" + NxrGlobal.supportDtr + ", [DistEnabled]=" +
            NxrGlobal.distortionEnabled + ", [useNvrSo]=" + NxrGlobal.useNvrSo + ", [code]=" + channelCode + ", [jar]=" + NxrGlobal.jarVersion + ", [so]=" + NxrGlobal.soVersion
            + ", [platform id]=" + NxrGlobal.platformID + ", [pl]=" + NxrGlobal.platPerformanceLevel + ",[offaxisDist]=" + NxrGlobal.offaxisDistortionEnabled + ",[mesh]=" + meshSizeX +
            "*" + meshSizeY + ",[fps]=" + NxrGlobal.refreshRate + "," + channelCode);

            // 读取cardboard参数
            string cardboardParams = "";
            CallStaticMethod<string>(ref cardboardParams, nibiruVR, "getNibiruVRConfigFull");
            if (cardboardParams.Length > 0)
            {
                Debug.Log("cardboardParams is " + cardboardParams);
                string[] profileData = cardboardParams.Split('_');
                for (int i = 0; i < NxrGlobal.dftProfileParams.Length; i++)
                {
                    if (i >= profileData.Length) break;

                    if (profileData[i] == null || profileData[i].Length == 0) continue;

                    NxrGlobal.dftProfileParams[i] = float.Parse(profileData[i]);
                }
            }
            else
            {
                Debug.Log("Nxr->AndroidDevice->getNibiruVRConfigFull Failed ! ");
            }

            // offaxis distortion
            if (NxrGlobal.offaxisDistortionEnabled)
            {
                string offaxisParams = "";
                CallStaticMethod<string>(ref offaxisParams, nibiruVR, "getOffAxisDistortionConfig");
                if (offaxisParams != null && offaxisParams.Length > 0)
                {
                    NxrGlobal.offaxisDistortionConfigData = offaxisParams;
                    // Debug.LogError(offaxisParams);
                }

                string sdkParams = "";
                CallStaticMethod<string>(ref sdkParams, nibiruVR, "getSDKConfig");
                if (sdkParams != null && sdkParams.Length > 0)
                {
                    NxrGlobal.sdkConfigData = sdkParams;
                    string[] linesCN = sdkParams.Split('\n');
                    //key=value
                    foreach (string line in linesCN)
                    {
                        if (line == null || line.Length <= 1)
                        {
                            continue;
                        }
                        string[] keyAndValue = line.Split('=');
                        //Debug.Log("line=" + line);
                        if (keyAndValue[0].Contains("oad_offset_x1"))
                        {
                            NxrGlobal.offaxisOffset[0] = int.Parse(keyAndValue[1]);
                        }
                        else if (keyAndValue[0].Contains("oad_offset_x2"))
                        {
                            NxrGlobal.offaxisOffset[1] = int.Parse(keyAndValue[1]);
                        }
                        else if (keyAndValue[0].Contains("oad_offset_y1"))
                        {
                            NxrGlobal.offaxisOffset[2] = int.Parse(keyAndValue[1]);
                        }
                        else if (keyAndValue[0].Contains("oad_offset_y2"))
                        {
                            NxrGlobal.offaxisOffset[3] = int.Parse(keyAndValue[1]);
                        }
                    }
                }

                Debug.Log("Offaxis Offset : " + NxrGlobal.offaxisOffset[0] + "," + NxrGlobal.offaxisOffset[1] + "," + NxrGlobal.offaxisOffset[2] + "," + NxrGlobal.offaxisOffset[3]);
            }

            // Debug.LogError("AndroidDevice-Ptr=" +this.GetHashCode());
            return pointer;
        }

        public override void SetDisplayQuality(int level)
        {
            CallStaticMethod(nibiruVR, "setDisplayQualityForUnity", level);
        }

        public override bool GazeApi(GazeTag tag, string param)
        {
            bool show = false;
            CallStaticMethod<bool>(ref show, nibiruVR, "gazeApiForUnity", (int)tag, param);
            return show;
        }

        public override void SetSplitScreenModeEnabled(bool enabled)
        {

        }
        public override void AndroidLog(string msg)
        {
            CallStaticMethod(activityListener, "log", msg);
        }
        public override void SetSystemParameters(string key, string value)
        {
            if (nibiruVR != null)
            {
                CallStaticMethod(nibiruVR, "setSystemParameters", key, value);
            }
        }

        public override void OnApplicationPause(bool pause)
        {
            base.OnApplicationPause(pause);
            //CallObjectMethod(activityListener, "onPause", pause);

            if (!pause && androidActivity != null)
            {
                RunOnUIThread(androidActivity, new AndroidJavaRunnable(runOnUiThread));
            }

        }

        public override void AppQuit()
        {
            if (androidActivity != null)
            {
                RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
                {
                    androidActivity.Call("finish");
                }));
            }
        }

        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
        }

        void runOnUiThread()
        {
            //mActivity.getWindow().addFlags(128);
            //mActivity.getWindow().getDecorView().setSystemUiVisibility(5894);
            AndroidJavaObject androidWindow = androidActivity.Call<AndroidJavaObject>("getWindow");
            androidWindow.Call("addFlags", 128);
            AndroidJavaObject androidDecorView = androidWindow.Call<AndroidJavaObject>("getDecorView");
            androidDecorView.Call("setSystemUiVisibility", 5894);
        }

        public override void SetIsKeepScreenOn(bool keep)
        {
            if (androidActivity != null)
            {
                RunOnUIThread(androidActivity, new AndroidJavaRunnable(() =>
                {
                    SetScreenOn(keep);
                }));
            }
        }
        //if(enable) {
        //	getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        //} else {
        //	getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        //}
        void SetScreenOn(bool enable)
        {
            if (enable)
            {
                AndroidJavaObject androidWindow = androidActivity.Call<AndroidJavaObject>("getWindow");
                androidWindow.Call("addFlags", 128);
            }
            else
            {
                AndroidJavaObject androidWindow = androidActivity.Call<AndroidJavaObject>("getWindow");
                androidWindow.Call("clearFlags", 128);
            }
        }

        private void SetApplicationState()
        {
        }

        /// <summary>
        ///    * @param path
        ///    * @param type23d  0=2d,1=3d
        ///    * @param mode  0=normal,1=360,2=180,3=fullmode
        ///    * @param decode 0=hardware,1=software
        /// </summary>
        public override void ShowVideoPlayer(string path, int type2D3D, int mode, int decode)
        {
            CallStaticMethod(nibiruVR, "showVideoPlayer", path, type2D3D, mode, decode);
        }

        //public override void DismissVideoPlayer()
        //{
        //    CallStaticMethod(nibiruVR, "dismissVideoPlayer");
        //}

        void InitNibiruVRService()
        {
            if (nibiruVRService == null)
            {
                // getNibiruVRService
                CallStaticMethod<AndroidJavaObject>(ref nibiruVRService, nibiruVR, "getNibiruVRService", null);
            }
        }

        public override void SetIpd(float ipd)
        {
            InitNibiruVRService();
            if (nibiruVRService != null)
            {
                CallObjectMethod(nibiruVRService, "setIpd", ipd);
            }
            else
            {
                Debug.LogError("SetIpd failed, because nibiruVRService is null !!!!");
            }
        }

        public override void SetTimeWarpEnable(bool enabled)
        {
            InitNibiruVRService();
            if (nibiruVRService != null)
            {
                CallObjectMethod(nibiruVRService, "setTimeWarpEnable", enabled);
            }
            else
            {
                Debug.LogError("SetTimeWarpEnable failed, because nibiruVRService is null !!!!");
            }
        }
        /// <summary>
        /// Not currently supported.
        /// </summary>
        /// <returns></returns>
        //public override void SetEnableSyncFrame(bool enabled)
        //{
        //    InitNibiruVRService();
        //    if (nibiruVRService != null)
        //    {
        //        CallObjectMethod(nibiruVRService, "setEnableSyncFrame", enabled);
        //    }
        //    else
        //    {
        //        Debug.LogError("SetEnableSyncFrame failed, because nibiruVRService is null !!!!");
        //    }
        //}

        //public override string GetSyncFrameUrl()
        //{
        //    InitNibiruVRService();
        //    if (nibiruVRService != null)
        //    {
        //        return nibiruVRService.Call<string>("getSyncFrameUrl");
        //    }
        //    else
        //    {
        //        Debug.LogError("GetSyncFrameUrl failed, because nibiruVRService is null !!!!");
        //    }
        //    return null;
        //}

        //public override bool IsSyncFrameEnabled()
        //{
        //    InitNibiruVRService();
        //    if (nibiruVRService != null)
        //    {
        //        return nibiruVRService.Call<bool>("isSyncFrameEnabled");
        //    }
        //    return false;
        //}

        //public override bool IsSyncFrameSupported()
        //{
        //    InitNibiruVRService();
        //    if (nibiruVRService != null)
        //    {
        //        return nibiruVRService.Call<bool>("isSyncFrameSupported");
        //    }
        //    return false;
        //}

        public override string GetStoragePath() { return GetAndroidStoragePath(); }

        public override void SetCameraNearFar(float near, float far)
        {
            CallStaticMethod(nibiruVR, "setProjectionNearFarForUnity", near, far);
        }

        public override void StopCapture()
        {
            CallStaticMethod(nibiruVR, "stopCaptureForUnity");
        }

        public override void OnDrawFrameCapture(int frameId)
        {
            CallStaticMethod(nibiruVR, "onDrawFrameForUnity", frameId);
        }

        public override NxrInstantNativeApi.NibiruDeviceType GetSixDofControllerPrimaryDeviceType()
        {
            string result = "3";
            CallStaticMethod<string>(ref result, nibiruVR, "getSystemProperty", "nxr.ctrl.primaryhand", "3");
            Debug.Log("primaryhand_" + result);
            int type = int.Parse(result);
            // 1 = left, 0 = right
            if (type == 0)
            {
                return NxrInstantNativeApi.NibiruDeviceType.RightController;
            } else if(type == 1)
            {
                return NxrInstantNativeApi.NibiruDeviceType.LeftController;
            }
            return NxrInstantNativeApi.NibiruDeviceType.None;
        }

        public override void SetSixDofControllerPrimaryDeviceType(NxrInstantNativeApi.NibiruDeviceType deviceType)
        {
            int type = -1;
            if(deviceType == NxrInstantNativeApi.NibiruDeviceType.LeftController)
            {
                type = 1;
            } else if(deviceType == NxrInstantNativeApi.NibiruDeviceType.RightController)
            {
                type = 0;
            }

            if (type >=0) CallStaticMethod(nibiruVR, "setSystemProperty", "nxr.ctrl.primaryhand", "" + type);
        }
        
        public override int GetControllerTipState()
        {
            string result = "0";
            CallStaticMethod<string>(ref result, nibiruVR, "getSystemProperty", "nxr.ctrl.calib.tip", "0");
            int state = int.Parse(result);
            // 1-手柄校准提示框已弹出 , 0-未弹出
            return state;
        }

        public override void SetControllerTipState(int state)
        {
            CallStaticMethod(nibiruVR, "setSystemProperty", "nxr.ctrl.calib.tip", "" + state);
        }
    }
}
/// @endcond

#endif
