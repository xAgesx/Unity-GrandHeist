using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CameraFOVMesh : MonoBehaviour
{
    [Header("Cone Shape")]
    public float viewDistance = 15f;
    public float viewAngle    = 50f;
    public float verticalAngle = 20f;
    public int   rayCount     = 40;
    public int   verticalSegments = 6;

    [Header("Appearance")]
    public Color tipColor     = new Color(1f, 0.1f, 0.05f, 0.9f);
    public Color edgeColor    = new Color(1f, 0.05f, 0.02f, 0.0f);
    public Color alertTipColor  = new Color(1f, 0.55f, 0.05f, 1f);
    public Color alertEdgeColor = new Color(1f, 0.3f,  0.02f, 0.0f);
    public float colorLerpSpeed = 4f;

    [Header("References")]
    public SecurityCamera securityCamera;

    MeshFilter   meshFilter;
    MeshRenderer meshRenderer;
    Material     coneMaterial;
    Color        currentTip;
    Color        currentEdge;

    void Awake()
    {
        meshFilter   = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        coneMaterial = new Material(shader);
        coneMaterial.SetFloat("_Surface", 1);
        coneMaterial.SetFloat("_Blend", 0);
        coneMaterial.SetFloat("_ZWrite", 0);
        coneMaterial.EnableKeyword("_ALPHABLEND_ON");
        coneMaterial.renderQueue              = 3000;
        coneMaterial.enableInstancing         = false;
        meshRenderer.material                 = coneMaterial;
        meshRenderer.shadowCastingMode        = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows           = false;

        currentTip  = tipColor;
        currentEdge = edgeColor;
    }

    void Update()
    {
        BuildMesh();
        UpdateColor();
    }

    void BuildMesh()
    {
        Mesh mesh = new Mesh();

        float halfH = viewAngle    * 0.5f;
        float halfV = verticalAngle * 0.5f;

        int totalRays    = rayCount + 1;
        int totalVerts   = totalRays * (verticalSegments + 1) + 1;
        int totalTris    = rayCount * verticalSegments * 2;

        Vector3[] vertices = new Vector3[totalVerts];
        Color[]   colors   = new Color[totalVerts];
        int[]     tris     = new int[totalTris * 3];

        vertices[0] = Vector3.zero;
        colors[0]   = currentTip;

        int vIdx = 1;
        for (int i = 0; i <= rayCount; i++)
        {
            float hAngle = Mathf.Lerp(-halfH, halfH, (float)i / rayCount) * Mathf.Deg2Rad;

            for (int j = 0; j <= verticalSegments; j++)
            {
                float vAngle = Mathf.Lerp(-halfV, halfV, (float)j / verticalSegments) * Mathf.Deg2Rad;

                Vector3 dir = new Vector3(
                    Mathf.Sin(hAngle),
                    Mathf.Sin(vAngle),
                    Mathf.Cos(hAngle) * Mathf.Cos(vAngle)
                ).normalized;

                float edgeFactor = Mathf.Abs((float)i / rayCount - 0.5f) * 2f;
                edgeFactor = Mathf.Max(edgeFactor, Mathf.Abs((float)j / verticalSegments - 0.5f) * 2f);

                vertices[vIdx] = dir * viewDistance;
                colors[vIdx]   = Color.Lerp(currentTip, currentEdge, edgeFactor);
                vIdx++;
            }
        }

        int tIdx = 0;
        int stride = verticalSegments + 1;
        for (int i = 0; i < rayCount; i++)
        {
            for (int j = 0; j < verticalSegments; j++)
            {
                int a = 1 + i * stride + j;
                int b = 1 + (i + 1) * stride + j;
                int c = a + 1;
                int d = b + 1;

                tris[tIdx++] = a;
                tris[tIdx++] = b;
                tris[tIdx++] = c;

                tris[tIdx++] = c;
                tris[tIdx++] = b;
                tris[tIdx++] = d;
            }
        }

        mesh.vertices  = vertices;
        mesh.colors    = colors;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    void UpdateColor()
    {
        float t = 0f;
        if (securityCamera != null)
            t = Mathf.Clamp01(securityCamera.DetectionMeter / securityCamera.detectionTime);

        currentTip  = Color.Lerp(Color.Lerp(tipColor,  alertTipColor,  t), alertTipColor,  t * t);
        currentEdge = Color.Lerp(edgeColor, alertEdgeColor, t);
    }
}