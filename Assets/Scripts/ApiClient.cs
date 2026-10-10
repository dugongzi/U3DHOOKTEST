using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 与 Flask 服务器的 HTTP 通信客户端。
/// 服务器地址、接口定义都在这里，方便视频讲解。
/// </summary>
public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [SerializeField] private string baseUrl = "http://127.0.0.1:5000";

    // 登录成功后服务器返回的 token，之后每个请求都带上
    public string Token { get; private set; }

    private void Awake()
    {
        Instance = this;
        Token = PlayerPrefs.GetString("saved_token", "");
    }

    public void ClearToken()
    {
        Token = "";
        PlayerPrefs.DeleteKey("saved_token");
    }

    private void SaveToken(string token)
    {
        Token = token;
        PlayerPrefs.SetString("saved_token", token);
    }

    // ---------- 接口定义 ----------

    /// <summary>注册账号 POST /register  {username, password}</summary>
    public IEnumerator Register(string username, string password, Action<bool, string> onResult)
    {
        var body = new JsonBody { username = username, password = password };
        yield return PostJson("/register", body, json =>
        {
            var resp = JsonUtility.FromJson<SimpleResponse>(json);
            onResult?.Invoke(resp.success, resp.message);
        });
    }

    /// <summary>登录 POST /login  {username, password} → {token}</summary>
    public IEnumerator Login(string username, string password, Action<bool, string> onResult)
    {
        var body = new JsonBody { username = username, password = password };
        yield return PostJson("/login", body, json =>
        {
            var resp = JsonUtility.FromJson<LoginResponse>(json);
            if (resp.success)
            {
                SaveToken(resp.token);
                onResult?.Invoke(true, "登录成功");
            }
            else
            {
                onResult?.Invoke(false, resp.message);
            }
        });
    }

    /// <summary>上传分数 POST /submit_score  {score}（带 token）</summary>
    public IEnumerator SubmitScore(int score, Action<bool, string> onResult)
    {
        var body = new ScoreBody { score = score };
        yield return PostJsonWithToken("/submit_score", body, json =>
        {
            var resp = JsonUtility.FromJson<SimpleResponse>(json);
            onResult?.Invoke(resp.success, resp.message);
        });
    }

    /// <summary>拉取排行榜 GET /leaderboard → [{username, score}]</summary>
    public IEnumerator GetLeaderboard(Action<bool, string> onResult)
    {
        using var request = new UnityWebRequest(baseUrl + "/leaderboard", "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + Token);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onResult?.Invoke(false, "网络错误: " + request.error);
            yield break;
        }

        var resp = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
        if (resp.success)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < resp.entries.Length; i++)
            {
                sb.AppendLine($"{i + 1}. {resp.entries[i].username} —— {resp.entries[i].score} 分");
            }
            onResult?.Invoke(true, sb.Length > 0 ? sb.ToString() : "暂无数据，快来抢第一吧");
        }
        else
        {
            onResult?.Invoke(false, resp.message);
        }
    }

    // ---------- 底层工具 ----------

    private IEnumerator PostJson(string path, object body, Action<string> onSuccess)
    {
        yield return SendJson(path, body, useToken: false, onSuccess);
    }

    private IEnumerator PostJsonWithToken(string path, object body, Action<string> onSuccess)
    {
        yield return SendJson(path, body, useToken: true, onSuccess);
    }

    private IEnumerator SendJson(string path, object body, bool useToken, Action<string> onSuccess)
    {
        string json = JsonUtility.ToJson(body);
        using var request = new UnityWebRequest(baseUrl + path, "POST");
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (useToken)
            request.SetRequestHeader("Authorization", "Bearer " + Token);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            // 服务器返回的错误信息也放出来，方便调试和录视频
            string serverMsg = request.downloadHandler != null ? request.downloadHandler.text : "";
            Debug.LogError($"请求 {path} 失败: {request.error}\n{serverMsg}");
            var failResp = JsonUtility.FromJson<SimpleResponse>(serverMsg);
            onSuccess?.Invoke(serverMsg);
        }
        else
        {
            onSuccess?.Invoke(request.downloadHandler.text);
        }
    }

    // ---------- JSON 数据结构 ----------

    [Serializable] private class JsonBody { public string username; public string password; }
    [Serializable] private class ScoreBody { public int score; }

    [Serializable] private class SimpleResponse { public bool success; public string message; }
    [Serializable] private class LoginResponse { public bool success; public string message; public string token; }
    [Serializable] public class LeaderboardEntry { public string username; public int score; }
    [Serializable] private class LeaderboardResponse { public bool success; public string message; public LeaderboardEntry[] entries; }
}
