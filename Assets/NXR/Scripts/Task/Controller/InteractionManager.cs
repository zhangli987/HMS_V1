using Nxr.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

namespace NibiruTask
{
    /// <summary>
    /// 
    /// </summary>
    public class InteractionManager
    {
        public enum TipLanguage
        {
            DEFAULT,
            ZH,
            EN
        }

        public struct ControllerTip
        {
            //语言环境，default默认，ZH中文，EN英文
            public string lan;

            //描述信息
            public string desc;

            //提示信息，NACTION_Key->type == 1时使用
            public string info;

            //提示图片完成路径：/system/etc/controller/26/en/tag_3dof_home.png
            public string picPath;
            public float[] position;
            public float[] rotation;
            public float[] size;
        }

        public struct ControllerKeyInfo
        {
            public int key;
            public int type;
            public string picName;
            public string desc;
            public List<ControllerTip> tipList;
        }


        public struct ControllerConfig
        {
            public string modelPath;
            public float[] modelPosition;
            public float[] modelRotation;
            public float[] modelScale;
            public float[] batteryPosition;
            public float[] batteryRotation;
            public float[] batteryScale;
            public float[] rayStartPosition;
            public float[] rayEndPosition;
            public string sourceString;
            public string objPath;
            public string mtlPath;
            public string pngPath;

            public string leftCtrlObjPath;
            public string leftCtrlMtlPath;
            public string leftCtrlPngPath;

            public string rightCtrlObjPath;
            public string rightCtrlMtlPath;
            public string rightCtrlPngPath;

            // 0=失败，1=成功,普通3dof手柄或者双手柄模型一样，2=左右手柄模型不一样
            public int mode;

            public List<ControllerKeyInfo> KeyInfoList;
        }

        public static AndroidJavaObject managerContext;

        static ControllerConfig mControllerConfig;
        static bool IsV2ModelParameterConfigured { set; get; }
        static bool IsSptModelConfigV2 { set; get; }

        /// <summary>
        /// Determine whether the current Controller has a Prompt UI.
        /// </summary>
        /// <returns></returns>
        public static bool IsSptControllerTipUI()
        {
            InitManagerContext();
            return IsSptModelConfigV2 && IsV2ModelParameterConfigured;
        }

        static void InitManagerContext()
        {
            if (managerContext == null)
            {
                for (int i = 0; i < 256; i++)
                {
                    // 默认up
                    keystate[i] = 1;
                    keystateLeft[i] = 1;
                    keystateRight[i] = 1;
                }
#if UNITY_ANDROID && !UNITY_EDITOR
                managerContext = new AndroidJavaObject("com.nibiru.lib.xr.unity.NibiruInteractionSDK");
#endif
                if (managerContext != null)
                {
                    // 首次需要执行GetControllerConfig
                    IsTriggerReloadCtrlConfig = true;
                    IsSupportControllerModel();
                    // 当前手柄可能已连接，获取支持状态
                    RefreshSupportStatus();
                }
            }
        }

        private static void RefreshSupportStatus()
        {
            // 未连接-连接 状态变换，需要刷新支持状态
            IsSptModelConfigV2 = managerContext.Call<bool>("isSptModelConfigV2");
            IsV2ModelParameterConfigured = managerContext.Call<bool>("isV2ModelParameterConfigured");
            Debug.Log("IsSptModelConfigV2=" + IsSptModelConfigV2 + ",IsV2ModelParameterConfigured=" +
                      IsV2ModelParameterConfigured);
        }

