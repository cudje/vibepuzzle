using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PromptInterpreter : MonoBehaviour
{
    [TextArea(2, 10)]
    public string test = @"r_move(1)
b_move(1)";

    public Behavior3DManager behavior;
    public Clear3DConditionManager condition;

    // �Ľ�/if ����
    private string[] split_monjang;
    private bool[] executeRobo;

    private void Start()
    {
        executeRobo = new bool[behavior.player_transform.Length];
        clearExe();
        changePrompt(test);
        // funsion_start();
    }

    private void clearExe()
    {
        for (int i = 0; i < executeRobo.Length; i++)
        {
            executeRobo[i] = true;
        }
    }

    private void GetSearch(int dir)
    {
        bool[] search;
        search = behavior.GetSearch(dir);

        for (int i = 0; i < search.Length; i++)
        {
            executeRobo[i] = search[i];
        }
    }

    // �ٹٲ� ������ �̷���� ������ ���ڿ��� ��ȯ
    public void changePrompt(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            split_monjang = Array.Empty<string>();
            return;
        }
        // �� �� ���� ���� + CRLF ����
        split_monjang = prompt.Replace("\r", "").Split('\n');
    }

    // �ܺο��� ȣ���ϴ� ������
    public void fusion_start(string prompt = null)
    {
        if (prompt != null) changePrompt(prompt);

        GameData.setCount();
        StopAllCoroutines(); // ���� ���� ����
        StartCoroutine(RunScriptSequential());
    }

    // ��ũ��Ʈ�� "����������" ����
    private IEnumerator RunScriptSequential()
    {
        foreach (string raw in split_monjang)
        {
            bool clearCon = false;
            if (!raw.StartsWith("    ")) {
                clearCon = true;
            }

            var line = raw?.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("//")) continue; // �ּ�

            // ���ǹ��� ó��.
            if (line.StartsWith("if", StringComparison.OrdinalIgnoreCase))
            {
                ProcessIfStatement(line);
                continue;
            }
            if (line.StartsWith("else", StringComparison.OrdinalIgnoreCase))
            {
                InvertExecuteRobo();
                continue;
            }

            // �Ϲ� ��� �� ���� ������ ������ ������ ��ٸ�
            if (clearCon)
            {
                clearExe();
            }
            yield return ExecuteLine(line);
        }

        // ��� �� ó�� ����, ���� �����ӱ��� ���� ���� Ŭ���� üũ
        yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
        if (condition != null)
            condition.CheckClear();
    }

    private void InvertExecuteRobo()
    {
        for (int i = 0; i < executeRobo.Length; i++)
        {
            executeRobo[i] = !executeRobo[i];
        }
    }

    private void ProcessIfStatement(string line)
    {
        // ���Խ����� search(����) ����
        var match = System.Text.RegularExpressions.Regex.Match(line, @"search\((\d+)\)");
        if (!match.Success) return;

        int dir = int.Parse(match.Groups[1].Value);

        // GetSearch ȣ�� (executeRobo ����)
        GetSearch(dir);

        // == �Ǵ� != ���� ����
        if (line.Contains("=="))
        {
            if (line.Contains("== 0"))
            {
                // true �� false ����
                InvertExecuteRobo();
            }
            // == 1�� ���� �״�� ��
        }
        else if (line.Contains("!="))
        {
            if (line.Contains("!= 1"))
            {
                // ����
                InvertExecuteRobo();
            }
            // != 0�� ���� �״�� ��
        }
    }

    // �� �� ����(���� ������ ��ٸ�)
    private IEnumerator ExecuteLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) yield break;

        // ��ü ���� �켱
        if (line[0] == 'h') // hold/pick
        {
            yield return ActWithWait(() => behavior?.Pick(executeRobo));
            //Debug.Log($"{line} ___ hold");
            yield break;
        }
        if (line[0] == 'p') // put/drop
        {
            yield return ActWithWait(() => behavior?.Drop(executeRobo));
            //Debug.Log($"{line} ___ put");
            yield break;
        }

        // �̵� �迭
        if (line.StartsWith("f_"))
        {
            if (TryParseArg(line, out int a))
                yield return MoveWithWait(() => behavior?.Down(executeRobo), a, line);
            else
                Debug.LogWarning($"[move_control] �߸��� �μ�: {line}");
        }
        else if (line.StartsWith("b_"))
        {
            if (TryParseArg(line, out int a))
                yield return MoveWithWait(() => behavior?.Up(executeRobo), a, line);
            else
                Debug.LogWarning($"[move_control] �߸��� �μ�: {line}");
        }
        else if (line.StartsWith("l_"))
        {
            if (TryParseArg(line, out int a))
                yield return MoveWithWait(() => behavior?.Left(executeRobo), a, line);
            else
                Debug.LogWarning($"[move_control] �߸��� �μ�: {line}");
        }
        else if (line.StartsWith("r_"))
        {
            if (TryParseArg(line, out int a))
                yield return MoveWithWait(() => behavior?.Right(executeRobo), a, line);
            else
                Debug.LogWarning($"[move_control] �߸��� �μ�: {line}");
        }
        // ��Ÿ ��� ������ ���⼭ Ȯ��
    }

    // ����(��/��� ��) 1ȸ: ���� ���� ���� ��� �� �� ƽ �纸 �� ���� �� ���� ���
    private IEnumerator ActWithWait(Action act)
    {
        yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
        yield return null; // ������ ��� �Ѱ� isMoving �ݿ� ��ȸ ����
        act?.Invoke();
        yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
    }

    // �̵� aȸ: �� ȸ ���� ���� ��� �� �� ƽ �纸 �� �̵� �� ���� ���
    private IEnumerator MoveWithWait(Action moveAction, int repeat, string dbgLine)
    {
        if (repeat < 1) repeat = 1;

        for (int i = 0; i < repeat; i++)
        {
            yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
            yield return null; // ������ ��� �ѱ����� ���� ȣ�� ���̽� ����
            moveAction?.Invoke();
            yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
        }
        //Debug.Log($"{dbgLine} ___ done x{repeat}");
    }

    // ��ȣ ���� ������ �����ϰ� �Ľ��ϴ� ����
    private bool TryParseArg(string line, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(line)) return false;

        int open = line.IndexOf('(');
        int close = line.IndexOf(')', open + 1);
        if (open < 0 || close <= open + 1) return false;

        string inner = line.Substring(open + 1, close - open - 1).Trim();
        return int.TryParse(inner, out value);
    }

}