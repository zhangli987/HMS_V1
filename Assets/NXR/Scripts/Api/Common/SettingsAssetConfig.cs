using UnityEngine;

namespace Nxr.Internal
{
    [CreateAssetMenu(fileName = "SettingsAssetConfig", order = 1)]
    public class SettingsAssetConfig : ScriptableObject
    {
        [SerializeField]
        public SixDofMode mSixDofMode = SixDofMode.Head_3Dof_Ctrl_6Dof;

        [SerializeField]
        public SleepTimeoutMode mSleepTimeoutMode = SleepTimeoutMode.NEVER_SLEEP;
        
        [SerializeField]
        public HeadControl mHeadControl = HeadControl.GazeApplication;
        
        [SerializeField]
        public TextureQuality mTextureQuality = TextureQuality.Best;
        
        [SerializeField]
        public TextureMSAA mTextureMSAA = TextureMSAA.MSAA_2X;
    }
}
