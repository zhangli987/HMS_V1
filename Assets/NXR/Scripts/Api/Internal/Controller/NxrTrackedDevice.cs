using NibiruAxis;
using NibiruTask;
using UnityEngine;

namespace Nxr.Internal
{
    public class NxrTrackedDevice : MonoBehaviour
    {
        public enum ButtonID
        {
            Trigger = 0,
            Grip = 1,
            Menu = 21,
            System = -1,
            TouchPad = 20,
            DPadUp = 5,
            DPadDown = 4,
            DPadLeft = 2,
            DPadRight = 3,
            DPadCenter = 6,
            TrackpadTouch = 7,
            InternalTrigger = 8,
        }

        public NxrInstantNativeApi.NibiruDeviceType deviceType;
        NxrInstantNativeApi.Nibiru_ControllerStates _prevStates;
        NxrInstantNativeApi.Nibiru_ControllerStates _currentStates;

        NxrControllerModel controllerModel;
        NibiruControllerPower controllerPower;
        NxrLaserPointer laserPointer;
        public bool isGamePad;
        public bool DebugInEditor;

        public NxrLaserPointer GetLaserPointer()
        {
            return laserPointer;
        }

        public void ReloadLaserPointer(NxrLaserPointer laserPointerIn)
        {
            this.laserPointer = laserPointerIn;
            if (laserPointer != null)
            {
                laserPointer.PointerIn += PointerInEventHandler;
                laserPointer.PointerOut += PointerOutEventHandler;
            }
        }

        private void Start()
        {
            isGamePad = gameObject.name.Contains("Gamepad");
            laserPointer = GetComponent<NxrLaserPointer>();
            if (laserPointer != null)
            {
                laserPointer.PointerIn += PointerInEventHandler;
                laserPointer.PointerOut += PointerOutEventHandler;
            }

            if (!isGamePad)
            {
                controllerModel = GetComponentInChildren<NxrControllerModel>();
                controllerPower = GetComponentInChildren<NibiruControllerPower>();

                if (controllerModel != null) controllerModel.gameObject.SetActive(false);
                if (controllerPower != null) controllerPower.gameObject.SetActive(false);
            }
#if UNITY_ANDROID
            NibiruTaskApi.deviceConnectState += OnDeviceConnectState;
            _currentStates = new NxrInstantNativeApi.Nibiru_ControllerStates();
            _prevStates = new NxrInstantNativeApi.Nibiru_ControllerStates();
            _currentStates.connectStatus = 0;
            _prevStates.connectStatus = 0;

            _currentStates.buttons = 0;
            _currentStates.touches = 0;
            _prevStates.buttons = 0;
            _prevStates.touches = 0;
#endif
        }

        private void OnDestroy()
        {
            if (laserPointer != null)
            {
                laserPointer.PointerIn -= PointerInEventHandler;
                laserPointer.PointerOut -= PointerOutEventHandler;
            }
#if UNITY_ANDROID
            NibiruTaskApi.deviceConnectState -= OnDeviceConnectState;
#endif
        }

        private int GetNoloType()
        {
            int noloType = (int) CDevice.NOLO_TYPE.NONE;
            if (deviceType == NxrInstantNativeApi.NibiruDeviceType.LeftController)
            {
                noloType = (int) CDevice.NOLO_TYPE.LEFT;
            }
            else if (deviceType == NxrInstantNativeApi.NibiruDeviceType.RightController)
            {
                noloType = (int) CDevice.NOLO_TYPE.RIGHT;
            }
            else if (deviceType == NxrInstantNativeApi.NibiruDeviceType.Hmd)
            {
                noloType = (int) CDevice.NOLO_TYPE.HEAD;
            }

            return noloType;
        }

        public void OnDeviceConnectState(int state, CDevice device)
        {
            if (device.getType() != CDevice.DEVICE_NOLO_SIXDOF) return;
            //0=connect,1=disconnect
            Debug.Log("NxrTrackedDevice.onDeviceConnectState:" + state + "," + device.getType() + "," + device.getName() + "," + device.getMode() + "," +
                device.getisQuat());
            if(state == 0)
            {
                _currentStates.connectStatus = 1;
                NxrViewer.Instance.SwitchControllerMode(true);
            }
            else
            {
                _currentStates.connectStatus = 0;
            }
        }

