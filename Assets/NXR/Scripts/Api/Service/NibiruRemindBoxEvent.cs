using UnityEngine;
namespace Nxr.Internal
{
    public class NibiruRemindBoxEvent : MonoBehaviour, INxrGazeResponder
    {
        public delegate void RemindBoxEvent();
        public RemindBoxEvent handleRemindBox;
        public void SetGazedAt(bool gazedAt)
        {
            GetComponent<MeshRenderer>().material.color = gazedAt ?new Color(8, 8, 8, 1f) : new Color(0,0,0,0f) ;
        }

        public void OnGazeEnter()
        {
            if (this != null)
            {
                Debug.Log("Enter:" + gameObject.name);
                SetGazedAt(true);
            }
            else
            {
                Debug.Log("null");
            }
        }

        public void OnGazeExit()
        {
            // Debug.Log("Exit:" + gameObject.name);
            SetGazedAt(false);
        }

        public void OnGazeTrigger()
        {
            // Debug.Log("Trigger:" + gameObject.name);
            handleRemindBox();
            //清除原点选中效果
            NxrReticle mNxrReticle = NxrViewer.Instance.GetNxrReticle();
            if (mNxrReticle != null)
            {
                mNxrReticle.OnGazeExit(null, null);
            }
        }

        public void OnUpdateIntersectionPosition(Vector3 position)
        {

        }
        void OnDestory()
        {
            
        }
    }
}
