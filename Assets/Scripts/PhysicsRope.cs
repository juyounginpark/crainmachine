using UnityEngine;
using System.Collections;

public class PhysicsRope : MonoBehaviour
{
    [Header("연결할 오브젝트 - 드래그앤드롭")]
    [SerializeField] private Rigidbody objectA;
    [SerializeField] private Rigidbody objectB;

    [Header("로프 설정")]
    [SerializeField] private int segmentCount = 60;
    [SerializeField] private float ropeWidth = 0.02f;
    [SerializeField] private float ropeLength = 5f; // 로프 길이 직접 설정

    [Header("물리 설정")]
    [SerializeField] private float segmentMass = 0.001f;
    [SerializeField] private float drag = 20f;
    [SerializeField] private float objectBKinematicTime = 1f;

    private Rigidbody[] segments;
    private LineRenderer lineRenderer;
    private float segmentLength;

    // objectB kinematic 상태 확인용
    public bool IsObjectBKinematic => objectB != null && objectB.isKinematic;

    void Start()
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogError("Object A와 Object B를 설정해주세요!");
            return;
        }

        CreateRope();
        SetupLineRenderer();

        // objectB를 kinematic으로 설정
        objectB.isKinematic = true;

        StartCoroutine(StabilizeRope());
    }

    void Update()
    {
        UpdateLineRenderer();
    }

    private void CreateRope()
    {
        segments = new Rigidbody[segmentCount];

        Vector3 start = objectA.position;

        // 로프 길이를 세그먼트로 나눔
        segmentLength = ropeLength / segmentCount;

        Rigidbody previousRb = objectA;

        for (int i = 0; i < segmentCount; i++)
        {
            // objectA에서 y좌표 아래로 수직으로 생성
            Vector3 pos = new Vector3(start.x, start.y - segmentLength * (i + 1), start.z);

            GameObject seg = new GameObject($"Rope_{i}");
            seg.transform.position = pos;
            seg.transform.parent = transform;

            // Rigidbody (초반에 kinematic으로 시작)
            Rigidbody rb = seg.AddComponent<Rigidbody>();
            rb.mass = segmentMass;
            rb.linearDamping = drag;
            rb.angularDamping = drag;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.isKinematic = true;

            // Collider
            CapsuleCollider col = seg.AddComponent<CapsuleCollider>();
            col.radius = ropeWidth;
            col.height = segmentLength;
            col.direction = 1; // Y축 방향 (수직)

            // HingeJoint로 연결
            HingeJoint joint = seg.AddComponent<HingeJoint>();
            joint.connectedBody = previousRb;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = new Vector3(0, segmentLength / 2, 0);
            joint.connectedAnchor = i == 0
                ? Vector3.zero
                : new Vector3(0, -segmentLength / 2, 0);
            joint.axis = new Vector3(1, 0, 0);
            joint.enablePreprocessing = false;

            segments[i] = rb;

            if (i > 0)
            {
                Physics.IgnoreCollision(col, segments[i - 1].GetComponent<Collider>());
            }

            previousRb = rb;
        }

        // 모든 세그먼트끼리 충돌 무시
        for (int i = 0; i < segmentCount; i++)
        {
            for (int j = i + 1; j < segmentCount; j++)
            {
                Physics.IgnoreCollision(
                    segments[i].GetComponent<Collider>(),
                    segments[j].GetComponent<Collider>()
                );
            }
        }

        // objectA, objectB와 세그먼트 충돌 무시
        Collider colA = objectA.GetComponent<Collider>();
        Collider colB = objectB.GetComponent<Collider>();
        for (int i = 0; i < segmentCount; i++)
        {
            Collider segCol = segments[i].GetComponent<Collider>();
            if (colA != null) Physics.IgnoreCollision(segCol, colA);
            if (colB != null) Physics.IgnoreCollision(segCol, colB);
        }

        // 마지막 세그먼트에서 objectB로 연결
        HingeJoint endJoint = segments[segmentCount - 1].gameObject.AddComponent<HingeJoint>();
        endJoint.connectedBody = objectB;
        endJoint.autoConfigureConnectedAnchor = false;
        endJoint.anchor = new Vector3(0, -segmentLength / 2, 0);
        endJoint.connectedAnchor = Vector3.zero;
        endJoint.axis = new Vector3(1, 0, 0);
        endJoint.enablePreprocessing = false;
    }

    private IEnumerator StabilizeRope()
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null)
            {
                segments[i].linearVelocity = Vector3.zero;
                segments[i].angularVelocity = Vector3.zero;
                segments[i].isKinematic = false;
            }
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(objectBKinematicTime);
        objectB.linearVelocity = Vector3.zero;
        objectB.angularVelocity = Vector3.zero;
        objectB.isKinematic = false;
    }

    private void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = segmentCount + 2;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
    }

    private void UpdateLineRenderer()
    {
        if (segments == null || lineRenderer == null) return;

        lineRenderer.SetPosition(0, objectA.position);

        for (int i = 0; i < segments.Length; i++)
        {
            lineRenderer.SetPosition(i + 1, segments[i].position);
        }

        lineRenderer.SetPosition(segmentCount + 1, objectB.position);
    }
}
