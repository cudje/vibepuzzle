using System.Collections.Generic;
using TMPro;

public static class GameData
{
    public static string userText = "User";

    public static int moveCount = 0;
    public static int promptLen = 0;

    public static bool[] stageClear;

    public static float rank_clear_time_percent = 0.0f;
    public static int rank_clear_time = 0;
    public static float rank_tokens_percent = 0.0f;
    public static int rank_tokens = 0;

    public static bool serverAck = true;

    private static Dictionary<string, int> stageIndexMap = new Dictionary<string, int>
    {
        {"A1", 1}, {"A2", 2}, {"A3", 3}, {"A4", 4},
    };

    public static void SetUserText(string text)
    {
        userText = text;
    }

    public static void setClear()
    {
        for (int i = 0; i < stageClear.Length; i++)
        {
            stageClear[i] = false;
        }
    }

    public static void setCount()
    {
        moveCount = 0;
    }

    public static void setStageClear(string stage)
    {
        if (stageIndexMap.TryGetValue(stage, out int index))
        {
            stageClear[index] = true;
        }
    }

    public static void setRecord()
    {
        rank_clear_time_percent = 0.0f;
        rank_clear_time = 0;
        rank_tokens_percent = 0.0f;
        rank_tokens = 0;
    }

}