        private static void ParseConfigKeyInfoList(string modelPath)
        {
            if (mControllerConfig.KeyInfoList != null) return;

            string keyListData = managerContext.Call<string>("getModelConfigV2KeyListStr");
            if (keyListData != null)
            {
                mControllerConfig.KeyInfoList = new List<ControllerKeyInfo>();

                string[] keyInfoList = keyListData.Split('#');
                foreach (string keyInfo in keyInfoList)
                {
                    if (keyInfo.Length <= 1) continue;
                    string[] keyInfoArray = keyInfo.Split(',');

                    ControllerKeyInfo nactionKey = new ControllerKeyInfo();
                    nactionKey.key = int.Parse(keyInfoArray[0]);
                    nactionKey.type = int.Parse(keyInfoArray[1]);
                    nactionKey.picName = keyInfoArray[2];
                    nactionKey.desc = keyInfoArray[3];
                    int tipConfigSize = int.Parse(keyInfoArray[4]);
                    nactionKey.tipList = new List<ControllerTip>();
                    for (int i = 0; i < tipConfigSize; i++)
                    {
                        string[] tipConfigArray = keyInfoArray[5 + i].Split('*');
                        // EN*english*Trigger*0.085*-0.025*-0.478*-90*0*0*0.15*0.03
                        ControllerTip nactionTipConfig = new ControllerTip();
                        nactionTipConfig.lan = tipConfigArray[0];
                        nactionTipConfig.desc = tipConfigArray[1];
                        nactionTipConfig.info = tipConfigArray[2];
                        nactionTipConfig.picPath = modelPath + "/" + nactionTipConfig.lan + "/" + nactionKey.picName;
                        nactionTipConfig.position = new float[3];
                        nactionTipConfig.position[0] = float.Parse(tipConfigArray[3]);
                        nactionTipConfig.position[1] = float.Parse(tipConfigArray[4]);
                        nactionTipConfig.position[2] = float.Parse(tipConfigArray[5]);
                        nactionTipConfig.rotation = new float[3];
                        nactionTipConfig.rotation[0] = float.Parse(tipConfigArray[6]);
                        nactionTipConfig.rotation[1] = float.Parse(tipConfigArray[7]);
                        nactionTipConfig.rotation[2] = float.Parse(tipConfigArray[8]);
                        nactionTipConfig.size = new float[2];
                        nactionTipConfig.size[0] = float.Parse(tipConfigArray[9]);
                        nactionTipConfig.size[1] = float.Parse(tipConfigArray[10]);
                        nactionKey.tipList.Add(nactionTipConfig);
                    }

                    mControllerConfig.KeyInfoList.Add(nactionKey);
                }
            }
        }

        private static int cacheEnabled = -1;

        /// <summary>
        /// Whether the new Controller Usage Mode is enabled.
        /// </summary>
        public static bool IsInteractionSDKEnabled()
        {
            InitManagerContext();

            if (managerContext == null) return false;
            if (cacheEnabled < 0)
            {
                cacheEnabled = managerContext.Call<bool>("isInteractionSDKEnabled") ? 1 : 0;
            }

            return cacheEnabled == 1;
        }

        public static int GetControllerDeviceType()
        {
            InitManagerContext();

            if (managerContext == null) return -1;
            return managerContext.Call<int>("getControllerDeviceType");
        }

        public static NACTION_CONTROLLER_TYPE GetControllerModeType()
        {
            //InitManagerContext();

            //if (managerContext == null) return NACTION_CONTROLLER_TYPE.CONTROL_NONE;
            //return (NACTION_CONTROLLER_TYPE) managerContext.Call<int>("getControllerModeType");

            return mControllerType;
        }

        /// <summary>
        /// Is there a Model that matches the current Controller.
        /// </summary>
        /// <returns></returns>
        public static bool IsSupportControllerModel()
        {
            InitManagerContext();

            if (managerContext == null) return false;
            bool isSpt = managerContext.Call<bool>("isSupportControllerModel");
            Debug.Log("isSpt=" + isSpt);
            if (!isSpt) return false;
            ControllerConfig cfg = GetControllerConfig();
            if (cfg.modelPath != null)
            {
                // system/etc/controller/108
                DirectoryInfo theFolder = new DirectoryInfo(cfg.modelPath);
                if (theFolder != null && theFolder.Exists)
                {
                    DirectoryInfo parentFolder = theFolder.Parent;
                    if (parentFolder != null && parentFolder.Exists)
                    {
                        // ParentFolder:/system/etc/controller
                        string versionFilePath = parentFolder.FullName + "/Version.txt";
                        bool isExist = System.IO.File.Exists(versionFilePath);
                        Debug.LogError("ParentFolder:" + parentFolder.FullName + "," + isExist);
                        return isExist;
                    }
                }
                else
                {
                    Debug.LogError("theFolder is empty, " + (theFolder == null ? "yes" : theFolder.Exists.ToString()));
                }
            }

            return cfg.modelPath != null;
        }

