using UnityEngine;
namespace Nxr.Internal
{
    public class CubeButtonGaze : MonoBehaviour, INxrGazeResponder
    {
        bool mGazeAt = false;
        public void SetGazedAt(bool gazedAt)
        {
            mGazeAt = gazedAt;
            GetComponent<Renderer>().material.color = gazedAt ? Color.green : Color.white;
            GetComponent<Renderer>().material.SetColor("_BaseColor", gazedAt ? Color.green : Color.white);
        }

        public bool isGazedAt()
        {
            return mGazeAt;
        }

        public void OnGazeEnter()
        {
            SetGazedAt(true);
        }

        public void OnGazeExit()
        {
            SetGazedAt(false);
        }

        public void OnGazeTrigger()
        {
          
        }

        public void OnUpdateIntersectionPosition(Vector3 position)
        {
             
        }
 
    }
}