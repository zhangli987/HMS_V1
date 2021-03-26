using UnityEngine;

namespace Nxr.Internal
{
    public class NxrDeviceImpl : MonoBehaviour
    {
        private static readonly NxrDeviceImpl _instance = null;
        static NxrDeviceImpl()
        {
            _instance = new NxrDeviceImpl();
        }

        private NxrDeviceImpl()
        {

        }

        public static NxrDeviceImpl Instance
        {
            get { return _instance; }
        }

        Camera GetMainCamera()
        {
            return NxrViewer.Instance.GetMainCamera();
        }

        Camera GetLeftEyeCamera()
        {
            return NxrViewer.Instance.GetLeftEyeCamera();
        }

        Camera GetRightEyeCamera()
        {
            return NxrViewer.Instance.GetRightEyeCamera();
        }

        int GetVolume()
        {
            return NxrViewer.Instance.GetNibiruService().GetVolumeValue();
        }

        bool SetVolumeNum(int volume)
        {
            return true;
        }

        void ResetController()
        {
            NxrViewer.Instance.Recenter();
        }

        bool IsCameraLocked()
        {
            return NxrViewer.Instance.IsCameraLocked();
        }

        void SetControllerActive(bool isActive)
        {
            NxrViewer.Instance.SetControllerActive(isActive);
        }

        bool IsControllerConnect()
        {
            return NxrViewer.Instance.IsControllerConnect();
        }

        void LockCamera(bool isLock)
        {
            NxrViewer.Instance.LockCamera(isLock);
        }

        Quaternion GetCameraQuaternion()
        {
            return NxrViewer.Instance.GetCameraQuaternion();
        }

        Quaternion GetControllerQuaternion()
        {
            return NxrViewer.Instance.GetControllerQuaternion();
        }

        Transform GetRayStartPoint()
        {
            return NxrViewer.Instance.GetRayStartPoint();
        }

        Transform GetRayEndPoint()
        {
            return NxrViewer.Instance.GetRayEndPoint();
        }
    }
}