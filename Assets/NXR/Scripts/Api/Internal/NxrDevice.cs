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

using UnityEngine;
using System.Runtime.InteropServices;
using System;
/// @cond
namespace Nxr.Internal
{
    public abstract class NxrDevice :
#if UNITY_ANDROID
  BaseAndroidDevice
#else
  BaseARDevice
#endif
    {
         
        // A relatively unique id to use when calling our C++ native render plugin.
        private const int kRenderEvent = 0x47554342;

        // Event IDs sent up from native layer.  Bit flags.
        // Keep in sync with the corresponding declaration in unity.h.
        private const int kTilted = 1 << 1;
        private const int kProfileChanged = 1 << 2;
        private const int kVRBackButtonPressed = 1 << 3;

        private float[] position = new float[3];
        private float[] headData = new float[16];
        private float[] viewData = new float[16 * 6 + 12];
        private float[] profileData = new float[15];

        private Matrix4x4 headView = new Matrix4x4();
        private Matrix4x4 leftEyeView = new Matrix4x4();
        private Matrix4x4 rightEyeView = new Matrix4x4();

        private int _timewarp_view_number = 0;
        private bool isHeadPoseUpdated = false;

        public override NibiruService GetNibiruService()
        {
            return NxrGlobal.nibiruService;
        }

        public override void Init()
        { 
            // Start will send a log event, so SetUnityVersion first.
            byte[] version = System.Text.Encoding.UTF8.GetBytes(Application.unityVersion);
            if (NxrViewer.USE_DTR)
            {  
                if (!NxrGlobal.nvrStarted)
                {
                    if (nibiruVRServiceId == 0)
                    {
                        // 初始化1次service
                        nibiruVRServiceId = CreateNibiruVRService(); 
                    }
                    _NVR_InitAPIs(NxrGlobal.useNvrSo);
                    _NVR_SetUnityVersion(version, version.Length);
                    _NVR_Start(nibiruVRServiceId);
                    SetDisplayQuality((int) NxrViewer.Instance.TextureQuality);
                    SetMultiThreadedRendering(SystemInfo.graphicsMultiThreaded);
                    Debug.LogError("graphicsMultiThreaded=" + SystemInfo.graphicsMultiThreaded);
                    //
                    if (NxrGlobal.soVersion >= 361)
                    {
                        ColorSpace colorSpace = QualitySettings.activeColorSpace;
                        if (colorSpace == ColorSpace.Gamma)
                        {
                            Debug.Log("Color Space - Gamma");
                            SetColorspaceType(0);
                        }
                        else if (colorSpace == ColorSpace.Linear)
                        {
                            Debug.Log("Color Space - Linear");
                            SetColorspaceType(1);
                        }
                    } else
                    {
                        Debug.LogError("System Api Not Support ColorSpace!!!");
                    }

                    if (NxrGlobal.soVersion >= 365)
                    {
                        Debug.Log("Controller Support Mode - " + NxrViewer.Instance.controllerSupportMode.ToString());
                        SetControllerSupportMode(NxrViewer.Instance.controllerSupportMode);
                    }
                    NxrGlobal.nvrStarted = true;
                    // 初始化服务
                    NibiruService nibiruService = new NibiruService();
                    nibiruService.Init();
                    NxrGlobal.nibiruService = nibiruService;

                    //
                    NxrSDKApi.Instance.IsSptEyeLocalRp = IsSptEyeLocalRotPos();
                    if (NxrSDKApi.Instance.IsSptEyeLocalRp)
                    {
                        _NVR_GetEyeLocalRotPos(NxrSDKApi.Instance.LeftEyeLocalRotation,
                            NxrSDKApi.Instance.LeftEyeLocalPosition, NxrSDKApi.Instance.RightEyeLocalRotation,
                            NxrSDKApi.Instance.RightEyeLocalPosition);
                    }
                } 
            } 
           Debug.Log("NxrDevice->Init.isSptEyeLocalRp=" + NxrSDKApi.Instance.IsSptEyeLocalRp);
        }

        public override int GetTimewarpViewNumber()
        {
            return _timewarp_view_number;
        }

        public override bool IsHeadPoseUpdated()
        {
            return isHeadPoseUpdated;
        }

        public override void UpdateState()
        {
            if (NxrViewer.USE_DTR)
            {
                _NVR_GetHeadPoseAndPosition(position, headData , ref _timewarp_view_number);
                NxrSDKApi.Instance.HeadPosition = new Vector3(position[0], position[1], position[2]);
                // Debug.LogError("HeadPosition:" + position[0] + "," + position[1] + "," + position[2]);

                if(NxrViewer.Instance.SixDofMode == SixDofMode.Head_3Dof_Ctrl_3Dof || 
                    NxrViewer.Instance.SixDofMode == SixDofMode.Head_3Dof_Ctrl_6Dof)
                {
                    position[0] = 0.0f;
                    position[1] = 0.0f;
                    position[2] = 0.0f;
                }

                if(position[0] != 0 || position[1] != 0 || position[2] != 0)
                {
                    if(NxrViewer.onSixDofPosition != null)
					   NxrViewer.onSixDofPosition(position[0], position[1], position[2]);
                }
            }

            // 头部锁定
            if ((NxrGlobal.verifyStatus >= 0 && NxrGlobal.verifyStatus != VERIFY_STATUS.SUCC) || NxrViewer.Instance.LockHeadTracker || (headData[0] == 0 && headData[15] == 0))
            {
                headData = new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
            } 

            if(NxrGlobal.verifyStatus >= 0 && NxrGlobal.verifyStatus != VERIFY_STATUS.SUCC)
            {
                AndroidLog("------------------------------Verify Failed : " +NxrGlobal.verifyStatus + "------------------------------");
            }

            ExtractMatrix(ref headView, headData);
            headPose.SetRightHanded(headView);
            headPose.SetPosition(new Vector3(position[0], position[1], position[2]));
            isHeadPoseUpdated = true;
        }

        public override void UpdateScreenData()
        {
            bool useDFT = NxrViewer.USE_DTR && !NxrGlobal.supportDtr;
            // so获取
            UpdateProfile();
            UpdateView();
            // so获取

            if(useDFT || NxrViewer.Instance.IsWinPlatform)
            {
                float far = NxrGlobal.fovFar > -1 ? NxrGlobal.fovFar : (Camera.main != null ? Camera.main.farClipPlane : 20000.0f);
                ComputeEyesFromProfile(1, far);
            }
           
            profileChanged = true;
        }

        public override void Recenter()
        {
            if (NxrViewer.USE_DTR)
            {
                _NVR_ResetHeadPose();
            }
        }

        public override void PostRender(RenderTexture stereoScreen)
        {
           // do nothing
        }

        public override void EnterARMode() {
            Debug.Log("NxrDevice->EnterARMode");
            NxrPluginEvent.Issue(NibiruRenderEventType.BeginVR);
            _NVR_ApplicationResume();
            // 更新参数信息
            UpdateScreenData();
        }

        public override void OnApplicationPause(bool pause)
        {
            Debug.Log("NxrDevice->OnApplicationPause." + pause);
            base.OnApplicationPause(pause);
            // 程序暂停
            if (pause)
            {
                Debug.Log("NxrDevice->OnPause");
                if (NxrViewer.USE_DTR)
                {
                    NxrSDKApi.Instance.IsInXRMode = false;
                    NxrPluginEvent.Issue(NibiruRenderEventType.EndVR);
                    _NVR_ApplicationPause();
                }
            }
            else
            {
                Debug.Log("NxrDevice->OnResume");
                if (NxrViewer.USE_DTR)
                {
                    NxrSDKApi.Instance.IsInXRMode = true;
                    NxrPluginEvent.Issue(NibiruRenderEventType.BeginVR);
                    _NVR_ApplicationResume();
                    UpdateScreenData();
                }
            }
        }

        public override void Destroy()
        {
            Debug.Log("NxrDevice->Destroy");
            base.Destroy();
        }

        private bool applicationQuited = false;
        public override void OnApplicationQuit()
        {
            if (NxrViewer.USE_DTR && !applicationQuited)
            {  // 关闭陀螺仪
                Input.gyro.enabled = false;
                NxrPluginEvent.Issue(NibiruRenderEventType.ShutDown);
                _NVR_ApplicationDestory();
            } 
            applicationQuited = true;
            base.OnApplicationQuit();
            Debug.Log("NxrDevice->OnApplicationQuit.");
        }

        private void UpdateView()
        {
            if (NxrViewer.USE_DTR)
            {
                _NVR_GetViewParameters(viewData);
            }

            int j = 0; 

            j = ExtractMatrix(ref leftEyeView, viewData, j);
            j = ExtractMatrix(ref rightEyeView, viewData, j);
            if (NxrViewer.USE_DTR)
            {
                // 转置处理
                leftEyeView = leftEyeView.transpose;
                rightEyeView = rightEyeView.transpose;
            }
            //leftEyePose.SetRightHanded(leftEyeView.inverse);
            //rightEyePose.SetRightHanded(rightEyeView.inverse);

            j = ExtractMatrix(ref leftEyeDistortedProjection, viewData, j);
            j = ExtractMatrix(ref rightEyeDistortedProjection, viewData, j);
            j = ExtractMatrix(ref leftEyeUndistortedProjection, viewData, j);
            j = ExtractMatrix(ref rightEyeUndistortedProjection, viewData, j);
            if (NxrViewer.USE_DTR)
            {
                // 转置处理
                leftEyeDistortedProjection = leftEyeDistortedProjection.transpose;
                rightEyeDistortedProjection = rightEyeDistortedProjection.transpose;
                leftEyeUndistortedProjection = leftEyeUndistortedProjection.transpose;
                rightEyeUndistortedProjection = rightEyeUndistortedProjection.transpose;
            }

            leftEyeUndistortedViewport.Set(viewData[j], viewData[j + 1], viewData[j + 2], viewData[j + 3]);
            leftEyeDistortedViewport = leftEyeUndistortedViewport;
            j += 4;

            rightEyeUndistortedViewport.Set(viewData[j], viewData[j + 1], viewData[j + 2], viewData[j + 3]);
            rightEyeDistortedViewport = rightEyeUndistortedViewport;
            j += 4;
            //  屏幕大小，纹理生成大小 1920*1080
            int screenWidth = (int)viewData[j];
            int screenHeight = (int)viewData[j + 1];
            j += 2;

            recommendedTextureSize = new Vector2(viewData[j], viewData[j + 1]);
            j += 2;

            if (NxrViewer.USE_DTR && !NxrGlobal.supportDtr) {
                // DFT 
                recommendedTextureSize = new Vector2(screenWidth, screenHeight);
                Debug.Log("DFT texture size : " +screenWidth + "," + screenHeight);
            }
        }

        private void UpdateProfile()
        {
            if (NxrViewer.USE_DTR)
            {
                _NVR_GetNVRConfig(profileData);
            }

            if (profileData[13] > 0)
            {
                NxrGlobal.fovNear = profileData[13];
            }

            if (profileData[14] > 0)
            {
                NxrGlobal.fovFar = profileData[14] > NxrGlobal.fovFar ? profileData[14] : NxrGlobal.fovFar;
            }


            if (NxrViewer.USE_DTR && !NxrGlobal.supportDtr && NxrGlobal.dftProfileParams[0] != 0)
            {
                // DFT模式加载cardboard参数 0.062_0.03725_0.06_40.0_40.0_43.3_43.3_0.11825_0.39027_1920.0_1080.0_0.003_0.126_0.0625_-1
                // fov
                profileData[0] = NxrGlobal.dftProfileParams[3];//45;
                profileData[1] = NxrGlobal.dftProfileParams[4]; //45;
                profileData[2] = NxrGlobal.dftProfileParams[5]; //51.5f;
                profileData[3] = NxrGlobal.dftProfileParams[6]; //51.5f;
                // screen size 
                profileData[4] = NxrGlobal.dftProfileParams[12]; //0.110f;
                profileData[5] = NxrGlobal.dftProfileParams[13]; //0.062f;
                // ipd
                profileData[7] = NxrGlobal.dftProfileParams[0]; //0.063f;
                // screen to lens
                profileData[9] = NxrGlobal.dftProfileParams[2]; //0.035f;
                // k1 k2
                profileData[11] = NxrGlobal.dftProfileParams[7]; //0.252f;
                profileData[12] = NxrGlobal.dftProfileParams[8]; //0.019f;
                if(NxrGlobal.offaxisDistortionEnabled)
                {
                    // profileData[7] = 0.058f;
                }
            }

            NxrProfile.Viewer device = new NxrProfile.Viewer();
            NxrProfile.Screen screen = new NxrProfile.Screen();
            // left top right bottom
            device.maxFOV.outer = profileData[0];
            device.maxFOV.upper = profileData[2];
            device.maxFOV.inner = profileData[1];
            device.maxFOV.lower = profileData[3];
            screen.width = profileData[4];
            screen.height = profileData[5];
            screen.border = profileData[6];
            device.lenses.separation = profileData[7];
            device.lenses.offset = profileData[8];
            device.lenses.screenDistance = profileData[9];
            device.lenses.alignment = (int)profileData[10];
            device.distortion.Coef = new[] { profileData[11], profileData[12] };
            Profile.screen = screen;
            Profile.viewer = device;

            float[] rect = new float[4];
            Profile.GetLeftEyeNoLensTanAngles(rect);
            float maxRadius = NxrProfile.GetMaxRadius(rect);
            Profile.viewer.inverse = NxrProfile.ApproximateInverse(
            Profile.viewer.distortion, maxRadius);
        }

        private static int ExtractMatrix(ref Matrix4x4 mat, float[] data, int i = 0)
        {
            // 列优先
            // Matrices returned from our native layer are in row-major order.
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++, i++)
                {
                    mat[r, c] = data[i];
                }
            }
            return i;
        }
         
        public override IntPtr NGetRenderEventFunc() {
            return _NVR_GetRenderEventFunc();
        }

        public override void NSetSystemSplitMode(int flag) {
            _NVR_SetSystemVRMode(flag);
        }

        public override void NLockTracker()
        {
            _NVR_LockHeadPose();
        }

        public override void NUnLockTracker()
        {
            _NVR_UnLockHeadPose();
        }

        public override void SetTextureSizeNative(int w, int h)
        {
            // 1920*1080 = 1920*10000 + 1080 = 19201080
            _NVR_SetParamI(1002, w * 10000 + h);
        }

        public override void SetCpuLevel(NxrOverrideSettings.PerfLevel level)
        {
            _NVR_SetParamI(1003, (int) level);
        }

        public override void SetGpuLevel(NxrOverrideSettings.PerfLevel level)
        {
            _NVR_SetParamI(1004, (int)level);
        }

        public override void NIssuePluginEvent(int eventID)
        {
            // Queue a specific callback to be called on the render thread
            GL.IssuePluginEvent(NxrViewer.Instance.GetDevice().NGetRenderEventFunc(), eventID);
        }
		
		public override void SetColorspaceType(int colorSpace)
        {
            _NVR_SetParamI((int)PARAMS_KEY.COLOR_SPACE, colorSpace);
        }

        public override void SetControllerSupportMode(ControllerSupportMode csm)
        {
            _NVR_SetParamI((int)PARAMS_KEY.CONTROLLER_SUPPORT, (int) csm);
        }

        public override void SetMultiThreadedRendering(bool isMultiThreadedRendering)
        {
            _NVR_SetParamI((int)PARAMS_KEY.MULTITHREAD_RENDERING, isMultiThreadedRendering ? 1 : 0);
        }

        public override bool IsSptEyeLocalRotPos()
        {
            return _NVR_GetParamI((int)PARAMS_KEY.EYE_LOCAL_ROT_POS) == 1;
        }

        public override Quaternion GetEyeLocalRotation(NxrViewer.Eye eye)
        {
            float[] eulerAngles = new float[3];
            if(eye == NxrViewer.Eye.Left)
            {
                NxrCameraUtils.RotationMatrixToEulerAngles(ref eulerAngles, NxrSDKApi.Instance.LeftEyeLocalRotation);
            }
            else if(eye == NxrViewer.Eye.Right)
            {
                NxrCameraUtils.RotationMatrixToEulerAngles(ref eulerAngles, NxrSDKApi.Instance.RightEyeLocalRotation);
            }
            return Quaternion.Euler(eulerAngles[0], eulerAngles[1], eulerAngles[2]);
        }

        public override Vector3 GetEyeLocalPosition(NxrViewer.Eye eye)
        {
            if (eye == NxrViewer.Eye.Left)
            {
                return new Vector3(NxrSDKApi.Instance.LeftEyeLocalPosition[0], NxrSDKApi.Instance.LeftEyeLocalPosition[1], NxrSDKApi.Instance.LeftEyeLocalPosition[2]);
            }
            else if (eye == NxrViewer.Eye.Right)
            {
                return new Vector3(NxrSDKApi.Instance.RightEyeLocalPosition[0], NxrSDKApi.Instance.RightEyeLocalPosition[1], NxrSDKApi.Instance.RightEyeLocalPosition[2]);
            }
            return Vector3.zero;
        }



        public enum PARAMS_KEY
        {
            CONTROLLER_SUPPORT=1006,
            COLOR_SPACE = 1007,
            TURN_AROUND_STATE=1008,
            TURN_AROUND_YAWOFFSET=1009,
            MULTITHREAD_RENDERING=1010,
            EYE_LOCAL_ROT_POS = 1011
        }

        //  调用跳转so
        private const string nvrDllName = "nvr_unity";

        [DllImport(nvrDllName)]
        private static extern int _NVR_InitAPIs(bool supportDTR);

        [DllImport(nvrDllName)]
        private static extern bool _NVR_Start(long pointer);

        [DllImport(nvrDllName)]
        private static extern void _NVR_SetUnityVersion(byte[] version_str, int version_length);

        [DllImport(nvrDllName)]
        private static extern int _NVR_GetEventFlags();

        [DllImport(nvrDllName)]
        private static extern void _NVR_GetNVRConfig(float[] profile);

        [DllImport(nvrDllName)]
        private static extern void _NVR_GetHeadPose(float[] pose, ref int viewNumber);

        [DllImport(nvrDllName)]
        private static extern void _NVR_GetHeadPoseAndPosition(float[] position, float[] pose, ref int viewNumber);

        [DllImport(nvrDllName)]
        private static extern void _NVR_ResetHeadPose();

        [DllImport(nvrDllName)]
        private static extern void _NVR_GetViewParameters(float[] viewParams);

        [DllImport(nvrDllName)]
        private static extern void _NVR_ApplicationPause();

        [DllImport(nvrDllName)]
        private static extern void _NVR_ApplicationResume();

        [DllImport(nvrDllName)]
        private static extern void _NVR_ApplicationDestory();

        [DllImport(nvrDllName)]
        private static extern IntPtr _NVR_GetRenderEventFunc();

        [DllImport(nvrDllName)]
        private static extern void _NVR_LockHeadPose();

        [DllImport(nvrDllName)]
        private static extern void _NVR_UnLockHeadPose();

        [DllImport(nvrDllName)]
        private static extern void _NVR_SetSystemVRMode(int flag);

        [DllImport(nvrDllName)]
        private static extern void _NVR_SetParamI(int key, int value);

        [DllImport(nvrDllName)]
        private static extern int _NVR_GetParamI(int key);

        [DllImport(nvrDllName)]
        private static extern void _NVR_GetEyeLocalRotPos(float[] leftEyeRot, float[] leftEyePos, float[] rightEyeRot, float[] rightEyePos);
    }
}
/// @endcond
