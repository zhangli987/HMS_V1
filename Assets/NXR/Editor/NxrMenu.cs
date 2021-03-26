
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nxr.Internal
{
    public class NxrMenu
    {

        [InitializeOnLoad]
        public class Startup
        {
            static Startup()
            {
                // 首次加载调用
                Debug.Log("NXR-Plugin-StartUp");
                NxrQualitySettingsEditor.InputManagerAssistant mInputManagerAssistant = new NxrQualitySettingsEditor.InputManagerAssistant();
                NxrQualitySettingsEditor.InputManagerAssistant.InputAxis axis3th = new NxrQualitySettingsEditor.InputManagerAssistant.InputAxis();
                axis3th.name = "3th axis";
                axis3th.type = NxrQualitySettingsEditor.InputManagerAssistant.AxisType.JoystickAxis;
                axis3th.axis = 3;
                mInputManagerAssistant.AddAxis(axis3th);

                axis3th.name = "4th axis";
                axis3th.type = NxrQualitySettingsEditor.InputManagerAssistant.AxisType.JoystickAxis;
                axis3th.axis = 4;
                mInputManagerAssistant.AddAxis(axis3th);


                axis3th.name = "5th axis";
                axis3th.type = NxrQualitySettingsEditor.InputManagerAssistant.AxisType.JoystickAxis;
                axis3th.axis = 5;
                mInputManagerAssistant.AddAxis(axis3th);


                axis3th.name = "6th axis";
                axis3th.type = NxrQualitySettingsEditor.InputManagerAssistant.AxisType.JoystickAxis;
                axis3th.axis = 6;
                mInputManagerAssistant.AddAxis(axis3th);

                axis3th.name = "joystick_Horizontal";
                axis3th.type = NxrQualitySettingsEditor.InputManagerAssistant.AxisType.JoystickAxis;
                axis3th.axis = 1;
                mInputManagerAssistant.AddAxis(axis3th);

                axis3th.name = "joystick_Vertical";
                axis3th.type = NxrQualitySettingsEditor.InputManagerAssistant.AxisType.JoystickAxis;
                axis3th.axis =2;
                mInputManagerAssistant.AddAxis(axis3th);
            }
        }

        [MenuItem("NibiruXR/About Nibiru XR", false, 200)]
        private static void OpenAbout()
        {
            EditorUtility.DisplayDialog("Nibiru XR SDK for Unity",
                "Version: " + NxrViewer.NXR_SDK_VERSION + "\n\n"
                + "QQ Group: 464811686. \n"
                + "Email: support@nibiruplayer.com. \n\n"
                + "Copyright: ©2020 Nibiru Inc. All rights reserved.\n"
                + "https://dev.inibiru.com",
                "OK");
        }

        // Add menu named "My Window" to the Window menu
        // [MenuItem("NibiruXR/SDK Repair", false, 88)]
        static void SDKSelfCheck()
        {
            string dirPath =  Application.dataPath + "/Plugins/Android/";
            DirectoryInfo root = new DirectoryInfo(dirPath);
            FileInfo[] files = root.GetFiles();
            if (files == null) return;

            FileInfo deleteFileInfo = null;
            foreach(FileInfo fileInfo in files)
            {
                if (fileInfo != null && fileInfo.Extension.Equals(".jar"))
                {
                    if (fileInfo.Name.Contains("nibiru_vr_pro_sdk_") && !fileInfo.Name.Contains("latest"))
                    {
                        deleteFileInfo = fileInfo;
                        break;
                    }
                }
            }

            if(deleteFileInfo != null)
            {
                File.Delete(deleteFileInfo.FullName);
                Debug.Log("[SDK Repair] Delete file." + deleteFileInfo.FullName);
            }

        }

        [MenuItem("NibiruXR/APK Encryption", false, 199)]
        public static void createApkEncryptionMenu()
        {
            NxrApkEncryptionEditor window = (NxrApkEncryptionEditor)EditorWindow.GetWindow(typeof(NxrApkEncryptionEditor), false, "Nibiru Apk Encryption");
            window.minSize = new Vector2(300, 60);
            window.Show();
        }

        [MenuItem("NibiruXR/SDK Verify", false, 199)]
        public static void createSDKVerifyMenu()
        {
            NxrSDKVerifyEditor window = (NxrSDKVerifyEditor)EditorWindow.GetWindow(typeof(NxrSDKVerifyEditor), false, "Nibiru SDK Verify");
            window.minSize = new Vector2(300, 60);
            window.Show();
        }

    }
}