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
#if UNITY_EDITOR || UNITY_STANDALONE_WIN

using UnityEngine;
using System.Collections.Generic;
using NibiruAxis;

/// @cond
namespace Nxr.Internal
{
    // Sends simulated values for use when testing within the Unity Editor.
    public class EditorDevice : BaseARDevice
    {
        // Simulated neck model.  Vector from the neck pivot point to the point between the eyes.
        private static readonly Vector3 neckOffset = new Vector3(0, 0.075f, 0.08f);

        // Use mouse to emulate head in the editor.
        private float mouseX = 0;
        private float mouseY = 0;
        private float mouseZ = 0;

        private bool loadConfigData = false;
        private float[] deviceConfigData;
        Quaternion remoteQaut;
        public override void Init()
        {
            Input.gyro.enabled = true;
            // Debug.Log("RemoteDebug_" + NxrViewer.Instance.RemoteDebug);
            if (NxrViewer.Instance.RemoteDebug)
            {
                NxrViewer.Instance.InitialRecenter = false;
                NibiruEmulatorManager nibiruEmulatorManager = NibiruEmulatorManager.Instance;
                nibiruEmulatorManager.OnConfigDataEvent += ConfigDataLoaded;
                nibiruEmulatorManager.OnHmdPoseDataEvent += HmdPoseDataEvent;
                nibiruEmulatorManager.OnHmdStatusEvent += HmdStatusEvent;
                nibiruEmulatorManager.OnControllerPoseDataEvent += ControllerPoseDataEvent;
            }
        }

        private void ControllerPoseDataEvent(NibiruEmulatorClientSocket.ControllerPoseData data)
        {
            Loom.QueueOnMainThread((param) => {
                NibiruEmulatorClientSocket.ControllerPoseData controllerPoseData = (NibiruEmulatorClientSocket.ControllerPoseData) param;
                NibiruEmulatorClientSocket.TrackingQuat quat = controllerPoseData.right_controller_Pose_Orientation;
                NxrPlayerCtrl.Instance.EditorRemoteQuat = new Quaternion(quat.x, quat.y, quat.z, quat.w); 
            }, data);
        }

        private void HmdStatusEvent(NibiruEmulatorClientSocket.HmdStatusData data)
        {
            bool IsControllerConnect = data.controllerStatus == 1;
            Loom.QueueOnMainThread((param) => {
                NxrPlayerCtrl.Instance.debugInEditor = (bool) param;
            }, IsControllerConnect);
        }

        private void HmdPoseDataEvent(NibiruEmulatorClientSocket.HmdPoseData data)
        {
            Loom.QueueOnMainThread((param) => {
                NibiruEmulatorClientSocket.TrackingQuat quat = ((NibiruEmulatorClientSocket.HmdPoseData) param).HeadPose_Pose_Orientation;
                NibiruEmulatorClientSocket.TrackingVector3 pos = ((NibiruEmulatorClientSocket.HmdPoseData)param).HeadPose_Pose_Position;
                remoteQaut = new Quaternion(quat.x, quat.y, quat.z, quat.w);
            }, data);
        }

        private void ConfigDataLoaded(NibiruEmulatorClientSocket.OpticalConfigData data)
        {
            if (!loadConfigData)
            {
                loadConfigData = true;
                deviceConfigData = new float[14];
            }
        }

        public override bool SupportsNativeDistortionCorrection(List<string> diagnostics)
        {
            return false;  // No need for diagnostic message.
        }

        // Since we can check all these settings by asking Nvr.Instance, no need
        // to keep a separate copy here.
        public override void SetSplitScreenModeEnabled(bool enabled) { }

        private Quaternion initialRotation = Quaternion.identity;

        public override void UpdateState()
        {
            Quaternion rot = Quaternion.identity;

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                mouseX += Input.GetAxis("Mouse X") * 5;
                if (mouseX <= -180)
                {
                    mouseX += 360;
                }
                else if (mouseX > 180)
                {
                    mouseX -= 360;
                }
                mouseY -= Input.GetAxis("Mouse Y") * 2.4f;
                mouseY = Mathf.Clamp(mouseY, -85, 85);
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                mouseZ += Input.GetAxis("Mouse X") * 5;
                mouseZ = Mathf.Clamp(mouseZ, -85, 85);
            }

