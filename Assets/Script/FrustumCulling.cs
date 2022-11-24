using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FrustumCulling : MonoBehaviour
{
    const uint maxVertexPerPlane = 4;
    [SerializeField] Color frustumLineColor = Color.green;
    [SerializeField] Color frustumPlaneColor = Color.green;

    Vector3[] frustumCornerFar = new Vector3[maxVertexPerPlane];
    Vector3[] frustumCornerNear = new Vector3[maxVertexPerPlane];
    Vector3[] frustumCornerLeft = new Vector3[maxVertexPerPlane];
    Vector3[] frustumCornerRight = new Vector3[maxVertexPerPlane];
    Vector3[] frustumCornerUp = new Vector3[maxVertexPerPlane];
    Vector3[] frustumCornerDown = new Vector3[maxVertexPerPlane];

    Camera cam = default;

    [SerializeField] MeshFilter[] filters = default;

    BackFaceCulling backFaceScript = default;

    private void Start()
    {
        cam = Camera.main;
        backFaceScript = GetComponent<BackFaceCulling>();
        filters = FindObjectsOfType<MeshFilter>();
        CalculateFrustum();

    }

    private void Update()
    {
        CalculateFrustum();
        FrustumUpdate();
    }

    /// <summary>
    /// Calculo el Frustum
    /// </summary>
    private void CalculateFrustum()
    {
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornerFar);
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornerNear);

        // Se calcula los vertices del plano izquierdo del frustum
        frustumCornerLeft[0] = frustumCornerNear[1];
        frustumCornerLeft[1] = frustumCornerFar[1];
        frustumCornerLeft[2] = frustumCornerFar[0];
        frustumCornerLeft[3] = frustumCornerNear[0];

        // Se calcula los vertices del plano derecho del frustum
        frustumCornerRight[0] = frustumCornerNear[3];
        frustumCornerRight[1] = frustumCornerFar[3];
        frustumCornerRight[2] = frustumCornerFar[2];
        frustumCornerRight[3] = frustumCornerNear[2];

        // Se calcula los vertices del plano superior del frustum
        frustumCornerUp[0] = frustumCornerNear[2];
        frustumCornerUp[1] = frustumCornerFar[2];
        frustumCornerUp[2] = frustumCornerFar[1];
        frustumCornerUp[3] = frustumCornerNear[1];

        // Se calcula los vertices del plano inferior del frustum
        frustumCornerDown[0] = frustumCornerNear[0];
        frustumCornerDown[1] = frustumCornerFar[0];
        frustumCornerDown[2] = frustumCornerFar[3];
        frustumCornerDown[3] = frustumCornerNear[3];

        for (int i = 0; i < maxVertexPerPlane; i++)
        {
            frustumCornerFar[i] = FromLocalToWolrd(frustumCornerFar[i], cam.transform);
            frustumCornerNear[i] = FromLocalToWolrd(frustumCornerNear[i], cam.transform);
            frustumCornerLeft[i] = FromLocalToWolrd(frustumCornerLeft[i], cam.transform);
            frustumCornerRight[i] = FromLocalToWolrd(frustumCornerRight[i], cam.transform);
            frustumCornerUp[i] = FromLocalToWolrd(frustumCornerUp[i], cam.transform);
            frustumCornerDown[i] = FromLocalToWolrd(frustumCornerDown[i], cam.transform);
        }
    }

    /// <summary>
    /// Transformo las posiciones locales en globales
    /// </summary>
    /// <param name="point"></param>
    /// <param name="transformRef"></param>
    /// <returns></returns>
    private Vector3 FromLocalToWolrd(Vector3 point, Transform transformRef)
    {
        Vector3 result = Vector3.zero;

        result = new Vector3(point.x * transformRef.localScale.x, point.y * transformRef.localScale.y, point.z * transformRef.localScale.z);

        result = transformRef.localRotation * result;

        return result + transformRef.position;
    }

    /// <summary>
    /// Uso todos los datos de las mesh que estan dentro de la escena para hacer los respectivos calculos (prendo o apago el game object)
    /// </summary>
    private void FrustumUpdate()
    {
        foreach (var item in filters)
        {
            // Para calcular las normales necesito el indice de grupo de vertices, para saber cuales forman una cara
            for (int i = 0; i < item.mesh.GetIndices(0).Length; i += 3) // Salto de a 3 vertices para mantener el orden
            {
                // Tomo los vertices ordenados proporcionados por unity

                Vector3 v1 = item.mesh.vertices[item.mesh.GetIndices(0)[i]];
                Vector3 v2 = item.mesh.vertices[item.mesh.GetIndices(0)[i + 1]];
                Vector3 v3 = item.mesh.vertices[item.mesh.GetIndices(0)[i + 2]];

                // Paso las coordenadas locales a globales...
                v1 = FromLocalToWolrd(v1, item.transform);
                v2 = FromLocalToWolrd(v2, item.transform);
                v3 = FromLocalToWolrd(v3, item.transform);

                // Aca se deberia pasar la data a otro script que realice el Back Face Culling
                if (IsVertexInFrustum(v1) || IsVertexInFrustum(v2) || IsVertexInFrustum(v3))
                {
                    // Le paso el mesh filter (objeto a dibujar) al la clase encargada del Back Face Culling
                    //backFaceScript.SetFrustrumPlanes(frustumCornerFar, frustumCornerNear, item);
                    if (!item.gameObject.activeSelf)
                    {
                        item.gameObject.SetActive(true);

                    }
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Verifico si un vertice esta contenido en el frustum
    /// </summary>
    /// <param name="vertex"></param>
    /// <returns></returns>
    private bool IsVertexInFrustum(Vector3 vertex)
    {
        return IsVertexInNormalPlane(vertex, CenterOfPlane(frustumCornerFar), NormalFromPlane(frustumCornerFar)) &&
            IsVertexInNormalPlane(vertex, CenterOfPlane(frustumCornerNear), -NormalFromPlane(frustumCornerNear)) &&
            IsVertexInNormalPlane(vertex, CenterOfPlane(frustumCornerLeft), NormalFromPlane(frustumCornerLeft)) &&
            IsVertexInNormalPlane(vertex, CenterOfPlane(frustumCornerRight), NormalFromPlane(frustumCornerRight)) &&
            IsVertexInNormalPlane(vertex, CenterOfPlane(frustumCornerUp), NormalFromPlane(frustumCornerUp)) &&
            IsVertexInNormalPlane(vertex, CenterOfPlane(frustumCornerDown), NormalFromPlane(frustumCornerDown));
    }

    /// <summary>
    /// Calculo la normal de un plano y la direccion desde el centro del plano al vertice, para saber si esta en la misma direccion de la normal del plano.
    /// </summary>
    /// <param name="vertex"></param>
    /// <param name="centerPlane"></param>
    /// <param name="normalPlane"></param>
    /// <returns></returns>
    private bool IsVertexInNormalPlane(Vector3 vertex, Vector3 centerPlane, Vector3 normalPlane)
    {
        return Vector3.Dot((vertex - centerPlane).normalized, normalPlane) > 0; // Si es mayor a cero esta por delante de la cara del plano.
    }

    /// <summary>
    /// Calculo la normal de un plano en base a sus vertices
    /// </summary>
    /// <param name="plane"></param>
    /// <returns></returns>
    Vector3 NormalFromPlane(Vector3[] plane)
    {
        // Vector3 dir = Vector3.Cross(b - a, c - a); solo tomamos 3 vertices, descartamos el cuarto
        Vector3 dir = Vector3.Cross(plane[1] - plane[0], plane[2] - plane[0]);
        Vector3 norm = Vector3.Normalize(dir);
        return norm;
    }

    /// <summary>
    /// Calculo el centro del plano en base a sus vertices
    /// </summary>
    /// <param name="plane"></param>
    /// <returns></returns>
    Vector3 CenterOfPlane(Vector3[] plane)
    {
        float sumX = 0;
        float sumY = 0;
        float sumZ = 0;

        for (int i = 0; i < plane.Length; i++)
        {
            sumX += plane[i].x;
            sumY += plane[i].y;
            sumZ += plane[i].z;
        }

        return new Vector3(sumX / (float)plane.Length, sumY / (float)plane.Length, sumZ / (float)plane.Length);
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {

        Vector3[] tempDrawVertex = new Vector3[5];

        // Visualizo el Frustum far plane
        Handles.color = frustumPlaneColor;
        Handles.DrawAAConvexPolygon(frustumCornerFar);
        Handles.color = frustumLineColor;
        tempDrawVertex = new Vector3[] { frustumCornerFar[0], frustumCornerFar[1], frustumCornerFar[2], frustumCornerFar[3], frustumCornerFar[0] };
        Handles.DrawAAPolyLine(tempDrawVertex);
        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, CenterOfPlane(frustumCornerFar), Quaternion.LookRotation(NormalFromPlane(frustumCornerFar)), 1, EventType.Repaint);

        // Visualizo el Frustum near plane
        Handles.color = frustumPlaneColor;
        Handles.DrawAAConvexPolygon(frustumCornerNear);
        Handles.color = frustumLineColor;
        tempDrawVertex = new Vector3[] { frustumCornerNear[0], frustumCornerNear[1], frustumCornerNear[2], frustumCornerNear[3], frustumCornerNear[0] };
        Handles.DrawAAPolyLine(tempDrawVertex);
        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, CenterOfPlane(frustumCornerNear), Quaternion.LookRotation(-NormalFromPlane(frustumCornerNear)), 1, EventType.Repaint);

        // Visualizo el Frustum left plane
        Handles.color = frustumPlaneColor;
        Handles.DrawAAConvexPolygon(frustumCornerLeft);
        Handles.color = frustumLineColor;
        tempDrawVertex = new Vector3[] { frustumCornerLeft[0], frustumCornerLeft[1], frustumCornerLeft[2], frustumCornerLeft[3], frustumCornerLeft[0] };
        Handles.DrawAAPolyLine(tempDrawVertex);
        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, CenterOfPlane(frustumCornerLeft), Quaternion.LookRotation(NormalFromPlane(frustumCornerLeft)), 1, EventType.Repaint);

        // Visualizo el Frustum right plane
        Handles.color = frustumPlaneColor;
        Handles.DrawAAConvexPolygon(frustumCornerRight);
        Handles.color = frustumLineColor;
        tempDrawVertex = new Vector3[] { frustumCornerRight[0], frustumCornerRight[1], frustumCornerRight[2], frustumCornerRight[3], frustumCornerRight[0] };
        Handles.DrawAAPolyLine(tempDrawVertex);
        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, CenterOfPlane(frustumCornerRight), Quaternion.LookRotation(NormalFromPlane(frustumCornerRight)), 1, EventType.Repaint);

        // Visualizo el Frustum up plane
        Handles.color = frustumPlaneColor;
        Handles.DrawAAConvexPolygon(frustumCornerUp);
        Handles.color = frustumLineColor;
        tempDrawVertex = new Vector3[] { frustumCornerUp[0], frustumCornerUp[1], frustumCornerUp[2], frustumCornerUp[3], frustumCornerUp[0] };
        Handles.DrawAAPolyLine(tempDrawVertex);
        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, CenterOfPlane(frustumCornerUp), Quaternion.LookRotation(NormalFromPlane(frustumCornerUp)), 1, EventType.Repaint);

        // Visualizo el Frustum down plane
        Handles.color = frustumPlaneColor;
        Handles.DrawAAConvexPolygon(frustumCornerDown);
        Handles.color = frustumLineColor;
        tempDrawVertex = new Vector3[] { frustumCornerDown[0], frustumCornerDown[1], frustumCornerDown[2], frustumCornerDown[3], frustumCornerDown[0] };
        Handles.DrawAAPolyLine(tempDrawVertex);
        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, CenterOfPlane(frustumCornerDown), Quaternion.LookRotation(NormalFromPlane(frustumCornerDown)), 1, EventType.Repaint);
    }
#endif
}
