using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Nxr.Internal
{
    public class NxrUICanvas : MonoBehaviour
    {
        protected BoxCollider canvasBoxCollider;
        protected Rigidbody canvasRigidBody;
        private void OnEnable()
        {
            SetupCanvas();
        }

     
        void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();

            if (!canvas || canvas.renderMode != RenderMode.WorldSpace)
            {
                return;
            }

            var canvasRectTransform = canvas.GetComponent<RectTransform>();
            var canvasSize = canvasRectTransform.sizeDelta;

            var defaultRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
            var customRaycaster = canvas.gameObject.GetComponent<NxrUIGraphicRaycaster>();


            if (!customRaycaster)
            {
                customRaycaster = canvas.gameObject.AddComponent<NxrUIGraphicRaycaster>();
            }

            if (defaultRaycaster && defaultRaycaster.enabled)
            {
                customRaycaster.ignoreReversedGraphics = defaultRaycaster.ignoreReversedGraphics;
                customRaycaster.blockingObjects = defaultRaycaster.blockingObjects;
                defaultRaycaster.enabled = false;
            }
            if (!canvas.gameObject.GetComponent<BoxCollider>())
            {
                float zSize = 0.1f;
                float zScale = zSize / canvasRectTransform.localScale.z;

                canvasBoxCollider = canvas.gameObject.AddComponent<BoxCollider>();
                canvasBoxCollider.size = new Vector3(canvasSize.x, canvasSize.y, zScale);
                canvasBoxCollider.isTrigger = true;
            }

            if (!canvas.gameObject.GetComponent<Rigidbody>())
            {
                canvasRigidBody = canvas.gameObject.AddComponent<Rigidbody>();
                canvasRigidBody.isKinematic = true;
            }
        }

      
        void RemoveCanvas()
        {
            var canvas = GetComponent<Canvas>();

            if (!canvas)
            {
                return;
            }

            var defaultRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
            var customRaycaster = canvas.gameObject.GetComponent<NxrUIGraphicRaycaster>();
            //if a custom raycaster exists then remove it
            if (customRaycaster)
            {
                Destroy(customRaycaster);
            }

            //If the default raycaster is disabled, then re-enable it
            if (defaultRaycaster && !defaultRaycaster.enabled)
            {
                defaultRaycaster.enabled = true;
            }
            if (canvasBoxCollider)
            {
                Destroy(canvasBoxCollider);
            }

            if (canvasRigidBody)
            {
                Destroy(canvasRigidBody);
            }
        }

        private void OnDestroy()
        {
            RemoveCanvas();
        }

        private void OnDisable()
        {
            RemoveCanvas();
        }
    }
}