            bool IsNeedUpdatePose = false;
            if (Application.isEditor && NxrViewer.Instance.RemoteDebug)
            {
                if (remoteQaut.w != 0)
                {
                    rot = remoteQaut;
                    IsNeedUpdatePose = true;
                }
            }

            if (mouseX != 0 || mouseY != 0 || mouseZ != 0)
            {
                rot = Quaternion.Euler(mouseY, mouseX, mouseZ);
                IsNeedUpdatePose = true;
            }

            if (IsNeedUpdatePose)
            {
                headPose.Set(Vector3.zero, rot);
            }

#if UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
            // ÊÖ±ú¼üÖµ×´Ì¬
            if (NxrInstantNativeApi.Inited)
            {
                NxrInstantNativeApi.Nibiru_Pose pose = NxrInstantNativeApi.GetPoseByDeviceType(NxrInstantNativeApi.NibiruDeviceType.Hmd);
                if (pose.rotation.w == 0)
                {
                    pose.rotation.w = 1;
                }
                this.headPose.Set(pose.position, new Quaternion(pose.rotation.x, pose.rotation.y, -pose.rotation.z, -pose.rotation.w));
            }
#endif
            isHeadPoseUpdated = true;
        }

        public override void PostRender(RenderTexture stereoScreen)
        {
            // Do nothing.
        }

        public override void UpdateScreenData()
        {
            // configData is 0.062000,0.037250,0.039000,40.000000,40.000000,43.299999,43.299999,0.127560,0.084400,2560.000000,1440.000000,0.003000,0.120960,0.068040
            string deviceConfigInfo = "ar device config parameter : ";
            if (deviceConfigData != null)
            {
                for (int i = 0; i < deviceConfigData.Length; i++)
                {
                    deviceConfigInfo += deviceConfigData[i];
                    if (i < deviceConfigData.Length - 1)
                    {
                        deviceConfigInfo += ",";
                    }
                }
                Debug.Log(deviceConfigInfo);
            }

            Profile = NxrProfile.GetKnownProfile(NxrViewer.Instance.ScreenSize, NxrViewer.Instance.ViewerType);
            if (loadConfigData && deviceConfigData != null)
            {
                Profile.screen.width = deviceConfigData[12];
                Profile.screen.height = deviceConfigData[13];
                Profile.viewer = new NxrProfile.Viewer
                {
                    lenses = {
                      separation = deviceConfigData[0],
                      offset = deviceConfigData[1],
                      screenDistance = deviceConfigData[2],
                      alignment = NxrProfile.Lenses.AlignBottom,
                    },
                    maxFOV = {
                              outer = deviceConfigData[3],
                              inner = deviceConfigData[4],
                              upper = deviceConfigData[5],
                              lower = deviceConfigData[6]
                            },
                    distortion = {
                          Coef = new [] { deviceConfigData[7], deviceConfigData[8] },
                        },
                    inverse = NxrProfile.ApproximateInverse(new[] { deviceConfigData[7], deviceConfigData[8] })
                };

            }

            if(userIpd > 0)
            {
                Profile.viewer.lenses.separation = userIpd;
            }

            ComputeEyesFromProfile(1, 2000);
            profileChanged = true;
            Debug.Log("UpdateScreenData=" + Profile.viewer.lenses.separation);
        }

        public override void Recenter()
        {
            mouseX = mouseZ = 0;  // Do not reset pitch, which is how it works on the phone.
        }
        public override bool GazeApi(GazeTag tag, string param)
        {
            return true;
        }

        public override void SetCameraNearFar(float near, float far)
        {
            Debug.Log("EditorDevice.SetCameraNearFar : " + near + "," + far);
        }

        private bool isHeadPoseUpdated = false;
        private float userIpd = -1;
        public override void SetIpd(float ipd)
        {
            userIpd = ipd;
        }

        public override bool IsHeadPoseUpdated()
        {
            return isHeadPoseUpdated;
        }
    }
}
/// @endcond

#endif
