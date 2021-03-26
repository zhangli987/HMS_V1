using Nxr.Internal;
using UnityEngine;
namespace NXR.Samples
{
    public class RecordTestScript : MonoBehaviour
    {
        TextMesh textMesh_Status;
        // Use this for initialization
        void Start()
        {
            GameObject Obj = GameObject.Find("RecordStatus");
            if (Obj != null)
            {
                textMesh_Status = Obj.GetComponent<TextMesh>();
                textMesh_Status.text = "Click [Start]\n to record!";
            }

            NibiruService.OnRecorderSuccessHandler += videoCaptureSuccess;
            NibiruService.OnRecorderFailedHandler += videoCaptureFailed;
        }

        void videoCaptureFailed()
        {
            Debug.Log("videoCaptureFailed");
            if (textMesh_Status != null)
            {
                textMesh_Status.text = "Recorded failed,\nPlease check Log Info!";
            }
        }

        void videoCaptureSuccess()
        {
            Debug.Log("videoCaptureSuccess");
            if (textMesh_Status != null)
            {
                textMesh_Status.text = "Recorded successfully,\nplease check\nsdcard/unityrecord.mp4\n file!";
            }
        }

        public void StartRecord()
        {
            if (NxrViewer.Instance.GetNibiruService() != null)
            {
                NibiruService.SetCaptureVideoSize(VIDEO_SIZE.V720P);
                string filePath = NxrViewer.Instance.GetStoragePath() + "/unityrecord.mp4";
                NxrViewer.Instance.GetNibiruService().StartCapture(filePath);
            }
            textMesh_Status.text = "Recording is in progress,\nyou can click [Stop]\n to stop!";
        }

        public void PauseRecord()
        {
            if (NxrViewer.Instance.GetNibiruService() != null)
            {
                NxrViewer.Instance.GetNibiruService().StopCapture();
            }
            textMesh_Status.text = "Stop recording!";
        }

        private void OnDestroy()
        {
            NibiruService.OnRecorderSuccessHandler -= videoCaptureSuccess;
            NibiruService.OnRecorderFailedHandler -= videoCaptureFailed;
            if (NxrViewer.Instance.GetNibiruService() != null)
            {
                NxrViewer.Instance.GetNibiruService().StopCapture();
            }
        }
    }
}
