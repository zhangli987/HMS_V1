using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Nxr.Internal
{
    public class NxrInstantNativeApi
    {
        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct Nibiru_Pose
        {
            public Vector3 position;
            public Quaternion rotation;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Nibiru_ControllerStates
        {
            public uint battery; // 电量
            public uint connectStatus;//连接状态 : hmd/left/right
            public uint buttons;//手柄按键
            public uint hmdButtons;// 一体机按键：上，下，左，右，确认
            public uint touches;//手柄触摸
            public Vector2 touchpadAxis;//触摸坐标
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Nibiru_Config
        {
            public uint controllerType; //0=nolo,2=3dof,3=none
            public float ipd;
            public float near;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public float[] eyeFrustumParams;//Left 4, Right 4 (left,right,bottom,top)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] textureSize; // width, height

        }

        public struct NvrInitParams
        {
            public int renderWidth;
            public int renderHeight;
            public int bitRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DebugInfo
        {
            public int frameIndex;
        }
        #endregion

        public enum NibiruDeviceType
        {
            Hmd = 0,
            LeftController,
            RightController,
            None=3
        }

        // 手柄类型
        public enum NibiruControllerId
        {
            // 0=NOLO,1=...,2=3DOF Controller,3=NONE
            NOLO, EXPAND, NORMAL_3DOF, NONE
        }

        public enum RenderEvent
        {
            SubmitFrame = 1
        };

        public static bool Inited = false;
        public static int nativeApiVersion = -1;
        public static int driverVersion = -1;

#if UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
        internal const string dllName = "NvrPluginNative";

        [DllImport(dllName)]
        public static extern void SetVersionInfo(int apiVersion, string unity_version_str, int unity_version_length);

        [DllImport(dllName)]
        public static extern bool Init(NvrInitParams args);

        [DllImport(dllName)]
        public static extern IntPtr GetRenderEventFunc();

        [DllImport(dllName)]
        public static extern void SetFrameTexture(IntPtr texturePointer);

        [DllImport(dllName)]
        public static extern void Cleanup();

        [DllImport(dllName)]
        public static extern Nibiru_Config GetNibiruConfig();

        // 手柄Pose
        [DllImport(dllName)]
        public static extern Nibiru_Pose GetPoseByDeviceType(NibiruDeviceType type);

        [DllImport(dllName)]
        public static extern Nibiru_ControllerStates GetControllerStates(NibiruDeviceType type);

        [DllImport(dllName)]
        public static extern void SetNibiruConfigCallback(NxrViewer.NibiruConfigCallback callback);

        //---------------------New Api----V1-----------------
        [DllImport(dllName)]
        public static extern void GetVersionInfo(ref int apiVersion, ref int driverVersion);

        [DllImport(dllName)]
        public static extern void SendFrame();

        [DllImport(dllName)]
        public static extern void GetTextureResolution(ref int width, ref int height);

        [DllImport(dllName)]
        public static extern DebugInfo GetDebugInfo();

        //---------------------New Api----V1-----------------


        [DllImport(dllName)]
        public static extern UInt32 GetDecodeRate();

        [DllImport(dllName)]
        public static extern UInt32 GetRefreshRate();

        [DllImport(dllName)]
        public static extern IntPtr GetLeapMotionData();
        //---------------------New Api----V2-----------------
        //---------------------New Api----V2-----------------

#elif UNITY_ANDROID

        public static void SetVersionInfo(int apiVersion, string unity_version_str, int unity_version_length) { }

        public static bool Init(NvrInitParams args) { return false; }

        public static IntPtr GetRenderEventFunc() { return IntPtr.Zero; }

        public static void SetFrameTexture(IntPtr texturePointer) { }

        public static void Cleanup() { }

        public static Nibiru_Config GetNibiruConfig() { return new Nibiru_Config(); }

        // 手柄Pose
        public static Nibiru_Pose GetPoseByDeviceType(NibiruDeviceType type) { return new Nibiru_Pose(); }

        public static Nibiru_ControllerStates GetControllerStates(NibiruDeviceType type) { return new Nibiru_ControllerStates(); }

        public static void SetNibiruConfigCallback(NxrViewer.NibiruConfigCallback callback) { }

        //---------------------New Api----V1-----------------
        public static void GetVersionInfo(ref int apiVersion, ref int driverVersion) { }

        public static void SendFrame() { }

        public static void GetTextureResolution(ref int width, ref int height) { }

        public static DebugInfo GetDebugInfo() { return new DebugInfo(); }
        //---------------------New Api----V1-----------------
        public static IntPtr GetLeapMotionData() { return IntPtr.Zero; }
        public static UInt32 GetRefreshRate() { return 0; }
        public static UInt32 GetDecodeRate() { return 0; }
        //---------------------New Api----V2-----------------
        //---------------------New Api----V2-----------------
#endif

    }
}
