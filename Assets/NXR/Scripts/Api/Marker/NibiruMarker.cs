using UnityEngine;
namespace Nxr.Internal
{
    /// <summary>
    /// 
    /// </summary>
    public class NibiruMarker : MonoBehaviour
    {
        [Tooltip("Scale the position from Marker units (mm) into Unity units (m). example : \n" +
            " 1 means = mm -> m \n 10 means = mm -> dm \n 100 means = mm ->cm ")]
        public float PositonScaleFactor = 1.0f;

        // 
        public delegate void OnMarkerFound();
        public delegate void OnMarkerLost();
        /// <summary>
        /// The callback when Marker is found.
        /// </summary>
        public static OnMarkerFound OnMarkerFoundHandler = null;
        /// <summary>
        /// The callback when Marker is lost.
        /// </summary>
        public static OnMarkerLost OnMarkerLostHandler = null;

        NibiruService nibiruService;
        Transform mTransform;
        bool visible = false;

        private Matrix4x4 cameraPoseMat;
        private Matrix4x4 cameraPoseMatLeft;
        private Matrix4x4 cameraPoseMatRight;

        GameObject origin;

        public bool AutoStartMarkerRecognize = true;

        public bool IsVisible()
        {
            return visible;
        }

        // Use this for initialization
        void Start()
        {
            nibiruService = NxrViewer.Instance.GetNibiruService();
            mTransform = gameObject.transform;

            origin = GameObject.Find("MarkerRoot");

            // Polaroid DTR
            bool dtrMode = NxrGlobal.supportDtr && NxrGlobal.distortionEnabled;
            if (dtrMode)
            {
                NxrViewer.Instance.SwitchControllerMode(false);
            }

            NxrGlobal.isMarkerVisible = false;

            if (nibiruService != null && AutoStartMarkerRecognize)
            {
                nibiruService.StartMarkerRecognize();
            }

            cameraPoseMat = new Matrix4x4();
        }

        // Update is called once per frame
        void Update()
        {
            if (nibiruService != null && nibiruService.IsMarkerRecognizeRunning)
            {
                float[] leftEyeArray = nibiruService.GetMarkerViewMatrix(0);
                float[] rightEyeArray = nibiruService.GetMarkerViewMatrix(1);

                if (leftEyeArray != null && rightEyeArray != null)
                {
                    leftEyeArray[12] *= 0.001f * PositonScaleFactor;
                    leftEyeArray[13] *= 0.001f * PositonScaleFactor;
                    leftEyeArray[14] *= 0.001f * PositonScaleFactor;

                    rightEyeArray[12] *= 0.001f * PositonScaleFactor;
                    rightEyeArray[13] *= 0.001f * PositonScaleFactor;
                    rightEyeArray[14] *= 0.001f * PositonScaleFactor;

                    Matrix4x4 matrixRawLeft = ARUtilityFunctions.MatrixFromFloatArray(leftEyeArray);
                    Matrix4x4 matrixRawRight = ARUtilityFunctions.MatrixFromFloatArray(rightEyeArray);

                    Matrix4x4 transformationMatrixLeft = ARUtilityFunctions.LHMatrixFromRHMatrix(matrixRawLeft);
                    Matrix4x4 transformationMatrixRight = ARUtilityFunctions.LHMatrixFromRHMatrix(matrixRawRight);
                    cameraPoseMatLeft = transformationMatrixLeft.inverse;
                    cameraPoseMatRight = transformationMatrixRight.inverse;
                }

                float[] array = nibiruService.GetMarkerViewMatrix();
                if (array != null)
                {
                    if (!visible)
                    {
                        if (OnMarkerFoundHandler != null)
                        {
                            OnMarkerFoundHandler();
                        }
                        visible = true;
                        NxrGlobal.isMarkerVisible = true;
                        for (int i = 0; i < mTransform.childCount; i++)
                        {
                            mTransform.GetChild(i).gameObject.SetActive(true);
                            Debug.Log("MARKER VISBILE " + mTransform.GetChild(i).gameObject.name);
                        }

                        if (origin != null)
                        {
                            Matrix4x4 pose;
                            // If this marker is the base, no need to take base inverse etc.
                            pose = origin.transform.localToWorldMatrix;

                            transform.position = ARUtilityFunctions.PositionFromMatrix(pose);
                            transform.rotation = ARUtilityFunctions.QuaternionFromMatrix(pose);
                        }

                    }

                    // Filter data
                    array = FilterData(array);

                    // Scale the position from ARToolKit units (mm) into Unity units (m).
                    array[12] *= 0.001f * PositonScaleFactor;
                    array[13] *= 0.001f * PositonScaleFactor;
                    array[14] *= 0.001f * PositonScaleFactor;

                    Matrix4x4 matrixRaw = ARUtilityFunctions.MatrixFromFloatArray(array);

                    // Debug.Log("MarkerViewMatrix>>>>" + matrixRaw.ToString());

                    Matrix4x4 transformationMatrix = ARUtilityFunctions.LHMatrixFromRHMatrix(matrixRaw);
                    cameraPoseMat = transformationMatrix.inverse;

                    //Debug.Log("transformationMatrix>>>>" + transformationMatrix.ToString());
                    //Quaternion quaternion = ARUtilityFunctions.QuaternionFromMatrix(transformationMatrix);
                    //Vector3 eulerAngles = quaternion.eulerAngles;
                    // z轴朝上，转换成z轴朝里，符合unity
                    //mTransform.rotation = Quaternion.Euler(eulerAngles.x - 90, eulerAngles.y, eulerAngles.z);
                    //mTransform.position = ARUtilityFunctions.PositionFromMatrix(transformationMatrix);

                }
                else if (array == null)
                {
                    if (visible)
                    {
                        Debug.Log("MARKER VISIBLE -> NOT VISBILE");
                    }
                    else
                    {
                        Debug.Log("MARKER INIT NOT VISBILE");
                    }

                    if (OnMarkerLostHandler != null)
                    {
                        OnMarkerLostHandler();
                    }
                    NxrGlobal.isMarkerVisible = false;
                    visible = false;
                    for (int i = 0; i < mTransform.childCount; i++)
                    {
                        mTransform.GetChild(i).gameObject.SetActive(false);
                    }
                }

            }

        }

