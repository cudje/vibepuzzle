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

    // ���������� ������ �߰��Ͽ� Ȯ���� �� ���� e.g. �÷��̾ ������������ �������(�� ��� playerisGoal�� üũ�����ϰ� playerisHaving �̷� ������ ����� üũ�Ͽ� ���)
    public bool playerisGoal;
    public bool playerisHaving;
    public bool pieceisGoal;

    public Transform[] clearDoor;

    public AudioSource clearSource;
    public AudioSource doorSource;
    public AudioClip success;
    public AudioClip failed;
    public AudioClip door;

    // �÷��̾� ��ġ �� ȸ�� �����
    private Vector3[] originPlayerPositions;
    private Quaternion[] originPlayerRotations;

    // ����(pieces) ��ġ �� ȸ�� �����
    private Vector3[] originPiecePositions;
    private Quaternion[] originPieceRotations;

    void Start()
    {
        // �迭 �ʱ�ȭ
        originPlayerPositions = new Vector3[players.Length];
        originPlayerRotations = new Quaternion[players.Length];
        originPiecePositions = new Vector3[piece.allPieces.Length];
        originPieceRotations = new Quaternion[piece.allPieces.Length];

        // ��ġ �� ȸ�� ����
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
        // playerisGoal�� üũ�� ��� �÷��̾ �� �����ߴ����� Ȯ����. ���ο� ������ �߰��Ǹ� �Ʒ� �׿� �̾ Ȯ���ϸ� ��.
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

        // ���� ���� ��ٸ���
        yield return new WaitUntil(() => GameData.serverAck);

        // ������ ���� ����
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
        StartCoroutine(RotateDoor(clearDoor[0], 10f, 1f));   // 1�� ���� Y�� 10��
        StartCoroutine(RotateDoor(clearDoor[1], -10f, 1f));  // 1�� ���� Y�� -10��
        doorSource.PlayOneShot(door);
    }

    public void ShowClear()
    {
        if (!ClearPopup.activeSelf)
        {
            clearText.text = $"�����մϴ�!\nŬ�����Ͽ����ϴ�.\n" +
                $"������Ʈ ���� : {GameData.promptLen}({GameData.rank_tokens}��, ���� {GameData.rank_tokens_percent:F1}%)\n" +
                $"�̵��ð� : {GameData.moveCount}({GameData.rank_clear_time}��, ���� {GameData.rank_clear_time_percent:F1}%)";
            ClearPopup.SetActive(true);
        }
        OpenDoors();
        // ������� ��.
    }

    // ��� �÷��̾ ��� goal�� ��ġ�ϴ���.
    public bool arrivedPlayer()
    {
        return !players.Where((t, i) =>
        {
            Vector3 a = t.position;
            Vector3 b = goal[i].position;

            // Y�� ������ ��� �Ÿ� ��
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
