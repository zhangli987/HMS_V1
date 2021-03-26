using UnityEngine;
namespace NXR.Samples
{
    public class HmdType : MonoBehaviour
    {
        TextMesh textMesh;

        // Use this for initialization
        void Start()
        {
            textMesh = GetComponent<TextMesh>();
#if NIBIRU_VR
            if(textMesh != null)
            {
                textMesh.text = "Declare HMD Type : VR";
            }
            
#elif NIBIRU_AR
            if (textMesh != null)
            {
                textMesh.text = "Declare HMD Type : AR";
            }
#endif
        }
    }
}