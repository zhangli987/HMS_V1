using Nxr.Internal;
using UnityEngine;
namespace NXR.Samples
{
    public class FollowHead : MonoBehaviour
    {
        Transform mTransform, headTransform;
        // Use this for initialization
        void Start()
        {
            mTransform = this.transform;
        }

        // Update is called once per frame
        void Update()
        {
            if (headTransform == null)
            {
                headTransform = NxrViewer.Instance.GetHead().transform;
            }
            else
            {
                mTransform.localRotation = headTransform.localRotation;
            }
        }
    }
}