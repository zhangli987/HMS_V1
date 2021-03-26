using UnityEngine;
namespace Nxr.Internal
{
    [RequireComponent(typeof(Collider))]
    public class NibiruSystemUITeleport : MonoBehaviour, INxrGazeResponder
    {
        public void OnGazeEnter()
        {
            Debug.Log("NibiruSystemUITeleport.OnGazeEnter");
        }

        public void OnGazeExit()
        {
            Debug.Log("NibiruSystemUITeleport.OnGazeExit");

            if (nxrNotificationScript == null)
            {
                nxrNotificationScript = gameObject.GetComponent<NxrNotificationScript>();
            }

            nxrNotificationScript.SendCmdToJava(NxrNotificationScript.CMD_ID.HOVER, "-1,-1");

        }

        public void OnGazeTrigger()
        {
            Debug.Log("NibiruSystemUITeleport.OnGazeTrigger");


            if (nxrNotificationScript == null)
            {
                nxrNotificationScript = gameObject.GetComponent<NxrNotificationScript>();
            }

            // CLICK
            nxrNotificationScript.SendCmdToJava(NxrNotificationScript.CMD_ID.CLICK, UVRadio[0] * nxrNotificationScript.PreTextureWidth + "," +
                (1 - UVRadio[1]) * nxrNotificationScript.PreTextureHeight);
        }

        NxrNotificationScript nxrNotificationScript;
        public void OnUpdateIntersectionPosition(Vector3 position)
        {
            // update intersection
            //Debug.Log("OnUpdateIntersectionPosition------->" + position.ToString() + "," +
            //transform.InverseTransformVector(position).ToString());

            float xRadio = (position.x - leftX) / (rightX - leftX);
            float yRadio = (position.y - bottomeY) / (topY - bottomeY);

            UVRadio[0] = Mathf.Clamp(xRadio, 0, 1);
            UVRadio[1] = Mathf.Clamp(yRadio, 0, 1);

            // Debug.Log("UV is " + UVRadio[0] + "," + UVRadio[1]);

            if (nxrNotificationScript == null)
            {
                nxrNotificationScript = gameObject.GetComponent<NxrNotificationScript>();
            }

            // HOVER View Top Left is 0,0
            //nxrNotificationScript.SendCmdToJava(NxrNotificationScript.CMD_ID.HOVER, UVRadio[0] * nxrNotificationScript.PreTextureWidth + "," + 
            // (1-UVRadio[1]) * nxrNotificationScript.PreTextureHeight);

            //Debug.Log("On Hover : " + UVRadio[0] * nxrNotificationScript.PreTextureWidth + "," +
            //(1 - UVRadio[1]) * nxrNotificationScript.PreTextureHeight);


        }

        public float leftX, rightX, topY, bottomeY;
        [Tooltip("bottome left is (0,0), top right is (1,1)")]
        public Vector2 UVRadio;

        // Use this for initialization
        void Start()
        {
            // Debug.Log("" + transform.localPosition.ToString() + "," + transform.localScale.ToString());

            leftX = transform.localPosition.x - transform.localScale.x / 2;
            rightX = transform.localPosition.x + transform.localScale.x / 2;

            topY = transform.localPosition.y + transform.localScale.y / 2;
            bottomeY = transform.localPosition.y - transform.localScale.y / 2;
        }

    }
}