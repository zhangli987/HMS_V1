//======= Copyright (c) Valve Corporation, All rights reserved. ===============
using UnityEngine;
namespace Nxr.Internal
{
    public struct PointerEventArgs
    {
        public uint controllerIndex;
        public uint flags;
        public float distance;
        public Transform target;
    }

    public delegate void PointerEventHandler(object sender, PointerEventArgs e);


    public class NxrLaserPointer : MonoBehaviour
    {
        public Color color = Color.white;
        public float thickness = 0.004f;
        public GameObject holder;
        public GameObject pointer;

        private GameObject losdot;

        private GameObject hitObject;

        bool isActive = false;
        public bool addRigidBody = false;
        public event PointerEventHandler PointerIn;
        public event PointerEventHandler PointerOut;
        public NxrInstantNativeApi.NibiruDeviceType deviceType = NxrInstantNativeApi.NibiruDeviceType.RightController;

        Transform previousContact = null;

        float zDistance = 10.0f;

        Transform cacheTransform;

        public Transform GetTransform()
        {
            return cacheTransform;
        }

        public void SetHolderLocalPosition(Vector3 localPosition)
        {
            if (holder == null)
            {
                holder = new GameObject("NxrLaserPointer");
                holder.transform.parent = this.transform;
                holder.transform.localPosition = localPosition;
                holder.transform.localRotation = Quaternion.identity;
            } else
            {
                holder.transform.localPosition = localPosition;
            }
        }

        NxrUIPointer mNxrUIPointer;
        private void Awake()
        {
            mNxrUIPointer = GetComponent<NxrUIPointer>();
            if (mNxrUIPointer == null)
            {
                mNxrUIPointer = gameObject.AddComponent<NxrUIPointer>();
            }
        }


        Ray raycast;
        RaycastHit hit;
        // Use this for initialization
        void Start()
        {
            raycast = new Ray();
            hit = new RaycastHit();
            cacheTransform = transform;

            if(holder == null)
            {
                holder = new GameObject("NxrLaserPointer");
                holder.transform.parent = this.transform;
                holder.transform.localPosition = new Vector3(0, -0.005f, 0.08f);
                holder.transform.localRotation = Quaternion.identity;
            }

            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.transform.parent = holder.transform;
            pointer.transform.localScale = new Vector3(thickness, thickness, zDistance);
            pointer.transform.localPosition = new Vector3(0f, 0f, 50f);
            pointer.transform.localRotation = Quaternion.identity;
            BoxCollider collider = pointer.GetComponent<BoxCollider>();
            if (addRigidBody)
            {
                if (collider)
                {
                    collider.isTrigger = true;
                }
                Rigidbody rigidBody = pointer.AddComponent<Rigidbody>();
                rigidBody.isKinematic = true;
            }
            else
            {
                if (collider)
                {
                    Object.Destroy(collider);
                }
            }
            Material newMaterial = new Material(Shader.Find("Unlit/Color"));
            newMaterial.SetColor("_Color", color);
            pointer.GetComponent<MeshRenderer>().material = newMaterial;     
            losdot = Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NxrLosDot"));
            // 解决射线白点有偏转问题 
            // losdot.transform.parent = holder.transform;
            losdot.gameObject.name = "LosDot_" + Time.frameCount +(Time.deltaTime * 1000) + "_" + deviceType.ToString();
            losdot.SetActive(false);
        }

        public virtual void OnPointerIn(PointerEventArgs e)
        {
            if (PointerIn != null)
                PointerIn(this, e);
        }

        public virtual void OnPointerOut(PointerEventArgs e)
        {
            if (PointerOut != null)
                PointerOut(this, e);
        }

        void OnDisable()
        {
            if (losdot != null)
            {
                Destroy(losdot);
                losdot = null;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!isActive)
            {
                if(holder != null)
                {
                    holder.SetActive(true);
                    isActive = true;
                }
            }

            if(losdot != null && holder != null)
            {
                losdot.SetActive(holder.activeSelf);
                if(pointer != null) pointer.SetActive(holder.activeSelf);
                if (mNxrUIPointer != null)
                {
                    mNxrUIPointer.enabled = holder.activeSelf;
                }
            }

            if(holder == null || pointer == null || !holder.activeSelf)
            {
                return;
            }

            float dist = zDistance;

            raycast.origin = cacheTransform.position;
            raycast.direction = cacheTransform.forward;
     
            bool bHit = Physics.Raycast(raycast, out hit, zDistance); // max distance
#if UNITY_EDITOR
            Debug.DrawRay(cacheTransform.position, cacheTransform.forward, Color.yellow);
#endif

            CheckCanvasGraphicRayCaster(hit.transform, previousContact);

            if (previousContact && previousContact != hit.transform)
            {
                PointerEventArgs args = new PointerEventArgs();
                args.distance = 0f;
                args.flags = 0;
                args.target = previousContact;
                OnPointerOut(args);
                previousContact = null;
            }

            if (bHit && previousContact != hit.transform)
            {
                PointerEventArgs argsIn = new PointerEventArgs();
                argsIn.distance = hit.distance;
                argsIn.flags = 0;
                argsIn.target = hit.transform;
                OnPointerIn(argsIn);
                previousContact = hit.transform;

                hitObject = hit.collider.gameObject;
            }

            if (!bHit)
            {
                previousContact = null;
                hitObject = null;
                if(losdot != null) losdot.SetActive(false);
            }

            if (bHit && hit.distance < zDistance)
            {
                dist = hit.distance;
                if (losdot != null)
                {
                    losdot.SetActive(true);
                    losdot.transform.position = hit.point - new Vector3(0, -holder.transform.localPosition.y , 0.01f);
                }
            }
            pointer.transform.localScale = new Vector3(thickness, thickness, dist);
            pointer.transform.localPosition = new Vector3(0f, 0f, dist / 2f);
        }

        public GameObject GetLosDot()
        {
            return losdot;
        }

        private void CheckCanvasGraphicRayCaster(Transform currentHit, Transform lastHit)
        {
            if (currentHit != null && lastHit != null && currentHit != lastHit)
            {
                if (lastHit.GetComponent<NxrUIGraphicRaycaster>() && lastHit.transform.gameObject.activeInHierarchy && lastHit.GetComponent<NxrUIGraphicRaycaster>().enabled)
                {
                    lastHit.GetComponent<NxrUIGraphicRaycaster>().enabled = false;
                }
            }
            if (currentHit != null && lastHit != null && currentHit == lastHit)
            {
                if (currentHit.GetComponent<NxrUIGraphicRaycaster>() && !currentHit.GetComponent<NxrUIGraphicRaycaster>().enabled)
                {
                    currentHit.GetComponent<NxrUIGraphicRaycaster>().enabled = true;

                }
            }
        }
    }
}