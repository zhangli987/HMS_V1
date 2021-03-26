using UnityEngine;
using UnityEditor;
using System.IO;

namespace Nxr.Internal
{
    public class NxrPluginEditor : EditorWindow
    {
        // Add menu named "My Window" to the Window menu
        [MenuItem("NibiruXR/Plugin Manager", false, 88)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            NxrPluginEditor window = (NxrPluginEditor)EditorWindow.GetWindow(typeof(NxrPluginEditor));
            window.minSize = new Vector2(320, 300);
            string data = Read("AndroidManifest.xml");
            string[] lines = data.Split('\n');
            for (int i = 0, l = lines.Length; i < l; i++)
            {
                string lineContent = lines[i];
                if (lineContent.Contains("NIBIRU_PLUGIN_IDS"))
                {
                    if (lineContent.Contains("6DOF"))
                    {
                        window.mSixDofMode = NxrSDKApi.Instance.GetSettingsAssetConfig().mSixDofMode;
                    }
                    if (lineContent.Contains("RECORD"))
                    {
                        window.useRecord = true;
                    }
                    if (lineContent.Contains("MARKER"))
                    {
                        window.useMarker = true;
                    }
                    break;
                }
            }

            window.Show();
        }

        //public bool useVoice = false;
        //public bool useGesture = false;
        public bool useRecord = false;
        public bool useMarker = false;
        //public bool useRecoginize = false;
        SixDofMode mSixDofMode = SixDofMode.Head_3Dof_Ctrl_3Dof;

        void OnGUI()
        {
            GUILayout.Space(20);
            GUILayout.Label("Choose the plugin ids :", EditorStyles.boldLabel);

            //"VOICE", "6DOF", "GESTURE", "RECORD", "MARKER", "RECOGINIZE" 
            //useVoice = EditorGUILayout.Toggle("VOICE", useVoice);
            mSixDofMode = (SixDofMode)EditorGUILayout.EnumPopup(new GUIContent("SixDof Mode", "Choose SixDofMode: position is absolute data") , mSixDofMode);
            //useGesture = EditorGUILayout.Toggle("GESTURE", useGesture);
            useRecord = EditorGUILayout.Toggle("RECORD", useRecord);
            useMarker = EditorGUILayout.Toggle("MARKER", useMarker);
            //useRecoginize = EditorGUILayout.Toggle("RECOGINIZE", useRecoginize);
            bool use6DOF = mSixDofMode != SixDofMode.Head_3Dof_Ctrl_3Dof;
            string declaredStr = //(useVoice ? "VOICE," : "") +
                 (use6DOF ? "6DOF," : "") +
                   //(useGesture ? "GESTURE," : "") +
                   (useRecord ? "RECORD," : "") +
                    (useMarker ? "MARKER," : "");
                    //(useRecoginize ? "RECOGINIZE," : "");

            if (declaredStr.Length > 0 && declaredStr.LastIndexOf(',') == declaredStr.Length - 1)
            {
                declaredStr = declaredStr.Remove(declaredStr.Length - 1);
            }

            int useCameraPluginIds = 0;
            if(useMarker)
            {
                useCameraPluginIds++;
            }

            if (useRecord)
            {
                useCameraPluginIds++;
            }

            EditorGUILayout.TextField("Declared Plugin Ids",
              declaredStr
               , EditorStyles.boldLabel);


            if (GUILayout.Button("Confirm Choose", GUILayout.Width(200), GUILayout.Height(30)))
            {
                var asset = NxrSDKApi.Instance.GetSettingsAssetConfig();
                asset.mSixDofMode = mSixDofMode;
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                //must Refresh
                AssetDatabase.Refresh();

                if (useCameraPluginIds > 1)
                {
                    bool res = EditorUtility.DisplayDialog("Conflict Warning",
                    "Can not choose them at the same time : [GESTURE][RECORD] [MARKER] [RECOGINIZE], because they all use camera, choose only one is ok !!!",
                   "Force Use", "Cancel");
                    Debug.Log(res);
                    if(res)
                    {
                        NxrPluginEditor window = (NxrPluginEditor)EditorWindow.GetWindow(typeof(NxrPluginEditor));
                        window.Close();
                    } else
                    {
                        return;
                    }
                }
                else
                {
                    NxrPluginEditor window = (NxrPluginEditor)EditorWindow.GetWindow(typeof(NxrPluginEditor));
                    window.Close();
                }

                string data = Read("AndroidManifest.xml");
                string[] lines = data.Split('\n');
                string newdata = "";
                for (int i = 0, l = lines.Length; i < l; i++)
                {
                    string lineContent = lines[i];
                    if (lineContent.Contains("NIBIRU_PLUGIN_IDS"))
                    {
                        lineContent = "    <meta-data android:value=\"" + declaredStr + "\" android:name=\"NIBIRU_PLUGIN_IDS\"/>";
                    }
                    newdata = newdata + lineContent + "\n";
                }

                Write("AndroidManifest.xml", newdata);
            }

        }

        private static string GetFullPath(string name)
        {
            return Application.dataPath + "/Plugins/Android/" + name;
        }

        /// 检测文件是否存在Application.dataPath目录
        public static bool IsFileExists(string fileName)
        {
            if (fileName.Equals(string.Empty))
            {
                return false;
            }

            return System.IO.File.Exists(GetFullPath(fileName));
        }


        /// 从对应文件读取数据
        public static string Read(string fileName)
        {

            if (IsFileExists(fileName))
            {
                return System.IO.File.ReadAllText(GetFullPath(fileName));
            }
            else
            {
                return "";
            }
        }

        public static void Write(string fileName, string contents)
        {
            int lio = fileName.LastIndexOf('/');
            if (lio > 0) CreateFolder(fileName.Substring(0, fileName.LastIndexOf('/')));

            System.IO.TextWriter tw = new System.IO.StreamWriter(GetFullPath(fileName), false);
            tw.Write(contents);
            tw.Close();

            AssetDatabase.Refresh();
        }

        /// 检测是否存在文件夹
        public static bool IsFolderExists(string folderPath)
        {
            if (folderPath.Equals(string.Empty))
            {
                return false;
            }

            return System.IO.Directory.Exists(GetFullPath(folderPath));
        }

        /// 创建文件夹
        public static void CreateFolder(string folderPath)
        {
            if (!IsFolderExists(folderPath))
            {
                System.IO.Directory.CreateDirectory(GetFullPath(folderPath));

                AssetDatabase.Refresh();
            }
        }

    }
}