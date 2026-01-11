using UnityEngine;
using System.Collections;

public class home : MonoBehaviour
{
    [Header("Spot Database - 드래그앤드롭으로 추가")]
    [SerializeField] private Transform[] spots;

    [Header("오브젝트 설정")]
    [SerializeField] private Transform objectA;
    [SerializeField] private Transform objectB;

    [Header("회전할 오브젝트 데이터베이스")]
    [SerializeField] private Transform[] rotatingObjects;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;

    [Header("시퀀스 설정")]
    [SerializeField] private float moveToYDuration = 1f;    // n초: objectB의 y로 이동하는 시간
    [SerializeField] private float waitAfterY = 1f;          // m초: 대기 시간
    [SerializeField] private float waitBeforeSpot = 1f;      // spot 이동 전 대기
    [SerializeField] private float initialHomeDuration = 2f; // 시작 시 home 이동 시간
    [SerializeField] private float homeRotationDuration = 1f; // k초: home 도착 후 회전 복귀 시간

    private int currentSpotIndex = 0;
    private bool isMoving = false;
    private float objectAInitialY;
    private Rigidbody objectBRb;

    // 다른 스크립트에서 home 작동 중인지 확인용
    public bool IsMoving => isMoving;

    void Start()
    {
        if (spots == null || spots.Length == 0)
        {
            Debug.LogWarning("Spot 데이터베이스가 비어있습니다. Inspector에서 Spot을 추가해주세요!");
            return;
        }

        // objectA 최초 Y좌표 저장
        if (objectA != null)
        {
            objectAInitialY = objectA.position.y;
        }

        // objectB Rigidbody 가져오기
        if (objectB != null)
        {
            objectBRb = objectB.GetComponent<Rigidbody>();
        }

        // 게임 시작 시 home(마지막 spot)으로 천천히 이동
        StartCoroutine(InitialHomeMove());
    }

    private IEnumerator InitialHomeMove()
    {
        isMoving = true;

        // objectB를 kinematic으로 설정 (이동 중 물리 비활성화)
        bool wasKinematic = false;
        if (objectBRb != null)
        {
            wasKinematic = objectBRb.isKinematic;
            objectBRb.isKinematic = true;
            objectBRb.linearVelocity = Vector3.zero;
            objectBRb.angularVelocity = Vector3.zero;
        }

        int lastIndex = spots.Length - 1;
        if (spots[lastIndex] != null)
        {
            Vector3 startPos = transform.position;
            Vector3 startPosB = objectB != null ? objectB.position : Vector3.zero;
            Vector3 targetPos = new Vector3(spots[lastIndex].position.x, transform.position.y, spots[lastIndex].position.z);
            float elapsed = 0f;

            while (elapsed < initialHomeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / initialHomeDuration;

                Vector3 prevPos = transform.position;
                transform.position = Vector3.Lerp(startPos, targetPos, t);

                // objectB도 같이 이동
                if (objectB != null)
                {
                    Vector3 delta = transform.position - prevPos;
                    objectB.position += delta;
                }

                yield return null;
            }
            transform.position = targetPos;
        }

        // objectB 물리 다시 활성화
        if (objectBRb != null)
        {
            objectBRb.linearVelocity = Vector3.zero;
            objectBRb.angularVelocity = Vector3.zero;
            objectBRb.isKinematic = wasKinematic;
        }

        isMoving = false;
    }

    void Update()
    {
        if (spots == null || spots.Length == 0)
            return;

        // 스페이스바를 누르면 시퀀스 시작
        if (Input.GetKeyDown(KeyCode.Space) && !isMoving)
        {
            StartCoroutine(HomeSequence());
        }
    }

