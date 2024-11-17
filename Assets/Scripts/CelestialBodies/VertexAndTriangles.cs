using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VertexAndTriangles
{
    public static int[] GetTriangles(int vertexPerRow, int resolution)
    {
        int vertexCount = vertexPerRow * vertexPerRow;

        int spaceNeededToStoreATriangle = 6;
        int trianglesNumber = (2 * ((int)Mathf.Pow(2, 2 * resolution))) * 3;
        int[] triangles = new int[trianglesNumber * spaceNeededToStoreATriangle];

        int triangleCounter = 0;

        for (int i = 0; triangleCounter < (vertexCount - vertexPerRow); i += 6)
        {
            // Skip edge vertices
            if (i != 0 && ((i / 6) + 1) % vertexPerRow == 0)
            {
                triangleCounter++;
                continue;
            }

            triangles[i] = triangleCounter;
            triangles[i + 1] = triangleCounter + vertexPerRow + 1;
            triangles[i + 2] = triangleCounter + vertexPerRow;

            triangles[i + 3] = triangleCounter;
            triangles[i + 4] = triangleCounter + 1;
            triangles[i + 5] = triangleCounter + vertexPerRow + 1;

            triangleCounter++;
        }

        /*
         * Example of triangles with resolution = 1
           triangles[0] = 0;
           triangles[1] = 4;
           triangles[2] = 3;

           triangles[3] = 0;
           triangles[4] = 1;
           triangles[5] = 4;

           triangles[6] = 1;
           triangles[7] = 5;
           triangles[8] = 4;

           triangles[9] = 1;
           triangles[10] = 2;
           triangles[11] = 5;

           triangles[12] = 3;
           triangles[13] = 7;
           triangles[14] = 6;

           triangles[15] = 3;
           triangles[16] = 4;
           triangles[17] = 7;

           triangles[18] = 4;
           triangles[19] = 8;
           triangles[20] = 7;

           triangles[21] = 4;
           triangles[22] = 5;
           triangles[23] = 8;
        */

        return triangles;
    }

    public static Vector3[] GetVertices(int vertexPerRow, int numberOfVertices, int size)
    {
        Vector3[] vertices = new Vector3[numberOfVertices];

        float vertexOffset = (float)size / (vertexPerRow - 1);

        float xStartOffset = -((float)size / 2);
        float zStartOffset = ((float)size / 2);

        for (int i = 0; i < vertexPerRow; i++)
        {
            for (int j = 0; j < vertexPerRow; j++)
            {
                float xPositiion = xStartOffset + (vertexOffset * j);
                vertices[j + i * vertexPerRow] = new Vector3(xPositiion, 0f, zStartOffset);
            }

            zStartOffset -= vertexOffset;
        }

        /*
         * Example of vertices when resolution is equals to 1 and the center is in the origin
           vertices[0] = new Vector3(-.5f, 0f, -.5f);
           vertices[1] = new Vector3(0f, 0f, -.5f);
           vertices[2] = new Vector3(.5f, 0f, -.5f);
           
           vertices[3] = new Vector3(-.5f, 0f, 0f);
           vertices[4] = new Vector3(0f, 0f, 0f);
           vertices[5] = new Vector3(.5f, 0f, 0f);
           
           vertices[6] = new Vector3(-.5f, 0f, .5f);
           vertices[7] = new Vector3(0f, 0f, .5f);
           vertices[8] = new Vector3(.5f, 0f, .5f);
        */

        return vertices;
    }

    public static Vector3[] GetCubeVertices(int vertexPerRow, int numberOfVertices, int size, Vector3 upDirection)
    {
        Vector3[] vertices = new Vector3[numberOfVertices];

        Vector3 localUp = upDirection;
        // The axis tha are going to modify the localUp vector
        Vector3 Dx = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 Dy = Vector3.Cross(localUp, Dx);

        for (int i = 0; i < vertexPerRow; i++)
        {
            for (int j = 0; j < vertexPerRow; j++)
            {
                float yVertexPercent = (float)i / (vertexPerRow - 1);
                float xVertexPercent = (float)j / (vertexPerRow - 1);
                vertices[j + i * vertexPerRow] = (localUp + (xVertexPercent - .5f) * 2 * Dx + (yVertexPercent - .5f) * 2 * Dy) * size;
            }
        }

        return vertices;
    }


    public static Vector3[] GetSphereVertices(int vertexPerRow, int numberOfVertices, int size, Vector3 upDirection)
    {
        Vector3[] vertices = new Vector3[numberOfVertices];

        Vector3 localUp = upDirection;
        Vector3 Dx = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 Dy = Vector3.Cross(localUp, Dx);

        for (int i = 0; i < vertexPerRow; i++)
        {
            for (int j = 0; j < vertexPerRow; j++)
            {
                float yVertexPercent = (float)i / (vertexPerRow - 1);
                float xVertexPercent = (float)j / (vertexPerRow - 1);
                vertices[j + i * vertexPerRow] = localUp + (xVertexPercent - .5f) * 2 * Dx + (yVertexPercent - .5f) * 2 * Dy;
                vertices[j + i * vertexPerRow] = vertices[j + i * vertexPerRow].normalized * size;
            }
        }

        return vertices;
    }

    public static int GetVertexPerRow(int resolution)
    {
        int vertexCount = 2;

        for (int i = 0; i < resolution; i++)
        {
            vertexCount += (int)Mathf.Pow(2, i);
        }

        return vertexCount;
    }
}