        public Matrix4x4 CameraPose()
        {
            return cameraPoseMat;
        }

        public Vector3 CameraPosition(int eyeType)
        {
            Vector3 arPosition = ARUtilityFunctions.PositionFromMatrix(eyeType == 0 ? cameraPoseMatLeft : cameraPoseMatRight);
            return arPosition;
        }


        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                visible = false;
                NxrGlobal.isMarkerVisible = false;
                for (int i = 0; i < mTransform.childCount; i++)
                {
                    mTransform.GetChild(i).gameObject.SetActive(false);
                }
            }


            if (AutoStartMarkerRecognize && pause && nibiruService != null)
            {
                nibiruService.StopMarkerRecognize();
            }
            else if (AutoStartMarkerRecognize && nibiruService != null)
            {
                nibiruService.StartMarkerRecognize();
            }
        }

        private void OnDestroy()
        {
            // change scene
            if (AutoStartMarkerRecognize && nibiruService != null && nibiruService.IsMarkerRecognizeRunning)
            {
                nibiruService.StopMarkerRecognize();
            }
        }

        private void OnApplicationQuit()
        {
            // exit app
            if (AutoStartMarkerRecognize && nibiruService != null && nibiruService.IsMarkerRecognizeRunning)
            {
                nibiruService.StopMarkerRecognize();
            }
        }

        //******************Filter Data******************
        private float[] lastDataArr;
        public bool filterMarkerJitter = true;

        private float[] FilterData(float[] newArray)
        {
            if (!filterMarkerJitter)
            {
                return newArray;
            }
            if (lastDataArr == null)
            {
                lastDataArr = new float[newArray.Length];
                newArray.CopyTo(lastDataArr, 0);
            }

            int length = newArray.Length;
            for (int i = 0; i < length; i++)
            {
                float diff = Mathf.Abs(newArray[i] - lastDataArr[i]);

                float absValue = Mathf.Abs(newArray[i]);
                if (absValue > 100 && diff < 0.05f)
                {
                    continue;
                }
                else if (absValue > 20 && diff < 0.014f)
                {
                    continue;
                }
                else if (diff < 0.004f)
                {
                    continue;
                }

                // 大数据更新
                lastDataArr[i] = newArray[i];

            }
            return lastDataArr;
        }
        //******************Filter Data******************




    }
}