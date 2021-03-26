using Nxr.Internal;
using UnityEngine;
namespace NXR.Samples
{
    public class GazeTest : MonoBehaviour
    {
        public TextMesh statusText;
        // Start is called before the first frame update
        void Start()
        {
            switch (NxrViewer.Instance.HeadControl)
            {
                case HeadControl.GazeApplication:
                    statusText.text = "Gaze App";
                    break;
                case HeadControl.GazeSystem:
                    statusText.text = "Gaze Native";
                    break;
                case HeadControl.Hover:
                    statusText.text = "Gaze Hover";
                    break;
                case HeadControl.Controller:
                    statusText.text = "Gaze Controller";
                    break;
            }
        }

        public void OnGazeEnter(GameObject gameObject)
        {
            gameObject.GetComponent<Renderer>().material.color = Color.green;
            gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.green);
        }

        public void OnGazeExit(GameObject gameObject)
        {
            gameObject.GetComponent<Renderer>().material.color = Color.white;
            gameObject.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.white);
        }


        public void ChangeToApp()
        {
            if(NxrViewer.Instance.HeadControl == HeadControl.Controller)
            {
                statusText.text = "Gaze Controller";
                return;
            }
            statusText.text = "Gaze App";
            NxrViewer.Instance.HeadControl = HeadControl.GazeApplication;
        }

        public void ChangeToNative()
        {
            if (NxrViewer.Instance.HeadControl == HeadControl.Controller)
            {
                statusText.text = "Gaze Controller";
                return;
            }
            statusText.text = "Gaze Native";
            NxrViewer.Instance.HeadControl = HeadControl.GazeSystem;
        }

        public void ChangeToContrl()
        {
            if (NxrViewer.Instance.HeadControl == HeadControl.Controller)
            {
                statusText.text = "Gaze Controller";
                return;
            }
            statusText.text = "Gaze Hover";
            NxrViewer.Instance.HeadControl = HeadControl.Hover;
        }


    }
}
