using UnityEngine;
namespace Nxr.Internal
{
    public class NibiruVirtualMouse : MonoBehaviour
    {
        NibiruService nibiruService;
        // Use this for initialization
        void Start()
        {
            nibiruService = NxrViewer.Instance.GetNibiruService();
            if (nibiruService != null)
            {
                nibiruService.RegisterVirtualMouseService(OnServiceConnected);
            }
        }

        void OnServiceConnected(bool succ)
        {
            // when service is connected, succ = true, call the api SetEnableVirtualMouse to show/dismiss virtual mouse;
            // nibiruService.SetEnableVirtualMouse(true);
            Debug.Log("------------VirtualMouse Service Connected : " + succ);
        }

        private void OnDestroy()
        {
            if (nibiruService != null)
            {
                nibiruService.UnRegisterVirtualMouseService();
                Debug.Log("NibiruVirtualMouse.OnDestroy");
            }
        }
    }
}
