#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using System;
using UnityEditor.Splines;
using System.IO;

namespace Polyart
{
    using UnityEditor;
    using UnityEditor.EditorTools;

    [ExecuteInEditMode]
    public class PathTool02 : MonoBehaviour
    {
        public SplineContainer splineContainer;
        private MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public int lengthSegments = 50;
        public int widthSegments = 4;
        public float width = 2f, tileLength = 1f;
        [Range(0f, 0.05f)]
        public float verticalOffset = 0.01f;
        public LayerMask terrainLayer;
        public Material material;
        private Material prevMaterial = null;
        private Material materialInstance;
        public float raycastHeight = 50f;
        public float raycastDistance = 100f;
        private Vector3 prevTransform;
        private Quaternion prevRotation;
        private int prevHash;
        private Vector3 offsetVector;
        [Range(0f, 0.04f)]
        public float initialTerrainOffset = 0.02f;
        public bool mirrorUV = false;

        // ==== ฟีเจอร์ใหม่: วาง Mesh Prefab 3D ตาม spline ====
        [Header("Mesh Object Placement")]
        public GameObject meshPrefab;
        public float objectSpacing = 2f;
        public float lateralOffset = 0f;
        public float objectVerticalOffset = 0f;
        public bool alignToSplineForward = true;
        public bool snapToTerrain = true;
        private string PLACED_PARENT_NAME = "_PlacedMeshObjects";
        // ======================================================

        private void Reset()
        {
            splineContainer = GetComponent<SplineContainer>();
            GenerateInitialMesh();
        }

        private void OnEnable()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            splineContainer = GetComponent<SplineContainer>();
            meshFilter = GetComponent<MeshFilter>();
            prevTransform = transform.position;
            prevRotation = transform.rotation;
            prevHash = 0;
            terrainLayer = LayerMask.GetMask("Terrain");
        }

        private void OnValidate()
        {
            if (material != prevMaterial)
            {
                SetMaterial();
                prevMaterial = material;
            }

            offsetVector = new Vector3(0f, initialTerrainOffset, 0f);

            if (materialInstance != null)
            {
                materialInstance.SetFloat("_Tiling", splineContainer.CalculateLength() / width);
                materialInstance.SetFloat("_Length", splineContainer.CalculateLength());
                materialInstance.SetFloat("_MirrorUVs", mirrorUV ? 1f : 0f);
            }

            EditorApplication.delayCall += () =>
            {
                if (this != null)
                    GenerateInitialMesh();
            };
        }

        public void Update()
        {
            if (prevTransform != transform.position || prevRotation != transform.rotation)
                GenerateInitialMesh();

            prevTransform = transform.position;
            prevRotation = transform.rotation;

            int currHash = CalculateSplineHash(splineContainer);
            if (prevHash != currHash)
            {
                GenerateInitialMesh();
                materialInstance.SetFloat("_Tiling", splineContainer.CalculateLength() / width);
                materialInstance.SetFloat("_Length", splineContainer.CalculateLength());
            }
            prevHash = currHash;
        }