        /// <summary>
        /// Get data related to the current controller.
        ///  [0]=obj
        ///  [1]=mtl
        ///  [2]=png
        /// </summary>
        public static ControllerConfig GetControllerConfig()
        {
            if (Application.isEditor)
            {
                bool IsSixDofCtrl = false;
                mControllerConfig.modelPath = Application.dataPath + "/NXR/Resources/Controller/Objs/26";
                if (IsSixDofCtrl)
                {
                    mControllerConfig.objPath = mControllerConfig.modelPath + "/left/controller_model.obj";
                    mControllerConfig.mtlPath = mControllerConfig.modelPath + "/left/controller_model.mtl";
                    mControllerConfig.pngPath = mControllerConfig.modelPath + "/left/controller_model.png";
                }
                else
                {
                    mControllerConfig.objPath = mControllerConfig.modelPath + "/controller_model.obj";
                    mControllerConfig.mtlPath = mControllerConfig.modelPath + "/controller_model.mtl";
                    mControllerConfig.pngPath = mControllerConfig.modelPath + "/controller_model.png";
                }

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(mControllerConfig.modelPath + "/controller_config.xml");
                XmlNode rootNode = xmldoc.SelectSingleNode("controller_config");
                XmlNode unityNode = rootNode.ChildNodes.Item(1);
                // model_position->x->0
                XmlNodeList dataNodeList = unityNode.ChildNodes[0].ChildNodes;
                mControllerConfig.modelPosition = new float[3]
                {
                    float.Parse(dataNodeList[0].InnerText),
                    float.Parse(dataNodeList[1].InnerText),
                    float.Parse(dataNodeList[2].InnerText)
                };
                dataNodeList = unityNode.ChildNodes[1].ChildNodes;
                mControllerConfig.modelRotation = new float[3]
                {
                    float.Parse(dataNodeList[0].InnerText),
                    float.Parse(dataNodeList[1].InnerText),
                    float.Parse(dataNodeList[2].InnerText)
                };
                dataNodeList = unityNode.ChildNodes[2].ChildNodes;
                mControllerConfig.modelScale = new float[3]
                {
                    float.Parse(dataNodeList[0].InnerText),
                    float.Parse(dataNodeList[1].InnerText),
                    float.Parse(dataNodeList[2].InnerText)
                };
                dataNodeList = unityNode.ChildNodes[3].ChildNodes;
                mControllerConfig.batteryPosition = new float[3]
                {
                    float.Parse(dataNodeList[0].InnerText),
                    float.Parse(dataNodeList[1].InnerText),
                    float.Parse(dataNodeList[2].InnerText)
                };
                dataNodeList = unityNode.ChildNodes[4].ChildNodes;
                mControllerConfig.batteryRotation = new float[3]
                {
                    float.Parse(dataNodeList[0].InnerText),
                    float.Parse(dataNodeList[1].InnerText),
                    float.Parse(dataNodeList[2].InnerText)
                };
                dataNodeList = unityNode.ChildNodes[5].ChildNodes;
                mControllerConfig.batteryScale = new float[3]
                {
                    float.Parse(dataNodeList[0].InnerText),
                    float.Parse(dataNodeList[1].InnerText),
                    float.Parse(dataNodeList[2].InnerText)
                };
                dataNodeList = unityNode.ChildNodes[6].ChildNodes;
                mControllerConfig.rayStartPosition = new float[3]
                {
                    float.Parse(dataNodeList[0].InnerText),
                    float.Parse(dataNodeList[1].InnerText),
                    float.Parse(dataNodeList[2].InnerText)
                };
                dataNodeList = unityNode.ChildNodes[0].ChildNodes;
                mControllerConfig.rayEndPosition = new float[3]
                {
                    float.Parse(dataNodeList[0].InnerText),
                    float.Parse(dataNodeList[1].InnerText),
                    float.Parse(dataNodeList[2].InnerText)
                };
            }
            else
            {
                ReloadControllerConfigData();
            }
            // Debug.Log("modelPath=" + mControllerConfig.modelPath + "," + IsSptModelConfigV2 + "," + IsV2ModelParameterConfigured);

            return mControllerConfig;
        }

