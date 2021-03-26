using UnityEngine;

namespace Nxr.Internal
{
    /// <summary>
    /// 
    /// </summary>
    public class NxrDragableItem : MonoBehaviour
    {
        public bool IsDraging { set; get; }

        Transform mTransform;

        Collider mCollider;

        // Start is called before the first frame update
        void Start()
        {
            mCollider = GetComponent<Collider>();
            mTransform = transform;
            IsDraging = false;
        }

        /// <summary>
        /// On begin drag game object
        /// </summary>
        /// <param name="parent"></param>
        public void OnBeginDrag(Transform parent)
        {
            mTransform.SetParent(parent);
            mCollider.enabled = false;
            IsDraging = true;
        }

        /// <summary>
        /// On end drag game object
        /// </summary>
        /// <param name="parent"></param>
        public void OnEndDrag(Transform parent)
        {
            mTransform.SetParent(parent);
            mCollider.enabled = true;
            IsDraging = false;
        }
    }
}