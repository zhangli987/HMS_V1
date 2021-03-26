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
using NibiruAxis;
using UnityEngine;

/// This script provides head tracking support for a camera.
///
/// Attach this script to any game object that should match the user's head motion.
/// By default, it continuously updates the local transform to NxrViewer.HeadView.
/// A target object may be specified to provide an alternate reference frame for the motion.
///
/// This script will typically be attached directly to a _Camera_ object, or to its
/// parent if you need to offset the camera from the origin.
/// Alternatively it can be inserted as a child of the _Camera_ but parent of the
/// NxrEye camera.  Do this if you already have steering logic driving the
/// mono Camera and wish to have the user's head motion be relative to that.  Note
/// that in the latter setup, head tracking is visible only when VR Mode is enabled.
///
/// In some cases you may need two instances of NxrHead, referring to two
/// different targets (one of which may be the parent), in order to split where
/// the rotation is applied from where the positional offset is applied.  Use the
/// #trackRotation and #trackPosition properties in this case.
namespace Nxr.Internal
{
    [AddComponentMenu("NXR/NxrHead")]
    public class NxrHead : MonoBehaviour
    {
        public Vector3 BasePosition { set; get; }

        /// Determines whether to apply the user's head rotation to this gameobject's
        /// orientation.  True means to update the gameobject's orientation with the
        /// user's head rotation, and false means don't modify the gameobject's orientation.
        private bool trackRotation = true;

        /// Determines whether to apply ther user's head offset to this gameobject's
        /// position.  True means to update the gameobject's position with the user's head offset,
        /// and false means don't modify the gameobject's position.
        private bool trackPosition = false;

        /// <summary>
        /// Whether track position
        /// </summary>
        /// <param name="b"></param>
        public void SetTrackPosition(bool b)
        {
            Debug.Log("NxrHead.SetTrackPosition." + b);
            trackPosition = b;
        }

        /// <summary>
        /// Whether track rotation
        /// </summary>
        /// <param name="b"></param>
        public void SetTrackRotation(bool b)
        {
            trackRotation = b;
        }

        public bool IsTrackRotation()
        {
            return trackRotation;
        }

        public bool IsTrackPosition()
        {
            return trackPosition;
        }

        public void Update3rdPartyPosition(Vector3 pos)
        {
            if (NxrViewer.Instance.UseThirdPartyPosition)
            {
                mTransform.position = pos;
            }
        }

        void Awake()
        {
            NxrViewer.Create();
        }

        protected Transform mTransform;

        public Transform GetTransform()
        {
            return mTransform;
        }

        void Start()
        {
            mTransform = this.transform;
        }

        // Normally, update head pose now.
        void LateUpdate()
        {
            NxrViewer.Instance.UpdateHeadPose();
            UpdateHead();
        }

        // 初始的Yaw欧拉角，有时进入时正方向有偏转，此时需要校正一下
        private float initEulerYAngle = float.MaxValue;

        private float totalTime = 0;
        private bool triggerLerp = false;
        // 初次进入，方向回正后，进行reset操作
        private bool hasResetTracker = true;
        private float moveSpeed = 2.0f;
        // Compute new head pose.
        private void UpdateHead()
        {
            if (NxrGlobal.hasInfinityARSDK)
            {
                trackRotation = false;
                trackPosition = false;
            }

            if (NxrViewer.Instance.GetNibiruService() != null && NxrViewer.Instance.GetNibiruService().IsMarkerRecognizeRunning)
            {
                if (NxrGlobal.isMarkerVisible)
                {
                    // Marker识别成功时，取消NxrHead的效果
                    trackRotation = false;
                    trackPosition = false;
                    return;
                }
                else
                {
                    // Marker识别失败时，恢复头部转动效果
                    trackRotation = true;
                    trackPosition = false;
                }
            }

            if (trackRotation)
            {
                float[] eulerRange = NxrViewer.Instance.GetHeadEulerAnglesRange();
               Quaternion rot = NxrViewer.Instance.HeadPose.Orientation;
                /*
                if (rot.eulerAngles.y != 0 && initEulerYAngle == float.MaxValue)
                {
                    initEulerYAngle = rot.eulerAngles.y;
                    if (float.IsNaN(initEulerYAngle))
                    {
                        Debug.Log("DATA IS ABNORMAL--------------------------->>>>>>>>>");
                        initEulerYAngle = float.MaxValue;
                    }
                }

                if (initEulerYAngle != float.MaxValue && NxrViewer.Instance.InitialRecenter && !triggerLerp
                     && (Mathf.Abs(initEulerYAngle) <= 345 && Mathf.Abs(initEulerYAngle) >= 15))
                {
                    // 初始位置有偏移，只要角度偏差很大时才进行校正操作
                    triggerLerp = true;
                    moveSpeed += (Mathf.Abs(initEulerYAngle) > 60 ? 0.3f : Mathf.Abs(initEulerYAngle) > 90 ? 0.5f : 0);
                    Debug.Log("triggerLerp.yaw=" + initEulerYAngle + ", sp=" + moveSpeed);
                }

                if (triggerLerp)
                {
                    totalTime += Time.deltaTime * 2.10f;
                    if (totalTime > 1)
                    {
                        if (!hasResetTracker)
                        {
                            // 校正完毕，进行reset操作
                            rot.eulerAngles = new Vector3(rot.eulerAngles.x, rot.eulerAngles.y - initEulerYAngle, rot.eulerAngles.z);
                            hasResetTracker = true;
                            NxrViewer.Instance.ResetHeadTrackerFromAndroid();
                        }
                    }
                    else
                    {
                        rot.eulerAngles = new Vector3(rot.eulerAngles.x, Mathf.LerpAngle(initEulerYAngle, 0, totalTime), rot.eulerAngles.z);
                    }

                }
                */

                if(!hasResetTracker)
                {
                    hasResetTracker = true;
                    NxrViewer.Instance.Recenter();
                }

                Vector3 eulerAngles = rot.eulerAngles;
                if (eulerRange == null ||
                        (
                        //  水平有限制
                        (eulerRange != null && (eulerAngles[1] >= eulerRange[0] || eulerAngles[1] < eulerRange[1]) &&
                        //   垂直有限制
                        (eulerAngles[0] >= eulerRange[2] || eulerAngles[0] < eulerRange[3]))
                        )
                    )
                {
                    mTransform.localRotation = rot;
                }

            }

#if UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
            Vector3 pos = NxrViewer.Instance.HeadPose.Position;
            if (pos.x !=0 && pos.y !=0 && pos.z != 0)
            {
                mTransform.localPosition = BasePosition + pos;
            }
#elif UNITY_ANDROID
            if (trackPosition)
            {
                Vector3 pos = NxrViewer.Instance.HeadPose.Position;
                mTransform.localPosition = BasePosition + pos;
                if (NxrPlayerCtrl.Instance != null)
                {
                    NxrPlayerCtrl.Instance.HeadPosition = mTransform.position;
                }
            }
#endif
        }

        public void ResetInitEulerYAngle()
        {
            initEulerYAngle = 0;
        }

#if UNITY_EDITOR
        private void Update()
        {
            Vector3 start = transform.position;
            Vector3 vector = transform.TransformDirection(Vector3.forward);
            UnityEngine.Debug.DrawRay(start, vector * 20, Color.red);
        }
#endif
    }
}