        private static void ReloadControllerConfigData()
        {
            InitManagerContext();
            if (managerContext != null && (mControllerConfig.modelPath == null || IsTriggerReloadCtrlConfig))
            {
                string source = managerContext.Call<string>("getControllerModelConfig");
                if (source == null)
                {
                    Debug.LogError("getControllerModelConfig return null");
                    mControllerConfig.modelPath = null;
                    return;
                }

                IsTriggerReloadCtrlConfig = false;

                // Debug.Log("ReloadControllerConfigData=" + source);

                string[] dataArray = source.Split(',');
                mControllerConfig.modelPath = dataArray[0];

                mControllerConfig.modelPosition = new float[3];
                mControllerConfig.modelPosition[0] = float.Parse(dataArray[1]);
                mControllerConfig.modelPosition[1] = float.Parse(dataArray[2]);
                mControllerConfig.modelPosition[2] = float.Parse(dataArray[3]);

                mControllerConfig.modelRotation = new float[3];
                mControllerConfig.modelRotation[0] = float.Parse(dataArray[4]);
                mControllerConfig.modelRotation[1] = float.Parse(dataArray[5]);
                mControllerConfig.modelRotation[2] = float.Parse(dataArray[6]);

                mControllerConfig.modelScale = new float[3];
                mControllerConfig.modelScale[0] = float.Parse(dataArray[7]);
                mControllerConfig.modelScale[1] = float.Parse(dataArray[8]);
                mControllerConfig.modelScale[2] = float.Parse(dataArray[9]);

                mControllerConfig.batteryPosition = new float[3];
                mControllerConfig.batteryPosition[0] = float.Parse(dataArray[10]);
                mControllerConfig.batteryPosition[1] = float.Parse(dataArray[11]);
                mControllerConfig.batteryPosition[2] = float.Parse(dataArray[12]);

                mControllerConfig.batteryRotation = new float[3];
                mControllerConfig.batteryRotation[0] = float.Parse(dataArray[13]);
                mControllerConfig.batteryRotation[1] = float.Parse(dataArray[14]);
                mControllerConfig.batteryRotation[2] = float.Parse(dataArray[15]);

                mControllerConfig.batteryScale = new float[3];
                mControllerConfig.batteryScale[0] = float.Parse(dataArray[16]);
                mControllerConfig.batteryScale[1] = float.Parse(dataArray[17]);
                mControllerConfig.batteryScale[2] = float.Parse(dataArray[18]);

                mControllerConfig.rayStartPosition = new float[3];
                mControllerConfig.rayStartPosition[0] = float.Parse(dataArray[19]);
                mControllerConfig.rayStartPosition[1] = float.Parse(dataArray[20]);
                mControllerConfig.rayStartPosition[2] = float.Parse(dataArray[21]);

                mControllerConfig.rayEndPosition = new float[3];
                mControllerConfig.rayEndPosition[0] = float.Parse(dataArray[22]);
                mControllerConfig.rayEndPosition[1] = float.Parse(dataArray[23]);
                mControllerConfig.rayEndPosition[2] = float.Parse(dataArray[24]);

                // obj/mtl/png
                mControllerConfig.objPath = mControllerConfig.modelPath + "/controller_model.obj";
                mControllerConfig.mtlPath = mControllerConfig.modelPath + "/controller_model.mtl";
                mControllerConfig.pngPath = mControllerConfig.modelPath + "/controller_model.png";

                if (dataArray.Length > 25)
                {
                    int mode = int.Parse(dataArray[25]);
                    Debug.Log("mode=" + mode);
                    mControllerConfig.mode = mode;
                    if (mode == 2)
                    {
                        mControllerConfig.leftCtrlObjPath = mControllerConfig.modelPath + "/left/controller_model.obj";
                        mControllerConfig.leftCtrlMtlPath = mControllerConfig.modelPath + "/left/controller_model.mtl";
                        mControllerConfig.leftCtrlPngPath = mControllerConfig.modelPath + "/left/controller_model.png";

                        mControllerConfig.rightCtrlObjPath =
                            mControllerConfig.modelPath + "/right/controller_model.obj";
                        mControllerConfig.rightCtrlMtlPath =
                            mControllerConfig.modelPath + "/right/controller_model.mtl";
                        mControllerConfig.rightCtrlPngPath =
                            mControllerConfig.modelPath + "/right/controller_model.png";

                        mControllerConfig.objPath = mControllerConfig.rightCtrlObjPath;
                        mControllerConfig.mtlPath = mControllerConfig.rightCtrlMtlPath;
                        mControllerConfig.pngPath = mControllerConfig.rightCtrlPngPath;
                    }
                    else if (mode == 1)
                    {
                        mControllerConfig.leftCtrlObjPath = mControllerConfig.modelPath + "/controller_model.obj";
                        mControllerConfig.leftCtrlMtlPath = mControllerConfig.modelPath + "/controller_model.mtl";
                        mControllerConfig.leftCtrlPngPath = mControllerConfig.modelPath + "/controller_model.png";

                        mControllerConfig.rightCtrlObjPath = mControllerConfig.modelPath + "/controller_model.obj";
                        mControllerConfig.rightCtrlMtlPath = mControllerConfig.modelPath + "/controller_model.mtl";
                        mControllerConfig.rightCtrlPngPath = mControllerConfig.modelPath + "/controller_model.png";
                    }
                }
                else
                {
                    Debug.Log("mode=-1");
                    mControllerConfig.leftCtrlObjPath = mControllerConfig.modelPath + "/controller_model.obj";
                    mControllerConfig.leftCtrlMtlPath = mControllerConfig.modelPath + "/controller_model.mtl";
                    mControllerConfig.leftCtrlPngPath = mControllerConfig.modelPath + "/controller_model.png";

                    mControllerConfig.rightCtrlObjPath = mControllerConfig.modelPath + "/controller_model.obj";
                    mControllerConfig.rightCtrlMtlPath = mControllerConfig.modelPath + "/controller_model.mtl";
                    mControllerConfig.rightCtrlPngPath = mControllerConfig.modelPath + "/controller_model.png";
                }

                // data update
                if (mControllerConfig.KeyInfoList != null)
                {
                    mControllerConfig.KeyInfoList.Clear();
                    mControllerConfig.KeyInfoList = null;
                }

                Debug.Log("ReloadControllerConfigData modelPath=" + mControllerConfig.modelPath + "," +
                          IsSptModelConfigV2 + "," +
                          IsV2ModelParameterConfigured);
            }
#if !UNITY_EDITOR && UNITY_ANDROID
                ParseConfigKeyInfoList(mControllerConfig.modelPath);
#endif
        }

