// Copyright 2016 Nibiru. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using UnityEngine.UI;

/// Draws a circular reticle in front of any object that the user gazes at.
/// The circle dilates if the object is clickable.
namespace Nxr.Internal
{
    [AddComponentMenu("NXR/UI/NxrReticle")]
    [RequireComponent(typeof(Renderer))]
    public class NxrReticle : MonoBehaviour, INxrGazePointer
    {
        /// Number of segments making the reticle circle.20
        private int reticleSegments = 20;

        /// Growth speed multiplier for the reticle/
        public float reticleGrowthSpeed = 10.0f;

        ///  Show reticle or not ,if show the material's alpha is 1,else is 0.
        private bool showReticle = true;

        // Private members
        private Material materialComp;
        // private GameObject targetObj;

        // Current inner angle of the reticle (in degrees).
        private float reticleInnerAngle = 0.0f;
        // Current outer angle of the reticle (in degrees).
        private float reticleOuterAngle = 0.25f;
        // Current distance of the reticle (in meters).
        private float reticleDistanceInMeters = 50.0f;

        // Minimum inner angle of the reticle (in degrees).
        private const float kReticleMinInnerAngle = 0.0f;
        // Minimum outer angle of the reticle (in degrees).
        protected float kReticleMinOuterAngle = 0.25f;
        // Angle at which to expand the reticle when intersecting with an object
        // (in degrees).
        private const float kReticleGrowthAngle = 0.4f;

        // Minimum distance of the reticle (in meters).
        private const float kReticleDistanceMin = 0.45f;
        // Maximum distance of the reticle (in meters).
        private const float kReticleDistanceMax = 50.0f;

        // Current inner and outer diameters of the reticle,
        // before distance multiplication.
        private float reticleInnerDiameter = 0.0f;
        private float reticleOuterDiameter = 0.0f;

        /// Sorting order to use for the reticle's renderer.
        /// Range values come from https://docs.unity3d.com/ScriptReference/Renderer-sortingOrder.html.
        /// Default value 32767 ensures gaze reticle is always rendered on top.
        [Range(-32767, 32767)]
        public int reticleSortingOrder = 32767;

        GameObject reticlePointer;
        Transform cacheTransform;
        Color tempColor;
        private void Awake()
        {
            cacheTransform = transform;
            reticlePointer = new GameObject("Pointer");
            reticlePointer.transform.parent = cacheTransform;
            reticlePointer.transform.localPosition = Vector3.zero;
            reticlePointer.transform.localRotation = Quaternion.identity;
        }

        void Start()
        {
            CreateReticleVertices();
            Renderer rendererComponent = GetComponent<Renderer>();
            rendererComponent.sortingOrder = reticleSortingOrder;
            materialComp = rendererComponent.material;
            tempColor = new Color(materialComp.color.r, materialComp.color.g, materialComp.color.b, alphaValue);
        }

        public GameObject GetReticlePointer()
        {
            return reticlePointer;
        }

        void OnEnable()
        {
            GazeInputModule.gazePointer = this;
            Debug.Log("NxrReticle OnEnable");
        }

        void OnDisable()
        {
            Debug.Log("NxrReticle OnDisable");
            if (GazeInputModule.gazePointer == this)
            {
                GazeInputModule.gazePointer = null;
                showReticle = false;
            }
            if (headControl != null)
            {
                Destroy(headControl);
                headControl = null;
            }

        }

        private float alphaValue = -1.0f;

        // Only call device.UpdateState() once per frame.
        private int updatedToFrame = 0;

        private void Update()
        {
            if(GazeInputModule.gazePointer == null && !showReticle)
            {
                UpdateStatus();
            }
        }

        public void UpdateStatus()
        {
            if (updatedToFrame == Time.frameCount) return;

            updatedToFrame = Time.frameCount;
            if (showReticle)
            {
                UpdateDiameters();
            }

            float valueTmp = showReticle ? 1.0f : 0.0f;
            if (valueTmp != alphaValue)
            {
                alphaValue = valueTmp;
                tempColor.r = materialComp.color.r;
                tempColor.g = materialComp.color.g;
                tempColor.b = materialComp.color.b;
                tempColor.a = alphaValue;
                materialComp.color = tempColor;
            }
        }

        /// This is called when the 'BaseInputModule' system should be enabled.
        public void OnGazeEnabled()
        {

        }

        /// This is called when the 'BaseInputModule' system should be disabled.
        public void OnGazeDisabled()
        {

        }

