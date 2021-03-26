using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nxr.Internal
{
    public class NxrQualitySettingsEditor : EditorWindow
    {
        public const string PRODUCT_NAME_VR = "NibiruXRDemo_VR";
        public const string PRODUCT_NAME_AR = "NibiruXRDemo_AR";

        public const string PACKAGE_NAME_XR = "com.nibiru.xr.unitydemo";

        const string NXR_ANDROID_ARM64_PATH = "/Plugins/Android/libs/arm64-v8a/";
        string[] NXR_ANDROID_ARM64_SO = new string[] {
                      "libnvr_local.so",
                      "libnvr_unity.so",
                      "libtensorflow_inference.so"
        };
        const string TMP_SUFFIX = ".tmpa";

        public enum QualityLevel
        {
            Low = 0,
            Middle,
            High
        }

        const string NRRPath = "NibiruXR/Android Remote NRR";
        // ANDROID_REMOTE_NRR
        [MenuItem(NRRPath, false, 89)]
        public static void MenuCheckRemoteNRRMode()
        {
#if UNITY_ANDROID && UNITY_EDITOR
            bool flag = Menu.GetChecked(NRRPath);
            flag = !flag;
            Menu.SetChecked(NRRPath, flag);
            if(flag)
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);

                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols + ";ANDROID_REMOTE_NRR");
            } else
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
                if(symbols.Contains("ANDROID_REMOTE_NRR"))
                {
                    symbols = symbols.Replace("ANDROID_REMOTE_NRR", "");
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols);
            }
#endif
        }

        [MenuItem(NRRPath, true)]
        public static bool RemoteNRRMenuCheckBefore()
        {
            bool isChecked = false;
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            if (symbols.Contains("ANDROID_REMOTE_NRR"))
            {
                isChecked = true;
            }
            Menu.SetChecked(NRRPath, isChecked);
            return true;
        }

        [MenuItem("NibiruXR/XR Settings", false, 80)]
        public static void createMenu()
        {
            NxrQualitySettingsEditor window = (NxrQualitySettingsEditor)EditorWindow.GetWindow(typeof(NxrQualitySettingsEditor));
            window.qualityLevel = (QualityLevel) EditorPrefs.GetInt("qualitylevel");
            window.hmdType = window.GetHmdType();
            window.RefreshTargetArchitectures();
            window.Show();
        }
 
        QualityLevel qualityLevel = QualityLevel.Middle;
        HMD_TYPE hmdType = HMD_TYPE.NONE;
        Target_Architectures targetArchitectures = Target_Architectures.ARMV7;

        bool IsARM64Enabled()
        {
            FileInfo fi = new FileInfo(Application.dataPath + NXR_ANDROID_ARM64_PATH + NXR_ANDROID_ARM64_SO[0]); //xx/xx/aa.rar
            return fi.Exists;
        }

        void RefreshTargetArchitectures()
        {
            targetArchitectures = IsARM64Enabled() ? Target_Architectures.ARMV7_AND_ARM64 : Target_Architectures.ARMV7;
        }

        private void OnGUI()
        {
            GUILayout.Space(20);

            GUIStyle firstLevelStyle = new GUIStyle(GUI.skin.label);
            firstLevelStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Quality settings :", firstLevelStyle);
            qualityLevel = (QualityLevel)EditorGUILayout.EnumPopup("", qualityLevel);
            GUILayout.Space(20);
            if (qualityLevel == QualityLevel.Low)
            {
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadowResolution = ShadowResolution.Low;
            }
            else if (qualityLevel == QualityLevel.Middle)
            {
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
            }
            else if (qualityLevel == QualityLevel.High)
            {
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadowResolution = ShadowResolution.High;
            }

            EditorGUILayout.LabelField("HMD type :", firstLevelStyle);
            hmdType = (HMD_TYPE)EditorGUILayout.EnumPopup("", hmdType);
            GUILayout.Space(20);

            EditorGUILayout.LabelField("Target Architectures :", firstLevelStyle);
            targetArchitectures = (Target_Architectures)EditorGUILayout.EnumPopup("", targetArchitectures);
            GUILayout.Space(20);

            EditorGUILayout.LabelField("Player settings :", firstLevelStyle);

            EditorGUILayout.LabelField("Orientation   :  LandscapeLeft");
            EditorGUILayout.LabelField("GraphicsAPIs : OpenglEs2.0");
            EditorGUILayout.LabelField("V Sync Count : Don't Sync");
            EditorGUILayout.LabelField("MultiThreadRendering : Disable ");
            EditorGUILayout.LabelField("Blit Type : Never");
            EditorGUILayout.LabelField("Use 32-bit Display Buffer");
            if (GUILayout.Button("Confirm", GUILayout.Width(100), GUILayout.Height(20)))
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] {
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2});
                PlayerSettings.use32BitDisplayBuffer = true;
