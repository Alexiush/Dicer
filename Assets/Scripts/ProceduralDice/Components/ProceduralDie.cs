using ProceduralMeshes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralDie : MonoBehaviour
{
    private Dictionary<string, DieMeshJobScheduleDelegate> _dieBuildingJobs = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => t.GetInterfaces().Contains(typeof(IDiceGenerator)))
        .ToDictionary(keySelector: t => t.Name, elementSelector: t => (Activator.CreateInstance(t) as IDiceGenerator).DefaultDieJobHandle);


    private Dictionary<string, IDiceGenerator> _diceGenerators = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => t.GetInterfaces().Contains(typeof(IDiceGenerator)))
        .ToDictionary(keySelector: t => t.Name, elementSelector: t => (Activator.CreateInstance(t) as IDiceGenerator));

    private List<string> DiceGeneratorJobs => _dieBuildingJobs.Keys.ToList();

    [SerializeField, Dropdown("DiceGeneratorJobs")]
    private string _diceGenerator;

    [SerializeField]
    private Material _material;
    public Material Material
    {
        get
        {
            if (_meshRenderer == null)
            {
                return null;
            }

            return Application.isEditor ? _meshRenderer.sharedMaterial : _meshRenderer.material;
        }
    }

    public Mesh Mesh
    {
        get
        {
            if (_meshFilter == null)
            {
                return null;
            }

            return Application.isEditor ? _meshFilter.sharedMesh : _meshFilter.mesh;
        }
    }

    private Vector3[] _vertices, _normals;
    private Vector4[] _tangents;
    private int[] _triangles;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    [SerializeField]
    private SideTextureRenderer _sideTextureRenderer;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _meshFilter.mesh = new Mesh
        {
            name = "Procedural Dice"
        };
    }

    public delegate void OnNewGenerationEvent(Mesh mesh);
    public event OnNewGenerationEvent OnNewGeneration;

    public IDiceGenerator Generator => _diceGenerators[_diceGenerator];

    public void Generate()
    {
        if (Mesh == null)
        {
            Debug.Log("Die is not initialized yet");
            return;
        }

        Generator.Resolution = _resolution;
        Generator.DieSize = DieSize;

        if (!IsValid)
        {
            Debug.LogError("Invalid die data");
            return;
        }

        _vertices = null;
        _normals = null;
        _tangents = null;
        _triangles = null;

        GenerateMesh();
        OnNewGeneration?.Invoke(Mesh);

        _material.SetTexture("_Mask", Generator.GenerateNumbersTexture(256, 256, _sideTextureRenderer));
        _meshRenderer.material = _material;
    }

    private void Start()
    {
        if (Mesh.vertexCount != 0)
        {
            return;
        }

        Generate();
    }

    private void OnValidate()
    {
        Generate();
    }

    public bool IsValid => Generator.Validate();

    public void SideRotation(int side, Vector3 topDirection, Vector3 forwardDirection) => Generator
        .SideRotation(
            transform, Mesh, side, 
            topDirection.normalized,
            forwardDirection.normalized
        );
    public int GetRolledSide(Vector3 normal) => Generator.GetRolledSide(transform, Mesh, normal.normalized);

    [System.Flags]
    public enum GizmoMode
    {
        Nothing = 0,
        Vertices = 1,
        Normals = 2,
        Tangents = 4,
        Triangles = 8
    }

    [SerializeField]
    private GizmoMode _gizmos;

    void OnDrawGizmos()
    {
        if (_gizmos == GizmoMode.Nothing || Mesh == null)
        {
            return;
        }

        bool drawVertices = (_gizmos & GizmoMode.Vertices) != 0;
        bool drawNormals = (_gizmos & GizmoMode.Normals) != 0;
        bool drawTangents = (_gizmos & GizmoMode.Tangents) != 0;
        bool drawTriangles = (_gizmos & GizmoMode.Triangles) != 0;

        if (_vertices == null)
        {
            _vertices = Mesh.vertices;
        }

        Transform t = transform;
        for (int i = 0; i < _vertices.Length; i++)
        {
            Vector3 position = t.TransformPoint(_vertices[i]);

            if (drawVertices)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(position, 0.02f);
            }

            if (drawNormals && Mesh.HasVertexAttribute(VertexAttribute.Normal))
            {
                if (_normals == null)
                {
                    _normals = Mesh.normals;
                }

                Gizmos.color = Color.green;
                Gizmos.DrawRay(position, t.TransformDirection(_normals[i]) * 0.2f);
            }

            if (drawTangents && Mesh.HasVertexAttribute(VertexAttribute.Tangent))
            {
                if (_tangents == null)
                {
                    _tangents = Mesh.tangents;
                }

                Gizmos.color = Color.red;
                Gizmos.DrawRay(position, t.TransformDirection(_tangents[i]) * 0.2f);
            }

            if (drawTriangles)
            {
                if (_triangles == null)
                {
                    _triangles = Mesh.triangles;
                }

                float colorStep = 1f / (_triangles.Length - 3);
                for (int triangle = 0; triangle < _triangles.Length; triangle += 3)
                {
                    float c = triangle * colorStep;
                    Gizmos.color = new Color(c, 0f, c);
                    Gizmos.DrawSphere(
                        t.TransformPoint((
                            _vertices[_triangles[triangle]] +
                            _vertices[_triangles[triangle + 1]] +
                            _vertices[_triangles[triangle + 2]]
                        ) * (1f / 3f)),
                        0.02f
                    );
                }
            }
        }
    }

    [SerializeField, Range(1, 5)]
    private int _resolution = 1;
    [field: SerializeField, Range(1, 100)]
    public int DieSize { get; private set; } = 1;

    [System.Flags]
    public enum MeshOptimizationMode
    {
        Nothing = 0,
        ReorderIndices = 1,
        ReorderVertices = 2
    }

    [SerializeField]
    private MeshOptimizationMode _meshOptimization;

    private void GenerateMesh()
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        if (_diceGenerator == string.Empty)
        {
            _diceGenerator = DiceGeneratorJobs[0];
        }

        _dieBuildingJobs[_diceGenerator](Mesh, meshData, _resolution, DieSize, default).Complete();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, Mesh);

        if (_meshOptimization == MeshOptimizationMode.ReorderIndices)
        {
            Mesh.OptimizeIndexBuffers();
        }
        else if (_meshOptimization == MeshOptimizationMode.ReorderVertices)
        {
            Mesh.OptimizeReorderVertexBuffer();
        }
        else if (_meshOptimization != MeshOptimizationMode.Nothing)
        {
            Mesh.Optimize();
        }
    }
}

[CustomEditor(typeof(ProceduralDie))]
public class ProceduralDiceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var proceduralDie = target as ProceduralDie; 

        EditorGUILayout.Space(20f);
        if (GUILayout.Button("Regenerate"))
        {
            proceduralDie.Generate();
        }
    }
}


