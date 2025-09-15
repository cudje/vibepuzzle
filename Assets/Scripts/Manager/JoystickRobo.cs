using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickRobo : MonoBehaviour
{
    public VariableJoystick variableJoystick;
    public AudioSource footstepSource;    // 캐릭터에 붙인 AudioSource
    public AudioClip footstepClip;        // 발소리 하나

    private float speed = 7f;
    private float dragRotationSpeed = 0.1f;
    private float stepInterval = 0.33f;     // 걸음 간격(초)

    private float cameraX = 0f;  // X축 상하 회전 (Pitch)
    private float cameraY = 0f;  // Y축 좌우 회전 (Yaw)

    private int cameraOffset = 0;
    private float stepTimer;

    // 회전 전용 입력 상태
    private bool isRotating = false;
    private int rotationTouchId = -1;         // 모바일용 활성 터치 ID
    private Vector2 rotationPrevPos;

    bool IsRightHalf(Vector2 screenPos) => screenPos.x > Screen.width * 0.45f;

    void Start()
    {
        // rotationObject의 초기 회전을 가져와서 cameraX/Y에 저장
        Vector3 angles = SceneHubManager.I.rotationObjectT.eulerAngles;
        cameraX = angles.x;
        cameraY = angles.y;
    }

    void Update()
    {
        HandleDragRotation(); // 마우스/터치 드래그로 회전
        UpdateFootsteps();

        //Debug.DrawRay(SceneHubManager.I.mainCameraT.position, SceneHubManager.I.mainCameraT.forward * 10f, Color.red);

        // Raycast 검사
        Ray ray = new Ray(SceneHubManager.I.mainCameraT.position, SceneHubManager.I.mainCameraT.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, 10f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        if (hits.Length > 0)
        {
            string firstTag = hits[0].collider.tag;
            string secondTag = hits.Length > 1 ? hits[1].collider.tag : "";

            // 조건: 0번이 InvisibleWall이고 1번이 Player면 아무 동작도 안 함
            if (firstTag == "InvisibleWall" && secondTag == "Player")
            {
                return;
            }

            // 줌인: Player가 안 보이면 앞으로 이동
            while (firstTag != "Player" && cameraOffset < 70)
            {
                cameraOffset++;
                SceneHubManager.I.mainCameraT.position += SceneHubManager.I.mainCameraT.forward * 0.1f;

                // 재검사
                ray = new Ray(SceneHubManager.I.mainCameraT.position, SceneHubManager.I.mainCameraT.forward);
                hits = Physics.RaycastAll(ray, 10f);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                firstTag = hits[0].collider.tag;
            }

            // 줌아웃: Player가 보이거나 아무것도 없을 때 뒤로 이동
            while (cameraOffset > 0 && (hits.Length == 0 || hits[0].collider.tag == "Player"))
            {
                cameraOffset--;
                SceneHubManager.I.mainCameraT.position -= SceneHubManager.I.mainCameraT.forward * 0.1f;

                ray = new Ray(SceneHubManager.I.mainCameraT.position, SceneHubManager.I.mainCameraT.forward);
                hits = Physics.RaycastAll(ray, 10f);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }
        }
    }

    void FixedUpdate()
    {
        // 조이스틱 입력 방향
        Vector3 inputDir = Vector3.forward * variableJoystick.Vertical + Vector3.right * variableJoystick.Horizontal;

        Vector3 moveDir = SceneHubManager.I.rotationObjectT.TransformDirection(inputDir);
        moveDir.y = 0f; // 수평 이동만

        // Rigidbody 이동
        Vector3 velocity = moveDir.normalized * speed;
        velocity.y = SceneHubManager.I.charRoboRigidbody.velocity.y; // 중력 유지
        SceneHubManager.I.charRoboRigidbody.velocity = velocity;

        // 캐릭터 회전 (방향이 있을 때만)
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            SceneHubManager.I.roboT.rotation = Quaternion.Slerp(SceneHubManager.I.roboT.rotation, targetRotation, 10f * Time.deltaTime);
        }
        SceneHubManager.I.roboAnimator.SetFloat("Speed", inputDir.magnitude);
    }

    void HandleDragRotation()
    {
        // --- 모바일 터치 처리 ---
        if (Input.touchCount > 0)
        {
            // 이미 회전 중이면: 해당 fingerId만 추적
            if (isRotating && rotationTouchId >= 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.fingerId != rotationTouchId) continue;

                    if (t.phase == TouchPhase.Moved)
                    {
                        Vector2 delta = t.position - rotationPrevPos;
                        rotationPrevPos = t.position;

                        cameraY += delta.x * dragRotationSpeed;
                        cameraX -= delta.y * dragRotationSpeed;
                        cameraX = Mathf.Clamp(cameraX, -35f, 25f);
                        SceneHubManager.I.rotationObjectT.rotation = Quaternion.Euler(cameraX, cameraY, 0f);
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        isRotating = false;
                        rotationTouchId = -1;
                    }
                    // 다른 phase는 무시
                    return; // 회전 터치만 처리하고 종료
                }
            }

            // 회전이 아직 아님: "우측 절반 + UI가 아닌 곳" 에서 Began일 때만 시작
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;

                // 좌/우 절반 스크린 분기
                if (!IsRightHalf(t.position)) continue; // 좌측은 이동만

                // 시작 시점 UI 위면 회전 시작 금지
                // (시작 이후에는 UI 여부를 보지 않음)
                bool beganOverUI = EventSystem.current != null &&
                                   EventSystem.current.IsPointerOverGameObject(t.fingerId);
                if (beganOverUI) continue;

                // 회전 시작
                isRotating = true;
                rotationTouchId = t.fingerId;
                rotationPrevPos = t.position;
                return;
            }

            return;
        }

        // --- 데스크톱 마우스 처리 ---
        // 좌클릭 Down: 우측 절반 + UI가 아닌 곳에서만 회전 시작
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Input.mousePosition;

            if (IsRightHalf(pos))
            {
                bool beganOverUI = EventSystem.current != null &&
                                   EventSystem.current.IsPointerOverGameObject(); // 마우스는 fingerId 없음
                if (!beganOverUI)
                {
                    isRotating = true;
                    rotationPrevPos = pos;
                }
            }
            else
            {
                // 좌측 클릭은 이동용(조이스틱/UI)으로 남겨둠 → 회전 시작 안 함
                isRotating = false;
            }
        }

        // 드래그 중: 시작만 우측/비UI였으면 계속 회전 (UI 위여도 계속)
        if (isRotating && Input.GetMouseButton(0))
        {
            Vector2 pos = Input.mousePosition;
            Vector2 delta = pos - rotationPrevPos;
            rotationPrevPos = pos;

            cameraY += delta.x * dragRotationSpeed;
            cameraX -= delta.y * dragRotationSpeed;
            cameraX = Mathf.Clamp(cameraX, -35f, 25f);
            SceneHubManager.I.rotationObjectT.rotation = Quaternion.Euler(cameraX, cameraY, 0f);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }
    }

    void UpdateFootsteps()
    {
        if (footstepClip == null || footstepSource == null)
            return;

        // 수평 속도
        Vector3 v = SceneHubManager.I.charRoboRigidbody.velocity; v.y = 0f;
        float horizontalSpeed = v.magnitude;

        // 땅 위에서 일정 속도 이상일 때
        if (horizontalSpeed > 0.2f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                footstepSource.PlayOneShot(footstepClip);
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }
}