#if UNITY_2017_4_OR_NEWER
                PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, false);
#else
                PlayerSettings.mobileMTRendering = false;
#endif

#if UNITY_2017_4_OR_NEWER
                PlayerSettings.Android.blitType = AndroidBlitType.Never;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
#endif

#if UNITY_2018_4_OR_NEWER
                PlayerSettings.Android.startInFullscreen = true;
                if(targetArchitectures == Target_Architectures.ARMV7)
                {
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
                    ReleaseARM64So(false);
                }  else if(targetArchitectures == Target_Architectures.ARMV7_AND_ARM64)
                {
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.All;
                    ReleaseARM64So(true);
                }
#endif

                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel21;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "NIBIRU_" + hmdType.ToString());
                if(hmdType == HMD_TYPE.AR)
                {
                    PlayerSettings.productName = PRODUCT_NAME_AR;
                } else if(hmdType == HMD_TYPE.VR)
                {
                    PlayerSettings.productName = PRODUCT_NAME_VR;
                }

#if UNITY_2017_1_OR_NEWER
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PACKAGE_NAME_XR);
#else
                PlayerSettings.bundleIdentifier = PACKAGE_NAME_XR;
#endif

                QualitySettings.pixelLightCount = 1;
                QualitySettings.masterTextureLimit = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
#if UNITY_5_5_OR_NEWER
                QualitySettings.shadows = ShadowQuality.Disable;