        public enum NACTION_CONNECT_STATE
        {
            DISCONNECT,
            CONNECT,
            CONNECTING,
            DISCONNECTING
        }

        public enum NACTION_CONTROLLER_ACTION
        {
            DOWN,
            UP,
            MOVE
        }

        public enum NACTION_HAND_TYPE
        {
            HAND_LEFT, //左手
            HAND_RIGHT, //右手
            HEAD,
            NONE
        }

        public enum NACTION_CONTROLLER_TYPE
        {
            CONTROL_NONE = 0, //无指定类型
            CONTROL_6DOF, //6DOF手柄
            CONTROL_3DOF, //3DOF手柄
        };

        // 检查当前左右手模式
        /// <summary>
        /// Determine whether the current controller mode is left hand mode. 
        /// </summary>
        /// <returns></returns>
        public static bool IsLeftHandMode()
        {
            return currentHandMode == HAND_MODE_LEFT && IsLeftControllerConnected();
        }

        /// <summary>
        /// Determine whether the current controller mode is right hand mode.
        /// </summary>
        /// <returns></returns>
        public static bool IsRightHandMode()
        {
            return currentHandMode == HAND_MODE_RIGHT && IsRightControllerConnected();
        }

        public static NACTION_HAND_TYPE GetHandTypeByHandMode()
        {
            if (IsLeftHandMode()) return NACTION_HAND_TYPE.HAND_LEFT;
            if (IsRightHandMode()) return NACTION_HAND_TYPE.HAND_RIGHT;
            return NACTION_HAND_TYPE.HAND_RIGHT;
        }

