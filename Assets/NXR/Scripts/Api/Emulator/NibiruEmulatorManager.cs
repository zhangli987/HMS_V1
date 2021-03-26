using NibiruTask;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Nxr.Internal
{
    public class NibiruEmulatorManager : MonoBehaviour
    {
        public enum ControllerButtonID
        {
            Menu = 1,
            TouchPad = 0
        }

        private IEnumerator emulatorUpdate;
        private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        public static NibiruEmulatorManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var gameObject = new GameObject("XRDeviceRemote");
                    instance = gameObject.AddComponent<NibiruEmulatorManager>();
                    // This object should survive all scene transitions.
                    GameObject.DontDestroyOnLoad(instance);
                }
                return instance;
            }
        }

        private static NibiruEmulatorManager instance = null;
 
        // 事件
        public delegate void OnConfigDataLoaded(NibiruEmulatorClientSocket.OpticalConfigData data);
        public OnConfigDataLoaded OnConfigDataEvent;

        public delegate void OnHmdPoseData(NibiruEmulatorClientSocket.HmdPoseData data);
        public OnHmdPoseData OnHmdPoseDataEvent;

        public delegate void OnControllerPoseData(NibiruEmulatorClientSocket.ControllerPoseData data);
        public OnControllerPoseData OnControllerPoseDataEvent;

        public delegate void OnHmdStatus(NibiruEmulatorClientSocket.HmdStatusData data);
        public OnHmdStatus OnHmdStatusEvent;
        // 事件
        public int[] KeyStateController3DOF = new int[256];

        private Queue pendingEvents = Queue.Synchronized(new Queue());
        private NibiruEmulatorClientSocket socket;
        private long lastDownTimeMs;

        public bool Connected
        {
            get
            {
                return socket != null && socket.connected;
            }
        }

        public void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogWarning("ARDeviceRemote must be a singleton.");
                enabled = false;
                return;
            }

            for(int i=0; i< KeyStateController3DOF.Length; i++)
            {
                KeyStateController3DOF[i] = CKeyEvent.ACTION_UP;
            }
        }

        public void Start()
        {
            socket = gameObject.AddComponent<NibiruEmulatorClientSocket>();
            socket.Init(this);
            emulatorUpdate = EndOfFrame();
            StartCoroutine(emulatorUpdate);

            OnControllerPoseDataEvent += ControllerPoseDataEvent;
            OnHmdStatusEvent += HmdStatusEvent;
        }

        public bool IsLeftControllerConenct { set; get; }
        public bool IsRightControllerConenct { set; get; }
        private void HmdStatusEvent(NibiruEmulatorClientSocket.HmdStatusData data)
        {
            bool IsControllerConnect = data.controllerStatus == 1;
            if (IsControllerConnect)
            {
                IsLeftControllerConenct = data.controllerType == 0;
                IsRightControllerConenct = data.controllerType == 1;
            }
        }

        private void ControllerPoseDataEvent(NibiruEmulatorClientSocket.ControllerPoseData data)
        {
                Loom.QueueOnMainThread((param) => {
                NibiruEmulatorClientSocket.ControllerPoseData controllerPoseData = (NibiruEmulatorClientSocket.ControllerPoseData)param;
                int ControllerButton = IsRightControllerConenct ? controllerPoseData.rightControllerButton : controllerPoseData.leftControllerButton;
 
                bool MenuDown = (ControllerButton & (1 << (int)ControllerButtonID.Menu)) != 0;
                bool TouchPadDown = (ControllerButton & (1 << (int)ControllerButtonID.TouchPad)) != 0;
                if (MenuDown)
                {
                    KeyStateController3DOF[CKeyEvent.KEYCODE_CONTROLLER_MENU] = CKeyEvent.ACTION_DOWN;
                }
                else
                {
                    KeyStateController3DOF[CKeyEvent.KEYCODE_CONTROLLER_MENU] = CKeyEvent.ACTION_UP;
                }

                if (TouchPadDown)
                {
                    KeyStateController3DOF[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD] = CKeyEvent.ACTION_DOWN;
                }
                else
                {
                    KeyStateController3DOF[CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD] = CKeyEvent.ACTION_UP;
                }
                }, data);
        }

        IEnumerator EndOfFrame()
        {
            while (true)
            {
                yield return waitForEndOfFrame;
                lock (pendingEvents.SyncRoot)
                {
                    while (pendingEvents.Count > 0)
                    {
                        ARDeviceEvent phoneEvent = (ARDeviceEvent)pendingEvents.Dequeue();
                        ProcessEventAtEndOfFrame(phoneEvent);
                    }
                }
            }
        }

        public void OnPhoneEvent(ARDeviceEvent e)
        {
            pendingEvents.Enqueue(e);
        }

        private void ProcessEventAtEndOfFrame(ARDeviceEvent e)
        {
         

        }



    }
}