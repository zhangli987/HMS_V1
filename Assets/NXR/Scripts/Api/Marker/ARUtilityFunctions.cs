 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace Nxr.Internal
{
    public static class ARUtilityFunctions
    {

        /// <summary>
        /// Returns the named camera or null if not found.
        /// </summary>
        /// <param name="name">Camera name to search for.</param>
        /// <returns>The named <see cref="Camera"/> or null if not found.</returns>
        public static Camera FindCameraByName(string name)
        {
            foreach (Camera c in Camera.allCameras)
            {
                if (c.gameObject.name == name) return c;
            }

            return null;
        }


        /// <summary>
        /// Creates a Unity matrix from an array of floats.
        /// </summary>
        /// <param name="values">Array of 16 floats to populate the matrix.</param>
        /// <returns>A new <see cref="Matrix4x4"/> with the given values.</returns>
        public static Matrix4x4 MatrixFromFloatArray(float[] values)
        {
            if (values == null || values.Length < 16) throw new ArgumentException("Expected 16 elements in values array", "values");

            Matrix4x4 mat = new Matrix4x4();
            for (int i = 0; i < 16; i++) mat[i] = values[i];
            return mat;
        }

#if false
	// Posted on: http://answers.unity3d.com/questions/11363/converting-matrix4x4-to-quaternion-vector3.html
	public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
	{
	    // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
	    Quaternion q = new Quaternion();
	    q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
	    q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
	    q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
	    q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
	    q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
	    q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
	    q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
	    return q;
	}
#else
        public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            if(float.IsNaN(m[0]) || float.IsNaN(m[5]))
            {
                Debug.LogError("QuaternionFromMatrix is NaN !!!");
                return Quaternion.identity;
            }

            // Trap the case where the matrix passed in has an invalid rotation submatrix.
            if (m.GetColumn(2) == Vector4.zero)
            {
                return Quaternion.identity;
            }
            return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
        }
#endif

        public static Vector3 PositionFromMatrix(Matrix4x4 m)
        {
            Vector3 posVec = m.GetColumn(3);
            if(float.IsNaN(posVec.x) || float.IsNaN(posVec.y) || float.IsNaN(posVec.z))
            {
                Debug.LogError("PositionFromMatrix is NaN !!!");
                return Vector3.zero;
            }
            return posVec;
        }

        // Convert from right-hand coordinate system with <normal vector> in direction of +x,
        // <orthorgonal vector> in direction of +y, and <approach vector> in direction of +z,
        // to Unity's left-hand coordinate system with <normal vector> in direction of +x,
        // <orthorgonal vector> in direction of +y, and <approach vector> in direction of +z.
        // This is equivalent to negating row 2, and then negating column 2.
        public static Matrix4x4 LHMatrixFromRHMatrix(Matrix4x4 rhm)
        {
            Matrix4x4 lhm = new Matrix4x4(); ;

            // Column 0.
            lhm[0, 0] = rhm[0, 0];
            lhm[1, 0] = rhm[1, 0];
            lhm[2, 0] = -rhm[2, 0];
            lhm[3, 0] = rhm[3, 0];

            // Column 1.
            lhm[0, 1] = rhm[0, 1];
            lhm[1, 1] = rhm[1, 1];
            lhm[2, 1] = -rhm[2, 1];
            lhm[3, 1] = rhm[3, 1];

            // Column 2.
            lhm[0, 2] = -rhm[0, 2];
            lhm[1, 2] = -rhm[1, 2];
            lhm[2, 2] = rhm[2, 2];
            lhm[3, 2] = -rhm[3, 2];

            // Column 3.
            lhm[0, 3] = rhm[0, 3];
            lhm[1, 3] = rhm[1, 3];
            lhm[2, 3] = -rhm[2, 3];
            lhm[3, 3] = rhm[3, 3];

            return lhm;
        }



        //test
        //float[] cameraMatrix = new float[] { 678.29388f, 637.77411f, 318.29779f, 237.90047f };
        //projectionMatrix = ARUtilityFunctions.GetGLProjectionMatrix(0.01f, 1000f, cameraMatrix, 1920, 1080);
        //test
        // 相机内部矩阵转换成GL投影矩阵
        public static Matrix4x4 GetGLProjectionMatrix(float near, float far, float[] cameraMatrix, int cameraPreviewWidth, int cameraPreviewHeight)
        //-----------------------------------------------------------------------------
        {
            // float near = 0.01;  // Near clipping distance
            // float far = 1000;  // Far clipping distance
            float f_x = cameraMatrix[0]; // Focal length in x axis
            float f_y = cameraMatrix[1]; // Focal length in y axis (usually the same?)
            float c_x = cameraMatrix[2]; // Camera primary point x
            float c_y = cameraMatrix[3]; // Camera primary point y

            float[] glMatrix = new float[16];
            glMatrix[0] = 2.0f * f_x / cameraPreviewWidth;
            glMatrix[1] = 0.0f;
            glMatrix[2] = 0.0f;
            glMatrix[3] = 0.0f;

            glMatrix[4] = 0.0f;
            glMatrix[5] = 2.0f * f_y / cameraPreviewHeight;
            glMatrix[6] = 0.0f;
            glMatrix[7] = 0.0f;

            glMatrix[8] = 1.0f - 2.0f * c_x / cameraPreviewWidth;
            glMatrix[9] = -1.0f + 2.0f * c_y / cameraPreviewHeight;
            glMatrix[10] = -(far + near) / (far - near);
            glMatrix[11] = -1.0f;

            glMatrix[12] = 0.0f;
            glMatrix[13] = 0.0f;
            glMatrix[14] = -2.0f * far * near / (far - near);
            glMatrix[15] = 0.0f;

            return MatrixFromFloatArray(glMatrix);
        }
    }
}