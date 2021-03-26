using UnityEngine;

namespace Nxr.Internal
{
    public class NxrControllerHelper
    {
        public static int RIGHT_HAND_MODE = 0;
        public static int LEFT_HAND_MODE = 1;

        public static int HandMode3DOF
        {
            set; get;
        }

        public static bool Is3DofControllerConnected
        {
            set;get;
        }

        public static bool IsLeftNoloControllerConnected
        {
            set;get;
        }

        public static bool IsRightNoloControllerConnected
        {
            set; get;
        }

        public static int ControllerType
        {
            set;get;
        }

        public static void Reset()
        {
            HandMode3DOF = RIGHT_HAND_MODE;
            Is3DofControllerConnected = false;
            IsLeftNoloControllerConnected = false;
            IsRightNoloControllerConnected = false;
            ControllerRaycastObject = null;
            ControllerType = (int)NxrInstantNativeApi.NibiruControllerId.NONE;
        }

        public static void InitController(int type)
        {
            ControllerType = type;
            switch (type)
            {
                case (int)NxrInstantNativeApi.NibiruControllerId.NOLO:
                    IsLeftNoloControllerConnected = true;
                    IsRightNoloControllerConnected = true;
                    CreateNoloController();
                    break;
                case (int)NxrInstantNativeApi.NibiruControllerId.NORMAL_3DOF:
                    Is3DofControllerConnected = true;
                    HandMode3DOF = RIGHT_HAND_MODE;
                    break;
                default:
                    Is3DofControllerConnected = false;
                    break;
            }
        }

        public static GameObject CreateNoloController()
        {
            GameObject obj = GameObject.Find("ControllerNOLO");
            if (obj != null)
            {
                return obj;
            }
            GameObject objPrefab = (GameObject)Resources.Load("Prefabs/ControllerNOLO");
            return (GameObject)GameObject.Instantiate(objPrefab, Vector3.zero, Quaternion.identity);
        }

        public static GameObject ControllerRaycastObject { set; get; }
    }
}
