using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Clear3DConditionManager : MonoBehaviour
{
    public GameObject ClearPopup;
    public TMP_Text clearText;
    public TMP_InputField recentStage;

    public Transform[] players;
    public Transform[] goal;
    public Transform[] pieceGoal;

    public Behavior3DManager behavior;
    public Piece3DManager piece;
    public AI_WebSocketClient wsclient;
    public SaveRecord saveRecord;

    // 스테이지별 조건을 추가하여 확장할 수 있음 e.g. 플레이어가 데이터조각을 들었는지(이 경우 playerisGoal은 체크해제하고 playerisHaving 이런 변수를 만들어 체크하여 사용)
    public bool playerisGoal;
    public bool playerisHaving;
    public bool pieceisGoal;

    public Transform[] clearDoor;

    public AudioSource clearSource;
    public AudioSource doorSource;
    public AudioClip success;
    public AudioClip failed;
    public AudioClip door;

    // 플레이어 위치 및 회전 저장용
    private Vector3[] originPlayerPositions;
    private Quaternion[] originPlayerRotations;

    // 조각(pieces) 위치 및 회전 저장용
    private Vector3[] originPiecePositions;
    private Quaternion[] originPieceRotations;

    void Start()
    {
        // 배열 초기화
        originPlayerPositions = new Vector3[players.Length];
        originPlayerRotations = new Quaternion[players.Length];
        originPiecePositions = new Vector3[piece.allPieces.Length];
        originPieceRotations = new Quaternion[piece.allPieces.Length];

        // 위치 및 회전 저장
        for (int i = 0; i < players.Length; i++)
        {
            originPlayerPositions[i] = players[i].position;
            originPlayerRotations[i] = players[i].rotation;
        }

        for (int i = 0; i < piece.allPieces.Length; i++)
        {
            originPiecePositions[i] = piece.allPieces[i].transform.position;
            originPieceRotations[i] = piece.allPieces[i].transform.rotation;
        }
    }


    public void CheckClear()
    {
        // playerisGoal이 체크된 경우 플레이어가 골에 도착했는지를 확인함. 새로운 조건이 추가되면 아래 항에 이어서 확장하면 됨.
        if (playerisGoal && !arrivedPlayer())
        {
            ResetObject();
            clearSource.PlayOneShot(failed);
            wsclient.isBusy = false;
            return;
        }
            
        if (playerisHaving && !heldPieces())
        {
            ResetObject();
            clearSource.PlayOneShot(failed);
            wsclient.isBusy = false;
            return;
        }

        if (pieceisGoal && !arrivePiece())
        {
            ResetObject();
            clearSource.PlayOneShot(failed);
            wsclient.isBusy = false;
            return;
        }

        StartCoroutine(RunClearFlow());
    }

    IEnumerator RunClearFlow()
    {
        GameData.setRecord();
        saveRecord.SendRunLog();

        // 서버 응답 기다리기
        yield return new WaitUntil(() => GameData.serverAck);

        // 응답이 오면 실행
        GameData.setStageClear(recentStage.text);
        clearSource.PlayOneShot(success);
        wsclient.isBusy = false;
        ShowClear();
    }

    public void ResetObject()
    {
        behavior.clearHeld();
        for (int i = 0; i < players.Length; i++)
        {
            players[i].position = originPlayerPositions[i];
            players[i].rotation = originPlayerRotations[i];
        }

        for (int i = 0; i < piece.allPieces.Length; i++)
        {
            piece.allPieces[i].transform.position = originPiecePositions[i];
            piece.allPieces[i].transform.rotation = originPieceRotations[i];
        }
    }

    IEnumerator RotateDoor(Transform door, float targetY, float duration)
    {
        Quaternion startRot = door.rotation;
        Quaternion endRot = Quaternion.Euler(
            door.eulerAngles.x,
            targetY,
            door.eulerAngles.z
        );

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            door.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
    }

    public void OpenDoors()
    {
        StartCoroutine(RotateDoor(clearDoor[0], 10f, 1f));   // 1초 동안 Y축 10도
        StartCoroutine(RotateDoor(clearDoor[1], -10f, 1f));  // 1초 동안 Y축 -10도
        doorSource.PlayOneShot(door);
    }

    public void ShowClear()
    {
        if (!ClearPopup.activeSelf)
        {
            clearText.text = $"축하합니다!\n클리어하였습니다.\n" +
                $"프롬프트 길이 : {GameData.promptLen}({GameData.rank_tokens}위, 상위 {GameData.rank_tokens_percent:F1}%)\n" +
                $"이동시간 : {GameData.moveCount}({GameData.rank_clear_time}위, 상위 {GameData.rank_clear_time_percent:F1}%)";
            ClearPopup.SetActive(true);
        }
        OpenDoors();
        // 문열어야 함.
    }

    // 모든 플레이어가 모든 goal과 일치하는지.
    public bool arrivedPlayer()
    {
        return !players.Where((t, i) =>
        {
            Vector3 a = t.position;
            Vector3 b = goal[i].position;

            // Y축 제외한 평면 거리 비교
            a.y = 0;
            b.y = 0;

            return Vector3.Distance(a, b) > 0.1f;
        }).Any();
    }

    public bool heldPieces()
    {
        return behavior.checkHeld();
    }

    public bool arrivePiece()
    {
        float arrivalRadius = 1.0f;

        foreach (Transform goal in pieceGoal)
        {
            bool hasPiece = piece.allPieces.Any(p =>
            {
                if (p == null) return false;

                Vector3 piecePos = p.transform.position;
                Vector3 goalPos = goal.position;

                piecePos.y = 0;
                goalPos.y = 0;

                return Vector3.Distance(piecePos, goalPos) <= arrivalRadius;
            });

            if (!hasPiece)
                return false;
        }

        return true;
    }
}
