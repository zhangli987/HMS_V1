using UnityEngine;
using UnityEngine.SceneManagement;
using Nxr.Internal;

namespace NXR.Samples
{
    public class FirstScene : MonoBehaviour
    {
        public void JumpToGazeModeScene()
        {
            SceneManager.LoadScene("GazeModeScene");
        }
        
        public void JumpToUIScene()
        {
            SceneManager.LoadScene("UIScene");
        }
        
        public void JumpToInputKeyScene()
        {
            SceneManager.LoadScene("InputKeyScene");
        }
             
        public void JumpToSixdofScene()
        {
            SceneManager.LoadScene("SixdofScene");
        }
        
        public void JumpToCustomCtrlScene()
        {
            SceneManager.LoadScene("CustomCtrlScene");
        }
        
        public void JumpToTeleportScene()
        {
            SceneManager.LoadScene("CameraTeleportScene");
        }
        
        public void JumpToDragScene()
        {
            SceneManager.LoadScene("ControllerDragScene");
        }
        
        public void JumpToSystemApi()
        {
            SceneManager.LoadScene("SystemApi");
        }
        
        public void JumpToServiceScene()
        {
            SceneManager.LoadScene("ServiceScene");
        }
        
        public void JumpToCameraScene()
        {
            SceneManager.LoadScene("CameraScene");
        }
        
        public void JumpToRecordScene()
        {
            SceneManager.LoadScene("RecordScene");
        }
        
        public void JumpToMarkerScene()
        {
            SceneManager.LoadScene("MarkerScene");
        }

        private GameObject gazeObject;

        void OnGazeEvent(GameObject targetObject)
        {
            gazeObject = targetObject;
            // Debug.Log("OnGazeEvent->" + targetObject.name);
        }

        // Use this for initialization
        void Start()
        {
            NxrOverrideSettings.OnGazeEvent += OnGazeEvent;
        }

        // Update is called once per frame
        void Update()
        {
#if UNITY_EDITOR
            if (gazeObject != null)
            {
                Vector3 start = NxrViewer.Instance.GetHead().transform.position;
                float zLength = gazeObject.transform.position.z - start.z;
                Vector3 vector = NxrViewer.Instance.GetHead().transform.TransformDirection(Vector3.forward);
                UnityEngine.Debug.DrawRay(start, vector * zLength, Color.red);
            }
#endif
        }

        private void OnDestroy()
        {
            NxrOverrideSettings.OnGazeEvent = OnGazeEvent;
            Debug.Log("FirstScene.OnDestroy");
        }
    }
}