using NibiruTask;
using Nxr.Internal;
using UnityEngine;
using UnityEngine.UI;

namespace NibiruAxis
{
    /// <summary>
    /// 
    /// </summary>
    public class NxrPlayerCtrl : MonoBehaviour
    {
        private static NxrPlayerCtrl m_instance = null;

        private bool isCreateControllerHandler = false;

        public bool debugInEditor;

        [SerializeField] private bool gamepadEnabled = true;

        public bool controllerModelDisplay = true;

        public bool GamepadEnabled
        {
            get { return gamepadEnabled; }
            set { gamepadEnabled = value; }
        }

        public static NxrPlayerCtrl Instance
        {
            get { return m_instance; }
        }

        public Vector3 HeadPosition { get; set; }

        NxrArmModel nxrArmModel;
        public Transform mTransform;
        Quaternion controllerQuat = new Quaternion(0, 0, 0, 1);
        public Quaternion EditorRemoteQuat = new Quaternion(0, 0, 0, 1);

        public void OnDeviceConnectState(int state, CDevice device)
        {
            Debug.Log("NxrPlayerCtrl.onDeviceConnectState:" + (state == 0 ? " Connect " : " Disconnect "));
            if (state == 0)
            {
                // NibiruRemindBox.Instance.CalibrationDelay();
                NxrViewer.Instance.HideHeadControl();
                NxrViewer.Instance.SwitchControllerMode(true);
            }
        }

        private void Awake()
        {
            m_instance = this;
            mTransform = transform;
        }

        void Start()
        {
            HeadPosition = Vector3.zero;
            nxrArmModel = GetComponent<NxrArmModel>();

#if UNITY_ANDROID && !UNITY_EDITOR
           if(!InteractionManager.IsInteractionSDKEnabled())
            {
                ControllerAndroid.onStart();
                NibiruTaskApi.setOnDeviceListener(OnDeviceConnectState);
               
            }
#endif
        }

        public bool IsQuatConn()
        {
            if (debugInEditor) return true;

            if (gamepadEnabled)
            {
                if (NxrControllerHelper.Is3DofControllerConnected)
                {
                    return true;
                }

                if (InteractionManager.IsInteractionSDKEnabled())
                {
                    return InteractionManager.Is3DofControllerConnected();
                }
#if UNITY_ANDROID && !UNITY_EDITOR
                return ControllerAndroid.isQuatConn();
#endif
            }

            return false;
        }

