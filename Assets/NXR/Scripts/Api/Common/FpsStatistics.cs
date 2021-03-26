using System.Runtime.InteropServices;
using UnityEngine;
namespace Nxr.Internal
{
    public class FpsStatistics : MonoBehaviour
    {
        NibiruService mNibiruService;
        TextMesh textMesh;
        // Use this for initialization
        void Start()
        {
            textMesh = GetComponent<TextMesh>();
            if (NxrViewer.Instance.ShowFPS)
            {
                mNibiruService = NxrViewer.Instance.GetNibiruService();
                if(mNibiruService != null)
                {
                    mNibiruService.SetEnableFPS(true);
                }
            } else
            {
                Debug.Log("Display FPS is disabled.");
            }

            Debug.Log("TrackerPosition=" + NxrViewer.Instance.TrackerPosition);
        }

        // Update is called once per frame
        void Update()
        {
            if(mNibiruService != null)
            {
                float[] fps = mNibiruService.GetFPS();
                textMesh.text = "FBO " + fps[0] + ", DTR " + fps[1];
            }
        }
    }
}