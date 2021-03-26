using UnityEngine;
using Nxr.Internal;
using NibiruTask;

namespace NXR.Samples
{
    public class InputTest : MonoBehaviour
    {
        public TextMesh textMesh;
        public TextMesh textMeshControllerLeft;
        public TextMesh textMeshControllerRight;

        InteractionManager.NACTION_HAND_TYPE[] handTypes = new InteractionManager.NACTION_HAND_TYPE[]
        {
            InteractionManager.NACTION_HAND_TYPE.HAND_LEFT,
            InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT
        };

        int[] threeDofCtrlKeyCode = new int[] {
            CKeyEvent.KEYCODE_CONTROLLER_TRIGGER,
            CKeyEvent.KEYCODE_CONTROLLER_MENU,
            CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD,
            CKeyEvent.KEYCODE_CONTROLLER_VOLUMN_DOWN,
            CKeyEvent.KEYCODE_CONTROLLER_VOLUMN_UP,
            CKeyEvent.KEYCODE_BUTTON_A,
            CKeyEvent.KEYCODE_BUTTON_B,
            CKeyEvent.KEYCODE_BUTTON_X,
            CKeyEvent.KEYCODE_BUTTON_Y
        };

        string[] threeDofCtrlKeyStr = new string[] {
            "3Dof Controller Trigger : ",
            "3Dof Controller Menu : ",
            "3Dof Controller Touchpad : ",
            "3Dof Controller VolumnDown : ",
            "3Dof Controller VolumnUp : ",
            "3Dof Controller A : ",
            "3Dof Controller B : ",
            "3Dof Controller X : ",
            "3Dof Controller Y : "
        };

        int[] sixDofCtrlKeyCode = new int[] {
            CKeyEvent.KEYCODE_CONTROLLER_TRIGGER,
            CKeyEvent.KEYCODE_CONTROLLER_MENU,
            CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD,
            CKeyEvent.KEYCODE_CONTROLLER_GRIP,
            CKeyEvent.KEYCODE_BUTTON_A,
            CKeyEvent.KEYCODE_BUTTON_B,
            CKeyEvent.KEYCODE_BUTTON_X,
            CKeyEvent.KEYCODE_BUTTON_Y
        };

        string[] sixDofCtrlKeyStr = new string[] {
            "SixDof Controller Trigger : ",
            "SixDof Controller Menu : ",
            "SixDof Controller Touchpad : ",
            "SixDof Controller Grip : ",
            "SixDof Controller A : ",
            "SixDof Controller B : ",
            "SixDof Controller X : ",
            "SixDof Controller Y : ",
        };

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            //GameObject controllerDot = NibiruAxis.NxrPlayerCtrl.Instance.GetControllerLaserDot();
            //if (controllerDot != null)
            //{
            //    Debug.Log("Contoller Dot:" + controllerDot.transform.position.ToString());
            //}
            //NxrReticle reticle = NxrViewer.Instance.GetNxrReticle();
            //if (reticle.IsShowing())
            //{
            //    Debug.Log("Reticle:" + reticle.GetReticlePointer().transform.position.ToString());
            //}

            if (NxrInput.GetKeyDown(CKeyEvent.KEYCODE_DPAD_CENTER))
            {
                textMesh.text = "HMD Enter : Down";
            }

            if (NxrInput.GetKeyPressed(CKeyEvent.KEYCODE_DPAD_CENTER))
            {
                textMesh.text = "HMD Enter : Pressed";
            }

            if (NxrInput.GetKeyUp(CKeyEvent.KEYCODE_DPAD_CENTER))
            {
                textMesh.text = "HMD Enter  : Up";
            }

            if (NxrInput.GetKeyDown(CKeyEvent.KEYCODE_BACK))
            {
                textMesh.text = "HMD Back : Down";
            }

            if (NxrInput.GetKeyPressed(CKeyEvent.KEYCODE_BACK))
            {
                textMesh.text = "HMD Back : Pressed";
            }

            if (NxrInput.GetKeyUp(CKeyEvent.KEYCODE_BACK))
            {
                textMesh.text = "HMD Back : Up";
            }

            if (NxrInput.GetKeyDown(CKeyEvent.KEYCODE_DPAD_LEFT))
            {
                textMesh.text = "HMD Left : Down";
            }

            if (NxrInput.GetKeyPressed(CKeyEvent.KEYCODE_DPAD_LEFT))
            {
                textMesh.text = "HMD Left : Pressed";
            }