        void Update()
        {
#if UNITY_ANDROID //&& !UNITY_EDITOR
            bool isQuatConn = IsQuatConn();
            if (debugInEditor)
            {
                isQuatConn = true;
            }

            bool isNeedShowController = isQuatConn ? controllerModelDisplay : false;

            if (!isCreateControllerHandler && isNeedShowController)
            {
                CreateControllerHandler();
                isCreateControllerHandler = true;
            }
            else if (isCreateControllerHandler && !isNeedShowController)
            {
                DestroyChild(mTransform);
                isCreateControllerHandler = false;
                debugInEditor = false;
            }

            if (isQuatConn)
            {
                //四元素
                if (InteractionManager.IsControllerConnected())
                {
                    float[] res = InteractionManager.GetControllerPose(InteractionManager.GetHandTypeByHandMode());
                    controllerQuat.x = res[0];
                    controllerQuat.y = res[1];
                    controllerQuat.z = res[2];
                    controllerQuat.w = res[3];
                }
                else
                {
                    float[] res = ControllerAndroid.getQuat(1);
                    controllerQuat.x = res[0];
                    controllerQuat.y = res[1];
                    controllerQuat.z = res[2];
                    controllerQuat.w = res[3];
                }

#if UNITY_EDITOR
                if (NxrViewer.Instance.RemoteDebug && NxrViewer.Instance.RemoteController)
                {
                    controllerQuat = EditorRemoteQuat;
                }
#endif

                //赋值 te.q为九轴传过来的四元数信息
                mTransform.rotation = controllerQuat;
                if (nxrArmModel != null)
                {
                    float factor = 1;
                    if (InteractionManager.IsInteractionSDKEnabled())
                    {
                        factor = InteractionManager.IsLeftControllerConnected() ? -1 : 1;
                    }
                    else if (ControllerAndroid.isQuatConn())
                    {
                        factor = ControllerAndroid.getHandMode() == 0 ? 1 : -1;
                    }

                    nxrArmModel.OnControllerInputUpdated();
                    Vector3 armPos = new Vector3(nxrArmModel.ControllerPositionFromHead.x * factor,
                        nxrArmModel.ControllerPositionFromHead.y, nxrArmModel.ControllerPositionFromHead.z);
                    mTransform.position = HeadPosition + armPos;
                }

                if (NxrSDKApi.Instance.Is3DofSpriteFirstLoad && !isTipsCreated)
                {
                    CreateTipImgs();
                }
                else if (isTipsCreated)
                {
                    ChangeTipAlpha();
                }
            }

#elif UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
            if (NxrControllerHelper.Is3DofControllerConnected)
            {
                if (!NxrInstantNativeApi.Inited) return;

                _prevStates = _currentStates;
                NxrInstantNativeApi.NibiruDeviceType deviceTypeOf3dof =
                    NxrControllerHelper.HandMode3DOF == NxrControllerHelper.LEFT_HAND_MODE
                        ? NxrInstantNativeApi.NibiruDeviceType.LeftController
                        : NxrInstantNativeApi.NibiruDeviceType.RightController;
                _currentStates = NxrInstantNativeApi.GetControllerStates(deviceTypeOf3dof);


                NxrInstantNativeApi.Nibiru_Pose pose =
                    NxrInstantNativeApi.GetPoseByDeviceType(NxrInstantNativeApi.NibiruDeviceType.RightController);
                mTransform.rotation =
                    new Quaternion(pose.rotation.x, pose.rotation.y, pose.rotation.z, pose.rotation.w);

                //3dof位移
                if (nxrArmModel != null)
                {
                    nxrArmModel.OnControllerInputUpdated();
                    mTransform.position = HeadPosition + nxrArmModel.ControllerPositionFromHead;
                }

                if (GetButtonDown(NxrTrackedDevice.ButtonID.TouchPad))
                {
                    int[] KeyAction = NibiruTaskApi.GetKeyAction();
                    KeyAction[CKeyEvent.KEYCODE_DPAD_CENTER] = 1;
                }

                if (GetButtonUp(NxrTrackedDevice.ButtonID.TouchPad))
                {
                    int[] KeyAction = NibiruTaskApi.GetKeyAction();
                    KeyAction[CKeyEvent.KEYCODE_DPAD_CENTER] = 0;
                }
            }
#endif
        }

        private NxrInstantNativeApi.Nibiru_ControllerStates _prevStates;
        private NxrInstantNativeApi.Nibiru_ControllerStates _currentStates;

        bool GetButtonDown(NxrTrackedDevice.ButtonID btn)
        {
            return (_currentStates.buttons & (1 << (int) btn)) != 0 && (_prevStates.buttons & (1 << (int) btn)) == 0;
        }

        bool GetButtonUp(NxrTrackedDevice.ButtonID btn)
        {
            return (_currentStates.buttons & (1 << (int) btn)) == 0 && (_prevStates.buttons & (1 << (int) btn)) != 0;
        }

        void OnApplicationPause()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ControllerAndroid.onPause();
#endif
        }

