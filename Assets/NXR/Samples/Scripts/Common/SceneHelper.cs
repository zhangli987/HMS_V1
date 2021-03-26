// Copyright 2016 Nibiru. All rights reserved.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nxr.Internal;
using NibiruTask;

namespace NXR.Samples
{
    public class SceneHelper : MonoBehaviour
    {
        TextMesh textMesh, distanceText;

        // Use this for initialization
        void Start()
        {
            GameObject disObj = GameObject.Find("ObjectDistance");
            if (disObj != null)
            {
                distanceText = disObj.GetComponent<TextMesh>();
            }

            GameObject frameIdObj = GameObject.Find("FrameIdText");
            if (frameIdObj != null)
            {
                textMesh = frameIdObj.GetComponent<TextMesh>();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (distanceText != null)
            {
                distanceText.text = "Distance : " + Nxr.Internal.NxrGlobal.focusObjectDistance;
            }

            if (textMesh != null)
            {
                textMesh.text = "FrameId: " + NxrViewer.Instance.GetFrameId();
            }
        }

        /// <summary>
        /// 暂不支持-------
        /// </summary>
        //public void GoSyncFrameScene()
        //{
        //    NibiruKeyBoard.Instance.Dismiss();
        //    SceneManager.LoadScene("SyncFrameScene");
        //}
        public void PointerEnter()
        {
            Debug.Log("pointer enter");
        }

        public void PointerExit()
        {
            Debug.Log("pointer exit");
        }

        public void ShowKeyBoard()
        {
            // 
            Text text = GetComponentInParent<Text>();
            NibiruKeyBoard.Instance.SetText(text);
            // change keyboard postion or rotation
            NibiruKeyBoard.Instance.GetKeyBoardTransform();
            // get the input string
            NibiruKeyBoard.Instance.GetKeyBoardString();
            // show keyboard
            NibiruKeyBoard.Instance.Show(0, new Vector3(-0.5f, -2f, 5f), new Vector3(30, 0, 0));
        }
    }
}