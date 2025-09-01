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

    // 파싱/if 상태
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

    // 줄바꿈 등으로 이루어진 문장을 문자열로 변환
    public void changePrompt(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            split_monjang = Array.Empty<string>();
            return;
        }
        // 줄 끝 공백 제거 + CRLF 대응
        split_monjang = prompt.Replace("\r", "").Split('\n');
    }

    // 외부에서 호출하는 진입점
    public void fusion_start(string prompt = null)
    {
        if (prompt != null) changePrompt(prompt);

        GameData.setCount();
        StopAllCoroutines(); // 이전 실행 중지
        StartCoroutine(RunScriptSequential());
    }

    // 스크립트를 "순차적으로" 실행
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
            if (line.StartsWith("//")) continue; // 주석

            // 조건문들 처리.
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

            // 일반 명령 한 줄을 끝까지 실행할 때까지 기다림
            if (clearCon)
            {
                clearExe();
            }
            yield return ExecuteLine(line);
        }

        // 모든 줄 처리 이후, 남은 움직임까지 끝난 다음 클리어 체크
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
        // 정규식으로 search(숫자) 추출
        var match = System.Text.RegularExpressions.Regex.Match(line, @"search\((\d+)\)");
        if (!match.Success) return;

        int dir = int.Parse(match.Groups[1].Value);

        // GetSearch 호출 (executeRobo 갱신)
        GetSearch(dir);

        // == 또는 != 조건 추출
        if (line.Contains("=="))
        {
            if (line.Contains("== 0"))
            {
                // true ↔ false 반전
                InvertExecuteRobo();
            }
            // == 1일 때는 그대로 둠
        }
        else if (line.Contains("!="))
        {
            if (line.Contains("!= 1"))
            {
                // 반전
                InvertExecuteRobo();
            }
            // != 0일 때는 그대로 둠
        }
    }

    // 한 줄 실행(끝날 때까지 기다림)
    private IEnumerator ExecuteLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) yield break;

        // 객체 조작 우선
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

        // 이동 계열
        if (line.StartsWith("f_"))
        {
            if (TryParseArg(line, out int a))
                yield return MoveWithWait(() => behavior?.Down(executeRobo), a, line);
            else
                Debug.LogWarning($"[move_control] 잘못된 인수: {line}");
        }
        else if (line.StartsWith("b_"))
        {
            if (TryParseArg(line, out int a))
                yield return MoveWithWait(() => behavior?.Up(executeRobo), a, line);
            else
                Debug.LogWarning($"[move_control] 잘못된 인수: {line}");
        }
        else if (line.StartsWith("l_"))
        {
            if (TryParseArg(line, out int a))
                yield return MoveWithWait(() => behavior?.Left(executeRobo), a, line);
            else
                Debug.LogWarning($"[move_control] 잘못된 인수: {line}");
        }
        else if (line.StartsWith("r_"))
        {
            if (TryParseArg(line, out int a))
                yield return MoveWithWait(() => behavior?.Right(executeRobo), a, line);
            else
                Debug.LogWarning($"[move_control] 잘못된 인수: {line}");
        }
        // 기타 명령 포맷을 여기서 확장
    }

    // 동작(픽/드랍 등) 1회: 이전 동작 종료 대기 → 한 틱 양보 → 실행 → 종료 대기
    private IEnumerator ActWithWait(Action act)
    {
        yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
        yield return null; // 프레임 경계 넘겨 isMoving 반영 기회 제공
        act?.Invoke();
        yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
    }

    // 이동 a회: 매 회 이전 종료 대기 → 한 틱 양보 → 이동 → 종료 대기
    private IEnumerator MoveWithWait(Action moveAction, int repeat, string dbgLine)
    {
        if (repeat < 1) repeat = 1;

        for (int i = 0; i < repeat; i++)
        {
            yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
            yield return null; // 프레임 경계 넘김으로 동시 호출 레이스 방지
            moveAction?.Invoke();
            yield return new WaitUntil(() => behavior != null && !behavior.isMoving);
        }
        //Debug.Log($"{dbgLine} ___ done x{repeat}");
    }

    // 괄호 안의 정수를 안전하게 파싱하는 헬퍼
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