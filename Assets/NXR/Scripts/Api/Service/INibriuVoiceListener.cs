using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Internet connection is required to use voice service.
/// </summary>
namespace Nxr.Internal
{
    public interface INibiruVoiceListener
    {
        /// <summary>
        /// Speech recognition starts.
        /// </summary>
        void OnVoiceBegin();
        /// <summary>
        /// Speech recognition ends.
        /// </summary>
        void OnVoiceEnd();
        /// <summary>
        /// The result of speech recognition.
        /// </summary>
        /// <param name="param"></param>
        void OnVoiceFinishResult(string param);
        /// <summary>
        /// The change of volume.
        /// </summary>
        /// <param name="volume"></param>
        void OnVoiceVolume(string volume);
        /// <summary>
        /// The error of speech recognition.
        /// </summary>
        void OnVoiceFinishError(string errorMsg);

        /// <summary>
        /// Cancel speech recognition.
        /// </summary>
        void OnVoiceCancel();
    }
}