#endif
                // Don't Sync
                QualitySettings.vSyncCount = 0;

                SaveHmdType();
                Close();

          
            }
        }

        void ReleaseARM64So(bool release)
        {
            if(release)
            {
                // 释放64位so
                foreach(string soName in NXR_ANDROID_ARM64_SO)
                {
                    string curPath = Application.dataPath + NXR_ANDROID_ARM64_PATH + soName + TMP_SUFFIX;
                    string newPath = Application.dataPath + NXR_ANDROID_ARM64_PATH + soName;
                    FileInfo fi = new FileInfo(curPath); //xx/xx/aa.rar
                    if (fi.Exists)  fi.MoveTo(newPath); //xx/xx/xx.rar
                }
            }
            else
            {
                // 注释64位so
                foreach (string soName in NXR_ANDROID_ARM64_SO)
                {
                    string curPath = Application.dataPath + NXR_ANDROID_ARM64_PATH + soName;
                    string newPath = Application.dataPath + NXR_ANDROID_ARM64_PATH + soName + TMP_SUFFIX;
                    FileInfo fi = new FileInfo(curPath); //xx/xx/aa.rar
                    if(fi.Exists) fi.MoveTo(newPath); //xx/xx/xx.rar
                }
            }
        }

        HMD_TYPE GetHmdType()
        {
            string data = NxrPluginEditor.Read("AndroidManifest.xml");
            string[] lines = data.Split('\n');
            for (int i = 0, l = lines.Length; i < l; i++)
            {
                string lineContent = lines[i];
                if (lineContent.Contains("HMD_TYPE"))
                {
                    return lineContent.Contains("VR") ? HMD_TYPE.VR : (lineContent.Contains("AR") ? HMD_TYPE.AR : HMD_TYPE.NONE);
                }
             
            }
            return HMD_TYPE.NONE;
        }

        void SaveHmdType()
        {
            string data = NxrPluginEditor.Read("AndroidManifest.xml");
            string[] lines = data.Split('\n');
            string newdata = "";
            for (int i = 0, l = lines.Length; i < l; i++)
            {
                string lineContent = lines[i];
                if (lineContent.Contains("HMD_TYPE"))
                {
                    lineContent = "    <meta-data android:value=\"" + hmdType + "\" android:name=\"HMD_TYPE\"/>";
                }
                newdata = newdata + lineContent + "\n";
            }

            NxrPluginEditor.Write("AndroidManifest.xml", newdata);
        }


        private void OnDestroy()
        {
            EditorPrefs.SetInt("qualitylevel", (int) qualityLevel);
        }


        public class InputManagerAssistant
        {
            public enum AxisType
            {
                KeyOrMouseButton = 0,
                MouseMovement = 1,
                JoystickAxis = 2
            };

            public class InputAxis
            {
                public string name;
                public string descriptiveName;
                public string descriptiveNegativeName;
                public string negativeButton;
                public string positiveButton;
                public string altNegativeButton;
                public string altPositiveButton;

                public float gravity;
                public float dead=0.19f;
                public float sensitivity=1.0f;

                public bool snap = false;
                public bool invert = false;

                public AxisType type;

                public int axis;
                public int joyNum;
            }

            SerializedObject serializedObject = null;
            private SerializedProperty GetChildProperty(SerializedProperty parent, string name)
            {
                SerializedProperty child = parent.Copy();
                child.Next(true);
                do
                {
                    if (child.name == name) return child;
                }
                while (child.Next(false));
                return null;
            }

            private bool AxisDefined(string axisName)
            {
                if(serializedObject == null) serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
                SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

                axesProperty.Next(true);
                axesProperty.Next(true);
                while (axesProperty.Next(false))
                {
                    SerializedProperty axis = axesProperty.Copy();
                    axis.Next(true);
                    if (axis.stringValue == axisName) return true;
                }
                return false;
            }

            public void AddAxis(InputAxis axis)
            {
                if (AxisDefined(axis.name)) return;

                SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

                axesProperty.arraySize++;
                serializedObject.ApplyModifiedProperties();

                SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);

                GetChildProperty(axisProperty, "m_Name").stringValue = axis.name;
                GetChildProperty(axisProperty, "descriptiveName").stringValue = axis.descriptiveName;
                GetChildProperty(axisProperty, "descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
                GetChildProperty(axisProperty, "negativeButton").stringValue = axis.negativeButton;
                GetChildProperty(axisProperty, "positiveButton").stringValue = axis.positiveButton;
                GetChildProperty(axisProperty, "altNegativeButton").stringValue = axis.altNegativeButton;
                GetChildProperty(axisProperty, "altPositiveButton").stringValue = axis.altPositiveButton;
                GetChildProperty(axisProperty, "gravity").floatValue = axis.gravity;
                GetChildProperty(axisProperty, "dead").floatValue = axis.dead;
                GetChildProperty(axisProperty, "sensitivity").floatValue = axis.sensitivity;
                GetChildProperty(axisProperty, "snap").boolValue = axis.snap;
                GetChildProperty(axisProperty, "invert").boolValue = axis.invert;
                GetChildProperty(axisProperty, "type").intValue = (int)axis.type;
                GetChildProperty(axisProperty, "axis").intValue = axis.axis - 1;
                GetChildProperty(axisProperty, "joyNum").intValue = axis.joyNum;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
