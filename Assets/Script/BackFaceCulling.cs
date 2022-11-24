using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BackFaceCulling : MonoBehaviour
{
    [SerializeField, Range(8, 64)] int resolutionGrid = 20;
    [SerializeField] Color planeColor = Color.green;
    [SerializeField] Color gridColor = Color.green;
    [SerializeField] Color normalsColor = Color.yellow;
    [SerializeField, Range(.01f, 10f)] float dottedLineSize = .1f;
    Vector3[] gridPointsFarPlaneArr = new Vector3[0];
    Vector3[] gridPointsNearPlaneArr = new Vector3[0];
    Vector3[] farPlane = new Vector3[0];
    Vector3[] nearPlane = new Vector3[0];
    Vector3[] gridNormals = new Vector3[0];

    public void SetFrustrumPlanes(Vector3[] farPlane, Vector3[] nearPlane, MeshFilter meshRef)
    {
        this.farPlane = farPlane;
        this.nearPlane = nearPlane;
        gridPointsFarPlaneArr = CalculateGrid(this.farPlane);
        gridPointsNearPlaneArr = CalculateGrid(this.nearPlane);
        gridNormals = CalculateNormalsGrid(gridPointsFarPlaneArr, gridPointsNearPlaneArr);
        CheckMeshInGrid(meshRef, gridNormals);
    }

    private Vector3 NormalFromVertex(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 dir = Vector3.Cross(b - a, c - a);
        Vector3 norm = Vector3.Normalize(dir);
        return norm;
    }

    private Vector3[] CalculateGrid(Vector3[] plane)
    {
        List<Vector3> gridPointsLeft = new List<Vector3>();
        List<Vector3> gridPointsRight = new List<Vector3>();
        List<Vector3> gridPoints = new List<Vector3>();
        
        
        for (int i = 0; i <= resolutionGrid; i++)
        {
            gridPointsLeft.Add(Vector3.Lerp(plane[1], plane[0], (float)i / resolutionGrid));
            gridPointsRight.Add(Vector3.Lerp(plane[2], plane[3], (float)i / resolutionGrid));
        }
        

        for(int j = 1; j < gridPointsLeft.Count-1; j++)
        {
            for(int k = 1; k < resolutionGrid; k++)
            {
                gridPoints.Add(Vector3.Lerp(gridPointsLeft[j], gridPointsRight[j], (float)k / resolutionGrid));
            }
        }

        return gridPoints.ToArray();
    }

    private Vector3[] CalculateNormalsGrid(Vector3[] farGrid, Vector3[] nearGrid)
    {
        List<Vector3> gridNormals = new List<Vector3>();
        
        
        for(int i = 0; i < farGrid.Length; i++)
        {
            gridNormals.Add((nearGrid[i] - farGrid[i]).normalized);
        }


        return gridNormals.ToArray();
    }

    private void CheckMeshInGrid(MeshFilter meshRef, Vector3[] gridNormals)
    {
        // Para calcular las normales necesito el indice de grupo de vertices, para saber cuales forman una cara
        for (int i = 0; i < meshRef.mesh.GetIndices(0).Length; i += 3) // Salto de a 3 vertices para mantener el orden
        {
            // Tomo los vertices ordenados proporcionados por unity

            Vector3 v1 = meshRef.mesh.vertices[meshRef.mesh.GetIndices(0)[i]];
            Vector3 v2 = meshRef.mesh.vertices[meshRef.mesh.GetIndices(0)[i + 1]];
            Vector3 v3 = meshRef.mesh.vertices[meshRef.mesh.GetIndices(0)[i + 2]];

            // Paso las coordenadas locales a globales...
            v1 = FromLocalToWolrd(v1, meshRef.transform);
            v2 = FromLocalToWolrd(v2, meshRef.transform);
            v3 = FromLocalToWolrd(v3, meshRef.transform);

            for(int j = 0; j < gridNormals.Length; j++)
            {

                if(Vector3.Dot(NormalFromVertex(v1,v2,v3),gridNormals[j]) > 0)
                {
                    meshRef.gameObject.SetActive(true);
                    return;
                }
            }
            meshRef.gameObject.SetActive(false);
        }
    }

    private Vector3 FromLocalToWolrd(Vector3 point, Transform transformRef)
    {
        Vector3 result = Vector3.zero;

        result = new Vector3(point.x * transformRef.localScale.x, point.y * transformRef.localScale.y, point.z * transformRef.localScale.z);

        result = transformRef.localRotation * result;

        return result + transformRef.position;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = planeColor;
        if(farPlane.Length > 0) Handles.DrawAAConvexPolygon(farPlane);
        if (nearPlane.Length > 0) Handles.DrawAAConvexPolygon(nearPlane);

        // Dibujo la grilla del far plane
        Handles.color = gridColor;
        if (gridPointsFarPlaneArr.Length > 0)
        {
            for(int i = 0; i < gridPointsFarPlaneArr.Length; i++)
            {
                Handles.SphereHandleCap(0, gridPointsFarPlaneArr[i], Quaternion.identity, 0.25f, EventType.Repaint);
                Handles.SphereHandleCap(0, gridPointsNearPlaneArr[i], Quaternion.identity, 0.25f, EventType.Repaint);
                Handles.DrawDottedLine(gridPointsFarPlaneArr[i], gridPointsNearPlaneArr[i], dottedLineSize);
            }
        }

        // Dibujo las normales de far plane
        if (gridNormals.Length > 0)
        {
            for (int i = 0; i < gridNormals.Length; i++)
            {
                Handles.color = normalsColor;
                Handles.ArrowHandleCap(0, gridPointsFarPlaneArr[i], Quaternion.LookRotation(gridNormals[i]), 1, EventType.Repaint);
            }
        }

        // Borro para que no se dibuje nada si no hay objetos dentro del frustum
        farPlane = new Vector3[0];
        nearPlane = new Vector3[0];
        gridPointsFarPlaneArr = new Vector3[0];
        gridPointsNearPlaneArr = new Vector3[0];
        gridNormals = new Vector3[0];
    }
#endif
}
