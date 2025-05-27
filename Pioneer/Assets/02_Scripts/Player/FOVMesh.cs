using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FOVMesh : MonoBehaviour
{
    [Header("시야 각도 (Degree)")]
    public float viewAngle = 90f;

    [Header("시야 거리")]
    public float viewRadius = 10f;

    [Header("Ray 개수 (클수록 부드러움)")]
    public int rayCount = 50;

    [Header("장애물 레이어")]
    public LayerMask obstacleMask;

    private Mesh mesh;

    private Vector3 origin;

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        origin = Vector3.zero;
    }

    private void LateUpdate()
    {
        origin = transform.position;
        DrawFieldOfView();
    }

    private void DrawFieldOfView()
    {
        float angleStep = viewAngle / rayCount;
        float startAngle = transform.eulerAngles.y - viewAngle / 2f;

        List<Vector3> viewPoints = new List<Vector3>();

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 dir = DirFromAngle(angle);

            RaycastHit hit;
            Vector3 point;

            if (Physics.Raycast(origin, dir, out hit, viewRadius, obstacleMask))
            {
                // 장애물에 닿았을 때
                point = hit.point;
            }
            else
            {
                // 장애물이 없으면 최대 거리까지
                point = origin + dir * viewRadius;
            }

            viewPoints.Add(point);
        }

        // 메쉬 생성
        int vertexCount = viewPoints.Count + 1; // +1은 origin
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = transform.InverseTransformPoint(origin); // 로컬 좌표계 기준 원점

        for (int i = 0; i < viewPoints.Count; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
        }

        int triIndex = 0;
        for (int i = 0; i < vertexCount - 2; i++)
        {
            triangles[triIndex] = 0;
            triangles[triIndex + 1] = i + 1;
            triangles[triIndex + 2] = i + 2;
            triIndex += 3;
        }

        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private Vector3 DirFromAngle(float angleInDegrees)
    {
        // Y축 기준 회전 벡터 (XZ 평면)
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