        void PointerInEventHandler(object sender, PointerEventArgs e)
        {
            NxrControllerHelper.ControllerRaycastObject = e.target.gameObject;
            //Debug.Log("PointerInEventHandler---------" + e.target.gameObject.name);
        }

        void PointerOutEventHandler(object sender, PointerEventArgs e)
        {
            NxrControllerHelper.ControllerRaycastObject = null;
            //Debug.Log("PointerOutEventHandler---------" + e.target.gameObject.name);
        }

        // Update is called once per frame
        void Update()
        {
#if UNITY_ANDROID //&& !UNITY_EDITOR
            if (!NxrViewer.Instance.IsWinPlatform && !isGamePad)
            {
                // Android/NOLO
                int noloType = GetNoloType();
                int handType = noloType == (int)CDevice.NOLO_TYPE.LEFT ? (int)InteractionManager.NACTION_HAND_TYPE.HAND_LEFT : (int)InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT;
                bool isConnected = false;
                if (InteractionManager.IsInteractionSDKEnabled())
                {
                    isConnected = InteractionManager.IsSixDofControllerConnected(handType);
                }
                else
                {
                    isConnected = ControllerAndroid.isDeviceConn(noloType);
                }

                if (DebugInEditor)
                {
                    isConnected = true;
                }

                if (_currentStates.connectStatus == 0 && isConnected)
                {
                    NxrViewer.Instance.SwitchControllerMode(true);
                    _currentStates.connectStatus = 1;
                    NxrPlayerCtrl mNxrPlayerCtrl = NxrPlayerCtrl.Instance;
                    if (mNxrPlayerCtrl != null)
                    {
                        // 关闭3dof手柄显示
                        mNxrPlayerCtrl.GamepadEnabled = false;
                        // 关闭白点显示
                    }
                }
                else if (_currentStates.connectStatus == 1 && !isConnected)
                {
                    _currentStates.connectStatus = 0;
                }

                //Debug.Log("status="+_currentStates.connectStatus + ", " + deviceType.ToString() + ","
                //    + ControllerAndroid.isDeviceConn((int) CDevice.NOLO_TYPE.LEFT) + ", " + ControllerAndroid.isDeviceConn((int)CDevice.NOLO_TYPE.RIGHT));

                if (!IsConneted() && controllerModel != null && controllerModel.gameObject.activeSelf)
                {
                    if (controllerPower != null) controllerPower.gameObject.SetActive(false);
                    controllerModel.gameObject.SetActive(false);
                    laserPointer.holder.SetActive(false);
                    NxrControllerHelper.ControllerRaycastObject = null;
                    Debug.Log("controllerModel Dismiss " + deviceType + "," + controllerModel.gameObject.activeSelf);
                }
                else if (IsConneted() && controllerModel != null && !controllerModel.gameObject.activeSelf)
                {
                    if (controllerPower != null) controllerPower.gameObject.SetActive(true);
                    controllerModel.gameObject.SetActive(true);
                    laserPointer.holder.SetActive(true);
                    Debug.Log("controllerModel Show " + deviceType);
                }

                if (GetButtonUp(ButtonID.InternalTrigger))
                {
                    NxrSDKApi.Instance.SixDofControllerPrimaryDeviceType = deviceType;
                    Debug.Log("IsPrimaryControllerHand " + deviceType.ToString());
                }

                if (laserPointer != null && controllerModel != null)
                {
                    if (NxrSDKApi.Instance.SixDofControllerPrimaryDeviceType != deviceType)
                    {
                        // 非主手柄，隐藏射线
                        laserPointer.holder.SetActive(false);
                    }
                    else
                    {
                        laserPointer.holder.SetActive(controllerModel.gameObject.activeSelf);
                    }
                }

                if (IsConneted())
                {
                    processControllerKeyEvent(noloType);
                    float[] poseData = new float[8];
                    if (InteractionManager.IsSixDofControllerConnected(handType))
                    {
                        float[] pose = InteractionManager.GetControllerPose((InteractionManager.NACTION_HAND_TYPE) handType);
                        poseData[1] = pose[4];
                        poseData[2] = pose[5];
                        poseData[3] = pose[6];
                        for (int i = 0; i < 4; i++)
                        {
                            poseData[4 + i] = pose[i];
                        }
                    }
                    else
                    {
                        poseData = ControllerAndroid.getCPoseEvent(noloType, 1);
                    }

                    if(NxrViewer.Instance.SixDofMode == SixDofMode.Head_3Dof_Ctrl_6Dof)
                    {
                        Vector3 HeadPosition = NxrSDKApi.Instance.HeadPosition;
                        // Debug.LogError(noloType + ".NOLO Controller Y: " + poseData[2] + "/HeadY=" + HeadPosition.y);
                        poseData[1] = poseData[1] - HeadPosition.x;
                        poseData[2] = poseData[2] - HeadPosition.y;
                        poseData[3] = poseData[3] - HeadPosition.z;
                    }
                    else if (NxrViewer.Instance.SixDofMode == SixDofMode.Head_3Dof_Ctrl_3Dof)
                    {
                        var factor = handType == (int) InteractionManager.NACTION_HAND_TYPE.HAND_LEFT ? -1 : 1;
                        var headPos = NxrViewer.Instance.GetHead().transform.localPosition;
                        var pos = new Vector3(headPos.x + 0.25f * factor, headPos.y - 0.6f, headPos.z + 0.5f);
                        poseData[1] = pos.x;
                        poseData[2] = pos.y;
                        poseData[3] = pos.z;
                    }

                    transform.localPosition = new Vector3(poseData[1], poseData[2], poseData[3]);
                    transform.localRotation = new Quaternion(poseData[4], poseData[5], poseData[6], poseData[7]);
                }
            }
#endif

#if UNITY_STANDALONE_WIN
            if (NxrInstantNativeApi.Inited)
            {
                _prevStates = _currentStates;
                _currentStates = NxrInstantNativeApi.GetControllerStates(deviceType);
                if (isGamePad)
                {
                    // 3dof手柄左右手模式切换
                    if (!IsConneted())
                    {
                        _currentStates = NxrInstantNativeApi.GetControllerStates(NxrInstantNativeApi.NibiruDeviceType.LeftController);
                        if (!IsConneted())
                        {
                            deviceType = NxrInstantNativeApi.NibiruDeviceType.RightController;
                        }
                        else
                        {
                            NxrControllerHelper.HandMode3DOF = NxrControllerHelper.LEFT_HAND_MODE;
                            deviceType = NxrInstantNativeApi.NibiruDeviceType.LeftController;
                            Debug.Log("Current 3dof HandMode is Left !!!");
                        }
                    }
                }

                if (!IsConneted() && controllerModel != null && controllerModel.gameObject.activeSelf)
                {
                    controllerModel.gameObject.SetActive(false);
                    laserPointer.holder.SetActive(false);
                    if (deviceType == NxrInstantNativeApi.NibiruDeviceType.LeftController)
                    {
                        NxrControllerHelper.IsLeftNoloControllerConnected = false;
                    }
                    else if (deviceType == NxrInstantNativeApi.NibiruDeviceType.RightController)
                    {
                        NxrControllerHelper.IsRightNoloControllerConnected = false;
                    }
                    Debug.Log("controllerModel Dismiss " + deviceType + "," + controllerModel.gameObject.activeSelf);
                }
                else if (IsConneted() && controllerModel != null && !controllerModel.gameObject.activeSelf)
                {
                    controllerModel.gameObject.SetActive(true);
                    laserPointer.holder.SetActive(true);

                    if (NxrControllerHelper.ControllerType == (int)NxrInstantNativeApi.NibiruControllerId.NOLO)
                    {
                        if (deviceType == NxrInstantNativeApi.NibiruDeviceType.LeftController)
                        {
                            NxrControllerHelper.IsLeftNoloControllerConnected = true;
                        }
                        else if (deviceType == NxrInstantNativeApi.NibiruDeviceType.RightController)
                        {
                            NxrControllerHelper.IsRightNoloControllerConnected = true;
                        }
                    }
                    Debug.Log("controllerModel Show " + deviceType);
                }
                else if (!IsConneted() && isGamePad && NxrControllerHelper.Is3DofControllerConnected)
                {
                    NxrControllerHelper.Is3DofControllerConnected = false;
                    Debug.Log("Controller 3dof Dismiss.");
                }
                else if (IsConneted() && isGamePad && !NxrControllerHelper.Is3DofControllerConnected)
                {
                    if (NxrControllerHelper.ControllerType == (int)NxrInstantNativeApi.NibiruControllerId.NORMAL_3DOF)
                    {
                        NxrControllerHelper.Is3DofControllerConnected = true;
                    }
                    Debug.Log("Controller 3dof Show." + NxrControllerHelper.ControllerType);
                }

                if (!isGamePad)
                {
                    NxrInstantNativeApi.Nibiru_Pose pose = NxrInstantNativeApi.GetPoseByDeviceType(deviceType);
                    // NOLO 非3DOF
                    transform.localPosition = pose.position;
                    transform.localRotation = new Quaternion(pose.rotation.x, pose.rotation.y, pose.rotation.z, pose.rotation.w);
                }
            }
#endif
        }