        // hand代表左/右手，hand=0->左手，hand=1->右手，float数组为长度为4的四元数+长度为3的位移
        // 四元数的顺序: x-y-z-w
        public static float[] GetControllerPose(NACTION_HAND_TYPE handType = NACTION_HAND_TYPE.HAND_RIGHT)
        {
            InitManagerContext();
            if (managerContext == null || !IsControllerConnected()) return new float[] {0, 0, 0, 0, 0, 0, 0};

            if (handType == NACTION_HAND_TYPE.HAND_LEFT && !IsLeftControllerConnected())
            {
                return new float[] {0, 0, 0, 0, 0, 0, 0};
            }

            if (handType == NACTION_HAND_TYPE.HAND_RIGHT && !IsRightControllerConnected())
            {
                return new float[] {0, 0, 0, 0, 0, 0, 0};
            }

            return managerContext.Call<float[]>("getControllerPose", (int) handType);
        }

        /// <summary>
        /// Determine whether there is a controller connection.
        /// </summary>
        /// <returns></returns>
        public static bool IsControllerConnected()
        {
            return IsInteractionSDKEnabled() && (IsLeftControllerConnected() || IsRightControllerConnected());
        }

        /// <summary>
        /// Determine whether there is a 3dof controller connection.
        /// </summary>
        /// <returns></returns>
        public static bool Is3DofControllerConnected()
        {
            if (!IsInteractionSDKEnabled()) return false;
            if (mControllerType != NACTION_CONTROLLER_TYPE.CONTROL_3DOF) return false;

            if ((currentHandMode == HAND_MODE_LEFT && IsLeftControllerConnected()) ||
                (currentHandMode == HAND_MODE_RIGHT && IsRightControllerConnected()))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determine whether the left hand controller is connected.
        /// </summary>
        /// <returns></returns>
        public static bool IsLeftControllerConnected()
        {
            return IsInteractionSDKEnabled() && connectLeftHand == 1;
        }

        /// <summary>
        /// Determine whether the right hand controller is connected.
        /// </summary>
        /// <returns></returns>
        public static bool IsRightControllerConnected()
        {
            return IsInteractionSDKEnabled() && connectRightHand == 1;
        }

        /// <summary>
        /// Determine whether the left or right hand of the 6dof controller is connected.
        /// </summary>
        /// <param name="handType">left hand or right hand</param>
        /// <returns></returns>
        public static bool IsSixDofControllerConnected(int handType)
        {
            if (!IsSixDofController)
            {
                return false;
            }

            //Debug.Log("IsNoloControllerConnected." + handType + "," + IsLeftControllerConnected() + "/" + IsRightControllerConnected()
            //    +"/" + connectLeftHand + "/" + connectRightHand);

            if (handType == (int) NACTION_HAND_TYPE.HAND_LEFT)
            {
                return IsLeftControllerConnected();
            }

            if (handType == (int) NACTION_HAND_TYPE.HAND_RIGHT)
            {
                return IsRightControllerConnected();
            }

            return false;
        }

        // 连接状态
        private static bool IsTriggerReloadCtrlConfig = false;
        private static int connectLeftHand = -1;
        private static int connectRightHand = -1;
        private static int currentHandMode = -1;
        static int HAND_MODE_RIGHT = 0;

        static int HAND_MODE_LEFT = 1;

        // deviceName=NOLO CV1 HEAD
        /// <summary>
        /// Determine whether the connected controller is 6dof.
        /// </summary>
        public static bool IsSixDofController { set; get; }

        private static NACTION_CONTROLLER_TYPE mControllerType = NACTION_CONTROLLER_TYPE.CONTROL_NONE;

        public static void OnDeviceConnectState(string connectInfo)
        {
            Debug.Log("OnDeviceConnectState=" + connectInfo);
            string[] data = connectInfo.Split('_');
            int state = int.Parse(data[1]);
            int handType = int.Parse(data[2]);
            string deviceName = data[3];
            if ((NACTION_HAND_TYPE) handType == NACTION_HAND_TYPE.HEAD) return;
            RefreshSupportStatus();
            IsTriggerReloadCtrlConfig = true;
            currentHandMode = GetControllerHandMode();
            // msgId_state_handType_deviceName:10/11/12
            mControllerType = (NACTION_CONTROLLER_TYPE) (data.Length > 4 ? int.Parse(data[4]) : 0);
            IsSixDofController = mControllerType == NACTION_CONTROLLER_TYPE.CONTROL_6DOF;

            Debug.Log("OnDeviceConnectState: " + connectInfo + ", HandMode: " + currentHandMode + "(0=right,1=left)" +
                      ", IsSixDofController: " + IsSixDofController
                      + ", ControllerMode:" + mControllerType);

            if (state == (int) NACTION_CONNECT_STATE.CONNECT && handType == (int) NACTION_HAND_TYPE.HAND_LEFT)
            {
                connectLeftHand = 1;
                Debug.Log("Left Controller Connect");
            }
            else if (state == (int) NACTION_CONNECT_STATE.CONNECT && handType == (int) NACTION_HAND_TYPE.HAND_RIGHT)
            {
                connectRightHand = 1;
                Debug.Log("Right Controller Connect");
            }
            else if (state == (int) NACTION_CONNECT_STATE.DISCONNECT && handType == (int) NACTION_HAND_TYPE.HAND_LEFT)
            {
                connectLeftHand = -1;
                Debug.Log("Left Controller DisConnect");
            }
            else if (state == (int) NACTION_CONNECT_STATE.DISCONNECT && handType == (int) NACTION_HAND_TYPE.HAND_RIGHT)
            {
                connectRightHand = -1;
                Debug.Log("Right Controller DisConnect");
            }

            if (mControllerType == NACTION_CONTROLLER_TYPE.CONTROL_3DOF &&
                (connectLeftHand == 1 || connectRightHand == 1))
            {
                NibiruAxis.NxrPlayerCtrl mNxrPlayerCtrl = NibiruAxis.NxrPlayerCtrl.Instance;
                if (mNxrPlayerCtrl != null)
                {
                    // 3dof手柄显示
                    mNxrPlayerCtrl.GamepadEnabled = true;
                }
            }

            NxrSDKApi.Instance.ExecuteControllerStatusChangeEvent((NACTION_HAND_TYPE) handType,
                (state == (int) NACTION_CONNECT_STATE.CONNECT), IsSixDofController);
        }

        // private int ACTION_DOWN = 0;
        // private int ACTION_UP = 1;
        // Default is Up
        private static int[] keystate = new int[256];
        private static int[] keystateLeft = new int[256];
        private static int[] keystateRight = new int[256];

        /// <summary>
        ///  Get Controller Touchpad Touch Position
        ///  Horizontal direction： (Left->Right) -1~1
        ///  Vertical direction： (Up->Down) -1~1
        /// </summary>
        public static Vector2 TouchPadPosition { get; set; }

        public static Vector2 TouchPadPositionLeft { get; set; }

        /// <summary>
        ///   Get Right Controller Touchpad Touch Position
        /// </summary>
        public static Vector2 TouchPadPositionRight { get; set; }

        public static int[] GetKeyAction()
        {
            int[] tempKeyAction = new int[256];
            Array.Copy(keystate, tempKeyAction, keystate.Length);
            return tempKeyAction;
        }

        public static int[] GetKeyAction(int handType)
        {
            int[] tempKeyAction = new int[256];

            if (handType == (int) NACTION_HAND_TYPE.HAND_LEFT)
            {
                Array.Copy(keystateLeft, tempKeyAction, keystateLeft.Length);
            }

            if (handType == (int) NACTION_HAND_TYPE.HAND_RIGHT)
            {
                Array.Copy(keystateRight, tempKeyAction, keystateRight.Length);
            }

            return tempKeyAction;
        }

        public static void OnCKeyEvent(string keyCodeInfo)
        {
            // msgId_action_keyCode
            Debug.Log("OnCKeyEvent: " + keyCodeInfo + "," + Time.frameCount);
            string[] data = keyCodeInfo.Split('_');
            // 0=down,1=up
            int action = int.Parse(data[1]);
            int keyCode = int.Parse(data[2]);
            if (keyCode == 105)
            {
                // 统一使用103作为trigger
                keyCode = CKeyEvent.KEYCODE_CONTROLLER_TRIGGER;
            }

            if (!NxrViewer.Instance.IsAppHandleTriggerEvent && CKeyEvent.KEYCODE_CONTROLLER_TRIGGER == keyCode)
            {
                if (action == 1)
                {
                    NxrViewer.Instance.Triggered = true;
                }

                // action = 1;
                keyCode = CKeyEvent.KEYCODE_CONTROLLER_TRIGGER_INTERNAL;
            }

            NACTION_HAND_TYPE handType =
                int.Parse(data[3]) == 1 ? NACTION_HAND_TYPE.HAND_LEFT : NACTION_HAND_TYPE.HAND_RIGHT;
            // 1=left.2=right
            keystate[keyCode] = action;
            if (handType == NACTION_HAND_TYPE.HAND_LEFT)
            {
                keystateLeft[keyCode] = action;
            }
            else if (handType == NACTION_HAND_TYPE.HAND_RIGHT)
            {
                keystateRight[keyCode] = action;
            }
        }

        // action : 2=move,1=up
        //        -1
        // -1 --/-- 1
        //      1
        public static void OnCTouchEvent(string touchInfo)
        {
            //1016_2_0.0_0.0_2
            // msgId_action_x_y
            // Debug.Log("OnCTouchEvent: " + touchInfo);
            string[] data = touchInfo.Split('_');
            int action = int.Parse(data[1]);
            float x = (float) Math.Round(double.Parse(data[2]), 4);
            float y = (float) Math.Round(double.Parse(data[3]), 4);
            NACTION_HAND_TYPE handType =
                int.Parse(data[4]) == 1 ? NACTION_HAND_TYPE.HAND_LEFT : NACTION_HAND_TYPE.HAND_RIGHT;
            TouchPadPosition = new Vector2(x, y);
            if (handType == NACTION_HAND_TYPE.HAND_LEFT)
            {
                TouchPadPositionLeft = new Vector2(x, y);
            }
            else
            {
                TouchPadPositionRight = new Vector2(x, y);
            }

            if (action == CKeyEvent.ACTION_MOVE)
            {
                keystate[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH] = CKeyEvent.ACTION_DOWN;
                if (handType == NACTION_HAND_TYPE.HAND_LEFT)
                {
                    keystateLeft[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH] = CKeyEvent.ACTION_DOWN;
                }
                else if (handType == NACTION_HAND_TYPE.HAND_RIGHT)
                {
                    keystateRight[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH] = CKeyEvent.ACTION_DOWN;
                }
            }
            else if (action == CKeyEvent.ACTION_UP)
            {
                keystate[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH] = CKeyEvent.ACTION_UP;
                if (handType == NACTION_HAND_TYPE.HAND_LEFT)
                {
                    keystateLeft[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH] = CKeyEvent.ACTION_UP;
                }
                else if (handType == NACTION_HAND_TYPE.HAND_RIGHT)
                {
                    keystateRight[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH] = CKeyEvent.ACTION_UP;
                }
            }
        }

        // 0=left,1=right
        private static int[] powerCallCount = new int[] {-1, -1, -1, -1};

        private static int[] powerCacheValue = new int[] {-1, -1, -1, -1};

        // 3dof手柄不区分左右手，返回结果一致
        // 1s=60, 60s更新1次
        /// <summary>
        /// Get the power of controller.
        /// </summary>
        /// <param name="handType">left or right</param>
        /// <returns></returns>
        public static int GetControllerPower(NACTION_HAND_TYPE handType = NACTION_HAND_TYPE.HAND_RIGHT)
        {
            InitManagerContext();
            if (managerContext == null) return 0;
            if (powerCallCount[(int) handType] < 0 || powerCallCount[(int) handType] > 3600)
            {
                powerCacheValue[(int) handType] = managerContext.Call<int>("getControllerBatteryLevel", (int) handType);
                powerCallCount[(int) handType] = 0;
            }

            powerCallCount[(int) handType] = powerCallCount[(int) handType] + 1;
            return Mathf.Max(powerCacheValue[(int) handType], 0);
        }

        /// <summary>
        ///  Get hand mode : 0=right,1=left
        /// </summary>
        /// <returns></returns>
        public static int GetControllerHandMode()
        {
            InitManagerContext();
            if (managerContext == null) return -1;
            return managerContext.Call<int>("getControllerHandMode");
        }

        public static void Reset()
        {
            for (int i = 0; i < 256; i++)
            {
                // 默认up
                keystate[i] = CKeyEvent.ACTION_UP;
                keystateLeft[i] = CKeyEvent.ACTION_UP;
                keystateRight[i] = CKeyEvent.ACTION_UP;
            }

            TouchPadPosition = Vector2.zero;
            TouchPadPositionLeft = Vector2.zero;
            TouchPadPositionRight = Vector2.zero;
        }
    }
}