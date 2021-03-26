using NibiruTask;
using UnityEngine;

namespace NXR.Samples
{
    public class PluginStatus : MonoBehaviour
    {
        public Nxr.Internal.PLUGIN_ID pluginId;

        public TextMesh declaredTM;
        public TextMesh supportedTM;

        // Use this for initialization
        void Start()
        {
            Debug.Log("Get plugin status..." + pluginId + "//" + (int) pluginId);
            declaredTM.text = "Declared: " + NibiruTaskApi.IsPluginDeclared(pluginId);
            supportedTM.text = "Supported: " + NibiruTaskApi.IsPluginSupported(pluginId);
        }

       
    }
}