        void OnApplicationQuit()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ControllerAndroid.onStop();
#endif
        }

        /// <summary>
        /// Delete all child objects.
        /// </summary>
        /// <param name="_trsParent"></param>
        public void DestroyChild(Transform _trsParent)
        {
            for (int i = 0; i < _trsParent.childCount; i++)
            {
                GameObject go = _trsParent.GetChild(i).gameObject;
                Destroy(go);
            }
        }

        string Controller_Name_DEFAULT = "Handler_01";
        string Controller_Name_XIMMERSE = "Handler_03";
        string Controller_Name_CLEER = "Handler_04";

        string GetControllerName()
        {
            int deviceType = ControllerAndroid.getDeviceType();
            if (deviceType == CDevice.DEVICE_CLEER)
            {
                return Controller_Name_CLEER;
            }
            else if (deviceType == CDevice.DEVICE_XIMMERSE)
            {
                return Controller_Name_XIMMERSE;
            }

            return Controller_Name_DEFAULT;
        }

        bool DebugCtrlModelInEditor = false;
        public bool isNeedCustomModel;
        public string customModelPrefabName = "CustomModel";

        //创建手柄
        public void CreateControllerHandler()
        {
#if UNITY_EDITOR
            DebugCtrlModelInEditor = true;
#endif
            if (isNeedCustomModel)
            {
                if (string.IsNullOrEmpty(customModelPrefabName))
                {
                    Debug.LogError("The path of the custom handle model prefab is wrong");
                }
                else
                {
                    DestroyChild(mTransform);
                    var customModelPrefab =
                        Resources.Load<GameObject>(string.Concat("CustomModelPrefabs/", customModelPrefabName));
                    if (!customModelPrefab) Debug.LogError("The prefab was not created successfully");
                    else
                    {
                        var customModel = Instantiate(customModelPrefab);
                        customModel.transform.parent = mTransform;
                        var childTrans = customModel.transform.GetChild(0);
                        var prefabTrackDevice = childTrans.GetComponent<NxrTrackedDevice>();
                        prefabTrackDevice.enabled = false;
                        var nxrTrackedDevice = GetComponent<NxrTrackedDevice>();
                        if (nxrTrackedDevice != null)
                        {
                            nxrTrackedDevice.ReloadLaserPointer(childTrans.GetComponent<NxrLaserPointer>());
                        }

                        NxrViewer.Instance.SwitchControllerMode(true);
                    }

                    return;
                }
            }

            if (DebugCtrlModelInEditor || (InteractionManager.IsInteractionSDKEnabled() &&
                                           InteractionManager.IsSupportControllerModel()))
            {
                Debug.Log("CreateControllerModel.Controller3Dof...");
                CreateControllerModel("Controller3Dof", InteractionManager.GetControllerConfig());
                return;
            }

            string name = GetControllerName();
            Debug.Log("CreateControllerHandler." + name);
            DestroyChild(mTransform);
            GameObject handlerPrefabs = Resources.Load<GameObject>(string.Concat("Controller/", name));
            GameObject objHandler = Instantiate(handlerPrefabs);
            objHandler.transform.parent = mTransform;
            objHandler.transform.localPosition = new Vector3(0, 0, 0);
            objHandler.transform.localRotation = new Quaternion(0, 0, 0, 1);
            objHandler.transform.localScale = Vector3.one / 2;
            NxrTrackedDevice trackedDevice = GetComponent<NxrTrackedDevice>();
            if (trackedDevice != null)
            {
                trackedDevice.ReloadLaserPointer(objHandler.GetComponent<NxrLaserPointer>());
            }

            //close
            NxrViewer.Instance.SwitchControllerMode(true);
            Debug.Log("HideGaze.ForceUseReticle");
        }

        NxrTrackedDevice trackedDevice = null;

        public NxrLaserPointer GetControllerLaser()
        {
            if (trackedDevice == null)
            {
                trackedDevice = GetComponent<NxrTrackedDevice>();
            }

            if (trackedDevice == null)
            {
                return null;
            }

            if (trackedDevice.GetLaserPointer() == null)
            {
                return null;
            }

            return trackedDevice.GetLaserPointer();
        }

        public GameObject GetControllerLaserDot()
        {
            if (trackedDevice == null)
            {
                trackedDevice = GetComponent<NxrTrackedDevice>();
            }

            if (trackedDevice == null)
            {
                return null;
            }

            if (trackedDevice.GetLaserPointer() == null)
            {
                return null;
            }

            return trackedDevice.GetLaserPointer().GetLosDot();
        }

        public void ChangeControllerDisplay(bool show)
        {
            controllerModelDisplay = show;
        }

        public void CreateControllerModel(string objName, InteractionManager.ControllerConfig mControllerConfig)
        {
            string objPath = mControllerConfig.objPath;
            if (objPath == null) return;

            DestroyChild(mTransform);

            GameObject go = new GameObject(objName);
            NxrLaserPointer mNxrLaserPointer = go.AddComponent<NxrLaserPointer>();
            mNxrLaserPointer.deviceType = NxrInstantNativeApi.NibiruDeviceType.RightController;
            go.transform.SetParent(transform);
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = new Vector3(0, 0, 0);
            go.transform.localRotation = new Quaternion(0, 0, 0, 1);

            GameObject modelGOParent = new GameObject("model_P");
            modelGOParent.transform.SetParent(go.transform);
            modelGOParent.transform.localScale = new Vector3(-1, 1, 1);
            modelGOParent.transform.localPosition = new Vector3(0, 0, 0);
            modelGOParent.transform.localRotation = new Quaternion(0, 0, 0, 1);

            GameObject modelGO = new GameObject("model");
            modelGO.transform.SetParent(modelGOParent.transform);
            modelGO.transform.localScale = new Vector3(mControllerConfig.modelScale[0]
                , mControllerConfig.modelScale[1], mControllerConfig.modelScale[2]);
            modelGO.transform.localRotation = Quaternion.Euler(mControllerConfig.modelRotation[0],
                mControllerConfig.modelRotation[1], mControllerConfig.modelRotation[2]);
            modelGO.transform.localPosition = new Vector3(mControllerConfig.modelPosition[0]
                , mControllerConfig.modelPosition[1], mControllerConfig.modelPosition[2]);

            //  string objPath = "/system/etc/Objs/housing_bott.obj";
            Debug.Log("objPath=" + objPath);

            ObjModelLoader mObjModelLoader = GetComponent<ObjModelLoader>();
            if (mObjModelLoader == null)
            {
                gameObject.AddComponent<ObjMaterial>();
                mObjModelLoader = gameObject.AddComponent<ObjModelLoader>();
            }

            mObjModelLoader.LoadObjFile(objPath, modelGO.transform);

            GameObject powerGO = new GameObject("Power");
            powerGO.transform.SetParent(go.transform);

            MeshRenderer powerMeshRenderer = powerGO.AddComponent<MeshRenderer>();
            Mesh quadMesh = new Mesh();
            quadMesh.name = "QUAD";
            float quadSize = 0.5f;
            quadMesh.vertices = new Vector3[]
            {
                new Vector3(-1 * quadSize, -1 * quadSize, 0),
                new Vector3(-1 * quadSize, 1 * quadSize, 0),
                new Vector3(1 * quadSize, 1 * quadSize, 0),
                new Vector3(1 * quadSize, -1 * quadSize, 0)
            };
            quadMesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0)
            };
            int[] triangles = {0, 1, 2, 0, 2, 3};
            quadMesh.triangles = triangles;

            powerGO.AddComponent<MeshFilter>().mesh = quadMesh;
            powerGO.AddComponent<MeshCollider>();
            powerGO.AddComponent<NibiruControllerPower>();

            powerGO.transform.localPosition = new Vector3(mControllerConfig.batteryPosition[0],
                mControllerConfig.batteryPosition[1]
                , mControllerConfig.batteryPosition[2]);
            powerGO.transform.localRotation = Quaternion.Euler(mControllerConfig.batteryRotation[0],
                mControllerConfig.batteryRotation[1]
                , mControllerConfig.batteryRotation[2]);
            powerGO.transform.localScale = new Vector3(mControllerConfig.batteryScale[0],
                mControllerConfig.batteryScale[1]
                , mControllerConfig.batteryScale[2]);

            // 射线起点
            mNxrLaserPointer.SetHolderLocalPosition(new Vector3(mControllerConfig.rayStartPosition[0],
                mControllerConfig.rayStartPosition[1],
                mControllerConfig.rayStartPosition[2]));

            NxrTrackedDevice trackedDevice = GetComponent<NxrTrackedDevice>();
            if (trackedDevice != null)
            {
                trackedDevice.ReloadLaserPointer(mNxrLaserPointer);
            }

            //close
            NxrViewer.Instance.SwitchControllerMode(true);
            Debug.Log("HideGaze.ForceUseReticle2");
            CreateControllerTips(go.transform);
        }

        private GameObject tipsGo;
        private bool isTipsCreated;
        private string lastModelPath;

        /// <summary>
        /// Create the Parent GameObject of Controller Prompt UI and add components of Canvas and CanvasGroup.
        /// </summary>
        /// <param name="tipsParentTransform"></param>
        public void CreateControllerTips(Transform tipsParentTransform)
        {
            if(tipsGo) Destroy(tipsGo);  
#if UNITY_EDITOR
            tipsGo = Instantiate(Resources.Load("Controller/Objs/26/Canvas"), tipsParentTransform) as GameObject;
            tipsGo.transform.localPosition = Vector3.zero;
#elif UNITY_ANDROID
            Debug.Log("IsSptControllerTipUI："+NxrSDKApi.Instance.IsSptControllerTipUI());
            if (!NxrSDKApi.Instance.IsSptControllerTipUI()) return;
            tipsGo = new GameObject("Canvas");
            tipsGo.transform.SetParent(tipsParentTransform);
            var canvas = tipsGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            tipsGo.AddComponent<CanvasScaler>();
            tipsGo.AddComponent<GraphicRaycaster>();
            tipsGo.transform.localPosition = Vector3.zero;
            tipsGo.transform.localRotation = Quaternion.Euler(75, 0, 0);
            tipsGo.transform.localScale = Vector3.one * 0.001f;
            tipsGo.AddComponent<CanvasGroup>();
            RefreshSprites();
            Debug.Log("----------The GameObject of ControllerTips is created----------");
#endif
        }

        /// <summary>
        /// Get and Cache Controller Prompt UI.
        /// </summary>
        private void RefreshSprites()
        {
            InteractionManager.ControllerConfig controllerConfig = InteractionManager.GetControllerConfig();
            var controllerKeyInfoList = controllerConfig.KeyInfoList;
            if (controllerKeyInfoList == null)
            {
                Debug.LogError("----------KeyInfoList is null----------");
                return;
            }
            var imgsPath = new string[controllerKeyInfoList.Count];
            if (NxrSDKApi.Instance.Last3DofModelPath == null)
            {
                NxrSDKApi.Instance.Last3DofModelPath = controllerConfig.modelPath;
            }
            else
            {
                var currentModelPath = controllerConfig.modelPath;
                if (currentModelPath != NxrSDKApi.Instance.Last3DofModelPath) NxrSDKApi.Instance.Is3DofSpriteFirstLoad = false;
            }

            if (!NxrSDKApi.Instance.Is3DofSpriteFirstLoad)
            {
                NxrSDKApi.Instance.ClearCachSpriteDict();
                for (var i = 0; i < controllerKeyInfoList.Count; i++)
                {
                    var tipList = controllerKeyInfoList[i].tipList;
                    imgsPath[i] = tipList[(int) NxrSDKApi.Instance.GetTipLanguage()].picPath;
                }

                var spriteLoader = tipsGo.AddComponent<SpriteLoader>();
                spriteLoader.LoadSpriteFile(imgsPath);
            }

            isTipsCreated = false;
        }

        /// <summary>
        /// Create Controller Prompt UI and set the information of Position、Rotation and Scale.
        /// </summary>
        public void CreateTipImgs()
        {
            var controllerKeyInfoList = InteractionManager.GetControllerConfig().KeyInfoList;
            for (var i = 0; i < controllerKeyInfoList.Count; i++)
            {
                var tipGo = new GameObject(controllerKeyInfoList[i].desc);
                tipGo.transform.SetParent(tipsGo.transform);
                var img = tipGo.AddComponent<Image>();
                var tipList = controllerKeyInfoList[i].tipList;
                var currentControllerTip = tipList[(int) NxrSDKApi.Instance.GetTipLanguage()];
                var pos = currentControllerTip.position;
                var rotation = currentControllerTip.rotation;
                var scale = currentControllerTip.size;
                img.sprite = NxrSDKApi.Instance.GetSprite(currentControllerTip.picPath);
                img.SetNativeSize();
                img.transform.localPosition = new Vector3(pos[0], pos[1], pos[2]);
                img.transform.localRotation = new Quaternion(rotation[0], rotation[1], rotation[2], 1);
                img.transform.localScale = new Vector2(scale[0], scale[1]);
            }

            isTipsCreated = true;
            Debug.Log("----------The UI of ControllerTips is created----------");
        }

        /// <summary>
        /// Change the Alpha of Controller Prompt UI by judging the rotation angle of the x axis of the parent object.  
        /// </summary>
        public void ChangeTipAlpha()
        {
            if (!tipsGo) return;
            var parentRotationX = tipsGo.transform.parent.parent.localRotation.x;
            var tipsAlpha = parentRotationX / 0.5f;
            if (parentRotationX >= 0.0f && parentRotationX <= 0.5f)
            {
                tipsAlpha = Mathf.Clamp(tipsAlpha, 0.0f, 1.0f);
            }
            else
            {
                tipsAlpha = 0.0f;
            }

            tipsGo.GetComponent<CanvasGroup>().alpha = tipsAlpha;
            Debug.Log("The alpha of ControllerTips GameObject："+tipsAlpha);
        }

        public bool IsControllerExist()
        {
            return isCreateControllerHandler;
        }

        public Transform GetRayStartPoint()
        {
            return transform.GetChild(0);
        }

        public Transform GetRayEndPoint()
        {
            return GetControllerLaserDot().transform;
        }

        public Quaternion GetControllerQuaternion()
        {
            if (isCreateControllerHandler)
            {
                return transform.rotation;
            }

            return Quaternion.identity;
        }
    }
}