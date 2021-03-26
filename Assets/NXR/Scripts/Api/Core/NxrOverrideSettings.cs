using UnityEngine;

namespace Nxr.Internal
{
    public static class NxrOverrideSettings
    {
        public enum PerfLevel
        {
            NoOverride = -1,
            System = 0,
            Minimum = 1,
            Medium = 2,
            Maximum = 3
        };

        public delegate void OnProfileChangedCallback();
        /// <summary>
        /// Profile changed event
        /// </summary>
        public static OnProfileChangedCallback OnProfileChangedEvent;

        // 回调内添加针对单眼相机的特殊处理脚本
        public delegate void OnEyeCameraInitCallback(NxrViewer.Eye eye, GameObject goParent);
        /// <summary>
        /// Eye camera init callback
        /// </summary>
        public static OnEyeCameraInitCallback OnEyeCameraInitEvent;

        public delegate void OnGazeCallback(GameObject gazeObject);
        /// <summary>
        /// Gaze Event callback
        /// </summary>
        public static OnGazeCallback OnGazeEvent;
		
		public delegate void OnApplicationQuit();
        /// <summary>
        /// Application Quit callback
        /// </summary>
        public static OnApplicationQuit OnApplicationQuitEvent;

    }
}
