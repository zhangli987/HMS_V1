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

#if NIBIRU_VR
#define NXR_HACK
#endif

using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Rendering;

namespace Nxr.Internal
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("NXR/Controller/NxrStereoController")]
    public class NxrStereoController : MonoBehaviour
    {
        // Flags whether we rendered in stereo for this frame.
        private bool renderedStereo = false;

        // Cache for speed, except in editor (don't want to get out of sync with the scene).
        private NxrEye[] eyes;
        private NxrHead mHead;

        /// Returns an array of stereo cameras that are controlled by this instance of
        /// the script.
        /// @note This array is cached for speedier access.  Call
        /// InvalidateEyes if it is ever necessary to reset the cache.
        public NxrEye[] Eyes
        {
            get
            {
                if (eyes == null)
                {
                    eyes = GetComponentsInChildren<NxrEye>(true)
                           .Where(eye => eye.Controller == this)
                           .ToArray();
                }

                if (eyes == null)
                {
                    NxrEye[] NxrEyess = FindObjectsOfType<NxrEye>();
                    if (NxrEyess.Length > 0)
                    {
                        eyes = NxrEyess;
                    }
                }
                return eyes;
            }
        }

        /// Returns the nearest NxrHead that affects our eyes.
        /// @note Cached for speed.  Call InvalidateEyes to clear the cache.
        public NxrHead Head
        {
            get
            {
#if UNITY_EDITOR
                NxrHead mHead = null;  // Local variable rather than member, so as not to cache.
#endif
                if (mHead == null)
                {
                    mHead = FindObjectOfType<NxrHead>();
                }
                return mHead;
            }
        }

        public Camera cam { get; private set; }

        void Awake()
        {
            NxrViewer.Create();
            cam = GetComponent<Camera>();
            AddStereoRig();

            NxrOverrideSettings.OnProfileChangedEvent += OnProfileChanged;
        }

        void OnProfileChanged()
        {
            Debug.Log("OnProfileChanged");
            NxrEye[] eyes = NxrViewer.Instance.eyes;
            foreach (NxrEye eye in eyes)
            {
                if (eye != null)
                {
                    eye.UpdateCameraProjection();
                }
            }
        }

        /// Helper routine for creation of a stereo rig.  Used by the
        /// custom editor for this class, or to build the rig at runtime.
        public void AddStereoRig()
        {
            Debug.Log("AddStereoRig.CreateEye");
            CreateEye(NxrViewer.Eye.Left);
            CreateEye(NxrViewer.Eye.Right);

            if (Head == null)
            {
               gameObject.AddComponent<NxrHead>();
               // Don't track position for dynamically added Head components, or else
               // you may unexpectedly find your camera pinned to the origin.
            }
            Head.SetTrackPosition(NxrViewer.Instance.TrackerPosition);
        }

        // Helper routine for creation of a stereo eye.
        private void CreateEye(NxrViewer.Eye eye)
        {
            string nm = name + (eye == NxrViewer.Eye.Left ? " Left" : " Right");
            NxrEye[] eyes = GetComponentsInChildren<NxrEye>();
            NxrEye mNxrEye = null;
            if (eyes != null && eyes.Length > 0)
            {
                foreach(NxrEye mEye in eyes)
                {
                    if(mEye.eye == eye)
                    {
                        mNxrEye = mEye;
                        break;
                    }
                }
            }
            // 创建新的
            if (mNxrEye == null)
            {
                GameObject go = new GameObject(nm);
                go.transform.SetParent(transform, false);
                go.AddComponent<Camera>().enabled = false;
                mNxrEye = go.AddComponent<NxrEye>();
            }

            if(NxrOverrideSettings.OnEyeCameraInitEvent != null) NxrOverrideSettings.OnEyeCameraInitEvent(eye, mNxrEye.gameObject);

            mNxrEye.Controller = this;
            mNxrEye.eye = eye;
            mNxrEye.CopyCameraAndMakeSideBySide(this);
            mNxrEye.OnPostRenderListener += OnPostRenderListener;
            mNxrEye.OnPreRenderListener += OnPreRenderListener;
            NxrViewer.Instance.eyes[eye == NxrViewer.Eye.Left ? 0 : 1] = mNxrEye;
            Debug.Log("CreateEye:" + nm + (eyes == null));
        }

        void OnPreRenderListener(int cacheTextureId, NxrViewer.Eye eyeType)
        {
            if (NxrGlobal.isVR9Platform) return;
            if (NxrViewer.USE_DTR && NxrGlobal.supportDtr)
            {
                // 左右眼绘制开始
                NibiruRenderEventType eventType = eyeType == NxrViewer.Eye.Left ? NibiruRenderEventType.LeftEyeBeginFrame : NibiruRenderEventType.RightEyeBeginFrame;
                NxrPluginEvent.IssueWithData(eventType, cacheTextureId);
                if (NxrGlobal.DEBUG_LOG_ENABLED) Debug.Log("OnPreRender.eye[" + eyeType + "]");
            }
        }

        void OnPostRenderListener(int cacheTextureId, NxrViewer.Eye eyeType)
        {
            if (NxrGlobal.isVR9Platform)
            {
                if (eyeType == NxrViewer.Eye.Right && Application.isMobilePlatform)
                {
                    if (NxrGlobal.DEBUG_LOG_ENABLED) Debug.Log("OnPostRenderListener.PrepareFrame.Right");
                    NxrPluginEvent.Issue(NibiruRenderEventType.PrepareFrame);
                }
                return;
            }

            if (NxrViewer.USE_DTR && NxrGlobal.supportDtr)
            {
                // 左右眼绘制结束
                NibiruRenderEventType eventType = eyeType == NxrViewer.Eye.Left ? NibiruRenderEventType.LeftEyeEndFrame : NibiruRenderEventType.RightEyeEndFrame;
                // 左右眼绘制结束事件
                // int eyeTextureId = (int)cam.targetTexture.GetNativeTexturePtr();
                NxrPluginEvent.IssueWithData(eventType, cacheTextureId);
                if(NxrGlobal.DEBUG_LOG_ENABLED) Debug.Log("OnPostRender.eye[" + eyeType + "]");
            }

            if (NxrViewer.USE_DTR && eyeType == NxrViewer.Eye.Right)
            {
                // NxrViewer.Instance.EnterXRMode();
            }
        }

        void OnEnable()
        {
#if UNITY_2019_1_OR_NEWER
            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null)
            {
                RenderPipelineManager.beginCameraRendering += CameraPreRender;
            }
#endif
            StartCoroutine("EndOfFrame");

        }

        void OnDisable()
        {
#if UNITY_2019_1_OR_NEWER
            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null)
            {
                RenderPipelineManager.beginCameraRendering -= CameraPreRender;
            }