        private int[] lastState;
        private int[] curState;

        private void initState()
        {
            if (lastState == null)
            {
                lastState = new int[256];
                curState = new int[256];
                for (int i = 0; i < 256; i++)
                {
                    curState[i] = -1;
                    lastState[i] = -1;
                }
            }
        }

        private void processControllerKeyEvent(int noloType)
        {
            int handType = noloType == (int)CDevice.NOLO_TYPE.LEFT ? (int)InteractionManager.NACTION_HAND_TYPE.HAND_LEFT : (int)InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT;
            initState();

            _prevStates = _currentStates;
            lastState = curState;
            float[] touchInfo = new float[] {0, CKeyEvent.ACTION_UP, 0, 0}; // type-action-x-y
            if (InteractionManager.IsInteractionSDKEnabled())
            {
                curState = InteractionManager.GetKeyAction(handType);
                touchInfo[1] = curState[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH];
                if (noloType == (int) CDevice.NOLO_TYPE.LEFT)
                {
                    Vector3 pos = InteractionManager.TouchPadPositionLeft;
                    touchInfo[2] = pos.x;
                    touchInfo[3] = pos.y;
                }
                else
                {
                    Vector3 pos = InteractionManager.TouchPadPositionRight;
                    touchInfo[2] = pos.x;
                    touchInfo[3] = pos.y;
                }
            }
            else
            {
                touchInfo = ControllerAndroid.getTouchEvent(noloType, 1);
                curState = ControllerAndroid.getKeyState(noloType, 1);
            }

            // N
            int btnNibiru = curState[CKeyEvent.KEYCODE_BUTTON_NIBIRU];
            int btnStart = curState[CKeyEvent.KEYCODE_BUTTON_START];
            // Side A/B
            int btnSelect = curState[CKeyEvent.KEYCODE_BUTTON_SELECT];
            // Menu
            int btnApp = curState[CKeyEvent.KEYCODE_BUTTON_APP];
            // TouchPad
            int btnCenter = curState[CKeyEvent.KEYCODE_DPAD_CENTER];
            // Trigger
            int btnR1 = curState[CKeyEvent.KEYCODE_BUTTON_R1];
            int btnR2 = curState[CKeyEvent.KEYCODE_BUTTON_R2];
            int btnInternalTrigger = curState[CKeyEvent.KEYCODE_CONTROLLER_TRIGGER_INTERNAL];
            // Nolo TouchPad = Center
            // Nolo Menu = App
            // Nolo Trigger = R1
            // Nolo Side = Select
            // Debug.LogError("=======>_currentStates.buttons=" + _currentStates.buttons);
            if (touchInfo[1] == CKeyEvent.ACTION_MOVE)
            {
                _currentStates.touches |= 1 << (int) ButtonID.TrackpadTouch;
                _currentStates.touchpadAxis = new Vector2(touchInfo[2], touchInfo[3]);
            }
            else if (touchInfo[1] == CKeyEvent.ACTION_UP &&
                     ((_currentStates.touches & (1 << (int) ButtonID.TrackpadTouch)) != 0))
            {
                _currentStates.touches = 0;
                _currentStates.touchpadAxis = new Vector2(0, 0);
            }

            if (btnCenter == 0)
            {
                // down
                _currentStates.buttons |= 1 << (int) ButtonID.TouchPad;
            }
            else if (lastState[CKeyEvent.KEYCODE_DPAD_CENTER] == 0)
            {
                // up
                _currentStates.buttons -= 1 << (int) ButtonID.TouchPad;
            }

            if (btnApp == 0)
            {
                _currentStates.buttons |= 1 << (int) ButtonID.Menu;
            }
            else if (lastState[CKeyEvent.KEYCODE_BUTTON_APP] == 0)
            {
                _currentStates.buttons -= 1 << (int) ButtonID.Menu;
            }

            if (btnR1 == 0 || btnR2 == 0)
            {
                _currentStates.buttons |= 1 << (int) ButtonID.Trigger;
            }
            else if (lastState[CKeyEvent.KEYCODE_BUTTON_R1] == 0 || lastState[CKeyEvent.KEYCODE_BUTTON_R2] == 0)
            {
                _currentStates.buttons -= 1 << (int) ButtonID.Trigger;
            }

            if (btnInternalTrigger == 0)
            {
                _currentStates.buttons |= 1 << (int) ButtonID.InternalTrigger;
            }
            else if (lastState[CKeyEvent.KEYCODE_CONTROLLER_TRIGGER_INTERNAL] == 0)
            {
                _currentStates.buttons -= 1 << (int) ButtonID.InternalTrigger;
            }

            if (btnSelect == 0)
            {
                _currentStates.buttons |= 1 << (int) ButtonID.Grip;
            }
            else if (lastState[CKeyEvent.KEYCODE_BUTTON_SELECT] == 0)
            {
                _currentStates.buttons -= 1 << (int) ButtonID.Grip;
            }

            //Debug.LogError("=====>" + _currentStates.buttons + "->Start=" + btnStart +
            //   "->Nibiru=" + btnNibiru +
            //   "->Select=" + btnSelect +
            //   "->App=" + btnApp +
            //   "->Center=" + btnCenter +
            //    "->R1=" + btnR1);
        }


