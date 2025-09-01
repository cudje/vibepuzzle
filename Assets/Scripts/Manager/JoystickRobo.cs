using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickRobo : MonoBehaviour
{
    public float speed = 25f;
    public VariableJoystick variableJoystick;
    public Rigidbody rb;
    public Transform player;
    public Transform rotationObject;
    public float dragRotationSpeed = 0.2f;
    public Animator animator;
    public Transform cameraTransform;
    public AudioSource footstepSource;    // 캐릭터에 붙인 AudioSource
    public AudioClip footstepClip;        // 발소리 하나
    public float stepInterval = 0.33f;     // 걸음 간격(초)

    private Vector2 previousDragPos;
    private bool isDragging = false;
    private bool dragStartedOnRightSide = false;
    private float cameraX = 0f;  // X축 상하 회전 (Pitch)
    private float cameraY = 0f;  // Y축 좌우 회전 (Yaw)

    private int cameraOffset = 0;
    private float stepTimer;

    void Start()
    {
        // rotationObject의 초기 회전을 가져와서 cameraX/Y에 저장
        Vector3 angles = rotationObject.eulerAngles;
        cameraX = angles.x;
        cameraY = angles.y;
    }

    void Update()
    {
        HandleDragRotation(); // 마우스/터치 드래그로 회전

        if (cameraTransform != null)
        {
            Debug.DrawRay(cameraTransform.position, cameraTransform.forward * 10f, Color.red);

            // Raycast 검사
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
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
                    cameraTransform.position += cameraTransform.forward * 0.1f;

                    // 재검사
                    ray = new Ray(cameraTransform.position, cameraTransform.forward);
                    hits = Physics.RaycastAll(ray, 10f);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    firstTag = hits[0].collider.tag;
                }

                // 줌아웃: Player가 보이거나 아무것도 없을 때 뒤로 이동
                while (cameraOffset > 0 && (hits.Length == 0 || hits[0].collider.tag == "Player"))
                {
                    cameraOffset--;
                    cameraTransform.position -= cameraTransform.forward * 0.1f;

                    ray = new Ray(cameraTransform.position, cameraTransform.forward);
                    hits = Physics.RaycastAll(ray, 10f);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                }
            }
        }

        UpdateFootsteps();
    }

    void FixedUpdate()
    {
        // 조이스틱 입력 방향
        Vector3 inputDir = Vector3.forward * variableJoystick.Vertical + Vector3.right * variableJoystick.Horizontal;

        Vector3 moveDir = rotationObject.TransformDirection(inputDir);
        moveDir.y = 0f; // 수평 이동만

        // Rigidbody 이동
        Vector3 velocity = moveDir.normalized * speed;
        velocity.y = rb.velocity.y; // 중력 유지
        rb.velocity = velocity;

        // 캐릭터 회전 (방향이 있을 때만)
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            player.rotation = Quaternion.Slerp(player.rotation, targetRotation, 10f * Time.deltaTime);
        }
        animator.SetFloat("Speed", inputDir.magnitude);
    }

    void HandleDragRotation()
    {
        Vector2 currentPos = Vector2.zero;
        bool draggingNow = false;

        // 터치 입력
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            currentPos = touch.position;

            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                previousDragPos = currentPos;

                dragStartedOnRightSide = (currentPos.x > Screen.width * 0.5f);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }

            if (touch.phase == TouchPhase.Moved)
                draggingNow = true;
        }
        // 마우스 입력
        else if (Input.GetMouseButton(0))
        {
            currentPos = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                previousDragPos = currentPos;

                dragStartedOnRightSide = (currentPos.x > Screen.width * 0.5f);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            draggingNow = true;
        }

        if (isDragging && draggingNow && dragStartedOnRightSide)
        {
            Vector2 delta = currentPos - previousDragPos;
            previousDragPos = currentPos;

            cameraY += delta.x * dragRotationSpeed;
            cameraX -= delta.y * dragRotationSpeed;

            cameraX = Mathf.Clamp(cameraX, -35f, 25f);

            rotationObject.rotation = Quaternion.Euler(cameraX, cameraY, 0f);
        }
    }

    void UpdateFootsteps()
    {
        if (footstepClip == null || footstepSource == null)
            return;

        // 수평 속도
        Vector3 v = rb.velocity; v.y = 0f;
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