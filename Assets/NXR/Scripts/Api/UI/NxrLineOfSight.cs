using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nxr.Internal
{
    public class NxrLineOfSight : MonoBehaviour
    {
        internal delegate void NonLookAction();
        internal static event NonLookAction NonLook;

        private Transform _myTransform;
        public GameObject lineOfSightDot;
        public Canvas canvas;

        public bool drawDebugRay;

        // Use this for initialization
        void Start()
        {
            // dismiss original reticle or dtr point
            NxrViewer.Instance.SwitchControllerMode(true);

            _myTransform = GetComponentInParent<Transform>();
            if(_myTransform == null)
            {
                Debug.Log("_myTransform is NULL !!!");
            }
            if (lineOfSightDot != null)
            {
                lineOfSightDot = Instantiate(lineOfSightDot);
                lineOfSightDot.gameObject.SetActive(true);
            }
    
        }

        // Update is called once per frame
        void Update()
        {
            RaycastHit hitInfo;
            // _myTransform.position = new Vector3(0, 0, 0);
            //this.MyTransform.rotation 
            Vector3 vector = _myTransform.TransformDirection(Vector3.forward);
            Ray ray = new Ray(_myTransform.position, vector);

            if(drawDebugRay) UnityEngine.Debug.DrawRay(ray.origin, ray.direction * 250f, Color.cyan);

            if (Physics.Raycast(ray, out hitInfo))
            {
                if (this.lineOfSightDot != null)
                {
                    this.lineOfSightDot.SetActive(true);
                    this.lineOfSightDot.transform.position = hitInfo.point - hitInfo.normal * 0.1f;
                    this.lineOfSightDot.transform.localRotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
                }

                if (!this.InformObject(hitInfo.transform.gameObject))
                {
                    TriggerNonLook();
                }

            }
            else if (canvas != null && IsPointerOverUIObject(canvas, new Vector2(Screen.width / 2, Screen.height / 2)))
            {
                RaycastResult raycastResult =  results.Count >0 ? results[0] : new RaycastResult();
                if(raycastResult.gameObject != null)
                {
                    // Debug.Log("===UI=>" + raycastResult.distance + "," + GetIntersectionPosition().ToString("f4"));
                    if (this.lineOfSightDot != null)
                    {
                        this.lineOfSightDot.SetActive(true);
                        this.lineOfSightDot.transform.position = GetIntersectionPosition() + new Vector3(0,0,-0.2f);
                        this.lineOfSightDot.transform.localRotation = Quaternion.Euler(-90,0,0);
                    }

                    if (!this.InformObject(raycastResult.gameObject))
                    {
                        TriggerNonLook();
                    }

                }
            }
            else
            {
                if (this.lineOfSightDot != null)
                {
                    this.lineOfSightDot.SetActive(false);
                }
                TriggerNonLook();
            }
 
        }


        List<RaycastResult> results = new List<RaycastResult>();
        PointerEventData eventDataCurrentPosition;
        GraphicRaycaster uiRaycaster;
        // 通过画布上的 GraphicRaycaster 组件发射射线
        public bool IsPointerOverUIObject(Canvas canvas, Vector2 screenPosition)
        {
            //实例化点击事件
            if (eventDataCurrentPosition == null)
            {
                eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            } else
            {
                eventDataCurrentPosition.Reset();
            }
            //将点击位置的屏幕坐标赋值给点击事件
            eventDataCurrentPosition.position = screenPosition;
            //获取画布上的 GraphicRaycaster 组件
            if (uiRaycaster == null)
            {
                uiRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
            }

            results.Clear();
            // GraphicRaycaster 发射射线
            uiRaycaster.Raycast(eventDataCurrentPosition, results);

            if(results.Count > 0) eventDataCurrentPosition.pointerCurrentRaycast = results[0];
            return results.Count > 0;
        }

        Vector3 GetIntersectionPosition()
        {
            // Check for camera
            Camera cam = eventDataCurrentPosition.enterEventCamera;
            if (cam == null)
            {
                return Vector3.zero;
            }

            float intersectionDistance = eventDataCurrentPosition.pointerCurrentRaycast.distance + cam.nearClipPlane;
            Vector3 intersectionPosition = cam.transform.position + cam.transform.forward * intersectionDistance;

            return intersectionPosition;
        }

        private NxrInteractive currentObject;
        private bool InformObject(GameObject go)
        {
            NxrInteractive component = go.GetComponent<NxrInteractive>();
            if (component == null)
            {
                return false;
            }
            component.HandleIsLookedAt();
            if (this.currentObject != component)
            {
                if (this.currentObject == null)
                {
                    this.currentObject = null;
                }
                else
                {
                    this.currentObject.OtherIsLooked();
                }
                this.currentObject = component;
            }
            return true;
        }


        private static void TriggerNonLook()
        {
            if (NonLook != null)
            {
                NonLook();
            }
        }

    }


}