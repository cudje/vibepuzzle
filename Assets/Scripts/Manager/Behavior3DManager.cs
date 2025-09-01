using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Behavior3DManager : MonoBehaviour
{
    public Transform[] player_transform;
    public float moveSpeed = 3.5f;  // 초당 이동 속도
    public float moveDistance = 5f;  // 한 번 이동 시 거리
    public Piece3DManager pieceManager;

    public AudioClip footstepClip;     // 발소리 1개
    public AudioClip pickClip;       // 집기 사운드
    public AudioClip dropClip;       // 내려놓기 사운드
    public float stepStride = 1.2f;    // 한 발 디딜 때 이동거리(미터) 기준

    private Vector3[] target_t;
    public bool isMoving = false;

    private Animator[] animators;
    private GameObject[] heldPiece;
    private AudioSource[] footstepSources;
    private float[] stepMeters;        // 플레이어별 누적 이동거리

    void Start()
    {
        // 각 player_transform에 연결된 Animator를 가져옴
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
            src.spatialBlend = 0f; // 2D로 들리게 (원하면 3D로 바꿔도 됨)
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

        // 각 플레이어별로 검사
        for (int i = 0; i < player_transform.Length; i++)
        {
            Vector3 origin = player_transform[i].position;
            Vector3 direction = Vector3.zero;

            // dir에 따라 방향 설정
            switch (dir)
            {
                case 1: // 아래
                    direction = Vector3.back;   // 또는 -player_transform[i].forward
                    break;
                case 2: // 위
                    direction = Vector3.forward; // 또는 player_transform[i].forward
                    break;
                case 3: // 좌
                    direction = Vector3.left;
                    break;
                case 4: // 우
                    direction = Vector3.right;
                    break;
            }

            // Raycast 수행
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, 5f))
            {
                // InvisibleWall을 만난 경우 true 처리
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
        // 조건이 false면 "이미 완료"로 간주 → true 반환
        (conditions == null || !conditions[i]) || Vector3.Distance(t.position, destination[i]) < 0.01f
    )
    .All(b => b))
        {
            for (int i = 0; i < player_transform.Length; ++i)
            {
                if (conditions != null && !conditions[i]) continue;

                Vector3 beforePos = player_transform[i].position; // 이동 전 위치

                Vector3 direction = destination[i] - beforePos;

                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    player_transform[i].rotation = Quaternion.Slerp(player_transform[i].rotation, targetRotation, Time.deltaTime * 10f);
                }

                // 실제 이동
                float step = moveSpeed * Time.deltaTime;
                Vector3 newPosition = Vector3.MoveTowards(beforePos, destination[i], step);
                float moved = (newPosition - beforePos).magnitude;      // 이번 프레임 이동 거리
                player_transform[i].position = newPosition;

                // 애니메이션 Speed
                float actualSpeed = moved / Mathf.Max(Time.deltaTime, 1e-6f);
                if (animators[i] != null)
                    animators[i].SetFloat("Speed", actualSpeed);

                // === [추가] 발소리: 이동거리 누적 후 stride마다 재생 ===
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

        // 도착 후 Speed = 0으로 설정
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

                    // 코루틴 실행 후 저장
                    coroutines.Add(StartCoroutine(
                        MovePieceToHand(nearest.transform, new Vector3(0f, 2.5f, 3.5f), 0.5f, 0.5f)
                    ));
                }
            }
        }

        // 모든 코루틴이 끝날 때까지 대기
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

                // 동시에 실행 → 기다리지 않음
                coroutines.Add(StartCoroutine(
                    DropPieceSmoothly(
                        piece.transform,
                        player_transform[i].position + new Vector3(0f, -0.8f, 0f),
                        0.5f
                    )
                ));
            }
        }

        // 모든 코루틴이 끝날 때까지 대기
        foreach (var c in coroutines)
            yield return c;

        isMoving = false;
    }
}