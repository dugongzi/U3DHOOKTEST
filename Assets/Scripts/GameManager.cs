using System;
using UnityEngine;

/// <summary>
/// 游戏核心状态：当前玩家、分数。
/// 玩法：点金币 +1 分，点"上传分数"提交到服务器。
/// 状态故意存在"服务器"（Flask）里而不是本地——
/// 这正是演示重点：客户端本地什么都没有，一切进度靠服务器保存。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public string PlayerName { get; private set; }
    public int Score { get; private set; }

    // 分数变化时通知 UI 刷新
    public event Action<int> OnScoreChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void StartGame(string playerName)
    {
        PlayerName = playerName;
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }

    public void EndGame()
    {
        PlayerName = null;
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }

    public void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }
}
