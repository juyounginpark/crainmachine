using UnityEngine;
using System.Collections;

public class home : MonoBehaviour
{
    [Header("Spot Database - 드래그앤드롭으로 추가")]
    [SerializeField] private Transform[] spots;

    [Header("로프 설정")]
    [SerializeField] private PhysicsRope ropeScript;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;

    [Header("시퀀스 설정")]
    [SerializeField] private float ropeExtendDuration = 1f;   // n초: 로프 늘리는 시간
    [SerializeField] private float waitAfterExtend = 1f;       // 대기 시간
    [SerializeField] private float ropeRetractDuration = 1f;   // n초: 로프 줄이는 시간
    [SerializeField] private float waitBeforeSpot = 1f;        // spot 이동 전 대기

    private int currentSpotIndex = 0;
    private bool isMoving = false;

    // 다른 스크립트에서 home 작동 중인지 확인용
    public bool IsMoving => isMoving;

    void Start()
    {
        if (spots == null || spots.Length == 0)
        {
            Debug.LogWarning("Spot 데이터베이스가 비어있습니다. Inspector에서 Spot을 추가해주세요!");
            return;
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

        if (ropeScript != null)
        {
            // 1. A와 B 사이 거리만큼 로프를 n초 동안 늘림
            float distanceAtoB = ropeScript.GetDistanceAtoB();
            yield return StartCoroutine(ropeScript.ExtendRope(distanceAtoB, ropeExtendDuration));

            // 2. 대기
            yield return new WaitForSeconds(waitAfterExtend);

            // 3. n초 동안 원래 길이로 로프 줄임
            yield return StartCoroutine(ropeScript.RetractRope(ropeScript.GetInitialRopeLength(), ropeRetractDuration));
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
