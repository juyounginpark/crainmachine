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

        // 키보드 입력 받기
        float h = Input.GetAxis("Horizontal"); // A/D 또는 좌우 화살표
        float v = Input.GetAxis("Vertical");   // W/S 또는 상하 화살표

        if (h == 0 && v == 0) return;

        // 카메라 기준 방향 계산
        Camera cam = Camera.main;
        if (cam == null) return;

        // 카메라의 forward와 right 방향 (Y축 무시하고 수평으로)
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // 카메라 기준 이동 방향
        Vector3 dir = (camForward * v + camRight * h).normalized;

        // 이동 적용
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
    }
}
