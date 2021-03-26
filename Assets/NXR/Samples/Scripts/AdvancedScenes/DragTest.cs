using UnityEngine;
using Nxr.Internal;
using NibiruAxis;
using NibiruTask;

namespace NXR.Samples
{
    public class DragTest : MonoBehaviour
    {
        public TextMesh statusTextMesh;

        NxrDragableItem dragableItem;

        bool IsSelected = false;

        private Transform parentTrans;

        public void OnGazeEnter()
        {
            IsSelected = true;
            statusTextMesh.text = "Selected";
        }

        public void OnGazeExit()
        {
            IsSelected = false;
            statusTextMesh.text = "UnSelected";
            if (dragableItem != null && dragableItem.IsDraging)
            {
                statusTextMesh.text = "Draging";
            }
        }

        public void OnGazeTrigger()
        {
            bool isDraging = dragableItem != null && dragableItem.IsDraging;

            if (!IsSelected && !isDraging) return;
       
            if(!isDraging)
            {
                NxrLaserPointer nxrLaserPointer = NxrPlayerCtrl.Instance.GetControllerLaser();
                if(nxrLaserPointer != null)
                {
                    // 选中跟随
                    if (dragableItem != null)
                    {
                        dragableItem.OnBeginDrag(nxrLaserPointer.transform);
                    }
                } else
                {
                    // 头部
                    NxrReticle mReticle = NxrViewer.Instance.GetNxrReticle();
                    if(dragableItem != null)
                    {
                        dragableItem.OnBeginDrag(mReticle.gameObject.transform.parent);
                    }
                }
            } else
            {
                // 恢复
                if (dragableItem != null)
                {
                    dragableItem.OnEndDrag(parentTrans);
                }
            }
        }
         
        // Start is called before the first frame update
        void Start()
        {
            dragableItem = GetComponent<NxrDragableItem>();
            parentTrans = gameObject.transform.parent;
            statusTextMesh.text = "UnSelected";
        }

        private void Update()
        {

            if (NxrInput.GetKeyDown(CKeyEvent.KEYCODE_DPAD_CENTER) ||
              NxrInput.GetControllerKeyDown(CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD))
            {
                OnGazeTrigger();
            }
        }
    }
}