            if (NxrInput.GetKeyUp(CKeyEvent.KEYCODE_DPAD_LEFT))
            {
                textMesh.text = "HMD Left : Up";
            }

            if (NxrInput.GetKeyDown(CKeyEvent.KEYCODE_DPAD_RIGHT))
            {
                textMesh.text = "HMD Right : Down";
            }

            if (NxrInput.GetKeyPressed(CKeyEvent.KEYCODE_DPAD_RIGHT))
            {
                textMesh.text = "HMD Right : Pressed";
            }

            if (NxrInput.GetKeyUp(CKeyEvent.KEYCODE_DPAD_RIGHT))
            {
                textMesh.text = "HMD Right : Up";
            }

            if (NxrInput.GetKeyDown(CKeyEvent.KEYCODE_DPAD_UP))
            {
                textMesh.text = "HMD Up : Down";
            }

            if (NxrInput.GetKeyPressed(CKeyEvent.KEYCODE_DPAD_UP))
            {
                textMesh.text = "HMD Up : Pressed";
            }

            if (NxrInput.GetKeyUp(CKeyEvent.KEYCODE_DPAD_UP))
            {
                textMesh.text = "HMD Up : Up";
            }

            if (NxrInput.GetKeyDown(CKeyEvent.KEYCODE_DPAD_DOWN))
            {
                textMesh.text = "HMD Down : Down";
            }

            if (NxrInput.GetKeyPressed(CKeyEvent.KEYCODE_DPAD_DOWN))
            {
                textMesh.text = "HMD Down : Pressed";
            }

            if (NxrInput.GetKeyUp(CKeyEvent.KEYCODE_DPAD_DOWN))
            {
                textMesh.text = "HMD Down : Up";
            }

            // 3DOF Controller
            if(InteractionManager.Is3DofControllerConnected())
            {
                InteractionManager.NACTION_HAND_TYPE mHandType = InteractionManager.GetHandTypeByHandMode();
                TextMesh textMeshController = textMeshControllerRight;
                if (mHandType == InteractionManager.NACTION_HAND_TYPE.HAND_LEFT)
                {
                    textMeshController = textMeshControllerLeft;
                }

                bool HasKeyDown = false;
                for (int i = 0, size = threeDofCtrlKeyCode.Length; i < size; i++)
                {
                    int keyCode = threeDofCtrlKeyCode[i];
                    string keyStr = threeDofCtrlKeyStr[i];
                    if (NxrInput.GetControllerKeyDown(keyCode))
                    {
                        textMeshController.text = keyStr + "Down_" + mHandType.ToString();
                        HasKeyDown = true;
                    }

                    if (!HasKeyDown && NxrInput.GetControllerKeyPressed(keyCode))
                    {
                        textMeshController.text = keyStr + "Pressed_" + mHandType.ToString();
                        HasKeyDown = true;
                    }

                    if (!HasKeyDown)
                    {
                        if (NxrInput.GetControllerKeyUp(keyCode))
                        {
                            textMeshController.text = keyStr + "Up_" + mHandType.ToString();
                        }
                    }
                }
            }
            else
            {
                // SixDof Controller
                foreach (InteractionManager.NACTION_HAND_TYPE HandType in handTypes)
                {
                    TextMesh textMeshCtrl = null;
                    if (HandType == InteractionManager.NACTION_HAND_TYPE.HAND_LEFT)
                    {
                        textMeshCtrl = textMeshControllerLeft;
                    }
                    else
                    {
                        textMeshCtrl = textMeshControllerRight;
                    }

                    bool HasKeyDown = false;
                    for (int i = 0, size = sixDofCtrlKeyCode.Length; i < size; i++)
                    {
                        int keyCode = sixDofCtrlKeyCode[i];
                        string keyStr = sixDofCtrlKeyStr[i];
                        if (NxrInput.GetControllerKeyDown(keyCode, HandType))
                        {
                            textMeshCtrl.text = keyStr + "Down_" + HandType.ToString();
                            HasKeyDown = true;
                        }

                        if (!HasKeyDown && NxrInput.GetControllerKeyPressed(keyCode, HandType))
                        {
                            textMeshCtrl.text = keyStr + "Pressed_" + HandType.ToString();
                            HasKeyDown = true;
                        }

                        if (!HasKeyDown)
                        {
                            if (NxrInput.GetControllerKeyUp(keyCode, HandType))
                            {
                                textMeshCtrl.text = keyStr + "Up_" + HandType.ToString();
                            }
                        }
                    }
                }
            }
        }
    }
}