        /// Called when the user is looking on a valid GameObject. This can be a 3D
        /// or UI element.
        ///
        /// The camera is the event camera, the target is the object
        /// the user is looking at, and the intersectionPosition is the intersection
        /// point of the ray sent from the camera on the object.
        public void OnGazeStart(Camera camera, GameObject targetObject, Vector3 intersectionPosition,
                                bool isInteractive)
        {
            bool IsHoverMode = NxrViewer.Instance != null && NxrViewer.Instance.HeadControl == HeadControl.Hover;
            SetGazeTarget(intersectionPosition, isInteractive);
            if (headControl != null && isInteractive)
            {
                if(IsHoverMode)
                {
                    NxrHeadControl mNxrHeadControl = headControl.GetComponent<NxrHeadControl>();
                    mNxrHeadControl.Show();
                    mNxrHeadControl.HandleDown();
                    NxrHeadControl.eventGameObject = targetObject;
                }
            }
        }

        /// Called every frame the user is still looking at a valid GameObject. This
        /// can be a 3D or UI element.
        ///
        /// The camera is the event camera, the target is the object the user is
        /// looking at, and the intersectionPosition is the intersection point of the
        /// ray sent from the camera on the object.
        public void OnGazeStay(Camera camera, GameObject targetObject, Vector3 intersectionPosition,
                               bool isInteractive)
        {
            SetGazeTarget(intersectionPosition, isInteractive);
        }

        /// Called when the user's look no longer intersects an object previously
        /// intersected with a ray projected from the camera.
        /// This is also called just before **OnGazeDisabled** and may have have any of
        /// the values set as **null**.
        ///
        /// The camera is the event camera and the target is the object the user
        /// previously looked at.
        public void OnGazeExit(Camera camera, GameObject targetObject)
        {
            reticleDistanceInMeters = kReticleDistanceMax;
            reticleInnerAngle = kReticleMinInnerAngle;
            reticleOuterAngle = kReticleMinOuterAngle;
            bool IsHoverMode = NxrViewer.Instance != null && NxrViewer.Instance.HeadControl == HeadControl.Hover;
            if (headControl != null)
            {
                if(IsHoverMode)
                {
                    headControl.GetComponent<NxrHeadControl>().HandleUp();
                    // this.gameObject.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    // headControl.transform.localPosition = new Vector3(0, 0, 0);
                }
            }
        }

        /// Called when a trigger event is initiated. This is practically when
        /// the user begins pressing the trigger.
        public void OnGazeTriggerStart(Camera camera)
        {
            // Put your reticle trigger start logic here :)
        }

        /// Called when a trigger event is finished. This is practically when
        /// the user releases the trigger.
        public void OnGazeTriggerEnd(Camera camera)
        {
            // Put your reticle trigger end logic here :)
        }

        public void GetPointerRadius(out float innerRadius, out float outerRadius)
        {
            float min_inner_angle_radians = Mathf.Deg2Rad * kReticleMinInnerAngle;
            float max_inner_angle_radians = Mathf.Deg2Rad * (kReticleMinInnerAngle + kReticleGrowthAngle);
            innerRadius = 2.0f * Mathf.Tan(min_inner_angle_radians);
            outerRadius = 2.0f * Mathf.Tan(max_inner_angle_radians);
        }
        private void CreateReticleVertices()
        {
            Mesh mesh = new Mesh();
            gameObject.AddComponent<MeshFilter>();
            GetComponent<MeshFilter>().mesh = mesh;

            int segments_count = reticleSegments;
            int vertex_count = (segments_count + 1) * 2;

            #region Vertices

            Vector3[] vertices = new Vector3[vertex_count];

            const float kTwoPi = Mathf.PI * 2.0f;
            int vi = 0;
            for (int si = 0; si <= segments_count; ++si)
            {
                // Add two vertices for every circle segment: one at the beginning of the
                // prism, and one at the end of the prism.
                float angle = (float)si / (float)(segments_count) * kTwoPi;

                float x = Mathf.Sin(angle);
                float y = Mathf.Cos(angle);

                vertices[vi++] = new Vector3(x, y, 0.0f); // Outer vertex.
                vertices[vi++] = new Vector3(x, y, 1.0f); // Inner vertex.
            }
            #endregion

            #region Triangles
            int indices_count = (segments_count + 1) * 3 * 2;
            int[] indices = new int[indices_count];

            int vert = 0;
            int idx = 0;
            for (int si = 0; si < segments_count; ++si)
            {
                indices[idx++] = vert + 1;
                indices[idx++] = vert;
                indices[idx++] = vert + 2;

                indices[idx++] = vert + 1;
                indices[idx++] = vert + 2;
                indices[idx++] = vert + 3;

                vert += 2;
            }
            #endregion

            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
            //mesh.Optimize();
        }

