using UnityEngine;

public class move : MonoBehaviour
{
    // 속도 조절 변수 (인스펙터에서 수정 가능)
    public float speed = 5.0f;

    private home homeScript;

    void Start()
    {
        homeScript = GetComponent<home>();
    }

    void Update()
    {
        // home 작동 중이면 이동 안함
        if (homeScript != null && homeScript.IsMoving)
            return;

        // 키보드 입력 받기 (Input Manager의 Horizontal, Vertical 축 사용)
        // A/D 또는 좌우 화살표: -1.0 ~ 1.0
        float h = Input.GetAxis("Horizontal");
        // W/S 또는 상하 화살표: -1.0 ~ 1.0
        float v = Input.GetAxis("Vertical");

        // 이동 방향 벡터 생성 (y축은 0으로 고정하여 수평 이동)
        // .normalized는 대각선 이동 시 속도가 빨라지는 것을 방지합니다.
        Vector3 dir = new Vector3(h, 0, v).normalized;

        // 실제 이동 적용
        // Time.deltaTime을 곱해야 프레임 속도에 관계없이 일정하게 움직입니다.
        // Space.World를 사용하여 회전과 상관없이 절대 좌표(월드 기준)로 움직입니다.
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
    }
}