        public void GenerateInitialMesh()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            meshFilter.sharedMesh = GenerateMesh(1f);
            SetMaterial();
        }

        public Mesh GenerateMesh(float resolutionDivisor)
        {
            if (splineContainer == null)
                splineContainer = GetComponent<SplineContainer>();

            if (meshFilter == null)
                meshFilter = gameObject.GetComponent<MeshFilter>();

            Mesh mesh = new Mesh();

            float splineLength = Mathf.Round(splineContainer.Spline.GetLength());
            lengthSegments = (int)Mathf.Round(splineLength / (tileLength * resolutionDivisor));
            int currWidthSegments = (int)Mathf.Ceil((float)widthSegments / resolutionDivisor);

            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();
            List<Vector4> tangents = new();

            for (int i = 0; i <= lengthSegments; i++)
            {
                float t = i / (float)lengthSegments;
                Vector3 localPoint = splineContainer.Spline.EvaluatePosition(t);
                float3 tempTangent = splineContainer.Spline.EvaluateTangent(t);
                Vector3 tangent = new Vector3(tempTangent.x, tempTangent.y, tempTangent.z).normalized;
                Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;

                int currW = currWidthSegments;
                Vector3 left = localPoint - normal * width * 0.5f;
                Vector3 right = localPoint + normal * width * 0.5f;

                for (int j = 0; j <= currW; j++)
                {
                    Vector3 point = Vector3.Lerp(left, right, j / (float)currW);
                    point = transform.TransformPoint(point);
                    point.y = SampleTerrainHeight(point);
                    point = transform.InverseTransformPoint(point);

                    vertices.Add(point + offsetVector);
                    tangents.Add(new Vector4(tempTangent.x, tempTangent.y, tempTangent.z, 1));
                    uvs.Add(new Vector2(j / (float)currW, t));
                }

                if (i < lengthSegments)
                {
                    int startIndex = i * (currW + 1);
                    for (int j = 0; j < currW; j++)
                    {
                        triangles.Add(startIndex + j + 1);
                        triangles.Add(startIndex + j + currW + 1);
                        triangles.Add(startIndex + j);

                        triangles.Add(startIndex + j + 1);
                        triangles.Add(startIndex + j + 1 + currW + 1);
                        triangles.Add(startIndex + j + currW + 1);
                    }
                }
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.tangents = tangents.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void SetMaterial()
        {
            if (material != null)
            {
                if (material != prevMaterial)
                {
                    if (meshRenderer == null)
                        meshRenderer = GetComponent<MeshRenderer>();

                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    materialInstance = new Material(material);
                    meshRenderer.material = materialInstance;
                    materialInstance.SetFloat("_Tiling", splineContainer.CalculateLength() / width);
                    materialInstance.SetFloat("_Length", splineContainer.CalculateLength());
                }
            }
        }

        float SampleTerrainHeight(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * raycastHeight, Vector3.down, out RaycastHit hit, raycastDistance, terrainLayer))
                return hit.point.y;
            return point.y;
        }

        private static int CalculateSplineHash(SplineContainer container)
        {
            int hash = 17;
            foreach (var spline in container.Splines)
                foreach (var knot in spline.Knots)
                {
                    hash = hash * 23 + knot.Position.GetHashCode();
                    hash = hash * 23 + knot.Rotation.GetHashCode();
                }
            return hash;
        }

        public Material GetMaterialInstance() => materialInstance;

        // ==== สร้าง Mesh Prefabs ตามเส้น ====
        public void GenerateMeshObjects()
        {
            if (splineContainer == null) splineContainer = GetComponent<SplineContainer>();
            if (meshPrefab == null)
            {
                Debug.LogError("PathTool02: meshPrefab is null!");
                return;
            }

            Transform oldParent = transform.Find(PLACED_PARENT_NAME);
            if (oldParent != null)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(oldParent.gameObject);
#else
                DestroyImmediate(oldParent.gameObject);
#endif
            }

            GameObject parentGO = new(PLACED_PARENT_NAME);
            parentGO.transform.SetParent(transform, false);
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(parentGO, "Create Mesh Objects Parent");
#endif

            float totalLength = splineContainer.Spline.GetLength();
            for (float dist = 0f; dist <= totalLength; dist += objectSpacing)
            {
                float t = ApproximateTFromDistance(splineContainer.Spline, dist); // ? ใช้ฟังก์ชันใหม่แทน
                Vector3 localPos = splineContainer.Spline.EvaluatePosition(t);
                Vector3 forward = ((Vector3)splineContainer.Spline.EvaluateTangent(t)).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * -1f;
                localPos += right * lateralOffset;

                Vector3 worldPos = transform.TransformPoint(localPos);
                if (snapToTerrain) worldPos.y = SampleTerrainHeight(worldPos);
                worldPos.y += objectVerticalOffset;

                Quaternion rot = Quaternion.identity;
                if (alignToSplineForward)
                {
                    Vector3 flatFwd = forward; flatFwd.y = 0f;
                    if (flatFwd.sqrMagnitude < 0.001f) flatFwd = forward;
                    rot = Quaternion.LookRotation(flatFwd.normalized, Vector3.up);
                }

#if UNITY_EDITOR
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(meshPrefab);
                Undo.RegisterCreatedObjectUndo(obj, "Place Mesh Object");
#else
                GameObject obj = Instantiate(meshPrefab);
#endif
                obj.transform.SetParent(parentGO.transform);
                obj.transform.position = worldPos;
                obj.transform.rotation = rot;
            }

            Debug.Log($"PathTool02: Generated {parentGO.transform.childCount} mesh objects along spline.");
        }

        // ?? ฟังก์ชันช่วย: คำนวณค่า t จากระยะทาง (ใช้ได้กับ Unity ทุกเวอร์ชัน)
        private float ApproximateTFromDistance(Spline spline, float targetDistance, int steps = 200)
        {
            float totalLength = spline.GetLength();
            float accumulated = 0f;
            Vector3 prev = spline.EvaluatePosition(0f);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 curr = spline.EvaluatePosition(t);
                accumulated += Vector3.Distance(prev, curr);
                if (accumulated >= targetDistance)
                    return t;
                prev = curr;
            }
            return 1f;
        }
    }

    [CustomEditor(typeof(PathTool02))]
    public class PathTool02Editor : Editor
    {
        private PathTool02 pathTool;

        void OnEnable() => pathTool = (PathTool02)target;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("3D Mesh Placement", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Mesh Objects Along Path"))
            {
                pathTool.GenerateMeshObjects();
            }
        }
    }
}
#else
using UnityEngine;
namespace Polyart { public class PathTool02 : MonoBehaviour { } }
#endif
