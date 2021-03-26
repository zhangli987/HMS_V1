using Nxr.Internal;
using UnityEngine;

public class NibiruShutDownBox : MonoBehaviour
{
    public static NibiruShutDownBox Instance;
    private GameObject shutDownBox;
    private Transform shutDownBoxTrans;
    private MeshRenderer[] choiceMeshRenderers;
    private MeshRenderer closeMeshRenderer;
    private TextMesh[] textMeshes;
    private float time, timeEnd;
    private bool isShow;

    public bool Showing()
    {
        return isShow;
    }
    
    void Start()
    {
        timeEnd = 4f;
        isShow = false;
        Instance = this;
        choiceMeshRenderers = new MeshRenderer[2];
        textMeshes = new TextMesh[2];
    }

    void CloseBox()
    {
        if (TouchScreenKeyboard.visible) return;
        NibiruRemindBox.Instance.ReleaseDestory();
        if (!shutDownBox)
        {
            shutDownBox = Instantiate(Resources.Load<GameObject>("RemindBox/ShutDownBox"));
            shutDownBoxTrans = shutDownBox.transform;
            for (var i = 0; i < choiceMeshRenderers.Length; i++)
            {
                choiceMeshRenderers[i] =
                    shutDownBoxTrans.Find("Choice" + (i + 1) + "/ChoiceBg").GetComponent<MeshRenderer>();
                textMeshes[i] = shutDownBoxTrans.Find("Choice" + (i + 1) + "/ChoiceText").GetComponent<TextMesh>();
            }

            HandleShutDownBox(choiceMeshRenderers[0].gameObject, TurnOff);
            HandleShutDownBox(choiceMeshRenderers[1].gameObject, Restart);
            closeMeshRenderer = shutDownBoxTrans.Find("Close/CloseBg").GetComponent<MeshRenderer>();
            HandleShutDownBox(closeMeshRenderer.gameObject, Close);
            textMeshes[0].text = LocalizationManager.GetInstance.GetValue("remindbox_shutdown");
            textMeshes[1].text = LocalizationManager.GetInstance.GetValue("remindbox_reboot");
        }
        else
        {
            if (!shutDownBox.activeSelf) shutDownBox.gameObject.SetActive(true);
        }

        var cameraObject = GameObject.Find("MainCamera");
        var forward = cameraObject.transform.forward * 3;
        shutDownBoxTrans.position = cameraObject.transform.position + forward;
        var rotation = cameraObject.transform.rotation;
        shutDownBoxTrans.transform.rotation = new Quaternion(rotation.x, rotation.y, 0, rotation.w);
        SetAlpha(choiceMeshRenderers[0], 0);
        SetAlpha(choiceMeshRenderers[1], 0);
        SetAlpha(closeMeshRenderer, 0);
        time = 0f;
        isShow = true;
    }

    void HandleShutDownBox(GameObject meshGo, NibiruShutDownBoxEvent.ShutDownBoxEvent action)
    {
        var nibiruShutDownBoxEvent = meshGo.GetComponent<NibiruShutDownBoxEvent>();
        nibiruShutDownBoxEvent.handleShutDownBox = action;
    }

    void SetAlpha(MeshRenderer meshRenderer, float alpha)
    {
        var color = meshRenderer.material.color;
        meshRenderer.material.color = new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// Shutdown
    /// </summary>
    void TurnOff()
    {
        NxrViewer.Instance.TurnOff();
        ReleaseDestory();
    }

    /// <summary>
    /// Reboot
    /// </summary>
    void Restart()
    {
        NxrViewer.Instance.Reboot();
        ReleaseDestory();
    }

    /// <summary>
    /// Close page.
    /// </summary>
    void Close()
    {
        ReleaseDestory();
    }

    public void ReleaseDestory()
    {
        if (shutDownBox)
        {
            // Destroy(shutDownBox);
            shutDownBox.gameObject.SetActive(false);
            isShow = false;
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R))
        {
            CloseBox();
        }
#endif
        if (isShow)
        {
            time += Time.deltaTime;
            if (time >= timeEnd)
            {
                ReleaseDestory();
                NxrReticle mNxrReticle = NxrViewer.Instance.GetNxrReticle();
                if (mNxrReticle) mNxrReticle.OnGazeExit(null, null);
            }
        }
    }
}