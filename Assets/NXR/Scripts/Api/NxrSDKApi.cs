using System;
using NibiruTask;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nxr.Internal
{
    /// <summary>
    ///  Nxr SDK Api Global Single Instance
    /// </summary>
    public class NxrSDKApi
    {
        private static object syncRoot = new object();

        private static NxrSDKApi _instance = null;

        public static NxrSDKApi Instance
        {
            get
            {
                if (_instance == null) //第一重判断，先判断实例是否存在，不存在再加锁处理
                {
                    lock (syncRoot) //加锁，在某一时刻只允许一个线程访问
                    {
                        if (_instance == null) //第二重判断: 第一个线程进入Lock中执行创建代码，第二个线程处于排队等待状态，当第二个线程进入Lock后并不知道实例已创建，将会继续创建新的实例
                        {
                            _instance = new NxrSDKApi();
                        }
                    }
                }

                return _instance;
            }
        }

        private NxrSDKApi()
        {
            IsInXRMode = false;
        }

        /// <summary>
        ///  Is in xr mode(AR/VR)
        /// </summary>
        public bool IsInXRMode { set; get; }

        // Controller connection status change : handType 0=left, 1=right
        public delegate void ControllerStatusChange(NibiruTask.InteractionManager.NACTION_HAND_TYPE handType,
            bool isConnected, bool isSixDofController);

        public event ControllerStatusChange ControllerStatusChangeEvent;

        public void ExecuteControllerStatusChangeEvent(NibiruTask.InteractionManager.NACTION_HAND_TYPE handType,
            bool isConnected, bool isSixDofController)
        {
            Debug.Log("handtype=" + handType + "," + "isConnected=" + isConnected + "," + "isSixDofController=" + isSixDofController);
            if (ControllerStatusChangeEvent != null)
            {
                ControllerStatusChangeEvent(handType, isConnected, isSixDofController);
            }

            if (isConnected)
            {
                var controllerTipState = NxrViewer.Instance.GetDevice().GetControllerTipState();
                Debug.Log("ControllerTipState:" + controllerTipState);
                if (controllerTipState == 0)
                {
                    NibiruRemindBox.Instance.CalibrationDelay();
                    // NxrViewer.Instance.GetDevice().SetControllerTipState(1);
                }
            }
        }

        /// <summary>
        /// The Trigger key can switch the main controller, only the main controller can emit rays {global variable}.
        /// </summary>
        NxrInstantNativeApi.NibiruDeviceType sixDofControllerPrimaryDeviceType;

        /// <summary>
        /// Current sixDof controller primary device type.(LeftController or RightController)
        /// </summary>
        public NxrInstantNativeApi.NibiruDeviceType SixDofControllerPrimaryDeviceType
        {
            set
            {
                sixDofControllerPrimaryDeviceType = value;
                if (NxrViewer.Instance != null)
                {
                    NxrViewer.Instance.GetDevice()
                        .SetSixDofControllerPrimaryDeviceType(sixDofControllerPrimaryDeviceType);
                }
            }
            get { return sixDofControllerPrimaryDeviceType; }
        }

        /// <summary>
        /// The Position of Head.
        /// </summary>
        public Vector3 HeadPosition { set; get; }

        private Dictionary<string, ObjMesh> CacheMeshDict = new Dictionary<string, ObjMesh>();

        public void AddObjMesh(string name, ObjMesh mesh)
        {
            if (CacheMeshDict.ContainsKey(name))
            {
                CacheMeshDict[name] = mesh;
            }
            else
            {
                CacheMeshDict.Add(name, mesh);
            }
        }

        public ObjMesh GetObjMesh(string name)
        {
            if (CacheMeshDict.ContainsKey(name))
            {
                return CacheMeshDict[name];
            }
            else
            {
                return null;
            }
        }

        private Dictionary<string, Sprite> Cach3DofSpriteDict = new Dictionary<string, Sprite>();
        public Dictionary<string, Sprite> Cach6DofSpriteDict = new Dictionary<string, Sprite>();
        public bool Is3DofSpriteFirstLoad { set; get; }
        public bool Is6DofSpriteFirstLoad { set; get; }
        public string Last3DofModelPath { set; get; }
        public string Last6DofModelPath { set; get; }

        public void AddSprite(string name, Sprite sprite)
        {
            if (InteractionManager.GetControllerModeType() == InteractionManager.NACTION_CONTROLLER_TYPE.CONTROL_3DOF)
            {
                if (Cach3DofSpriteDict.ContainsKey(name))
                {
                    Cach3DofSpriteDict[name] = sprite;
                }
                else
                {
                    Cach3DofSpriteDict.Add(name, sprite);
                }
            }

            if (InteractionManager.GetControllerModeType() == InteractionManager.NACTION_CONTROLLER_TYPE.CONTROL_6DOF)
            {
                if (Cach6DofSpriteDict.ContainsKey(name))
                {
                    Cach6DofSpriteDict[name] = sprite;
                }
                else
                {
                    Cach6DofSpriteDict.Add(name, sprite);
                }
            }
        }

        public Sprite GetSprite(string name)
        {
            if (InteractionManager.GetControllerModeType() == InteractionManager.NACTION_CONTROLLER_TYPE.CONTROL_3DOF)
            {
                if (Cach3DofSpriteDict.ContainsKey(name)) return Cach3DofSpriteDict[name];
            }

            if (InteractionManager.GetControllerModeType() == InteractionManager.NACTION_CONTROLLER_TYPE.CONTROL_6DOF)
            {
                if (Cach6DofSpriteDict.ContainsKey(name)) return Cach6DofSpriteDict[name];
            }

            return null;
        }

        public void ClearCachSpriteDict()
        {
            if (InteractionManager.GetControllerModeType() == InteractionManager.NACTION_CONTROLLER_TYPE.CONTROL_3DOF)
            {
                Cach3DofSpriteDict.Clear();
            }

            if (InteractionManager.GetControllerModeType() == InteractionManager.NACTION_CONTROLLER_TYPE.CONTROL_6DOF)
            {
                Cach6DofSpriteDict.Clear();
            }
        }

        public void Destroy()
        {
            CacheMeshDict.Clear();
        }

        /// <summary>
        /// Is support multiThreaded rendering
        /// </summary>
        public bool IsSptMultiThreadedRendering { set; get; }

        /// <summary>
        /// Whether the current controller has a prompt UI.
        /// </summary>
        /// <returns></returns>
        public bool IsSptControllerTipUI()
        {
            return InteractionManager.IsSptControllerTipUI();
        }

        public List<InteractionManager.ControllerKeyInfo> GetControllerKeyInfoList()
        {
            return InteractionManager.GetControllerConfig().KeyInfoList;
        }

        /// <summary>
        /// The language of Controller Prompt UI. 
        /// </summary>
        private InteractionManager.TipLanguage tipLanguage;

        public InteractionManager.TipLanguage GetTipLanguage()
        {
            if (Application.systemLanguage == SystemLanguage.Chinese ||
                Application.systemLanguage == SystemLanguage.ChineseSimplified ||
                Application.systemLanguage == SystemLanguage.ChineseTraditional)
            {
                tipLanguage = InteractionManager.TipLanguage.ZH;
            }
            else if (Application.systemLanguage == SystemLanguage.English)
            {
                tipLanguage = InteractionManager.TipLanguage.EN;
            }
            else
            {
                tipLanguage = InteractionManager.TipLanguage.DEFAULT;
            }

            return tipLanguage;
        }

        public string assetPath = "Assets/NXR/Resources/Config/";

#if UNITY_EDITOR
        public SettingsAssetConfig GetSettingsAssetConfig()
        {
            var assetpath = assetPath + "SettingsAssetConfig.asset";
            SettingsAssetConfig asset;
            if (System.IO.File.Exists(assetpath))
            {
                asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SettingsAssetConfig>(assetpath);
            }
            else
            {
                asset = new SettingsAssetConfig();
                asset.mSixDofMode = SixDofMode.Head_3Dof_Ctrl_6Dof;
                asset.mSleepTimeoutMode = SleepTimeoutMode.NEVER_SLEEP;
                asset.mHeadControl = HeadControl.GazeApplication;
                asset.mTextureQuality = TextureQuality.Best;
                asset.mTextureMSAA = TextureMSAA.MSAA_2X;
                UnityEditor.AssetDatabase.CreateAsset(asset, assetpath);
            }

            return asset;
        }
#endif



        public bool IsSptEyeLocalRp { get; set; }
        public float[] LeftEyeLocalRotation = new float[9];
        public float[] LeftEyeLocalPosition = new float[3];
        public float[] RightEyeLocalRotation = new float[9];
        public float[] RightEyeLocalPosition = new float[3];

    }
}