#endif
            StopCoroutine("EndOfFrame");

        }

#if UNITY_2019_1_OR_NEWER
        public void CameraPreRender(ScriptableRenderContext context, Camera mcam)
        {
            if (mcam.gameObject == cam.gameObject)
            {
                OnPreCull();
            }
        }
#endif

        void OnPreCull()
        {
            if (NxrViewer.Instance.SplitScreenModeEnabled)
            {
                // Activate the eyes under our control.
                NxrEye[] eyes = Eyes;
                for (int i = 0, n = eyes.Length; i < n; i++)
                {
                    if(!eyes[i].cam.enabled) eyes[i].cam.enabled = true;
                }
                // Turn off the mono camera so it doesn't waste time rendering.  Remember to reenable.
                // @note The mono camera is left on from beginning of frame till now in order that other game
                // logic (e.g. referring to Camera.main) continues to work as expected.  center camera is only used for raycasting 
#if NXR_HACK
#warning Due to a Unity bug, a worldspace canvas in a camera that renders to a RenderTexture allocates infinite memory. Remove the hack ASAP as the fix gets released.
                BlackOutMonoCamera();
#else
                if (!NxrViewer.Instance.IsWinPlatform) cam.enabled = false;
#endif
                renderedStereo = true;
            }

        }

        public void EndOfFrameCore()
        {
            // If *we* turned off the mono cam, turn it back on for next frame.
            if (renderedStereo)
            {
#if NXR_HACK
                RestoreMonoCamera();
#else
                    cam.enabled = true;
#endif
                renderedStereo = false;
            }
        }
 
        IEnumerator EndOfFrame()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                EndOfFrameCore();
            }
        }

#if NXR_HACK
        private CameraClearFlags m_MonoCameraClearFlags;
        private Color m_MonoCameraBackgroundColor;
        private int m_MonoCameraCullingMask;

        private void BlackOutMonoCamera()
        {   
            if (NxrViewer.Instance.IsWinPlatform) return;
            m_MonoCameraClearFlags = cam.clearFlags;
            m_MonoCameraBackgroundColor = cam.backgroundColor;
            m_MonoCameraCullingMask = cam.cullingMask;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;
        }

        private void RestoreMonoCamera()
        {
            if (NxrViewer.Instance.IsWinPlatform) return;
            cam.clearFlags = m_MonoCameraClearFlags;
            cam.backgroundColor = m_MonoCameraBackgroundColor;
            cam.cullingMask = m_MonoCameraCullingMask;
        }
#endif
    }


}
