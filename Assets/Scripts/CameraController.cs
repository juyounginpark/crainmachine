using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("카메라 위치 오브젝트 (IJKL)")]
    [SerializeField] private Transform positionI;
    [SerializeField] private Transform positionJ;
    [SerializeField] private Transform positionK;
    [SerializeField] private Transform positionL;

    [Header("이동 설정")]
    [SerializeField] private float moveDuration = 0.5f; // 이동 시간
    [SerializeField] private bool instant = false; // true면 즉시 이동

    private Camera mainCamera;
    private Coroutine moveCoroutine;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) && positionI != null)
        {
            MoveToTarget(positionI);
        }
        else if (Input.GetKeyDown(KeyCode.J) && positionJ != null)
        {
            MoveToTarget(positionJ);
        }
        else if (Input.GetKeyDown(KeyCode.K) && positionK != null)
        {
            MoveToTarget(positionK);
        }
        else if (Input.GetKeyDown(KeyCode.L) && positionL != null)
        {
            MoveToTarget(positionL);
        }
    }

    private void MoveToTarget(Transform target)
    {
        if (mainCamera == null) return;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        if (instant)
        {
            mainCamera.transform.position = target.position;
            mainCamera.transform.rotation = target.rotation;
        }
        else
        {
            moveCoroutine = StartCoroutine(MoveCamera(target));
        }
    }

    private System.Collections.IEnumerator MoveCamera(Transform target)
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            t = t * t * (3f - 2f * t); // SmoothStep

            mainCamera.transform.position = Vector3.Lerp(startPos, target.position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);

            yield return null;
        }

        mainCamera.transform.position = target.position;
        mainCamera.transform.rotation = target.rotation;
    }
}