        public bool IsConneted()
        {
            return _currentStates.connectStatus == 1;
        }

        public bool GetButtonDown(ButtonID btn)
        {
            return (_currentStates.buttons & (1 << (int) btn)) != 0 && (_prevStates.buttons & (1 << (int) btn)) == 0;
        }

        public bool GetButtonUp(ButtonID btn)
        {
            return (_currentStates.buttons & (1 << (int) btn)) == 0 && (_prevStates.buttons & (1 << (int) btn)) != 0;
        }

        public bool GetButtonPressed(ButtonID btn)
        {
            return (_currentStates.buttons & (1 << (int) btn)) != 0;
        }

        public bool GetTouchPressed(ButtonID btn)
        {
            return (_currentStates.touches & (1 << (int) btn)) != 0;
        }

        public bool GetTouchDown(ButtonID btn)
        {
            return (_currentStates.touches & (1 << (int) btn)) != 0 && (_prevStates.touches & (1 << (int) btn)) == 0;
        }

        public bool GetTouchUp(ButtonID btn)
        {
            return (_currentStates.touches & (1 << (int) btn)) == 0 && (_prevStates.touches & (1 << (int) btn)) != 0;
        }

        public Vector2 GetTouchPosition(ButtonID axisIndex = ButtonID.TrackpadTouch)
        {
            if ((_currentStates.touches & (1 << (int) axisIndex)) != 0)
            {
                return new Vector2(_currentStates.touchpadAxis.x, _currentStates.touchpadAxis.y);
            }

            return Vector2.zero;
        }
    }
}