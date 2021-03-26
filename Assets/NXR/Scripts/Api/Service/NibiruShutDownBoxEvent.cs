using UnityEngine;

namespace Nxr.Internal
{
    public class NibiruShutDownBoxEvent : MonoBehaviour, INxrGazeResponder
    {
        public delegate void ShutDownBoxEvent();
        public ShutDownBoxEvent handleShutDownBox;

        void SetAlpha(float alpha)
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            var color = meshRenderer.material.color;
            meshRenderer.material.color = new Color(color.r, color.g, color.b, alpha);
        }
        
        public void OnGazeEnter()
        {
            if (this != null)
            {
                SetAlpha(1);
            }
        }

        public void OnGazeExit()
        {
            SetAlpha(0);
        }

        public void OnGazeTrigger()
        {
            handleShutDownBox();
            //清除原点选中效果
            NxrReticle mNxrReticle = NxrViewer.Instance.GetNxrReticle();
            if (mNxrReticle) mNxrReticle.OnGazeExit(null, null);
        }

        public void OnUpdateIntersectionPosition(Vector3 position)
        {
        }
    }
}