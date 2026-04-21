using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class InwardSphere : MonoBehaviour
{
    void Awake()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        // 1. 反轉法線 (Normals)，讓光照計算針對球體內部
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;

        // 2. 反轉三角形繪製順序 (Triangles)，讓內表面變成正面
        for (int m = 0; m < mesh.subMeshCount; m++)
        {
            int[] triangles = mesh.GetTriangles(m);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i + 0];
                triangles[i + 0] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            mesh.SetTriangles(triangles, m);
        }

        Debug.Log("球體法線已反轉，現在內部可見！");
    }
}