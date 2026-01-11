using UnityEngine;
using System.Collections;

public class home : MonoBehaviour
{
    [Header("Spot Database - 드래그앤드롭으로 추가")]
    [SerializeField] private Transform[] spots;

    [Header("오브젝트 설정")]
    [SerializeField] private Transform objectA;
    [SerializeField] private Transform objectB;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;

    [Header("시퀀스 설정")]
    [SerializeField] private float moveToYDuration = 1f;    // n초: objectB의 y로 이동하는 시간
    [SerializeField] private float waitAfterY = 1f;          // m초: 대기 시간
    [SerializeField] private float waitBeforeSpot = 1f;      // spot 이동 전 대기

    private int currentSpotIndex = 0;
    private bool isMoving = false;
    private float objectAInitialY;

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

        // 게임 시작 시 n번째(마지막) spot 위치로 배치
        int lastIndex = spots.Length - 1;
        if (spots[lastIndex] != null)
        {
            transform.position = new Vector3(spots[lastIndex].position.x, transform.position.y, spots[lastIndex].position.z);
        }
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

        // 1. objectA가 objectB의 y좌표로 n초 동안 이동
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
                yield return null;
            }
            objectA.position = targetPos;
        }

        // 2. m초 대기
        yield return new WaitForSeconds(waitAfterY);

        // 3. objectA의 최초 y좌표로 이동 (x, z는 현재 위치 유지)
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

        // 4. 대기 후 spot으로 이동
        yield return new WaitForSeconds(waitBeforeSpot);

        // spot 이동 시작
        currentSpotIndex = 0;
        yield return StartCoroutine(MoveToSpots());

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
