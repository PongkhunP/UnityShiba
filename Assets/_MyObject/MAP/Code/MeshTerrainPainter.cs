using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ติดบน Mesh ที่อยากเพนต์ (ต้องมี MeshFilter + MeshRenderer)
/// ใช้เพ้นต์ Vertex Color ให้เอาไป Blend Texture ใน Shader ได้
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class MeshTerrainPainter : MonoBehaviour
{
    [Header("Brush Settings")]
    public float brushSize = 1f;
    [Range(0f, 1f)] public float brushHardness = 0.5f;
    [Range(0f, 1f)] public float brushStrength = 0.5f;

    [Tooltip("0 = R, 1 = G, 2 = B, 3 = A")]
    [Range(0, 3)] public int textureToPaint = 0;

    [Header("Raycast")]
    public LayerMask paintLayers = ~0;   // เลเยอร์ที่จะโดนเพนต์
    public float raycastDistance = 500f;

    [Header("Advanced Blend")]
    [Tooltip("คูณเพิ่มความแรงของแปรงทั้งหมด (ไม่ต้องแก้โค้ด)")]
    public float strengthMultiplier = 1f;

    [Tooltip("เปิดไว้เพื่อให้เวลาระบาย Texture ปัจจุบัน มันกดช่องอื่นลงเล็กน้อย จะได้เห็น Texture ชัดขึ้น")]
    public bool fadeOtherChannels = true;

    [Range(0f, 1f), Tooltip("0 = แทบไม่กดช่องอื่น, 1 = กดช่องอื่นเยอะ")]
    public float fadeAmount = 0.5f;

    [HideInInspector] public Mesh workingMesh;

    void OnEnable()
    {
        Init();
    }

    void Init()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null) return;

#if UNITY_EDITOR
        // clone mesh ในโหมด editor เพื่อไม่แก้ asset ต้นฉบับ
        if (!Application.isPlaying)
        {
            workingMesh = Instantiate(mf.sharedMesh);
            workingMesh.name = mf.sharedMesh.name + "_Painted";
            mf.sharedMesh = workingMesh;
        }
        else
        {
            workingMesh = mf.mesh;
        }
#else
        workingMesh = mf.mesh;
#endif

        // sync mesh collider
        var col = GetComponent<MeshCollider>();
        col.sharedMesh = workingMesh;

        // ensure vertex colors array มีครบ
        if (workingMesh.colors == null || workingMesh.colors.Length != workingMesh.vertexCount)
        {
            var cols = new Color[workingMesh.vertexCount];
            for (int i = 0; i < cols.Length; i++)
                cols[i] = Color.black; // ค่าเริ่มต้น = 0 ทุก channel
            workingMesh.colors = cols;
        }
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(MeshTerrainPainter))]
public class MeshTerrainPainterEditor : Editor
{
    MeshTerrainPainter painter;
    bool painting;

    void OnEnable()
    {
        painter = (MeshTerrainPainter)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Mesh Terrain Painter", EditorStyles.boldLabel);

        // Brush settings
        EditorGUILayout.PropertyField(serializedObject.FindProperty("brushSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("brushHardness"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("brushStrength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textureToPaint"));

        // Raycast
        EditorGUILayout.PropertyField(serializedObject.FindProperty("paintLayers"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("raycastDistance"));

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Advanced Blend", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("strengthMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeOtherChannels"));
        if (serializedObject.FindProperty("fadeOtherChannels").boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeAmount"));
        }

        GUILayout.Space(10);

        if (GUILayout.Button(painting ? "Stop painting" : "Start painting"))
        {
            painting = !painting;
            SceneView.RepaintAll();
        }

        EditorGUILayout.HelpBox(
            "Left click = เพนต์\n" +
            "Ctrl + Left click = ลบ/ถอยสี\n" +
            "ใช้ Vertex Color (R,G,B,A) เป็น Weight ของ Texture แต่ละตัว",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!painting || painter == null || painter.workingMesh == null)
            return;

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, painter.raycastDistance, painter.paintLayers))
        {
            // วาดวง Brush
            Handles.color = new Color(1f, 1f, 0f, 0.7f);
            Handles.DrawWireDisc(hit.point, hit.normal, painter.brushSize);

            // คลิกซ้ายเพนต์
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                bool erase = e.control; // กด Ctrl เพื่อลบ
                ApplyPaint(hit, erase);
                e.Use();
            }
        }

        SceneView.RepaintAll();
    }

    void ApplyPaint(RaycastHit hit, bool erase)
    {
        Mesh mesh = painter.workingMesh;
        if (mesh == null) return;

        Vector3[] verts = mesh.vertices;
        Color[] cols = mesh.colors;
        Transform t = painter.transform;

        float radius = painter.brushSize;

        // ความแรงแปรง = brushStrength * strengthMultiplier
        float strengthBase = painter.brushStrength * Mathf.Max(0.01f, painter.strengthMultiplier);
        float strength = (erase ? -1f : 1f) * strengthBase;

        Undo.RegisterCompleteObjectUndo(mesh, "Mesh Vertex Paint");

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 worldPos = t.TransformPoint(verts[i]);
            float dist = Vector3.Distance(worldPos, hit.point);
            if (dist > radius) continue;

            float normalized = 1f - (dist / radius);
            float falloff = Mathf.Pow(normalized, Mathf.Lerp(4f, 1f, painter.brushHardness)); // hardness สูง = ขอบคม

            float delta = strength * falloff;

            Color c = cols[i];

            // ถ้าไม่ได้ลบ และเปิด fadeOtherChannels ให้กดช่องอื่นลงเล็กน้อย
            if (!erase && painter.fadeOtherChannels && painter.fadeAmount > 0f)
            {
                float fade = 1f - Mathf.Clamp01(Mathf.Abs(delta)) * painter.fadeAmount;
                fade = Mathf.Clamp01(fade);

                if (painter.textureToPaint != 0) c.r *= fade;
                if (painter.textureToPaint != 1) c.g *= fade;
                if (painter.textureToPaint != 2) c.b *= fade;
                if (painter.textureToPaint != 3) c.a *= fade;
            }

            // เพิ่ม/ลดช่องที่เราเพนต์
            switch (painter.textureToPaint)
            {
                case 0: c.r = Mathf.Clamp01(c.r + delta); break;
                case 1: c.g = Mathf.Clamp01(c.g + delta); break;
                case 2: c.b = Mathf.Clamp01(c.b + delta); break;
                case 3: c.a = Mathf.Clamp01(c.a + delta); break;
            }

            cols[i] = c;
        }

        mesh.colors = cols;
        mesh.UploadMeshData(false);

        // อัปเดต collider ด้วย (ไม่จำเป็นแต่เผื่อ)
        var col = painter.GetComponent<MeshCollider>();
        if (col) { col.sharedMesh = null; col.sharedMesh = mesh; }

        EditorUtility.SetDirty(mesh);
    }
}

#endif
