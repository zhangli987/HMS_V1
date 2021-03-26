using UnityEngine;
namespace Nxr.Internal
{
    public class NxrControllerModel : MonoBehaviour
    {
        NxrTrackedDevice trackedDevice;
        Transform touchpad;
        Transform menu;
        Transform system;
        Transform grip_left;
        Transform grip_right;
        Transform trigger;
        // Use this for initialization
        void OnEnable()
        {
            trackedDevice = GetComponentInParent<NxrTrackedDevice>();
            touchpad = transform.Find("buttons/button_touchpad");
            menu = transform.Find("buttons/button_menu");
            system = transform.Find("buttons/button_system");
            grip_left = transform.Find("buttons/button_grip_left");
            grip_right = transform.Find("buttons/button_grip_right");
            trigger = transform.Find("buttons/button_trigger");
        }

        // Update is called once per frame
        void Update()
        {
            if (trackedDevice == null) return;

            if (trackedDevice.GetButtonPressed(NxrTrackedDevice.ButtonID.TouchPad))
            {
                TouchPad_Down();
            }
            else
            {
                TouchPad_Up();
            }

            if (trackedDevice.GetButtonPressed(NxrTrackedDevice.ButtonID.Menu))
            {
                Menu_Down();
            }
            else
            {
                Menu_Up();
            }

            if (trackedDevice.GetButtonPressed(NxrTrackedDevice.ButtonID.System))
            {
                System_Down();
            }
            else
            {
                System_Up();
            }

            if (trackedDevice.GetButtonPressed(NxrTrackedDevice.ButtonID.Grip))
            {
                Grip_Down();
            }
            else
            {
                Grip_Up();
            }

            if (trackedDevice.GetButtonPressed(NxrTrackedDevice.ButtonID.Trigger))
            {
                Trigger_Down();
            }
            else
            {
                Trigger_Up();
            }
        }

        //touchpad
        void TouchPad_Down()
        {
            if(touchpad != null) touchpad.transform.localPosition = new Vector3(0, -1, 0);
        }
        void TouchPad_Up()
        {
            if (touchpad != null) touchpad.transform.localPosition = Vector3.zero;
        }
        //menu
        void Menu_Down()
        {
            if (menu != null) menu.transform.localPosition = new Vector3(0, -1, 0);
        }
        void Menu_Up()
        {
            if (menu != null) menu.transform.localPosition = Vector3.zero;
        }

        //system
        void System_Down()
        {
            if (system != null) system.transform.localPosition = new Vector3(0, -1, 0);
        }
        void System_Up()
        {
            if (system != null) system.transform.localPosition = Vector3.zero;
        }

        //trigger
        void Trigger_Down()
        {
            if (trigger != null) trigger.transform.localPosition = new Vector3(0, 12, -5);
            if (trigger != null) trigger.transform.localRotation = Quaternion.Euler(-20, 0, 0);
        }
        void Trigger_Up()
        {
            if (trigger != null) trigger.transform.localPosition = Vector3.zero;
            if (trigger != null) trigger.transform.localRotation = Quaternion.identity;
        }

        //grip
        void Grip_Down()
        {
            if (grip_left != null) grip_left.transform.localPosition = new Vector3(1, 0, 0);
            if (grip_right != null) grip_right.transform.localPosition = new Vector3(-1, 0, 0);
        }

        void Grip_Up()
        {
            if (grip_left != null) grip_left.transform.localPosition = Vector3.zero;
            if (grip_right != null) grip_right.transform.localPosition = Vector3.zero;
        }
    }
}