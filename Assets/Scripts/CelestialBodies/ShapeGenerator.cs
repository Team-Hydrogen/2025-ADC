using JetBrains.Annotations;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class ShapeGenerator
{
    public static Mesh GenerateSphereMesh(MeshRenderer renderer, MeshFilter filter, int resolution, int size, Vector3 direction)
    {
        renderer.sharedMaterial = new Material(Shader.Find("Standard"));

        Mesh planeMesh = UpdateSphereMesh(filter, resolution, size, direction);

        return planeMesh;
    }

    public static Mesh UpdateSphereMesh(MeshFilter filter, int resolution, int size, Vector3 direction)
    {
        Mesh planeMesh = new Mesh();

        int vertexPerRow = VertexAndTriangles.GetVertexPerRow(resolution);
        int numberOfVertices = vertexPerRow * vertexPerRow;
        Vector3[] vertices = VertexAndTriangles.GetSphereVertices(vertexPerRow, numberOfVertices, size, direction);

        int[] triangles = VertexAndTriangles.GetTriangles(vertexPerRow, resolution);

        planeMesh.vertices = vertices;
        planeMesh.triangles = triangles;
        planeMesh.uv = GenerateUV(vertices, vertexPerRow, vertexPerRow);

        planeMesh.RecalculateBounds();

        filter.mesh = planeMesh;

        return planeMesh;
    }

    public static Vector2[] GenerateUV(Vector3[] vertices, int height, int width)
    {
        Vector2[] uv = new Vector2[vertices.Length];

        for (int y = 0; y < height-1; y++)
        {
            for (int x = 0; x < width-1; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                try
                {
                    uv[x + y * (width + 1)] = new Vector2(u, v);
                } catch
                {
                    Debug.Log(x + y * (width + 1) + " " + vertices.Length);
                }
            }
        }

        return uv;
    }
}