        private void UpdateDiameters()
        {
            reticleDistanceInMeters =
              Mathf.Clamp(reticleDistanceInMeters, kReticleDistanceMin, kReticleDistanceMax);

            if (reticleInnerAngle < kReticleMinInnerAngle)
            {
                reticleInnerAngle = kReticleMinInnerAngle;
            }

            if (reticleOuterAngle < kReticleMinOuterAngle)
            {
                reticleOuterAngle = kReticleMinOuterAngle;
            }

            float inner_half_angle_radians = Mathf.Deg2Rad * reticleInnerAngle * 0.5f;
            float outer_half_angle_radians = Mathf.Deg2Rad * reticleOuterAngle * 0.5f;

            float inner_diameter = 2.0f * Mathf.Tan(inner_half_angle_radians);
            float outer_diameter = 2.0f * Mathf.Tan(outer_half_angle_radians);

            reticleInnerDiameter =
                Mathf.Lerp(reticleInnerDiameter, inner_diameter, Time.deltaTime * reticleGrowthSpeed);
            reticleOuterDiameter =
                Mathf.Lerp(reticleOuterDiameter, outer_diameter, Time.deltaTime * reticleGrowthSpeed);


            materialComp.SetFloat("_InnerDiameter", reticleInnerDiameter * reticleDistanceInMeters);
            materialComp.SetFloat("_OuterDiameter", reticleOuterDiameter * reticleDistanceInMeters);
            materialComp.SetFloat("_DistanceInMeters", reticleDistanceInMeters);
        }

        internal void Show()
        {
            if (materialComp != null)
            {
                tempColor.r = materialComp.color.r;
                tempColor.g = materialComp.color.g;
                tempColor.b = materialComp.color.b;
                tempColor.a = 1;
                materialComp.color = tempColor;
            }
            showReticle = true;
            // Debug.LogError("Reticle-Show");
        }

        public bool IsShowing()
        {
            return showReticle;
        }

        private GameObject headControl;
        private Transform headCtrlCanvasTrans;
        public void HeadShow()
        {
            if (headControl == null)
            {
                headControl = (GameObject)Instantiate(Resources.Load<GameObject>("Reticle/NarHeadControl"),  gameObject.transform);
                headCtrlCanvasTrans = headControl.GetComponent<Canvas>().transform;
            }
            else
            {
                //headControl.GetComponentInChildren<Image>().color = new Color(255, 255, 255, 1);
                headControl.GetComponent<NxrHeadControl>().Show();
            }
            headControl.transform.localRotation = Quaternion.identity;
            Debug.Log("HeadShow");
        }

        public void HeadDismiss()
        {
            if (headControl != null)
            {
                //headControl.GetComponentInChildren<Image>().color = new Color(255, 255, 255, 0);
                headControl.GetComponent<NxrHeadControl>().Hide();
                Debug.Log("HeadDismiss");
            }
        }

        public void Dismiss()
        {
            if (materialComp != null)
            {
                tempColor.r = materialComp.color.r;
                tempColor.g = materialComp.color.g;
                tempColor.b = materialComp.color.b;
                tempColor.a = 0;
                materialComp.color = tempColor;
            }
            showReticle = false;
            // Debug.LogError("Reticle-Dismiss");
        }

        private void SetGazeTarget(Vector3 target, bool interactive)
        {
            Vector3 targetLocalPosition = cacheTransform.InverseTransformPoint(target);
            reticlePointer.transform.localPosition = new Vector3(0, 0, targetLocalPosition.z - 0.02f);

            reticleDistanceInMeters =
                Mathf.Clamp(targetLocalPosition.z, kReticleDistanceMin, kReticleDistanceMax);

            bool IsHoverMode = NxrViewer.Instance != null && NxrViewer.Instance.HeadControl == HeadControl.Hover;
            if (IsHoverMode)
            {
                //this.gameObject.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                headControl.transform.localPosition = new Vector3(0, 0, reticleDistanceInMeters - 0.02f);
                headCtrlCanvasTrans.rotation = Quaternion.identity;
                headControl.GetComponent<NxrHeadControl>().HandleGazeStay();
                Dismiss();
            }
            else
            {
                reticleInnerAngle = kReticleMinInnerAngle + (interactive ? kReticleGrowthAngle : 0);
                reticleOuterAngle = kReticleMinOuterAngle + (interactive ? kReticleGrowthAngle : 0);
            }
        }

        public void UpdateColor(Color color)
        {
            if(materialComp  != null)
            {
                float alpha = materialComp.color.a;
                alphaValue = alpha;

                tempColor.r = color.r;
                tempColor.g = color.g;
                tempColor.b = color.b;
                tempColor.a = alphaValue;
                materialComp.color = tempColor;
            }
        }

        public void UpdateSize(float size)
        {
            kReticleMinOuterAngle = size;
        }

        public float GetSize()
        {
            return kReticleMinOuterAngle;
        }
    }
}