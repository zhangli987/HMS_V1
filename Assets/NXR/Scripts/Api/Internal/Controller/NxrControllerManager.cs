using UnityEngine;
using NibiruTask;
using System.Collections.Generic;
using NibiruAxis;
using UnityEngine.UI;

namespace Nxr.Internal
{
    public class NxrControllerManager : MonoBehaviour
    {
        public bool IsDebugInEditor;

        Dictionary<InteractionManager.NACTION_HAND_TYPE, NxrTrackedDevice> DualCtrlDeviceDict =
            new Dictionary<InteractionManager.NACTION_HAND_TYPE, NxrTrackedDevice>();

        bool[] DualCtrlConnected = new bool[5] {false, false, false, false, false};

        Transform transformCache;

        // Use this for initialization
        void Start()
        {
            transformCache = transform;
            DualCtrlDeviceDict.Clear();
            NxrSDKApi.Instance.ControllerStatusChangeEvent += OnControllerStatusChangeEvent;
        }

        private void Update()
        {
            if (IsDebugInEditor)
            {
                IsDebugInEditor = false;
                NxrSDKApi.Instance.ExecuteControllerStatusChangeEvent(InteractionManager.NACTION_HAND_TYPE.HAND_LEFT,
                    true, true);
                NxrSDKApi.Instance.ExecuteControllerStatusChangeEvent(InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT,
                    true, true);
            }

            if (InteractionManager.IsInteractionSDKEnabled() && InteractionManager.IsSixDofController &&
                DualCtrlDeviceDict.Count != 2)
            {
                if (InteractionManager.IsSixDofControllerConnected((int) InteractionManager.NACTION_HAND_TYPE.HAND_LEFT)
                    && !DualCtrlDeviceDict.ContainsKey(InteractionManager.NACTION_HAND_TYPE.HAND_LEFT))
                {
                    OnControllerStatusChangeEvent(InteractionManager.NACTION_HAND_TYPE.HAND_LEFT, true,
                        InteractionManager.IsSixDofController);
                }

                if (InteractionManager.IsSixDofControllerConnected(
                        (int) InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT)
                    && !DualCtrlDeviceDict.ContainsKey(InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT))
                {
                    OnControllerStatusChangeEvent(InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT, true,
                        InteractionManager.IsSixDofController);
                }
            }

            if (leftObj || rightObj)
            {
                InteractionManager.ControllerConfig controllerConfig = InteractionManager.GetControllerConfig();
                if (NxrSDKApi.Instance.Last6DofModelPath == null)
                {
                    NxrSDKApi.Instance.Last6DofModelPath = controllerConfig.modelPath;
                }
                else
                {
                    var currentModelPath = controllerConfig.modelPath;
                    if (currentModelPath != NxrSDKApi.Instance.Last6DofModelPath)
                    {
                        isLeftTipsGoGreated = false;
                        isRightTipsGoGreated = false;
                        NxrSDKApi.Instance.Is6DofSpriteFirstLoad = false;
                        isLoadSprites = false;
                        isLeftTipsCreated = false;
                        isRightTipsCreated = false;
                    }
                }

                if (leftObj && !isLeftTipsGoGreated)
                {
                    isLeftTipsGoGreated = true;
                    CreateControllerTips(leftObj);
                }

                if (rightObj && !isRightTipsGoGreated)
                {
                    isRightTipsGoGreated = true;
                    CreateControllerTips(rightObj);
                }

                if (leftTipsGo && !isLeftTipsCreated) CreateTipImgs(leftTipsGo);
                if (rightTipsGo && !isRightTipsCreated) CreateTipImgs(rightTipsGo);
                if (leftTipsGo && isLeftTipsCreated) ChangeTipAlpha(leftTipsGo);
                if (rightTipsGo && isRightTipsCreated) ChangeTipAlpha(rightTipsGo);
                if (leftModelGo && leftTipsGo) leftTipsGo.SetActive(leftModelGo.activeSelf);
                if (rightModelGo && rightTipsGo) rightTipsGo.SetActive(rightModelGo.activeSelf);
            }
        }

