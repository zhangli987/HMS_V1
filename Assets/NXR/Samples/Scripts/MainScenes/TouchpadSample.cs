using NibiruTask;
using Nxr.Internal;
using UnityEngine;
namespace NXR.Samples
{
    public class TouchpadSample : MonoBehaviour
    {
        public GameObject sphere;

        private Vector3 sphereInitPos = new Vector3(0, 0.165f, 0);

        // Start is called before the first frame update
        void Start()
        {
            if(sphere != null)
            {
                sphereInitPos = sphere.transform.localPosition;
            }

        }

        // Update is called once per frame
        void Update()
        {
            bool IsTouchpadTouched = false;
            if(NxrInput.GetControllerKeyPressed(CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH) ||
                NxrInput.GetControllerKeyPressed(CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH, InteractionManager.NACTION_HAND_TYPE.HAND_LEFT) ||
                NxrInput.GetControllerKeyPressed(CKeyEvent.KEYCODE_CONTROLLER_TOUCHPAD_TOUCH, InteractionManager.NACTION_HAND_TYPE.HAND_RIGHT))
            {
                IsTouchpadTouched = true;
            }

            if (sphere != null)
            {
                if(IsTouchpadTouched)
                {
                    Vector2 pos = InteractionManager.TouchPadPosition;
                    sphere.transform.localPosition = sphereInitPos + new Vector3(pos.x/2, -pos.y/2, 0);
                } else
                {
                    sphere.transform.localPosition = sphereInitPos;
                }
            }
        }
    }
}