// Copyright 2016 Nibiru. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;

/// Clears the entire screen.  This script and NxrPostRender work together
/// to draw the whole screen in XR Mode.  There should be exactly one of each
/// component in any NXR-enabled scene.  It is part of the _NxrCamera_
/// prefab, which is included in _NxrMain_.  The NxrViewer script will
/// create one at runtime if the scene doesn't already have it, so generally
/// it is not necessary to manually add it unless you wish to edit the _Camera_
/// component that it controls.
namespace Nxr.Internal
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("NXR/Internal/NxrPreRender")]
    public class NxrPreRender : MonoBehaviour
    {

        public Camera cam { get; private set; }

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Reset()
        {
#if UNITY_EDITOR
            // Member variable 'cam' not always initialized when this method called in Editor.
            // So, we'll just make a local of the same name.
            var cam = GetComponent<Camera>();
#endif
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;
            cam.useOcclusionCulling = false;
            cam.depth = -100;
            cam.farClipPlane = 100;
            if (NxrGlobal.isVR9Platform)
            {
                cam.clearFlags = CameraClearFlags.Nothing;
            }
        }
    }
}