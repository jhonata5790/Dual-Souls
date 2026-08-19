using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TriangleGenerator : MonoBehaviour
{
    void Start()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Triangulo Procedural";

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 1, 0),     // topo
            new Vector3(-1, -1, 0),   // canto inferior esquerdo
            new Vector3(1, -1, 0)     // canto inferior direito
        };

        int[] triangles = new int[]
        {
            0, 1, 2
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (renderer.material == null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
        }
    }
}