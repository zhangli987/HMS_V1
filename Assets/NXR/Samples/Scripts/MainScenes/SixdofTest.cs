using Nxr.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NXR.Samples
{
    public class SixdofTest : MonoBehaviour
    {
        public TextMesh PositionText;

        public TextMesh RotationText;

        // Start is called before the first frame update
        void Start()
        {
            NxrViewer.serviceReadyUpdatedDelegate += OnServiceReady;
        }

        void OnServiceReady(SERVICE_TYPE serviceType, bool isConnectedSucc)
        {
            Debug.Log("OnServiceReady---------------------" + serviceType + "," + isConnectedSucc);
            if (serviceType == SERVICE_TYPE.SIX_DOF)
            {
                bool support6DOF = NxrViewer.Instance.GetNibiruService().IsSupport6DOF();
                Debug.Log("Six Dof Support Status : " + (support6DOF ? 1 : 0));
            }
        }

        // Update is called once per frame
        void Update()
        {
            NxrHead head = NxrViewer.Instance.GetHead();
            if (head != null)
            {
                PositionText.text = "Pos:" + Math.Round(head.transform.position.x, 2) + "," +
                                    Math.Round(head.transform.position.y, 2) + "," +
                                    Math.Round(head.transform.position.z, 2);
                RotationText.text = "Rot:" + Math.Round(head.transform.eulerAngles.x, 2) + "," +
                                    Math.Round(head.transform.eulerAngles.y, 2) + "," +
                                    Math.Round(head.transform.eulerAngles.z, 2);
            }
        }

        void OnDestroy()
        {
            NxrViewer.serviceReadyUpdatedDelegate -= OnServiceReady;
        }
    }
}