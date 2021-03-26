using UnityEngine;

namespace Nxr.Internal
{
    [RequireComponent(typeof(Transform))]
    public class NibiruMarkerCamera : MonoBehaviour
    {
        public NibiruMarker nibiruMarker;
        private int updateProjMat = 0;
        NibiruService nibiruService;
        Camera cam;
        Transform cacheTransform;

        Vector3 firstFoundPosition = Vector3.zero;
        private float lerpTime = 0;
        // Use this for initialization
        void Start()
        {
            nibiruService = NxrViewer.Instance.GetNibiruService();
            cam = this.gameObject.GetComponent<Camera>();
            cacheTransform = transform;
        }

        // Update is called once per frame
        void Update()
        {
            if (nibiruService != null && nibiruService.IsMarkerRecognizeRunning && (nibiruMarker != null && nibiruMarker.IsVisible()) && updateProjMat < 10)
            {
                float[] array = nibiruService.GetMarkerProjectionMatrix();
                if (array != null)
                {
                    // Marker识别到才进行更新Projection
                    // Debug.Log(" >>>>>>>>>>>>>>>>>>>>>>>>>>cam.projectionMatrix : " + cam.projectionMatrix.ToString());
                    Matrix4x4 projectionMatrix = ARUtilityFunctions.MatrixFromFloatArray(nibiruService.GetMarkerProjectionMatrix());
                    cam.projectionMatrix = projectionMatrix; 

                    NxrEye[] eyes = NxrViewer.Instance.eyes;
                    if (eyes != null)
                    {
                        eyes[0].cam.projectionMatrix = projectionMatrix;
                        eyes[1].cam.projectionMatrix = projectionMatrix;

                        // 4:3=>12:9 || 16:9, 2/16
                        //eyes[0].cam.rect = new Rect(0.0625f, 0, 0.375f, 1);
                        //eyes[1].cam.rect = new Rect(0.5625f, 0, 0.375f, 1);
                    }

                    updateProjMat++;
                }
            }

            if (nibiruMarker != null && nibiruMarker.IsVisible())
            {
                Matrix4x4 cameraPose = nibiruMarker.CameraPose();
                Vector3 arPosition = ARUtilityFunctions.PositionFromMatrix(cameraPose);
                // Camera orientation: In ARToolKit, zero rotation of the camera corresponds to looking vertically down on a marker
                // lying flat on the ground. In Unity however, if we still treat markers as being flat on the ground, we clash with Unity's
                // camera "rotation", because an unrotated Unity camera is looking horizontally.
                // So we choose to treat an unrotated marker as standing vertically, and apply a transform to the scene to
                // to get it to lie flat on the ground.
                Quaternion arRotation = ARUtilityFunctions.QuaternionFromMatrix(cameraPose);

                Vector3 leftCameraPosDiff = nibiruMarker.CameraPosition(0) - arPosition;
                Vector3 rightCameraPosDiff = nibiruMarker.CameraPosition(1) - arPosition;
                leftCameraPosDiff.y = 0;
                leftCameraPosDiff.z = 0;
                rightCameraPosDiff.y = 0;
                rightCameraPosDiff.z = 0;

                //DefaultPosition=(-0.1, -0.2, -0.3), Left=(-0.2, -0.3, -0.2), Right=(-0.1, -0.3, -0.3)
                //Debug.Log("DefaultPosition=" + arPosition.ToString("f4") + ", Left=" + nibiruMarker.CameraPosition(0).ToString("f4")
                //    + ", Right=" + nibiruMarker.CameraPosition(1).ToString("f4") + ", Left Diff=" + leftCameraPosDiff.ToString("f4")
                //    + ", Right Diff=" + rightCameraPosDiff.ToString("f4"));

                bool dtrMode = NxrGlobal.supportDtr && NxrGlobal.distortionEnabled;

                if (cacheTransform.localPosition.x == 0 && cacheTransform.localPosition.y == 0 && cacheTransform.localPosition.z == 0
                     && (arPosition.x != 0 || arPosition.y != 0 || arPosition.z != 0))
                {
                    Debug.Log("Move from (0,0,0) to " + arPosition.ToString());
                    firstFoundPosition = arPosition;
                }

                if((firstFoundPosition.x !=0 || firstFoundPosition.y != 0 || firstFoundPosition.z != 0) && lerpTime <= 1.0f)
                {
                    // 插值移动到初始位置
                    lerpTime += Time.deltaTime;
                    cacheTransform.localPosition = Vector3.Lerp(cacheTransform.localPosition, firstFoundPosition, lerpTime);
                    // Debug.Log("====>" + cacheTransform.localPosition.ToString());
                } else
                {
                    cacheTransform.localPosition = arPosition;
                }


                cacheTransform.localRotation = arRotation;

                NxrGlobal.markerZDistance = Mathf.Abs(arPosition.z);

                // print
                // Debug.Log("POS = " + arPosition.ToString());
                // Debug.Log("ROT = " + arRotation.ToString());
                // print
                // 平滑处理
                // cacheTransform.localRotation = SmoothMove(cacheTransform.localRotation, arRotation);

                NxrEye[] eyes = NxrViewer.Instance.eyes;
                if (eyes != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        // 非DTR取消左右眼偏移
                        if (!dtrMode && !NxrGlobal.offaxisDistortionEnabled)
                        {
                            // NED+
                            eyes[i].cacheTransform.localPosition = new Vector3(0, 0, 0);
                        }
                        else if (!dtrMode && NxrGlobal.offaxisDistortionEnabled)
                        {
                            // Polaroid
                            float offAxisCameraOffset = i == 0 ?
                                -0.029f + leftCameraPosDiff.x + NxrGlobal.leftMarkerCameraOffSet :
                                0.029f + rightCameraPosDiff.x + NxrGlobal.rightMarkerCameraOffset;
                            eyes[i].cacheTransform.localPosition = new Vector3(offAxisCameraOffset, 0, 0);
                            // Debug.Log(i  + "-------> " + eyes[i].cacheTransform.localPosition.ToString("f4") + ",leftCameraPosDiff="+ leftCameraPosDiff.x
                            //+ ",rightCameraPosDiff="+ rightCameraPosDiff.x);
                        }
                        else
                        {
                            eyes[i].cacheTransform.localPosition = new Vector3(i == 0 ? -0.032f : 0.032f, 0, 0) + (i == 0 ? leftCameraPosDiff : rightCameraPosDiff);
                            // Debug.Log(i  + "-------> " + eyes[i].cacheTransform.localPosition.ToString("f4"));
                        }
                    }
                }

                // Debug.Log("camera pose->" + transform.localPosition.ToString() + "," + transform.localRotation.eulerAngles.ToString());
            }

        }

        private bool triggerStop = false;
        private void OnDestroy()
        {
            if (nibiruService != null && nibiruService.IsMarkerRecognizeRunning)
            {
                nibiruService.StopMarkerRecognize();
                triggerStop = true;
            }

            lerpTime = 0;
            firstFoundPosition = Vector3.zero;
            Debug.Log("NibiruMarkerCamera.OnDestroy");
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                // 暂停相机
                OnDestroy();
            }
            else if (triggerStop)
            {
                if (nibiruService != null)
                {
                    nibiruService.StartMarkerRecognize();
                    triggerStop = false;

                    updateProjMat = 0;
                }
            }
        }
          
    }
}