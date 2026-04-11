using UnityEngine;

public static class GameModeState
{
    private const string InfiniteUnlockedKey = "InfiniteModeUnlocked";

    // 현재 플레이 중인 게임이 무한 모드인지 아닌지 여부
    public static bool IsInfiniteMode { get; private set; }

    // 최초 엔딩 이후 무한 모드 해금 여부
    public static bool IsInfiniteModeUnlocked
    {
        get => PlayerPrefs.GetInt(InfiniteUnlockedKey, 0) == 1;
    }

    public static void UnlockInfiniteMode()
    {
        PlayerPrefs.SetInt(InfiniteUnlockedKey, 1);
        PlayerPrefs.Save();
    }

    public static void StartNormalMode()
    {
        IsInfiniteMode = false;
    }

    public static void StartInfiniteMode()
    {
        IsInfiniteMode = true;
        UnlockInfiniteMode();
    }

    public static void ResetInfiniteModeUnlock()
    {
        IsInfiniteMode = false;
        PlayerPrefs.DeleteKey("InfiniteModeUnlocked");
        PlayerPrefs.Save();
    }
}