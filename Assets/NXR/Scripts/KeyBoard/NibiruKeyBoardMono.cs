using NibiruAxis;
using NibiruTask;
using UnityEngine;
namespace Nxr.Internal
{
    public class NibiruKeyBoardMono : MonoBehaviour
    {

        public bool isCanUse = true;
        private float m_dwBackTime = 0;
        private bool isBack = false;

        /// <summary>
        /// The interval of Input.
        /// </summary>
        private float m_dwInputClip = 0.2f;
        // Use this for initialization
        void Start()
        {
            isCanUse = true;

        }

        // Update is called once per frame
        void Update()
        {
#if UNITY_ANDROID
            if (Application.platform == RuntimePlatform.Android && (NxrPlayerCtrl.Instance.IsQuatConn() || ControllerAndroid.IsNoloConn()))
            {
                int[] KeyAction = null;
                if (InteractionManager.IsControllerConnected())
                {
                    KeyAction = InteractionManager.GetKeyAction();
                }
                else
                {
                    KeyAction = NibiruTaskApi.GetKeyAction();
                }
                if (KeyAction[CKeyEvent.KEYCODE_DPAD_CENTER] == 0)
                {
                    if (isCanUse)
                    {
                        isCanUse = false;
                        Invoke("CanKey", m_dwInputClip);
                        NibiruKeyBoard.Instance.OnPressEnterByQuat();
                    }
                }
            }
            Transform mTransform = NxrViewer.Instance.GetHead().transform;

            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown((KeyCode)10) || Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetMouseButtonDown(0))
            {
                if (isCanUse)
                {
                    isCanUse = false;
                    Invoke("CanKey", m_dwInputClip);
                    NibiruKeyBoard.Instance.OnPressEnterByCamera();
                }
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                if (isCanUse)
                {
                    isCanUse = false;
                    Invoke("CanKey", m_dwInputClip);
                    NibiruKeyBoard.Instance.OnPressEnterByCamera();
                    // OnPressEnterByMouse PC通过鼠标选中
                }
            }

#endif
        }

        public void OnPressEnter()
        {
            if (isCanUse)
            {
                isCanUse = false;
                Invoke("CanKey", m_dwInputClip);

                //NibiruKeyBoard.Instance.OnPressEnter();
            }
        }

        public void CanKey()
        {
            isCanUse = true;
        }

        public void OnPressLeft()
        {

        }

        public void OnPressRight()
        {
        }

        public void OnPressDown()
        {
        }

        public void OnPressUp()
        {
        }

        public void OnPressBack()
        {
        }

        public void OnPressVolumnUp()
        {
        }

        public void OnPressVolumnDown()
        {
        }
    }
}