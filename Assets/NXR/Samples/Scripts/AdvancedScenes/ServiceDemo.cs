using System;
using Nxr.Internal;
using UnityEngine;
namespace NXR.Samples
{
    public class ServiceDemo : MonoBehaviour
    {
        NibiruService nibiruService;
        // Use this for initialization
        TextMesh textMesh_DriverVersion;
        TextMesh textMesh_VendorVersion;
        TextMesh textMesh_Light;
        TextMesh textMesh_Proximity;
        TextMesh textMesh_Brightness;
        TextMesh textMesh_SensorData;

        void Start()
        {
            nibiruService = NxrViewer.Instance.GetNibiruService();

            if (nibiruService == null)
            {
                Debug.Log("nibiruService is null >>>>>>>>>>>>>>>>>>");
            }
            NibiruService.OnSensorDataChangedHandler += onSensorDataChanged;

            Debug.Log("----------------nibiruService is Start----------------");

        }

        void onSensorDataChanged(NibiruSensorEvent sensorEvent)
        {
            // sensorEvent.printLog();
            if (textMesh_SensorData != null)
            {
                textMesh_SensorData.text = sensorEvent.sensorType.ToString() + "\nx="
                    + sensorEvent.x + ",\ny=" + sensorEvent.y + ",\nz=" + sensorEvent.z + "\ntimestamp=" + sensorEvent.timestamp;
            }
        }


        bool updateOnce = false;
        int updateCount = 0;
        // Update is called once per frame
        void Update()
        {
            updateCount++;
            if (textMesh_SensorData == null)
            {
                GameObject Obj = GameObject.Find("TextSensorData");
                if (Obj != null)
                {
                    textMesh_SensorData = Obj.GetComponent<TextMesh>();
                    Debug.Log("find TextSensorData");
                    return;
                }
            }

            if (textMesh_DriverVersion == null)
            {
                GameObject Obj = GameObject.Find("DriverVersion");
                if (Obj != null)
                {
                    textMesh_DriverVersion = Obj.GetComponent<TextMesh>();
                    Debug.Log("find DriverVersion");
                    return;
                }
            }

            if (textMesh_VendorVersion == null)
            {
                GameObject Obj = GameObject.Find("VendorSwVersion");
                if (Obj != null)
                {
                    textMesh_VendorVersion = Obj.GetComponent<TextMesh>();
                    Debug.Log("find VendorSwVersion");
                    return;
                }
            }

            if (textMesh_Light == null)
            {
                GameObject Obj = GameObject.Find("LightValue");
                if (Obj != null)
                {
                    textMesh_Light = Obj.GetComponent<TextMesh>();
                    Debug.Log("find LightValue");
                    return;
                }
            }

            if (textMesh_Proximity == null)
            {
                GameObject Obj = GameObject.Find("ProximityValue");
                if (Obj != null)
                {
                    textMesh_Proximity = Obj.GetComponent<TextMesh>();
                    Debug.Log("find ProximityValue");
                    return;
                }
            }

            if (textMesh_Brightness == null)
            {
                GameObject Obj = GameObject.Find("BrightnessValue");
                if (Obj != null)
                {
                    textMesh_Brightness = Obj.GetComponent<TextMesh>();
                    Debug.Log("find BrightnessValue");
                    return;
                }
            }


            if (nibiruService != null && !updateOnce)
            {
                // Avoid frequent calls
                updateOnce = true;

                Debug.Log("----------------------------------------Service-------------");
                textMesh_DriverVersion.text = "Driver board software version:" + nibiruService.GetVendorSWVersion();
                textMesh_VendorVersion.text = "System version:" + nibiruService.GetModel() + "," + nibiruService.GetOSVersion();
                textMesh_Brightness.text = "Screen brightness:" + nibiruService.GetBrightnessValue();
                textMesh_Light.text = "Light sensor:" + nibiruService.GetLightValue();
                textMesh_Proximity.text = "Distance sensor:" + nibiruService.GetProximityValue();
            }

            // 1s1次
            if (updateCount > 60 && textMesh_Brightness != null && nibiruService != null)
            {
                updateCount = 0;
                textMesh_Brightness.text = "Screen brightness:" + nibiruService.GetBrightnessValue();
            }
        }

        private void OnDestroy()
        {
            if (nibiruService != null)
            {
                nibiruService.UnRegisterSensorListener();
            }
        }
    }
}