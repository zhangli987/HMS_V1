using Nxr.Internal;
using UnityEngine;
namespace NXR.Samples
{
    /// <summary>
    /// 示例：演示如何更新相机位置
    /// </summary>
    [RequireComponent(typeof(Collider),typeof(MeshRenderer))]
    public class CameraTeleportTest : MonoBehaviour
    {
        Material material;
        public Color defaultColor;
        NxrHead nxrHead;
        // Start is called before the first frame update
        void Start()
        {
            material = GetComponent<Renderer>().material;
            defaultColor = material.color;
            SetGazedAt(false);
            nxrHead = NxrViewer.Instance.GetHead();
            nxrHead.SetTrackPosition(true);
        }

        private bool gazeAt = false;
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
            if(gameObject.name.Equals("CubeReset"))
            {
                nxrHead.BasePosition = Vector3.zero;
                return;
            }
            nxrHead.BasePosition = transform.position;
        }

        public void SetGazedAt(bool gazedAt)
        {
            gazeAt = gazedAt;
            material.color = gazedAt ? Color.green : defaultColor;
        }
    }
}