    private IEnumerator HomeSequence()
    {
        isMoving = true;

        // 각 오브젝트의 시작 회전값 저장
        float[] startRotations = new float[rotatingObjects != null ? rotatingObjects.Length : 0];
        Vector3[] fullRotations = new Vector3[startRotations.Length];
        for (int i = 0; i < startRotations.Length; i++)
        {
            if (rotatingObjects[i] != null)
            {
                fullRotations[i] = rotatingObjects[i].eulerAngles;
                startRotations[i] = fullRotations[i].x;
            }
        }

        // 1. objectA가 objectB의 y좌표로 n초 동안 이동 + 오브젝트들 X회전 +15
        if (objectA != null && objectB != null)
        {
            Vector3 startPos = objectA.position;
            Vector3 targetPos = new Vector3(objectA.position.x, objectB.position.y, objectA.position.z);
            float elapsed = 0f;

            while (elapsed < moveToYDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveToYDuration;
                objectA.position = Vector3.Lerp(startPos, targetPos, t);

                // 모든 회전 오브젝트 X회전 +15로
                for (int i = 0; i < startRotations.Length; i++)
                {
                    if (rotatingObjects[i] != null)
                    {
                        float newX = Mathf.Lerp(startRotations[i], 15f, t);
                        rotatingObjects[i].eulerAngles = new Vector3(newX, fullRotations[i].y, fullRotations[i].z);
                    }
                }

                yield return null;
            }
            objectA.position = targetPos;

            for (int i = 0; i < startRotations.Length; i++)
            {
                if (rotatingObjects[i] != null)
                    rotatingObjects[i].eulerAngles = new Vector3(15f, fullRotations[i].y, fullRotations[i].z);
            }
        }

        // 2. m초 대기 + 오브젝트들 X회전 -35로
        if (rotatingObjects != null && rotatingObjects.Length > 0)
        {
            float elapsed = 0f;

            while (elapsed < waitAfterY)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / waitAfterY;

                for (int i = 0; i < rotatingObjects.Length; i++)
                {
                    if (rotatingObjects[i] != null)
                    {
                        float newX = Mathf.Lerp(15f, -35f, t);
                        rotatingObjects[i].eulerAngles = new Vector3(newX, fullRotations[i].y, fullRotations[i].z);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < rotatingObjects.Length; i++)
            {
                if (rotatingObjects[i] != null)
                    rotatingObjects[i].eulerAngles = new Vector3(-35f, fullRotations[i].y, fullRotations[i].z);
            }
        }

        // 3. objectA의 최초 y좌표로 이동 (X회전 -35 유지)
        if (objectA != null)
        {
            Vector3 startPos = objectA.position;
            Vector3 targetPos = new Vector3(objectA.position.x, objectAInitialY, objectA.position.z);
            float elapsed = 0f;

            while (elapsed < moveToYDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveToYDuration;
                objectA.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            objectA.position = targetPos;
        }

        // 4. 대기 후 spot으로 이동 (X회전 -35 유지)
        yield return new WaitForSeconds(waitBeforeSpot);

        // spot 이동 시작
        currentSpotIndex = 0;
        yield return StartCoroutine(MoveToSpots());

        // 5. home 도착 후 X회전 0으로 k초 동안 복귀
        if (rotatingObjects != null && rotatingObjects.Length > 0)
        {
            float elapsed = 0f;

            while (elapsed < homeRotationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / homeRotationDuration;

                for (int i = 0; i < rotatingObjects.Length; i++)
                {
                    if (rotatingObjects[i] != null)
                    {
                        float newX = Mathf.Lerp(-35f, 0f, t);
                        rotatingObjects[i].eulerAngles = new Vector3(newX, fullRotations[i].y, fullRotations[i].z);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < rotatingObjects.Length; i++)
            {
                if (rotatingObjects[i] != null)
                    rotatingObjects[i].eulerAngles = new Vector3(0f, fullRotations[i].y, fullRotations[i].z);
            }
        }

        isMoving = false;
    }

    private IEnumerator MoveToSpots()
    {
        while (currentSpotIndex < spots.Length)
        {
            Transform targetSpot = spots[currentSpotIndex];
            if (targetSpot == null)
            {
                currentSpotIndex++;
                continue;
            }

            Vector3 targetPos = new Vector3(targetSpot.position.x, transform.position.y, targetSpot.position.z);

            while (Vector3.Distance(transform.position, targetPos) > arrivalThreshold)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            currentSpotIndex++;
        }

        Debug.Log("모든 Spot 순회 완료!");
    }
}
