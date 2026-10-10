#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 使用方法：Unity 菜单栏 → Tools → 一键生成游戏场景
/// 点击后自动创建 Canvas + 三个面板 + 全部 UI 控件，并挂好脚本。
/// </summary>
public static class SceneBuilder
{
    [MenuItem("Tools/一键生成游戏场景")]
    public static void Build()
    {
        RemoveGeneratedObject("Managers");
        RemoveGeneratedObject("Canvas");
        RemoveGeneratedObject("GameUIRoot");
        RemoveGeneratedObject("EventSystem");

        // ---------- Managers ----------
        var managers = new GameObject("Managers");
        managers.AddComponent<GameManager>();
        managers.AddComponent<ApiClient>();

        // ---------- Canvas ----------
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        // ---------- 三个面板 ----------
        var loginPanel = CreatePanel(canvasGo.transform, "LoginPanel", new Color(0.035f, 0.055f, 0.09f, 1));
        var gamePanel = CreatePanel(canvasGo.transform, "GamePanel", new Color(0.035f, 0.055f, 0.09f, 1));
        var lbPanel = CreatePanel(canvasGo.transform, "LeaderboardPanel", new Color(0.035f, 0.055f, 0.09f, 1));
        CreatePanelAccent(loginPanel.transform);
        CreatePanelAccent(gamePanel.transform);
        CreatePanelAccent(lbPanel.transform);

        // ---------- 登录面板 ----------
        CreateText(loginPanel.transform, "KickerText", "OFFLINE GAME RESTORATION", 14, TextAnchor.MiddleCenter,
            new Vector2(0, 155), new Vector2(500, 25), new Color(0.25f, 0.75f, 0.95f));
        var title = CreateText(loginPanel.transform, "TitleText", "老游戏复活计划", 38, TextAnchor.MiddleCenter,
            new Vector2(0, 105), new Vector2(600, 60), Color.white);
        CreateText(loginPanel.transform, "SubtitleText", "连接你的存档，继续未完成的冒险", 18, TextAnchor.MiddleCenter,
            new Vector2(0, 70), new Vector2(500, 30), new Color(0.62f, 0.7f, 0.82f));

        CreateText(loginPanel.transform, "UsernameLabel", "用户名", 16, TextAnchor.MiddleLeft,
            new Vector2(0, 32), new Vector2(320, 22), new Color(0.62f, 0.7f, 0.82f));
        CreateInput(loginPanel.transform, "UsernameInput", "输入用户名...", new Vector2(0, 4));

        CreateText(loginPanel.transform, "PasswordLabel", "密码", 16, TextAnchor.MiddleLeft,
            new Vector2(0, -40), new Vector2(320, 22), new Color(0.62f, 0.7f, 0.82f));
        CreateInput(loginPanel.transform, "PasswordInput", "输入密码...", new Vector2(0, -68), password: true);

        var loginBtn = CreateButton(loginPanel.transform, "LoginButton", "登 录", new Vector2(-85, -130),
            new Vector2(160, 50), new Color(0.15f, 0.55f, 0.9f));
        var regBtn = CreateButton(loginPanel.transform, "RegisterButton", "注 册", new Vector2(85, -130),
            new Vector2(160, 50), new Color(0.15f, 0.2f, 0.3f));

        CreateText(loginPanel.transform, "MessageText", "", 16, TextAnchor.MiddleCenter,
            new Vector2(0, -185), new Vector2(500, 26), new Color(0.85f, 0.65f, 0.35f));

        // ---------- 游戏面板 ----------
        CreateText(gamePanel.transform, "WelcomeText", "欢迎回来，玩家", 22, TextAnchor.MiddleCenter,
            new Vector2(0, 210), new Vector2(600, 36), new Color(0.62f, 0.7f, 0.82f));

        CreateText(gamePanel.transform, "ScoreLabel", "当前分数", 15, TextAnchor.MiddleCenter,
            new Vector2(0, 130), new Vector2(300, 22), new Color(0.45f, 0.75f, 1f));
        CreateText(gamePanel.transform, "ScoreText", "0", 64, TextAnchor.MiddleCenter,
            new Vector2(0, 70), new Vector2(400, 80), Color.white);

        var coinBtn = CreateButton(gamePanel.transform, "CoinButton", "点击金币  +1", new Vector2(0, -20),
            new Vector2(300, 76), new Color(0.95f, 0.72f, 0.18f));
        SetButtonTextColor(coinBtn, new Color(0.14f, 0.1f, 0.02f));

        CreateButton(gamePanel.transform, "SubmitButton", "上传分数到服务器", new Vector2(0, -115),
            new Vector2(300, 52), new Color(0.15f, 0.55f, 0.9f));
        CreateButton(gamePanel.transform, "LeaderboardButton", "查看排行榜", new Vector2(0, -180),
            new Vector2(300, 52), new Color(0.15f, 0.2f, 0.3f));
        CreateButton(gamePanel.transform, "LogoutButton", "退出登录", new Vector2(0, -245),
            new Vector2(300, 46), new Color(0.32f, 0.13f, 0.16f));

        // ---------- 排行榜面板 ----------
        CreateText(lbPanel.transform, "LeaderboardTitle", "服务器排行榜", 30, TextAnchor.MiddleCenter,
            new Vector2(0, 230), new Vector2(500, 44), Color.white);
        var lbText = CreateText(lbPanel.transform, "LeaderboardText", "加载中...", 22, TextAnchor.UpperLeft,
            new Vector2(0, 30), new Vector2(560, 360), new Color(0.85f, 0.9f, 1f));
        CreateButton(lbPanel.transform, "BackButton", "返回游戏", new Vector2(0, -230),
            new Vector2(240, 52), new Color(0.15f, 0.2f, 0.3f));

        // ---------- 挂 GameUI 并自动绑定 ----------
        var uiGo = new GameObject("GameUIRoot");
        uiGo.transform.SetParent(canvasGo.transform, false);
        var gameUI = uiGo.AddComponent<GameUI>();

        // GameUI 的 Awake 会按名字 GameObject.Find 自动查找，
        // 但 Awake 在物体未激活时也可能需要立即绑定，这里直接调用编辑器方法不可行，
        // 所以 GameUI 设计为 Awake 里 Find —— 保证所有物体已创建后再启用即可。
        EditorUtility.SetDirty(gameUI);
        EditorSceneManager_MarkSceneDirty();
    }

