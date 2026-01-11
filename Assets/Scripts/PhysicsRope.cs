using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhysicsRope : MonoBehaviour
{
    [Header("연결할 오브젝트 - 드래그앤드롭")]
    [SerializeField] private Rigidbody objectA;
    [SerializeField] private Rigidbody objectB;

    [Header("로프 설정")]
    [SerializeField] private int segmentsPerUnit = 5; // 단위 거리당 세그먼트 수
    [SerializeField] private float ropeWidth = 0.02f;

    [Header("물리 설정")]
    [SerializeField] private float segmentMass = 0.01f;
    [SerializeField] private float drag = 10f;
    [SerializeField] private float objectBKinematicTime = 1f;

    private List<Rigidbody> segments;
    private LineRenderer lineRenderer;
    private float segmentLength;
    private int segmentCount;

    public bool IsObjectBKinematic => objectB != null && objectB.isKinematic;

    void Start()
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogError("Object A와 Object B를 설정해주세요!");
            return;
        }

        segments = new List<Rigidbody>();

        // A와 B 사이 거리 = 세그먼트 길이 합
        float distance = Vector3.Distance(objectA.position, objectB.position);
        segmentCount = Mathf.Max(3, Mathf.RoundToInt(distance * segmentsPerUnit));
        segmentLength = distance / segmentCount;

        CreateRope();
        SetupLineRenderer();

        objectB.isKinematic = true;
        StartCoroutine(StabilizeRope());
    }

    void Update()
    {
        UpdateLineRenderer();
    }

    private void CreateRope()
    {
        Vector3 start = objectA.position;
        Vector3 end = objectB.position;
        Vector3 direction = (end - start).normalized;

        Rigidbody previousRb = objectA;

        for (int i = 0; i < segmentCount; i++)
        {
            // A에서 B 방향으로 직선 배치
            Vector3 pos = start + direction * segmentLength * (i + 0.5f);

            GameObject seg = new GameObject($"Rope_{i}");
            seg.transform.position = pos;
            seg.transform.parent = transform;

            Rigidbody rb = seg.AddComponent<Rigidbody>();
            rb.mass = segmentMass;
            rb.linearDamping = drag;
            rb.angularDamping = drag;
            rb.isKinematic = true;

            CapsuleCollider col = seg.AddComponent<CapsuleCollider>();
            col.radius = ropeWidth;
            col.height = segmentLength;
            col.direction = 1;

            // ConfigurableJoint로 연결
            ConfigurableJoint joint = seg.AddComponent<ConfigurableJoint>();
            joint.connectedBody = previousRb;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = Vector3.zero;

            // 위치 고정
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // 회전은 자유롭게
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            joint.enablePreprocessing = false;

            segments.Add(rb);

            if (i > 0)
            {
                Physics.IgnoreCollision(col, segments[i - 1].GetComponent<Collider>());
            }

            previousRb = rb;
        }

        // 세그먼트끼리 충돌 무시
        for (int i = 0; i < segments.Count; i++)
        {
            for (int j = i + 1; j < segments.Count; j++)
            {
                Physics.IgnoreCollision(
                    segments[i].GetComponent<Collider>(),
                    segments[j].GetComponent<Collider>()
                );
            }
        }

        // objectA, objectB와 충돌 무시
        Collider colA = objectA.GetComponent<Collider>();
        Collider colB = objectB.GetComponent<Collider>();
        foreach (var seg in segments)
        {
            Collider segCol = seg.GetComponent<Collider>();
            if (colA != null) Physics.IgnoreCollision(segCol, colA);
            if (colB != null) Physics.IgnoreCollision(segCol, colB);
        }

        // 마지막 세그먼트와 objectB 연결
        ConfigurableJoint endJoint = segments[segments.Count - 1].gameObject.AddComponent<ConfigurableJoint>();
        endJoint.connectedBody = objectB;
        endJoint.autoConfigureConnectedAnchor = false;
        endJoint.anchor = Vector3.zero;
        endJoint.connectedAnchor = Vector3.zero;
        endJoint.xMotion = ConfigurableJointMotion.Locked;
        endJoint.yMotion = ConfigurableJointMotion.Locked;
        endJoint.zMotion = ConfigurableJointMotion.Locked;
        endJoint.angularXMotion = ConfigurableJointMotion.Free;
        endJoint.angularYMotion = ConfigurableJointMotion.Free;
        endJoint.angularZMotion = ConfigurableJointMotion.Free;
        endJoint.enablePreprocessing = false;
    }

    private IEnumerator StabilizeRope()
    {
        // 안정화 대기
        yield return new WaitForSeconds(0.3f);

        // 세그먼트 순차적으로 활성화
        for (int i = 0; i < segments.Count; i++)
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
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
    }

    private void UpdateLineRenderer()
    {
        if (segments == null || lineRenderer == null) return;

        lineRenderer.positionCount = segments.Count + 2;
        lineRenderer.SetPosition(0, objectA.position);

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                lineRenderer.SetPosition(i + 1, segments[i].position);
            }
        }

        lineRenderer.SetPosition(segments.Count + 1, objectB.position);
    }
}
