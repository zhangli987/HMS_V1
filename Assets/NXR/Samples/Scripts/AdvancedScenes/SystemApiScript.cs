// Copyright 2016 Nibiru. All rights reserved.

using UnityEngine;
using UnityEngine.UI;
using NibiruTask;
using Nxr.Internal;

namespace NXR.Samples
{
    public class SystemApiScript : MonoBehaviour
    {
        Text InfoText,pathText , macAddressText, deviceIdText,sixDofPluginStatusText;

        string info = "";
        string path = "";

        // Use this for initialization
        void Start()
        {
            GameObject pathObj = GameObject.Find("FilePath");
            if (pathObj != null)
            {
                pathText = pathObj.GetComponent<Text>();
            }

            GameObject infoObj = GameObject.Find("SystemInfo");
            if (infoObj != null)
            {
                InfoText = infoObj.GetComponent<Text>();
            }

            NibiruTaskApi.setSelectionCallback(onSelectionResult);
            
            GameObject macAddressObj = GameObject.Find("MacAddress");
            if (macAddressObj != null)
            {
                macAddressText = macAddressObj.GetComponent<Text>();
                macAddressText.text = "MacAddress: " + NibiruTaskApi.GetMacAddress();
            }
            
            GameObject deviceIdObj = GameObject.Find("DeviceId");
            if (deviceIdObj != null)
            {
                deviceIdText = deviceIdObj.GetComponent<Text>();
                deviceIdText.text = "DeviceId: " + NibiruTaskApi.GetDeviceId();
            }
            
            GameObject sixDofPluginStatusObj = GameObject.Find("SixDofPluginStatus");
            if (sixDofPluginStatusObj != null)
            {
                sixDofPluginStatusText = sixDofPluginStatusObj.GetComponent<Text>();
                sixDofPluginStatusText.text = "SixDof Plugin Status: [Declared " + NibiruTaskApi.IsPluginDeclared(PLUGIN_ID.SIX_DOF)
                    + "],\n [Suppored " + NibiruTaskApi.IsPluginSupported(PLUGIN_ID.SIX_DOF) + "]";
            }
        }

        public void onSelectionResult(AndroidJavaObject task)
        {
            path = NibiruTaskApi.GetResultPathFromSelectionTask(task);
        }

        // Update is called once per frame
        void Update()
        {
            if (InfoText != null)
            {
                InfoText.text = "SystemInfo: " + info;
            }
            
            if (pathText != null)
            {
                pathText.text = "GetFilePath: " + path;
            }
        }

        public void OpenVideoPlayer()
        {
            //NvrViewer.Instance.OpenVideoPlayer(NvrViewer.Instance.GetStoragePath() + "/nibiru.mp4", 0, 2, 1);
            NibiruTaskApi.OpenVideoPlayer("sdcard/nibiru.mp4");
        }

        public void OpenSettings()
        {
            NibiruTaskApi.OpenSettingsMain();
        }

        public void OpenExplorer()
        {
            NibiruTaskApi.OpenBrowerExplorer("http://www.inibiru.com");
        }

        public void GetSystemInfo()
        {
            info = "GetVRVersion:" + NibiruTaskApi.GetVRVersion() + "\n"
                   + "GetOSVersion:" + NibiruTaskApi.GetOSVersion() + "\n"
                   + "GetSysSleepTime:" + NibiruTaskApi.GetSysSleepTime() + "\n"
                   + "GetCurrentLanguage:" + NibiruTaskApi.GetCurrentLanguage() + "\n"
                   + "GetCurrentTimezone:" + NibiruTaskApi.GetCurrentTimezone() + "\n"
                   + "GetDeviceName:" + NibiruTaskApi.GetDeviceName() + "\n";
        }

        public void OpenImage()
        {
            NibiruTaskApi.OpenImageGallery("sdcard/nibiru.png");
        }

        public void GetFilePath()
        {
            NibiruTaskApi.GetFilePath("sdcard");
        }

        public void SetIpd()
        {
            NxrViewer.Instance.SetIpd(0.040f);
        }

        public void ResetIpd()
        {
            NxrViewer.Instance.ResetIpd();
        }

        public void LaunchSDKDemo()
        {
            Debug.Log("LaunchSDKDemo");
            NibiruTaskApi.LaunchAppByPkgName("com.nibiru.vr.lib2.test");
        }
    }
}