        void OnControllerStatusChangeEvent(InteractionManager.NACTION_HAND_TYPE handType, bool isConnected,
            bool isSixDofController)
        {
            if (!isSixDofController)
            {
                Debug.Log("---OnControllerStatusChangeEvent---isSixDofController=false-");
                return;
            }

            DualCtrlConnected[(int) handType] = isConnected;
            if (handType == InteractionManager.NACTION_HAND_TYPE.HAND_LEFT && isConnected)
            {
                Debug.Log("---CreateControllerModel---Left-");
                LoadControllerModel(NxrInstantNativeApi.NibiruDeviceType.LeftController, "Controller6DofLeft");
            }

            if (handType == InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT && isConnected)
            {
                Debug.Log("---CreateControllerModel---Right-");
                LoadControllerModel(NxrInstantNativeApi.NibiruDeviceType.RightController, "Controller6DofRight");
            }
        }

        private void LoadControllerModel(NxrInstantNativeApi.NibiruDeviceType nibiruDeviceType, string objName)
        {
            if (InteractionManager.IsSupportControllerModel())
            {
                if (NxrPlayerCtrl.Instance.isNeedCustomModel)
                {
                    if (string.IsNullOrEmpty(NxrPlayerCtrl.Instance.customModelPrefabName))
                    {
                        Debug.LogError("The path of the custom handle model prefab is wrong");
                    }
                    else
                    {
                        LoadDefaultCtrl(NxrPlayerCtrl.Instance.customModelPrefabName);
                        return;
                    }
                }

                InteractionManager.ControllerConfig mControllerConfig = InteractionManager.GetControllerConfig();
                CreateControllerModel(nibiruDeviceType, objName, mControllerConfig);
            }
            else
            {
                LoadDefaultCtrl("CustomModel");
            }
        }

        private void LoadDefaultCtrl(string prefabName)
        {
            GameObject obj = GameObject.Find(prefabName);
            if (!obj)
            {
                GameObject objPrefab = (GameObject) Resources.Load(string.Concat("CustomModelPrefabs/", prefabName));
                if (!objPrefab) Debug.LogError("The prefab was not created successfully");
                GameObject mGameObject =
                    (GameObject) Instantiate(objPrefab, Vector3.zero, Quaternion.identity);
                var leftObj = mGameObject.transform.GetChild(0).gameObject;
                leftObj.name = "LeftModel";
                var rightObj = Instantiate(leftObj, mGameObject.transform) as GameObject;
                rightObj.name = "RightModel";
                NxrTrackedDevice trackedDeviceLeft = leftObj.GetComponent<NxrTrackedDevice>();
                trackedDeviceLeft.deviceType = NxrInstantNativeApi.NibiruDeviceType.LeftController;
                NxrTrackedDevice trackedDeviceRight = rightObj.GetComponent<NxrTrackedDevice>();
                trackedDeviceLeft.deviceType = NxrInstantNativeApi.NibiruDeviceType.RightController;
                if (!DualCtrlDeviceDict.ContainsKey(InteractionManager.NACTION_HAND_TYPE.HAND_LEFT))
                {
                    DualCtrlDeviceDict.Add(InteractionManager.NACTION_HAND_TYPE.HAND_LEFT, trackedDeviceLeft);
                }
                else
                {
                    DualCtrlDeviceDict[InteractionManager.NACTION_HAND_TYPE.HAND_LEFT] = trackedDeviceLeft;
                }

                if (!DualCtrlDeviceDict.ContainsKey(InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT))
                {
                    DualCtrlDeviceDict.Add(InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT, trackedDeviceRight);
                }
                else
                {
                    DualCtrlDeviceDict[InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT] = trackedDeviceRight;
                }

                NxrViewer.Instance.SwitchControllerMode(true);
            }
        }

