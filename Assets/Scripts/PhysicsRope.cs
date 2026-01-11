using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhysicsRope : MonoBehaviour
{
    [Header("연결할 오브젝트 - 드래그앤드롭")]
    [SerializeField] private Rigidbody objectA;
    [SerializeField] private Rigidbody objectB;

    [Header("로프 설정")]
    [SerializeField] private float segmentUnitLength = 0.1f; // 세그먼트 하나당 길이
    [SerializeField] private float ropeWidth = 0.02f;

    [Header("물리 설정")]
    [SerializeField] private float segmentMass = 0.001f;
    [SerializeField] private float drag = 20f;
    [SerializeField] private float objectBKinematicTime = 1f;

    private List<Rigidbody> segments;
    private LineRenderer lineRenderer;
    private float segmentLength;
    private float currentRopeLength;
    private float initialRopeLength;
    private bool isAdjusting = false;
    private int currentSegmentCount;
    private int initialSegmentCount;

    public bool IsObjectBKinematic => objectB != null && objectB.isKinematic;
    public bool IsAdjusting => isAdjusting;

    void Start()
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogError("Object A와 Object B를 설정해주세요!");
            return;
        }

        // objectA와 objectB 사이 거리로 초기 로프 길이 계산
        initialRopeLength = Vector3.Distance(objectA.position, objectB.position);
        currentRopeLength = initialRopeLength;

        // 세그먼트 개수 = 거리 / 세그먼트 단위 길이
        currentSegmentCount = Mathf.Max(5, Mathf.CeilToInt(initialRopeLength / segmentUnitLength));
        initialSegmentCount = currentSegmentCount;
        segmentLength = initialRopeLength / currentSegmentCount;

        segments = new List<Rigidbody>();

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
        segmentLength = currentRopeLength / currentSegmentCount;

        Rigidbody previousRb = objectA;

        for (int i = 0; i < currentSegmentCount; i++)
        {
            Rigidbody rb = CreateSegment(i, start, previousRb);
            segments.Add(rb);
            previousRb = rb;
        }

        SetupCollisionIgnore();
        ConnectToObjectB();
    }

    private Rigidbody CreateSegment(int index, Vector3 startPos, Rigidbody previousRb)
    {
        Vector3 pos = new Vector3(startPos.x, startPos.y - segmentLength * (index + 1), startPos.z);

        GameObject seg = new GameObject($"Rope_{index}");
        seg.transform.position = pos;
        seg.transform.parent = transform;

        Rigidbody rb = seg.AddComponent<Rigidbody>();
        rb.mass = segmentMass;
        rb.linearDamping = drag;
        rb.angularDamping = drag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.isKinematic = true; // 초반에 kinematic으로 시작

        CapsuleCollider col = seg.AddComponent<CapsuleCollider>();
        col.radius = ropeWidth;
        col.height = segmentLength;
        col.direction = 1;

        HingeJoint joint = seg.AddComponent<HingeJoint>();
        joint.connectedBody = previousRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = new Vector3(0, segmentLength / 2, 0);
        joint.connectedAnchor = index == 0 || previousRb == objectA
            ? Vector3.zero
            : new Vector3(0, -segmentLength / 2, 0);
        joint.axis = new Vector3(1, 0, 0);
        joint.enablePreprocessing = false;

        return rb;
    }

    private void SetupCollisionIgnore()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            for (int j = i + 1; j < segments.Count; j++)
            {
                Collider colI = segments[i].GetComponent<Collider>();
                Collider colJ = segments[j].GetComponent<Collider>();
                if (colI != null && colJ != null)
                {
                    Physics.IgnoreCollision(colI, colJ);
                }
            }
        }

        Collider colA = objectA.GetComponent<Collider>();
        Collider colB = objectB.GetComponent<Collider>();
        for (int i = 0; i < segments.Count; i++)
        {
            Collider segCol = segments[i].GetComponent<Collider>();
            if (segCol != null)
            {
                if (colA != null) Physics.IgnoreCollision(segCol, colA);
                if (colB != null) Physics.IgnoreCollision(segCol, colB);
            }
        }
    }

    private void ConnectToObjectB()
    {
        if (segments.Count == 0) return;

        // 기존 objectB 연결 제거
        Rigidbody lastSegment = segments[segments.Count - 1];
        HingeJoint[] existingJoints = lastSegment.GetComponents<HingeJoint>();
        foreach (var j in existingJoints)
        {
            if (j.connectedBody == objectB)
            {
                Destroy(j);
            }
        }

        HingeJoint endJoint = lastSegment.gameObject.AddComponent<HingeJoint>();
        endJoint.connectedBody = objectB;
        endJoint.autoConfigureConnectedAnchor = false;
        endJoint.anchor = new Vector3(0, -segmentLength / 2, 0);
        endJoint.connectedAnchor = Vector3.zero;
        endJoint.axis = new Vector3(1, 0, 0);
        endJoint.enablePreprocessing = false;
    }

    // 로프 늘리기 (Joint 거리 천천히 늘림 - objectB가 자연스럽게 떨어짐)
    public IEnumerator ExtendRope(float extraLength, float duration)
    {
        isAdjusting = true;

        float startLength = segmentLength;
        float targetSegmentLength = (currentRopeLength + extraLength) / segments.Count;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float newSegmentLength = Mathf.Lerp(startLength, targetSegmentLength, t);
            UpdateSegmentLength(newSegmentLength);
            yield return null;
        }

        UpdateSegmentLength(targetSegmentLength);
        segmentLength = targetSegmentLength;
        currentRopeLength += extraLength;
        isAdjusting = false;
    }

    private void UpdateSegmentLength(float newLength)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            HingeJoint[] joints = segments[i].GetComponents<HingeJoint>();
            foreach (var joint in joints)
            {
                if (joint.connectedBody == objectB)
                {
                    joint.anchor = new Vector3(0, -newLength / 2, 0);
                }
                else if (joint.connectedBody == objectA)
                {
                    joint.anchor = new Vector3(0, newLength / 2, 0);
                }
                else
                {
                    joint.anchor = new Vector3(0, newLength / 2, 0);
                    joint.connectedAnchor = new Vector3(0, -newLength / 2, 0);
                }
            }

            CapsuleCollider col = segments[i].GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.height = newLength;
            }
        }
    }

    private void AddSegmentAtEnd()
    {
        if (segments.Count == 0) return;

        Rigidbody lastSegment = segments[segments.Count - 1];

        // objectB와의 연결 제거
        HingeJoint[] joints = lastSegment.GetComponents<HingeJoint>();
        foreach (var j in joints)
        {
            if (j.connectedBody == objectB)
            {
                Destroy(j);
            }
        }

        // 새 세그먼트 추가
        Vector3 newPos = lastSegment.position + Vector3.down * segmentLength;

        GameObject seg = new GameObject($"Rope_{segments.Count}");
        seg.transform.position = newPos;
        seg.transform.parent = transform;

        Rigidbody rb = seg.AddComponent<Rigidbody>();
        rb.mass = segmentMass;
        rb.linearDamping = drag;
        rb.angularDamping = drag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        CapsuleCollider col = seg.AddComponent<CapsuleCollider>();
        col.radius = ropeWidth;
        col.height = segmentLength;
        col.direction = 1;

        HingeJoint joint = seg.AddComponent<HingeJoint>();
        joint.connectedBody = lastSegment;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = new Vector3(0, segmentLength / 2, 0);
        joint.connectedAnchor = new Vector3(0, -segmentLength / 2, 0);
        joint.axis = new Vector3(1, 0, 0);
        joint.enablePreprocessing = false;

        // 충돌 무시 설정
        Collider colA = objectA.GetComponent<Collider>();
        Collider colB = objectB.GetComponent<Collider>();
        if (colA != null) Physics.IgnoreCollision(col, colA);
        if (colB != null) Physics.IgnoreCollision(col, colB);
        foreach (var s in segments)
        {
            Collider sCol = s.GetComponent<Collider>();
            if (sCol != null) Physics.IgnoreCollision(col, sCol);
        }

        segments.Add(rb);

        // objectB 다시 연결
        ConnectToObjectB();
        UpdateLineRenderer();
    }

    // 로프 줄이기 (Joint 거리 천천히 줄임 - objectB가 자연스럽게 올라옴)
    public IEnumerator RetractRope(float targetLength, float duration)
    {
        isAdjusting = true;

        float startSegmentLength = segmentLength;
        float targetSegmentLength = targetLength / segments.Count;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float newSegmentLength = Mathf.Lerp(startSegmentLength, targetSegmentLength, t);
            UpdateSegmentLength(newSegmentLength);
            yield return null;
        }

        UpdateSegmentLength(targetSegmentLength);
        segmentLength = targetSegmentLength;
        currentRopeLength = targetLength;
        isAdjusting = false;
    }

    private void RemoveSegmentFromEnd()
    {
        if (segments.Count <= initialSegmentCount) return;

        Rigidbody lastSegment = segments[segments.Count - 1];

        // objectB와의 연결 제거
        HingeJoint[] joints = lastSegment.GetComponents<HingeJoint>();
        foreach (var j in joints)
        {
            if (j.connectedBody == objectB)
            {
                Destroy(j);
            }
        }

        segments.RemoveAt(segments.Count - 1);
        Destroy(lastSegment.gameObject);

        // objectB 다시 연결
        ConnectToObjectB();
        UpdateLineRenderer();
    }

    public float GetCurrentRopeLength()
    {
        return currentRopeLength;
    }

    public float GetInitialRopeLength()
    {
        return initialRopeLength;
    }

    public float GetDistanceAtoB()
    {
        if (objectA != null && objectB != null)
        {
            return Vector3.Distance(objectA.position, objectB.position);
        }
        return 0f;
    }

    private IEnumerator StabilizeRope()
    {
        yield return new WaitForSeconds(0.5f);

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
