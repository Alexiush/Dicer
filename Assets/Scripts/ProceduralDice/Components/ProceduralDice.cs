using ProceduralMeshes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralDice : MonoBehaviour
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

    private Mesh _mesh;
    private Vector3[] _vertices, _normals;
    private Vector4[] _tangents;
    private int[] _triangles;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    [SerializeField]
    private TextMeshPro _textMeshPro;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh
        {
            name = "Procedural Dice"
        };

        _meshFilter.mesh = _mesh;
    }

    void OnValidate() => enabled = true;

    public delegate void OnNewGenerationEvent(Mesh mesh);
    public event OnNewGenerationEvent OnNewGeneration;

    void Update()
    {
        var generator = _diceGenerators[_diceGenerator];

        generator.Resolution = _resolution;
        generator.DieSize = _dieSize;

        if (!generator.Validate())
        {
            Debug.LogError("Invalid die data");
            enabled = false;

            return;
        }

        _vertices = null;
        _normals = null;
        _tangents = null;
        _triangles = null;

        GenerateMesh();
        OnNewGeneration?.Invoke(_mesh);
        enabled = false;

        _material.SetTexture("_Mask", generator.GenerateNumbersTexture(256, 256, _textMeshPro));
        _meshRenderer.material = _material;
    }

    public int GetSelectedSide() => _diceGenerators[_diceGenerator].GetSelectedSide(transform, _mesh);

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
        if (_gizmos == GizmoMode.Nothing || _mesh == null)
        {
            return;
        }

        bool drawVertices = (_gizmos & GizmoMode.Vertices) != 0;
        bool drawNormals = (_gizmos & GizmoMode.Normals) != 0;
        bool drawTangents = (_gizmos & GizmoMode.Tangents) != 0;
        bool drawTriangles = (_gizmos & GizmoMode.Triangles) != 0;

        if (_vertices == null)
        {
            _vertices = _mesh.vertices;
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

            if (drawNormals && _mesh.HasVertexAttribute(VertexAttribute.Normal))
            {
                if (_normals == null)
                {
                    _normals = _mesh.normals;
                }

                Gizmos.color = Color.green;
                Gizmos.DrawRay(position, t.TransformDirection(_normals[i]) * 0.2f);
            }

            if (drawTangents && _mesh.HasVertexAttribute(VertexAttribute.Tangent))
            {
                if (_tangents == null)
                {
                    _tangents = _mesh.tangents;
                }

                Gizmos.color = Color.red;
                Gizmos.DrawRay(position, t.TransformDirection(_tangents[i]) * 0.2f);
            }

            if (drawTriangles)
            {
                if (_triangles == null)
                {
                    _triangles = _mesh.triangles;
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
    [SerializeField, Range(1, 100)]
    private int _dieSize = 1;

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

        _dieBuildingJobs[_diceGenerator](_mesh, meshData, _resolution, _dieSize, default).Complete();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mesh);

        if (_meshOptimization == MeshOptimizationMode.ReorderIndices)
        {
            _mesh.OptimizeIndexBuffers();
        }
        else if (_meshOptimization == MeshOptimizationMode.ReorderVertices)
        {
            _mesh.OptimizeReorderVertexBuffer();
        }
        else if (_meshOptimization != MeshOptimizationMode.Nothing)
        {
            _mesh.Optimize();
        }
    }
}
