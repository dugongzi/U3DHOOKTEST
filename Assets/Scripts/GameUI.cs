using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全部 UI 逻辑：登录界面 / 游戏界面 / 排行榜。
/// 所有面板在 Awake 里自动查找，挂在一个空物体上即可运行，
/// 场景搭建只负责按约定名字创建 UI 元素。
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject leaderboardPanel;

    [Header("登录界面")]
    [SerializeField] private InputField usernameInput;
    [SerializeField] private InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Text loginMessage;

    [Header("游戏界面")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text welcomeText;
    [SerializeField] private Button coinButton;      // 点金币加分
    [SerializeField] private Button submitButton;    // 上传分数
    [SerializeField] private Button showLeaderboardButton;
    [SerializeField] private Button logoutButton;

    [Header("排行榜界面")]
    [SerializeField] private Text leaderboardText;
    [SerializeField] private Button backToGameButton;

    private void Awake()
    {
        // 按名字自动绑定，避免手工拖引用漏掉
        loginPanel = GameObject.Find("LoginPanel");
        gamePanel = GameObject.Find("GamePanel");
        leaderboardPanel = GameObject.Find("LeaderboardPanel");

        usernameInput = loginPanel.transform.Find("UsernameInput").GetComponent<InputField>();
        passwordInput = loginPanel.transform.Find("PasswordInput").GetComponent<InputField>();
        loginButton = loginPanel.transform.Find("LoginButton").GetComponent<Button>();
        registerButton = loginPanel.transform.Find("RegisterButton").GetComponent<Button>();
        loginMessage = loginPanel.transform.Find("MessageText").GetComponent<Text>();

        scoreText = gamePanel.transform.Find("ScoreText").GetComponent<Text>();
        welcomeText = gamePanel.transform.Find("WelcomeText").GetComponent<Text>();
        coinButton = gamePanel.transform.Find("CoinButton").GetComponent<Button>();
        submitButton = gamePanel.transform.Find("SubmitButton").GetComponent<Button>();
        showLeaderboardButton = gamePanel.transform.Find("LeaderboardButton").GetComponent<Button>();
        logoutButton = gamePanel.transform.Find("LogoutButton").GetComponent<Button>();

        leaderboardText = leaderboardPanel.transform.Find("LeaderboardText").GetComponent<Text>();
        backToGameButton = leaderboardPanel.transform.Find("BackButton").GetComponent<Button>();

        // 绑定按钮事件
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        coinButton.onClick.AddListener(OnCoinClicked);
        submitButton.onClick.AddListener(OnSubmitClicked);
        showLeaderboardButton.onClick.AddListener(OnShowLeaderboard);
        logoutButton.onClick.AddListener(OnLogoutClicked);
        backToGameButton.onClick.AddListener(() => ShowPanel(gamePanel));

        ShowPanel(loginPanel);

        // 已有 token 的话直接提示可以自动登录（Flask 版可以后续加 /auto_login 接口）
        if (!string.IsNullOrEmpty(ApiClient.Instance.Token))
        {
            loginMessage.text = "检测到保存的登录凭证（演示用，仍需重新登录）";
        }
    }

    // ---------- 登录 ----------

    private void OnLoginClicked()
    {
        StartCoroutine(ApiClient.Instance.Login(usernameInput.text, passwordInput.text, (ok, msg) =>
        {
            loginMessage.text = msg;
            if (ok)
            {
                GameManager.Instance.StartGame(usernameInput.text);
                welcomeText.text = "欢迎回来，" + usernameInput.text;
                ShowPanel(gamePanel);
            }
        }));
    }

    private void OnRegisterClicked()
    {
        StartCoroutine(ApiClient.Instance.Register(usernameInput.text, passwordInput.text, (ok, msg) =>
        {
            loginMessage.text = ok ? "注册成功，请登录" : msg;
        }));
    }

    // ---------- 游戏 ----------

    private void OnCoinClicked()
    {
        GameManager.Instance.AddScore(1);
    }

    private void OnSubmitClicked()
    {
        StartCoroutine(ApiClient.Instance.SubmitScore(GameManager.Instance.Score, (ok, msg) =>
        {
            submitButton.interactable = false;
            submitButton.GetComponentInChildren<Text>().text = ok ? "已上传!" : "上传失败";
            StartCoroutine(ResetSubmitButton(1.5f));
        }));
    }

    private IEnumerator ResetSubmitButton(float delay)
    {
        yield return new WaitForSeconds(delay);
        submitButton.interactable = true;
        submitButton.GetComponentInChildren<Text>().text = "上传分数到服务器";
    }

    // ---------- 排行榜 ----------

    private void OnShowLeaderboard()
    {
        ShowPanel(leaderboardPanel);
        leaderboardText.text = "加载中...";
        StartCoroutine(ApiClient.Instance.GetLeaderboard((ok, text) =>
        {
            leaderboardText.text = ok ? text : text;
        }));
    }

    private void OnLogoutClicked()
    {
        ApiClient.Instance.ClearToken();
        GameManager.Instance.EndGame();
        ShowPanel(loginPanel);
    }

    // ---------- 工具 ----------

    private void ShowPanel(GameObject panel)
    {
        loginPanel.SetActive(panel == loginPanel);
        gamePanel.SetActive(panel == gamePanel);
        leaderboardPanel.SetActive(panel == leaderboardPanel);
    }

    private void Update()
    {
        scoreText.text = GameManager.Instance.Score.ToString();
    }
}
