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
using System.Collections.Generic;

/// Performs distortion correction on the rendered stereo screen.  This script
/// and NxrPreRender work together to draw the whole screen in AR Mode.
/// There should be exactly one of each component in any NXR-enabled scene. It
/// is part of the _NxrCamera_ prefab, which is included in
/// _NxrMain_. The NxrViewer script will create one at runtime if the
/// scene doesn't already have it, so generally it is not necessary to manually
/// add it unless you wish to edit the Camera component that it controls.
///
/// In the Unity editor, this script also draws the analog of the UI layer on
/// the phone (alignment marker, settings gear, etc).
namespace Nxr.Internal
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("NXR/Internal/NxrPostRender")]
    public class NxrPostRender : MonoBehaviour
    {

        // Convenient accessor to the camera component used through this script.
        public Camera cam { get; private set; }

        // Distortion mesh parameters.

        // Size of one eye's distortion mesh grid.  The whole mesh is two of these grids side by side.
        private const int kMeshWidth = 20;
        private const int kMeshHeight = 20;
        // Whether to apply distortion in the grid coordinates or in the texture coordinates.
        private const bool kDistortVertices = true;

        private Mesh distortionMesh;
        private Material meshMaterial;

        Mesh quadMesh;
        private float centerWidthPx;
        private float buttonWidthPx;
        private float xScale;
        private float yScale;
        private Matrix4x4 xfm;

        void Reset()
        {
#if UNITY_EDITOR
            // Member variable 'cam' not always initialized when this method called in Editor.
            // So, we'll just make a local of the same name.
            var cam = GetComponent<Camera>();
#endif
            cam.clearFlags = CameraClearFlags.Depth;
            cam.backgroundColor = Color.black;  // Should be noticeable if the clear flags change.
            cam.orthographic = true;
            cam.orthographicSize = 0.5f;
            cam.cullingMask = 0;
            cam.useOcclusionCulling = false;
            cam.depth = 100;
            if (NxrGlobal.isVR9Platform)
            {
                // ���л��Ʊ���ͼ����ʡ����Ҫ�Ļ��Ʋ���
                cam.clearFlags = CameraClearFlags.Nothing;
            }
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            Reset();
            meshMaterial = new Material(Shader.Find("NAR/UnlitTexture"));
        }


        private float aspectComparison;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        void OnPreCull()
        {
            // The Game window's aspect ratio may not match the fake device parameters.
            float realAspect = (float)Screen.width / Screen.height;
            float fakeAspect = NxrViewer.Instance.Profile.screen.width / NxrViewer.Instance.Profile.screen.height;
            aspectComparison = fakeAspect / realAspect;
            cam.orthographicSize = 0.5f * Mathf.Max(1, aspectComparison);
        }
#endif

        bool firstDraw = true;
        void OnRenderObject()
        {
            if (Camera.current != cam || !NxrViewer.Instance.SplitScreenModeEnabled)
                return;

            if (!Application.isEditor && NxrGlobal.supportDtr) return;
            
            RenderTexture stereoScreen = NxrViewer.Instance.GetStereoScreen(0);

            if (stereoScreen == null)
            {
                return;
            }

            bool useDFT = NxrViewer.USE_DTR && !NxrGlobal.supportDtr;

            if ((!NxrViewer.USE_DTR || useDFT) && NxrGlobal.distortionEnabled)
            {
                if (distortionMesh == null || NxrViewer.Instance.ProfileChanged)
                {
                    RebuildDistortionMesh();
                }

                meshMaterial.mainTexture = stereoScreen;
                meshMaterial.SetPass(0);

                if (!firstDraw)
                {
                    if (NxrGlobal.offaxisDistortionEnabled)
                    {
                        int offsetx1 = NxrGlobal.offaxisOffset[0] > 0 ? NxrGlobal.offaxisOffset[0] : 60;
                        int offsetx2 = NxrGlobal.offaxisOffset[1] > 0 ? NxrGlobal.offaxisOffset[1] : 60;
                        int offsety1 = NxrGlobal.offaxisOffset[2] > 0 ? NxrGlobal.offaxisOffset[2] : 10;
                        int offsety2 = NxrGlobal.offaxisOffset[3] > 0 ? NxrGlobal.offaxisOffset[3] : 170;
                        GL.Viewport(new Rect(offsetx1, (offsety1 + offsety2) / 2, Screen.width - offsetx1 - offsetx2, Screen.height - offsety1 - offsety2));
                    }
                    Graphics.DrawMeshNow(distortionMesh, transform.position, transform.rotation);
                }
                else
                {
                    firstDraw = false;
                }
            }
        }

        public Texture PreviewTexture;

        void OnGUI()
        {
            bool useDFT = NxrViewer.USE_DTR && !NxrGlobal.supportDtr;
            if (NxrGlobal.distortionEnabled || (!useDFT && NxrViewer.USE_DTR)) return;
            
            if (!Event.current.type.Equals(EventType.Repaint))
            {
                return;
            }

            if (PreviewTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width / 2, Screen.height), PreviewTexture);
                GUI.DrawTexture(new Rect(Screen.width / 2, 0, Screen.width / 2, Screen.height), PreviewTexture);
            }

            if ((!NxrViewer.USE_DTR || useDFT) && !NxrGlobal.distortionEnabled && !firstDraw)
            {
                RenderTexture stereoScreen = NxrViewer.Instance.GetStereoScreen(0);
                if (stereoScreen == null)
                {
                    return;
                }
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), stereoScreen);
            }
            else
            {
                firstDraw = false;
            }
        }

        private void BuildQuadMesh()
        {
            quadMesh = new Mesh();
            quadMesh.vertices = new Vector3[] { new Vector3(-1, -1, 1), new Vector3(-1, 1, 1), new Vector3(1, 1, 1), new Vector3(1, -1, 1) };
            quadMesh.uv = new Vector2[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 0, 1) };
            quadMesh.triangles = new int[]
            { 0, 1, 2,
          0, 2, 3
            };
            quadMesh.UploadMeshData(true);
        }

        private void RebuildDistortionMesh()
        {
            distortionMesh = new Mesh();
            Vector3[] vertices;
            Vector2[] tex;

            int meshWidth = kMeshWidth;
            int meshHeight = kMeshHeight;

            if (NxrGlobal.offaxisDistortionEnabled)
            {
                cam.orthographicSize = 0.5649f;// 1.12:2
                meshMaterial = new Material(Shader.Find("NAR/UnlitTextureOffaxis"));

                if (NxrGlobal.meshSize != null)
                {
                    meshWidth = NxrGlobal.meshSize[0];
                    meshHeight = NxrGlobal.meshSize[1];
                }
                ComputeMeshPointsOffAsix(meshWidth, meshHeight, kDistortVertices, out vertices, out tex);
            }
            else
            {
                ComputeMeshPoints(meshWidth, meshHeight, kDistortVertices, out vertices, out tex);
            }

            int[] indices = ComputeMeshIndices(meshWidth, meshHeight, kDistortVertices);
            Color[] colors = ComputeMeshColors(meshWidth, meshHeight, tex, indices, kDistortVertices);

            distortionMesh.vertices = vertices;
            distortionMesh.uv = tex;
            distortionMesh.colors = colors;
            distortionMesh.triangles = indices;
#if !UNITY_5_5_OR_NEWER
    // Optimize() is deprecated as of Unity 5.5.0p1.
    distortionMesh.Optimize();
#endif  // !UNITY_5_5_OR_NEWER

            distortionMesh.UploadMeshData(true);
        }

        private static Dictionary<string, float[]> configDict = new Dictionary<string, float[]>();
        private static void ComputeMeshPointsOffAsix(int width, int height, bool distortVertices,
                                              out Vector3[] vertices, out Vector2[] tex)
        {
            configDict.Clear();
            // test config
            string text = NxrGlobal.offaxisDistortionConfigData;
            if (text == null || text.Length == 0)
            {
                TextAsset taCN = Resources.Load<TextAsset>("NibiruDistortionConfig");
                text = taCN.text;
            }

            string[] linesCN = text.Split('\n');
            //eye,xi,yi=x,y,uvx,uvy
            foreach (string line in linesCN)
            {
                if (line == null || line.Length <= 1)
                {
                    continue;
                }
                string[] keyAndValue = line.Split('=');
                // Debug.Log("line=" + keyAndValue[0] + "," + keyAndValue[1]);
                string[] values = keyAndValue[1].Split(',');
                float[] uvInfo = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    uvInfo[i] = float.Parse(values[i]);
                }
                configDict.Add(keyAndValue[0], uvInfo);
            }
            // test config

            vertices = new Vector3[2 * width * height];
            tex = new Vector2[2 * width * height];
            for (int e = 0, vidx = 0; e < 2; e++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++, vidx++)
                    {
                        float x = -1.0f + 2.0f * i / width;
                        float y = -1.0f + 2.0f * j / height;
                        string key = e + "," + i + "," + j;
                        float[] uvInfo = new float[4];
                        configDict.TryGetValue(key, out uvInfo);
                        float u = uvInfo[2];
                        float v = uvInfo[3];
                        // -0.5~0.5
                        x = x / 2;
                        y = y / 2;

                        // original 0~1
                        if (e == 0)
                        {
                            // left 0~0.5
                            u = u * 0.5f;
                            x = x - 0.5f;
                        }
                        else
                        {
                            // right
                            u = u * 0.5f + 0.5f;
                            x = x + 0.5f;
                        }

                        vertices[vidx] = new Vector3(x, y, 1);
                        // 0~1
                        tex[vidx] = new Vector2(u, v);
                        // Debug.Log(e + "," + u + "," + v);
                    }
                }

            }
        }

        private static void ComputeMeshPoints(int width, int height, bool distortVertices,
                                              out Vector3[] vertices, out Vector2[] tex)
        {
            float[] lensFrustum = new float[4];
            float[] noLensFrustum = new float[4];
            Rect viewport;
            NxrProfile profile = NxrViewer.Instance.Profile;
            profile.GetLeftEyeVisibleTanAngles(lensFrustum);
            profile.GetLeftEyeNoLensTanAngles(noLensFrustum);
            viewport = profile.GetLeftEyeVisibleScreenRect(noLensFrustum);
            vertices = new Vector3[2 * width * height];
            tex = new Vector2[2 * width * height];
            for (int e = 0, vidx = 0; e < 2; e++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++, vidx++)
                    {
                        float u = (float)i / (width - 1);
                        float v = (float)j / (height - 1);
                        float s, t;  // The texture coordinates in StereoScreen to read from.
                        if (distortVertices)
                        {
                            // Grid points regularly spaced in StreoScreen, and barrel distorted in the mesh.
                            s = u;
                            t = v;
                            float x = Mathf.Lerp(lensFrustum[0], lensFrustum[2], u);
                            float y = Mathf.Lerp(lensFrustum[3], lensFrustum[1], v);
                            float d = Mathf.Sqrt(x * x + y * y);
                            float r = profile.viewer.distortion.distortInv(d);
                            float p = x * r / d;
                            float q = y * r / d;
                            u = (p - noLensFrustum[0]) / (noLensFrustum[2] - noLensFrustum[0]);
                            v = (q - noLensFrustum[3]) / (noLensFrustum[1] - noLensFrustum[3]);
                        }
                        else
                        {
                            // Grid points regularly spaced in the mesh, and pincushion distorted in
                            // StereoScreen.
                            float p = Mathf.Lerp(noLensFrustum[0], noLensFrustum[2], u);
                            float q = Mathf.Lerp(noLensFrustum[3], noLensFrustum[1], v);
                            float r = Mathf.Sqrt(p * p + q * q);
                            float d = profile.viewer.distortion.distort(r);
                            float x = p * d / r;
                            float y = q * d / r;
                            s = Mathf.Clamp01((x - lensFrustum[0]) / (lensFrustum[2] - lensFrustum[0]));
                            t = Mathf.Clamp01((y - lensFrustum[3]) / (lensFrustum[1] - lensFrustum[3]));
                        }
                        // Convert u,v to mesh screen coordinates.
                        float aspect = profile.screen.width / profile.screen.height;
                        u = (viewport.x + u * viewport.width - 0.5f) * aspect;
                        v = viewport.y + v * viewport.height - 0.5f;
                        vertices[vidx] = new Vector3(u, v, 1);
                        // Adjust s to account for left/right split in StereoScreen.
                        s = (s + e) / 2;
                        tex[vidx] = new Vector2(s, t);
                    }
                }
                float w = lensFrustum[2] - lensFrustum[0];
                lensFrustum[0] = -(w + lensFrustum[0]);
                lensFrustum[2] = w - lensFrustum[2];
                w = noLensFrustum[2] - noLensFrustum[0];
                noLensFrustum[0] = -(w + noLensFrustum[0]);
                noLensFrustum[2] = w - noLensFrustum[2];
                viewport.x = 1 - (viewport.x + viewport.width);
            }
        }

        private static Color[] ComputeMeshColors(int width, int height, Vector2[] tex, int[] indices,
                                                   bool distortVertices)
        {
            Color[] colors = new Color[2 * width * height];
            for (int e = 0, vidx = 0; e < 2; e++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++, vidx++)
                    {
                        colors[vidx] = Color.white;
                        if (distortVertices)
                        {
                            if (i == 0 || j == 0 || i == (width - 1) || j == (height - 1))
                            {
                                colors[vidx] = Color.black;
                            }
                        }
                        else
                        {
                            Vector2 t = tex[vidx];
                            t.x = Mathf.Abs(t.x * 2 - 1);
                            if (t.x <= 0 || t.y <= 0 || t.x >= 1 || t.y >= 1)
                            {
                                colors[vidx] = Color.black;
                            }
                        }
                    }
                }
            }
            return colors;
        }

        private static int[] ComputeMeshIndices(int width, int height, bool distortVertices)
        {
            int[] indices = new int[2 * (width - 1) * (height - 1) * 6];
            int halfwidth = width / 2;
            int halfheight = height / 2;
            for (int e = 0, vidx = 0, iidx = 0; e < 2; e++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++, vidx++)
                    {
                        if (i == 0 || j == 0)
                            continue;
                        // Build a quad.  Lower right and upper left quadrants have quads with the triangle
                        // diagonal flipped to get the vignette to interpolate correctly.
                        if ((i <= halfwidth) == (j <= halfheight))
                        {
                            // Quad diagonal lower left to upper right.
                            indices[iidx++] = vidx;
                            indices[iidx++] = vidx - width;
                            indices[iidx++] = vidx - width - 1;
                            indices[iidx++] = vidx - width - 1;
                            indices[iidx++] = vidx - 1;
                            indices[iidx++] = vidx;
                        }
                        else
                        {
                            // Quad diagonal upper left to lower right.
                            indices[iidx++] = vidx - 1;
                            indices[iidx++] = vidx;
                            indices[iidx++] = vidx - width;
                            indices[iidx++] = vidx - width;
                            indices[iidx++] = vidx - width - 1;
                            indices[iidx++] = vidx - 1;
                        }
                    }
                }
            }
            return indices;
        }

        /// <summary>
        /// This function is called when the MonoBehaviour will be destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("NxrPostRender.OnDestroy");
        }
    }
}