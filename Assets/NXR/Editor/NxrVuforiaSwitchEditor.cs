using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Nxr.Internal
{
    // <category android:name="com.nibiru.intent.category.NAR_VUFORIA" />
    // 和vuforia混用时加入这个标签
    // <category android:name="com.nibiru.intent.category.NAR_VUFORIA_SPLASH" />
    // 启动画面系统控制

    public class NxrVuforiaSwitchEditor : MonoBehaviour
    {
        #region Public Attributes          
        const string path = "NibiruXR/Vuforia Mode (Experimental)";
        #endregion
        // [MenuItem(path, false, 90)]
        public static void MenuCheckVuforiaMode()
        {
            bool flag = Menu.GetChecked(path);
            flag = !flag;
            Menu.SetChecked(path, flag);

            string data = NxrPluginEditor.Read("AndroidManifest.xml");
            string[] lines = data.Split('\n');
            string newdata = "";
            for (int i = 0, l = lines.Length; i < l; i++)
            {
                string lineContent = lines[i];
                if (flag && lineContent.Contains("category.NVR"))
                {
                    lineContent = lineContent + "\n        " + "<category android:name=\"com.nibiru.intent.category.NAR_VUFORIA\" />";
                    lineContent = lineContent + "\n        " + "<category android:name=\"com.nibiru.intent.category.NAR_VUFORIA_SPLASH\" />";
                } else if(!flag && lineContent.Contains("NAR_VUFORIA"))
                {
                    lineContent = "";
                }

                if(flag &&  lineContent.Contains("NIBIRU_PLUGIN_IDS"))
                {
                    lineContent = "    <meta-data android:value=\"\" android:name=\"NIBIRU_PLUGIN_IDS\"/>";
                }

                newdata = newdata + lineContent + "\n";
            }

            NxrPluginEditor.Write("AndroidManifest.xml", newdata);
        }
        
        [MenuItem(path, true)]
        public static bool MenuCheckBefore()
        {
            string data = NxrPluginEditor.Read("AndroidManifest.xml");
            string[] lines = data.Split('\n');

            bool isChecked = false;
            for (int i = 0, l = lines.Length; i < l; i++)
            {
                string lineContent = lines[i];
                if (lineContent.Contains("NAR_VUFORIA"))
                {
                    isChecked = true;
                    break;
                }
            }
            Menu.SetChecked(path, isChecked);
            return true;
        }

    }
}