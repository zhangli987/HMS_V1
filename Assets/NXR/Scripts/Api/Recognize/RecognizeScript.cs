using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Nxr.Internal
{
    public class RecognizeScript : MonoBehaviour
    {
        public GameObject cameraPreviewPanel;
        public TextMesh statusText;

        Transform cameraPreviewPanelTransform;
        private Vector2 previewPanelTL;
        private Vector2 previewPanelSize;

        CameraPreviewScript cameraPreviewScript;

        RecoginizeApi recoginizeApi;

        Transform cacheTransform;
        private bool isRecognizing = false;
        // Use this for initialization
        void Start()
        {
            cacheTransform = transform;

            recoginizeApi = RecoginizeApi.Instance;
            recoginizeApi.Init();
            cameraPreviewScript = cameraPreviewPanel.GetComponent<CameraPreviewScript>();
            cameraPreviewScript.AutoCameraFocus = false;

            cameraPreviewPanelTransform = cameraPreviewPanel.transform;
            previewPanelTL.x = cameraPreviewPanelTransform.localPosition.x - cameraPreviewPanelTransform.localScale.x / 2;
            previewPanelTL.y = cameraPreviewPanelTransform.localPosition.y + cameraPreviewPanelTransform.localScale.y / 2;
            previewPanelSize.x = cameraPreviewPanelTransform.localScale.x;
            previewPanelSize.y = cameraPreviewPanelTransform.localScale.y;

            // Debug.LogError(previewPanelTL.ToString());
            // 隐藏白点
            NxrViewer.Instance.GazeApi(GazeTag.Hide);

            if (statusText != null)
            {
                statusText.text = "Loading...";
            }
        }

        public bool AddTestFrame;
        private void test()
        {

            //for test // id=0,title=keyboard,confidence=0.64,location.(x:434.00, y:11.58, width:204.65, height:199.89), width=640, height=480
            List<Recognition> testlist = new List<Recognition>();
            Recognition recognition = new Recognition();
            recognition.id = "0";
            recognition.title = "keyboard";
            recognition.confidence = 0.94f;
            recognition.frameWidth = 640;
            recognition.frameHeight = 480;
            recognition.location = new Rect(434, 11.58f, 204.63f, 199.89f);
            testlist.Add(recognition);
            OnRecognizeSuccess(testlist);
            // for test

        }

        // Update is called once per frame
        void Update()
        {
            if(AddTestFrame)
            {
                AddTestFrame = false;
                test();
            }

            if (!isRecognizing && cameraPreviewScript.IsTextureUpdated())
            {
                // 
                isRecognizing = true;
                ClearRecognitionFrame();
                recoginizeApi.StartRecognize(OnRecognizeSuccess, OnRecognizeFailed);
                if (statusText != null)
                {
                    statusText.text = "Start recognizing...";
                }
            }
        }

        List<GameObject> frameGoList = new List<GameObject>(5);
        void OnRecognizeSuccess(List<Recognition> dataList)
        {
            ClearRecognitionFrame();

            if (!isRecognizing) return;

            for (int i = 0, size = dataList.Count; i < size; i++)
            {
                Recognition recog = dataList[i];
                float offsetX = recog.location.x / recog.frameWidth;
                float offsetY = recog.location.y / recog.frameHeight;
                float scaleX = recog.location.width / recog.frameWidth;
                float scaleY = recog.location.height / recog.frameHeight;

                float newScaleX = scaleX * previewPanelSize.x;
                float newScaleY = scaleY * previewPanelSize.y;
                float newCenterX = previewPanelTL.x + offsetX * previewPanelSize.x + newScaleX / 2;
                float newCenterY = previewPanelTL.y - offsetY * previewPanelSize.y - newScaleY / 2;
                //Debug.Log("Recognition==>" + recog.location.width + "/" + recog.frameWidth + "," +
                //    +previewPanelSize.x + "," + newCenterX + ","
                //   + offsetX + "," + newScaleX + "," + previewPanelTL.x);
                //recog.PrintInfo();
                if(cacheTransform == null)
                {
                    return;
                }
                GameObject frameGo = Instantiate(Resources.Load("Prefabs/RecognitionFrame")) as GameObject;
                frameGo.transform.SetParent(cacheTransform);
                frameGo.transform.localPosition = new Vector3(newCenterX, newCenterY, cameraPreviewPanelTransform.localPosition.z - 0.05f * i);
                frameGo.transform.localRotation = Quaternion.identity;
                frameGo.transform.Find("Frame").localScale = new Vector3(newScaleX, newScaleY, 1);

                Transform descTransform = frameGo.transform.Find("Desc");
                descTransform.localPosition = new Vector3(0.2f - newScaleX / 2, newScaleY / 2 - 0.2f, 0);
                TextMesh descTextMesh = descTransform.GetComponent<TextMesh>();
                descTextMesh.text = recog.title + "(" + recog.confidence + ")";

                frameGoList.Add(frameGo);
            }
        }

        void ClearRecognitionFrame()
        {
            for (int i = 0, size = frameGoList.Count; i < size; i++)
            {
                Destroy(frameGoList[i]);
            }
            frameGoList.Clear();
        }


        void OnRecognizeFailed(string message)
        {
            ClearRecognitionFrame();
            if (statusText != null)
            {
                statusText.text = "Recognize failed..." + message;
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                recoginizeApi.StopRecognize();
            }
            else
            {
                isRecognizing = false;
            }
        }

        private void OnApplicationQuit()
        {
            ClearRecognitionFrame();
            recoginizeApi.OnDestroy();
        }


        private void OnDestroy()
        {
            ClearRecognitionFrame();
            recoginizeApi.OnDestroy();
            Debug.Log("RecognizeScript.OnDestroy");
        }


    }
}