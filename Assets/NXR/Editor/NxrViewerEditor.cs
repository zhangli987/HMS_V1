// Copyright 2016 Nibiru Inc. All rights reserved.
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

using System.IO;
using UnityEngine;
using UnityEditor;

namespace Nxr.Internal
{
    /// A custom editor for properties on the NxrViewer script.  This appears in the
    /// Inspector window of a NxrViewer object.  Its purpose is to allow changing the
    /// `NxrViewer.Instance` object's properties from their default values.
    [CustomEditor(typeof(NxrViewer))]
    public class NxrViewerEditor : Editor
    {
       // GUIContent directRenderEnabledLabel = new GUIContent("DirectRender  Enabled",
       //"Whether to draw directly to the output window (true), or " +
       //             "to an offscreen buffer first and then blit (false).  Image " +
       //             " Effects and Deferred Lighting may only work if set to false.");

       // GUIContent distortionEnabledLabel = new GUIContent("Distortion  Enabled",
       //  "Sets whether Distortion is enabled in DFT Mode.");

        GUIContent splitModeLabel = new GUIContent("Split Mode Enabled",
          "Sets whether Split mode is enabled.");

        GUIContent arLockHeadTracker = new GUIContent("Lock HeadTracker",
              "Sets whether Lock HeadTracker In Android.");

        GUIContent editorSettingsLabel = new GUIContent("Unity Editor Emulation Settings",
          "Controls for the in-editor emulation of a Cardboard viewer.");

        GUIContent autoUntiltHeadLabel = new GUIContent("Auto Untilt Head",
            "When enabled, just release Ctrl to untilt the head.");

        GUIContent screenSizeLabel = new GUIContent("Screen Size",
            "The screen size to emulate.");

        GUIContent viewerTypeLabel = new GUIContent("Viewer Type",
            "The viewer type to emulate.");

        GUIContent qualityLabel = new GUIContent("Texture Quality",
            "The texture quality in android.");

        GUIContent trackerPositionLabel = new GUIContent("Tracker Position",
          "Update the Camera's position with the user's head offset.");

        GUIContent effectRenderLabel = new GUIContent("Effect Render",
      "If you wish to use Deferred Rendering or any Image Effects in stereo, turn this option on.");

        GUIContent recenterLabel = new GUIContent("Firstly Recenter",
       "Do Camera recenter after the sdk load succ ,but before screen render.");

        GUIContent debuggingLabel = new GUIContent("Device Remote Debug",
       "When XR device is usb connected, use XR device sensor data in editor run mode.");


        GUIContent headControl = new GUIContent("Head Control", "Sets Head Control is enabled.");

        GUIContent duration = new GUIContent("Duration", "Sets Duration");

        GUIContent use3rdPosTip = new GUIContent("Use Third Party Position Data", "Whether Use Third Party Position Data, true=Use 3rd Party, false= Use HMD Position Data");

        GUIContent displacementCoefficient = new GUIContent("DisplacementCoefficient", "6Dof Displacement Coefficient");

        GUIContent SleepContent= new GUIContent("Sleep Timeout Mode", "Prevent screen dimming or not");
        /// @cond HIDDEN
        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            GUIStyle headingStyle = new GUIStyle(GUI.skin.label);
            headingStyle.fontStyle = FontStyle.Bold;

            NxrViewer nvrViewer = (NxrViewer)target;

            EditorGUILayout.LabelField("General Settings", headingStyle);

            // 读取Config
            SettingsAssetConfig asset = NxrSDKApi.Instance.GetSettingsAssetConfig();
            nvrViewer.SixDofMode = asset.mSixDofMode;
            nvrViewer.SleepMode = asset.mSleepTimeoutMode;
            nvrViewer.HeadControl = asset.mHeadControl;
            nvrViewer.TextureQuality = asset.mTextureQuality;
            nvrViewer.TextureMSAA= asset.mTextureMSAA;
            // 读取Config

#if UNITY_ANDROID
            nvrViewer.TrackerPosition = EditorGUILayout.Toggle(trackerPositionLabel, nvrViewer.TrackerPosition);
#endif
            if (nvrViewer.TrackerPosition)
            {
                nvrViewer.DisplacementCoefficient = EditorGUILayout.Slider(displacementCoefficient, nvrViewer.DisplacementCoefficient,0,1);
            }

            nvrViewer.SleepMode = (SleepTimeoutMode) EditorGUILayout.EnumPopup(SleepContent, nvrViewer.SleepMode);

            nvrViewer.SplitScreenModeEnabled = EditorGUILayout.Toggle(splitModeLabel, nvrViewer.SplitScreenModeEnabled);
 
            nvrViewer.TextureQuality = (TextureQuality)EditorGUILayout.EnumPopup(qualityLabel, nvrViewer.TextureQuality);

            nvrViewer.RemoteDebug = EditorGUILayout.Toggle(debuggingLabel, nvrViewer.RemoteDebug);

            nvrViewer.RemoteController = EditorGUILayout.Toggle(new GUIContent("Device Remote Controller",
       "When XR device is usb connected, use XR device controller data in editor run mode."), nvrViewer.RemoteController);

            nvrViewer.ShowFPS = EditorGUILayout.Toggle("Show FPS in Scene", nvrViewer.ShowFPS);

            EditorGUILayout.LabelField("Advanced Settings", headingStyle);


            nvrViewer.HeadControl = (HeadControl)EditorGUILayout.EnumPopup(headControl, nvrViewer.HeadControl);

            if (nvrViewer.HeadControl == HeadControl.Hover)
            {
                nvrViewer.Duration = EditorGUILayout.DelayedFloatField(duration, nvrViewer.Duration);
            }

            nvrViewer.TextureMSAA = (TextureMSAA)EditorGUILayout.EnumPopup(new GUIContent("Texture MSAA",
            "The texture Anti-aliasing"), nvrViewer.TextureMSAA);

            EditorGUILayout.LabelField("SixDof Controller Settings", headingStyle);
            nvrViewer.IsAppHandleTriggerEvent = EditorGUILayout.Toggle(new GUIContent("App Handle Controller Trigger Event", "Sets Handle Controller Trigger Event"), 
                nvrViewer.IsAppHandleTriggerEvent);
#if UNITY_STANDALONE_WIN || ANDROID_REMOTE_NRR
            nvrViewer.TextureQuality = TextureQuality.Best;
            
            nvrViewer.TargetFrameRate = (FrameRate)EditorGUILayout.EnumPopup(new GUIContent("TargetFrameRate",
            "The target Frame Rate in PC."), nvrViewer.TargetFrameRate);

              nvrViewer.UseThirdPartyPosition = EditorGUILayout.Toggle(use3rdPosTip, nvrViewer.UseThirdPartyPosition);
#endif

            EditorGUILayout.Separator();
            if (GUI.changed)
            {
                asset.mSleepTimeoutMode = nvrViewer.SleepMode;
                asset.mHeadControl = nvrViewer.HeadControl;
                asset.mTextureQuality = nvrViewer.TextureQuality;
                asset.mTextureMSAA = nvrViewer.TextureMSAA;
                EditorUtility.SetDirty(asset);
                EditorUtility.SetDirty(nvrViewer);
            }
            // 保存序列化数据，否则会出现设置数据丢失情况
            serializedObject.ApplyModifiedProperties();
        }
    }

  

}