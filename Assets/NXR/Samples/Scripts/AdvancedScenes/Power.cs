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

using NibiruTask;
using UnityEngine;
namespace NXR.Samples
{
    [RequireComponent(typeof(TextMesh))]
    public class Power : MonoBehaviour
    {
        private TextMesh textField;
        private double power = 0;
        private bool IsNeedRefreshStatus = true;
        private void Start()
        {
            NibiruTaskApi.addOnPowerChangeListener(onPowerChanged);


            UpdateBluetoothAndNetwordStatus();
        }

        void UpdateBluetoothAndNetwordStatus()
        {
            if (!IsNeedRefreshStatus) return;
            IsNeedRefreshStatus = false;
            // 0=off, 1=on
            int networkStatus = NibiruTaskApi.GetNetworkStatus();
            int bluetoothStatus = NibiruTaskApi.GetBluetoothStatus();
            GameObject.Find("Bluetooth").GetComponent<TextMesh>().text = "Bluetooth: " + (bluetoothStatus == 1 ? "on" : "off");
            GameObject.Find("Network").GetComponent<TextMesh>().text = "Network: " + (networkStatus == 1 ? "on" : "off");
        }

        public void onPowerChanged(double value)
        {
            power = value;

        }

        void Awake()
        {
            textField = GetComponent<TextMesh>();

            //// change keyboard postion or rotation
            // NibiruKeyBoard.Instance.keyBoardTransform.Rotate(new Vector3(30, 0, 0));
            // // show keyboard
            // NibiruKeyBoard.Instance.Show();
        }


        void Update()
        {

            if (textField != null)
            {
                textField.text = "Power:" + ((int)(power * 100)) + "%";
            }

            UpdateBluetoothAndNetwordStatus();
        }

        private void OnDestroy()
        {
            NibiruTaskApi.removeOnPowerChangeListener(onPowerChanged);
            Debug.Log("Power.OnDestroy");
        }

        private void OnApplicationPause(bool pause)
        {
            Debug.Log("Power-OnApplicationPause." + pause);
            IsNeedRefreshStatus = !pause;
        }

    }
}