using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using NibiruTask;
using Nxr.Internal;

public class SpriteLoader : MonoBehaviour
{
    private byte[] fileBytes;
    private Dictionary<string, byte[]> spriteDict = new Dictionary<string, byte[]>();
    private int count;

    public void LoadSpriteFile(object imgsPath)
    {
        spriteDict.Clear();
        count = ((string[]) imgsPath).Length;
        Thread readFileThread = new Thread(new ParameterizedThreadStart(ReadFileCore));
        readFileThread.Start(imgsPath);
    }

    void ReadFileCore(object obj)
    {
        var imgsPath = (string[]) obj;
        for (var i = 0; i < imgsPath.Length; i++)
        {
            var fileStream = new FileStream(imgsPath[i], FileMode.Open, FileAccess.Read);
            fileStream.Seek(0, SeekOrigin.Begin);
            var binary = new byte[fileStream.Length];
            fileStream.Read(binary, 0, (int) fileStream.Length);
            spriteDict.Add(imgsPath[i], binary);
            fileStream.Close();
            fileStream.Dispose();
        }
    }

    void Update()
    {
        if (count > 0)
        {
            if (InteractionManager.GetControllerModeType() == InteractionManager.NACTION_CONTROLLER_TYPE.CONTROL_3DOF)
            {
                if (spriteDict.Count == count && !NxrSDKApi.Instance.Is3DofSpriteFirstLoad)
                {
                    CreateSpritesAndCach();
                    NxrSDKApi.Instance.Is3DofSpriteFirstLoad = true;
                }
            }

            if (InteractionManager.GetControllerModeType() == InteractionManager.NACTION_CONTROLLER_TYPE.CONTROL_6DOF)
            {
                if (spriteDict.Count == count && !NxrSDKApi.Instance.Is6DofSpriteFirstLoad)
                {
                    CreateSpritesAndCach();
                    NxrSDKApi.Instance.Is6DofSpriteFirstLoad = true;
                }
            }
        }
    }

    void CreateSpritesAndCach()
    {
        foreach (var keyValue in spriteDict)
        {
            var binary = keyValue.Value;
            var texture2D = new Texture2D(410, 80);
            texture2D.LoadImage(binary);
            var sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height),
                new Vector2(0.5f, 0.5f));
            NxrSDKApi.Instance.AddSprite(keyValue.Key, sprite);
        }

        count = 0;
    }
}