        private void CreateControllerModel(NxrInstantNativeApi.NibiruDeviceType deviceType, string objName,
            InteractionManager.ControllerConfig mControllerConfig)
        {
            string objPath = mControllerConfig.objPath;
            if (deviceType == NxrInstantNativeApi.NibiruDeviceType.LeftController)
            {
                objPath = mControllerConfig.leftCtrlObjPath;
            }
            else if (deviceType == NxrInstantNativeApi.NibiruDeviceType.RightController)
            {
                objPath = mControllerConfig.rightCtrlObjPath;
            }

            if (objPath == null)
            {
                Debug.LogError("CreateControllerModel failed, objPath is null......" + objName);
                return;
            }

            GameObject go = new GameObject(objName);
            NxrLaserPointer mNxrLaserPointer = go.AddComponent<NxrLaserPointer>();
            mNxrLaserPointer.deviceType = deviceType;
            go.transform.SetParent(transformCache);
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = new Vector3(0, 0, 0);
            go.transform.localRotation = new Quaternion(0, 0, 0, 1);

            GameObject modelGOParent = new GameObject("model_P");
            modelGOParent.transform.localScale = new Vector3(-1, 1, 1);
            modelGOParent.transform.localPosition = new Vector3(0, 0, 0);
            modelGOParent.transform.localRotation = new Quaternion(0, 0, 0, 1);
            modelGOParent.transform.SetParent(go.transform);

            GameObject modelGO = new GameObject("model");
            modelGO.transform.SetParent(modelGOParent.transform);
            modelGO.transform.localScale = new Vector3(mControllerConfig.modelScale[0]
                , mControllerConfig.modelScale[1], mControllerConfig.modelScale[2]);
            modelGO.transform.localRotation = Quaternion.Euler(mControllerConfig.modelRotation[0],
                mControllerConfig.modelRotation[1], mControllerConfig.modelRotation[2]);
            modelGO.transform.localPosition = new Vector3(mControllerConfig.modelPosition[0]
                , mControllerConfig.modelPosition[1], mControllerConfig.modelPosition[2]);
            modelGO.AddComponent<NxrControllerModel>();

            //  string objPath = "/system/etc/Objs/housing_bott.obj";
            Debug.Log("objPath=" + objPath);

            ObjModelLoader mObjModelLoader = go.GetComponent<ObjModelLoader>();
            if (mObjModelLoader == null)
            {
                go.AddComponent<ObjMaterial>();
                mObjModelLoader = go.AddComponent<ObjModelLoader>();
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

            NxrTrackedDevice trackedDevice = go.AddComponent<NxrTrackedDevice>();
            if (trackedDevice != null)
            {
                trackedDevice.ReloadLaserPointer(mNxrLaserPointer);
                trackedDevice.deviceType = deviceType;
                InteractionManager.NACTION_HAND_TYPE mHandType =
                    deviceType == NxrInstantNativeApi.NibiruDeviceType.LeftController
                        ? InteractionManager.NACTION_HAND_TYPE.HAND_LEFT
                        : InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT;

                if (!DualCtrlDeviceDict.ContainsKey(mHandType))
                {
                    DualCtrlDeviceDict.Add(mHandType, trackedDevice);
                }
                else
                {
                    DualCtrlDeviceDict[mHandType] = trackedDevice;
                }
            }

            if (deviceType == NxrInstantNativeApi.NibiruDeviceType.LeftController)
            {
                leftObj = go;
                leftModelGo = modelGO;
            }
            else if (deviceType == NxrInstantNativeApi.NibiruDeviceType.RightController)
            {
                rightObj = go;
                rightModelGo = modelGO;
            }

            //close
            NxrViewer.Instance.SwitchControllerMode(true);
            Debug.Log("HideGaze.ForceUseReticle3");
        }

        private GameObject leftObj, rightObj, leftModelGo, rightModelGo, leftTipsGo, rightTipsGo;
        private bool isLoadSprites, isLeftTipsGoGreated, isRightTipsGoGreated, isLeftTipsCreated, isRightTipsCreated;

        /// <summary>
        /// Create parent object of handle prompt UI.
        /// </summary>
        public void CreateControllerTips(GameObject obj)
        {
            GameObject tipsGo;
#if UNITY_EDITOR
            tipsGo =
                Instantiate(Resources.Load<GameObject>("Controller/Objs/108/Canvas"), obj.transform) as GameObject;
            tipsGo.transform.localPosition = Vector3.zero;
            if (leftObj && leftObj == obj) leftTipsGo = tipsGo;
            else if (rightObj && rightObj == obj) rightTipsGo = tipsGo;
#elif UNITY_ANDROID
            if (!NxrSDKApi.Instance.IsSptControllerTipUI()) return;
            tipsGo = new GameObject("Canvas");
            tipsGo.transform.SetParent(obj.transform);
            tipsGo.transform.localPosition = Vector3.zero;
            tipsGo.transform.localRotation = Quaternion.Euler(75, 0, 0);
            tipsGo.transform.localScale = Vector3.one * 0.001f;
            var canvas = tipsGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            tipsGo.AddComponent<CanvasScaler>();
            tipsGo.AddComponent<GraphicRaycaster>();
            tipsGo.AddComponent<CanvasGroup>();
            if (leftObj && leftObj == obj) leftTipsGo = tipsGo;
            else if (rightObj && rightObj == obj) rightTipsGo = tipsGo;
            if (!isLoadSprites)
            {
                isLoadSprites = true;
                RefreshSprites();
            }
#endif
        }

        /// <summary>
        /// Cach handle prompt UI.
        /// </summary>
        private void RefreshSprites()
        {
            var controllerKeyInfoList = InteractionManager.GetControllerConfig().KeyInfoList;
            if (controllerKeyInfoList == null)
            {
                Debug.LogError("----------KeyInfoList is null----------");
                return;
            }

            var imgsPath = new string[controllerKeyInfoList.Count];
            if (!NxrSDKApi.Instance.Is6DofSpriteFirstLoad)
            {
                NxrSDKApi.Instance.ClearCachSpriteDict();
                for (var i = 0; i < controllerKeyInfoList.Count; i++)
                {
                    var tipList = controllerKeyInfoList[i].tipList;
                    imgsPath[i] = tipList[(int) NxrSDKApi.Instance.GetTipLanguage()].picPath;
                }

                var spriteLoader = gameObject.AddComponent<SpriteLoader>();
                spriteLoader.LoadSpriteFile(imgsPath);
            }
        }

        /// <summary>
        /// Create handle prompt UI.
        /// </summary>
        public void CreateTipImgs(GameObject tipsGo)
        {
            if (!NxrSDKApi.Instance.Is6DofSpriteFirstLoad) return;
            var controllerKeyInfoList = InteractionManager.GetControllerConfig().KeyInfoList;
            for (var i = 0; i < controllerKeyInfoList.Count; i++)
            {
                if (controllerKeyInfoList[i].picName == "left.png" && rightTipsGo && rightTipsGo == tipsGo ||
                    controllerKeyInfoList[i].picName == "right.png" && leftTipsGo && leftTipsGo == tipsGo) continue;
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

            if (leftTipsGo && leftTipsGo == tipsGo) isLeftTipsCreated = true;
            if (rightTipsGo && rightTipsGo == tipsGo) isRightTipsCreated = true;
        }

        /// <summary>
        /// Change the Alpha of handle prompt UI by judging the angle of handle.  
        /// </summary>
        public void ChangeTipAlpha(GameObject tipsGo)
        {
            var parentRotationX = tipsGo.transform.parent.localEulerAngles.x;
            var tipsAlpha = (360.0f - parentRotationX) / 90.0f;
            if (parentRotationX >= 270.0f && parentRotationX <= 360.0f)
            {
                tipsAlpha = Mathf.Clamp(tipsAlpha, 0.0f, 1.0f);
            }
            else
            {
                tipsAlpha = 0.0f;
            }

            tipsGo.GetComponent<CanvasGroup>().alpha = tipsAlpha;
        }
    }
}