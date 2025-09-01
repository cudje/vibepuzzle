using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Behavior3DManager : MonoBehaviour
{
    public Transform[] player_transform;
    public float moveSpeed = 3.5f;  // �ʴ� �̵� �ӵ�
    public float moveDistance = 5f;  // �� �� �̵� �� �Ÿ�
    public Piece3DManager pieceManager;

    public AudioClip footstepClip;     // �߼Ҹ� 1��
    public AudioClip pickClip;       // ���� ����
    public AudioClip dropClip;       // �������� ����
    public float stepStride = 1.2f;    // �� �� ��� �� �̵��Ÿ�(����) ����

    private Vector3[] target_t;
    public bool isMoving = false;

    private Animator[] animators;
    private GameObject[] heldPiece;
    private AudioSource[] footstepSources;
    private float[] stepMeters;        // �÷��̾ ���� �̵��Ÿ�

    void Start()
    {
        // �� player_transform�� ����� Animator�� ������
        animators = new Animator[player_transform.Length];
        heldPiece = new GameObject[player_transform.Length];

        footstepSources = new AudioSource[player_transform.Length];
        stepMeters = new float[player_transform.Length];

        for (int i = 0; i < player_transform.Length; ++i)
        {
            animators[i] = player_transform[i].GetComponent<Animator>();

            var src = player_transform[i].GetComponent<AudioSource>();
            if (src == null) src = player_transform[i].gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.dopplerLevel = 0f;
            src.spatialBlend = 0f; // 2D�� �鸮�� (���ϸ� 3D�� �ٲ㵵 ��)
            footstepSources[i] = src;

            stepMeters[i] = 0f;
        }
    }

    public bool checkHeld()
    {
        for (int i = 0; i < player_transform.Length; ++i)
        {
            if (heldPiece[i] == null)
                return false;
        }
        return true;
    }

    public void clearHeld()
    {
        for(int i = 0;i < player_transform.Length; i++)
        {
            if (heldPiece[i] != null)
            {
                heldPiece[i].transform.SetParent(player_transform[i].parent);
                heldPiece[i] = null;
                animators[i].SetTrigger("Drop");
                animators[i].SetBool("Having", false);
            }
        }
    }

    public bool[] GetSearch(int dir)
    {
        bool[] search = new bool[player_transform.Length];

        // �� �÷��̾�� �˻�
        for (int i = 0; i < player_transform.Length; i++)
        {
            Vector3 origin = player_transform[i].position;
            Vector3 direction = Vector3.zero;

            // dir�� ���� ���� ����
            switch (dir)
            {
                case 1: // �Ʒ�
                    direction = Vector3.back;   // �Ǵ� -player_transform[i].forward
                    break;
                case 2: // ��
                    direction = Vector3.forward; // �Ǵ� player_transform[i].forward
                    break;
                case 3: // ��
                    direction = Vector3.left;
                    break;
                case 4: // ��
                    direction = Vector3.right;
                    break;
            }

            // Raycast ����
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, 5f))
            {
                // InvisibleWall�� ���� ��� true ó��
                if (hit.collider.CompareTag("InvisibleWall"))
                {
                    search[i] = true;
                }
                else
                {
                    search[i] = false;
                }
            }
            else
            {
                search[i] = false;
            }
        }

        Debug.Log(string.Join(", ", search));
        return search;
    }

    IEnumerator SmoothMove(Vector3[] destination, bool[] conditions)
    {
        isMoving = true;

        while (!player_transform.Select((t, i) =>
        // ������ false�� "�̹� �Ϸ�"�� ���� �� true ��ȯ
        (conditions == null || !conditions[i]) || Vector3.Distance(t.position, destination[i]) < 0.01f
    )
    .All(b => b))
        {
            for (int i = 0; i < player_transform.Length; ++i)
            {
                if (conditions != null && !conditions[i]) continue;

                Vector3 beforePos = player_transform[i].position; // �̵� �� ��ġ

                Vector3 direction = destination[i] - beforePos;

                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    player_transform[i].rotation = Quaternion.Slerp(player_transform[i].rotation, targetRotation, Time.deltaTime * 10f);
                }

                // ���� �̵�
                float step = moveSpeed * Time.deltaTime;
                Vector3 newPosition = Vector3.MoveTowards(beforePos, destination[i], step);
                float moved = (newPosition - beforePos).magnitude;      // �̹� ������ �̵� �Ÿ�
                player_transform[i].position = newPosition;

                // �ִϸ��̼� Speed
                float actualSpeed = moved / Mathf.Max(Time.deltaTime, 1e-6f);
                if (animators[i] != null)
                    animators[i].SetFloat("Speed", actualSpeed);

                // === [�߰�] �߼Ҹ�: �̵��Ÿ� ���� �� stride���� ��� ===
                if (footstepClip != null && footstepSources[i] != null)
                {
                    stepMeters[i] += moved;
                    while (stepMeters[i] >= stepStride)
                    {
                        footstepSources[i].PlayOneShot(footstepClip);
                        stepMeters[i] -= stepStride;
                    }
                }
            }

            yield return null;
        }

        // ���� �� Speed = 0���� ����
        for (int i = 0; i < player_transform.Length; ++i)
        {
            if (animators[i] != null)
                animators[i].SetFloat("Speed", 0f);
        }

        isMoving = false;
    }

    IEnumerator MovePieceToHand(Transform target, Vector3 targetLocalPosition, float duration = 0.5f, float delay = 0.5f)
    {
        yield return new WaitForSeconds(delay);

        Vector3 startLocalPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.localPosition = Vector3.Lerp(startLocalPos, targetLocalPosition, t);
            yield return null;
        }

        target.localPosition = targetLocalPosition;
    }

    IEnumerator DropPieceSmoothly(Transform pieceTransform, Vector3 targetWorldPosition, float duration = 0.5f)
    {
        Vector3 startPos = pieceTransform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            pieceTransform.position = Vector3.Lerp(startPos, targetWorldPosition, t);
            yield return null;
        }

        pieceTransform.position = targetWorldPosition;
    }

    public void Up(bool[] conditions = null)
    {
        if (isMoving) return;
        GameData.moveCount++;
        target_t = player_transform.Select(t => t.position + Vector3.forward * moveDistance).ToArray();
        StartCoroutine(SmoothMove(target_t, conditions));
    }

    public void Down(bool[] conditions = null)
    {
        if (isMoving) return;
        GameData.moveCount++;
        target_t = player_transform.Select(t => t.position + Vector3.back * moveDistance).ToArray();
        StartCoroutine(SmoothMove(target_t, conditions));
    }

    public void Left(bool[] conditions = null)
    {
        if (isMoving) return;
        GameData.moveCount++;
        target_t = player_transform.Select(t => t.position + Vector3.left * moveDistance).ToArray();
        StartCoroutine(SmoothMove(target_t, conditions));
    }

    public void Right(bool[] conditions = null)
    {
        if (isMoving) return;
        GameData.moveCount++;
        target_t = player_transform.Select(t => t.position + Vector3.right * moveDistance).ToArray();
        StartCoroutine(SmoothMove(target_t, conditions));
    }

    public void Pick(bool[] conditions = null)
    {
        if (isMoving) return;
        GameData.moveCount++;
        StartCoroutine(PickRoutine(conditions));
    }

    private IEnumerator PickRoutine(bool[] conditions)
    {
        isMoving = true;
        List<Coroutine> coroutines = new List<Coroutine>();

        for (int i = 0; i < player_transform.Length; ++i)
        {
            if (conditions != null && !conditions[i]) continue;
            if (animators[i] != null && heldPiece[i] == null)
            {
                animators[i].SetTrigger("Pick");

                if (pickClip && footstepSources[i])
                    footstepSources[i].PlayOneShot(pickClip);

                GameObject nearest = pieceManager.GetNearestAvailablePiece(player_transform[i].position, 1.0f);
                if (nearest != null)
                {
                    nearest.transform.SetParent(player_transform[i]);
                    animators[i].SetBool("Having", true);
                    heldPiece[i] = nearest;

                    // �ڷ�ƾ ���� �� ����
                    coroutines.Add(StartCoroutine(
                        MovePieceToHand(nearest.transform, new Vector3(0f, 2.5f, 3.5f), 0.5f, 0.5f)
                    ));
                }
            }
        }

        // ��� �ڷ�ƾ�� ���� ������ ���
        foreach (var c in coroutines)
            yield return c;

        isMoving = false;
    }

    public void Drop(bool[] conditions = null)
    {
        if (isMoving) return;
        GameData.moveCount++;
        StartCoroutine(DropRoutine(conditions));
    }

    private IEnumerator DropRoutine(bool[] conditions)
    {
        isMoving = true;
        List<Coroutine> coroutines = new List<Coroutine>();

        for (int i = 0; i < player_transform.Length; ++i)
        {
            if (conditions != null && !conditions[i]) continue;
            if (animators[i] != null && heldPiece[i] != null)
            {
                GameObject piece = heldPiece[i];
                piece.transform.SetParent(player_transform[i].parent);

                animators[i].SetTrigger("Drop");
                if (dropClip && footstepSources[i])
                    footstepSources[i].PlayOneShot(dropClip);
                animators[i].SetBool("Having", false);
                heldPiece[i] = null;

                // ���ÿ� ���� �� ��ٸ��� ����
                coroutines.Add(StartCoroutine(
                    DropPieceSmoothly(
                        piece.transform,
                        player_transform[i].position + new Vector3(0f, -0.8f, 0f),
                        0.5f
                    )
                ));
            }
        }

        // ��� �ڷ�ƾ�� ���� ������ ���
        foreach (var c in coroutines)
            yield return c;

        isMoving = false;
    }
}