    private static void EditorSceneManager_MarkSceneDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    // ---------- 控件工厂 ----------

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var img = panel.AddComponent<Image>();
        img.color = color;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize,
        TextAnchor anchor, Vector2? anchoredPos = null, Vector2? size = null, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color ?? Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        SetRect(go, anchoredPos: anchoredPos ?? Vector2.zero, size: size ?? new Vector2(300, 30));
        return text;
    }

    private static InputField CreateInput(Transform parent, string name, string placeholder,
        Vector2 pos, bool password = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.9f);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.black;
        text.supportRichText = false;
        text.font = text.font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        SetRect(textGo, anchorMin: Vector2.zero, anchorMax: Vector2.one,
            offsetMin: new Vector2(10, 0), offsetMax: new Vector2(-10, 0));

        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(go.transform, false);
        var ph = phGo.AddComponent<Text>();
        ph.text = placeholder;
        ph.fontSize = 20;
        ph.fontStyle = FontStyle.Italic;
        ph.alignment = TextAnchor.MiddleLeft;
        ph.color = new Color(0.5f, 0.5f, 0.5f, 1);
        ph.font = ph.font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (ph.font == null) ph.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        SetRect(phGo, anchorMin: Vector2.zero, anchorMax: Vector2.one,
            offsetMin: new Vector2(10, 0), offsetMax: new Vector2(-10, 0));

        var input = go.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = ph;
        if (password) input.contentType = InputField.ContentType.Password;

        SetRect(go, anchoredPos: pos, size: new Vector2(300, 40));
        return input;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos,
        Vector2? size = null, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color ?? new Color(0.15f, 0.55f, 0.9f);
        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var text = labelGo.AddComponent<Text>();
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = text.font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        SetRect(labelGo, anchorMin: Vector2.zero, anchorMax: Vector2.one, offsetMin: Vector2.zero, offsetMax: Vector2.zero);

        SetRect(go, anchoredPos: pos, size: size ?? new Vector2(220, 50));
        return button;
    }

    private static void SetButtonTextColor(Button button, Color color)
    {
        var text = button.GetComponentInChildren<Text>();
        if (text != null) text.color = color;
    }

    /// <summary>面板顶部加一条细的强调色横条，增加层次感。</summary>
    private static void CreatePanelAccent(Transform panel)
    {
        var bar = new GameObject("TopAccent", typeof(RectTransform));
        bar.transform.SetParent(panel, false);
        var img = bar.AddComponent<Image>();
        img.color = new Color(0.15f, 0.55f, 0.9f, 0.9f);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(260, 4);
        rt.anchoredPosition = new Vector2(0, -18);
    }

    private static void RemoveGeneratedObject(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);
    }

    private static void SetRect(GameObject go, Vector2? anchorMin = null, Vector2? anchorMax = null,
        Vector2? pivot = null, Vector2? anchoredPos = null, Vector2? size = null,
        Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
        rt.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        if (offsetMin.HasValue) rt.offsetMin = offsetMin.Value;
        if (offsetMax.HasValue) rt.offsetMax = offsetMax.Value;
        rt.anchoredPosition = anchoredPos ?? Vector2.zero;
        if (size.HasValue) rt.sizeDelta = size.Value;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
#endif
