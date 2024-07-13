using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour 
{
    private Dictionary<string, MeshJobScheduleDelegate> _meshBuildingJobs = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => {
            var interfaces = t.GetInterfaces();
            return (t.IsClass || t.IsStruct()) && interfaces.Contains(typeof(IMeshGenerator));
        })
        .ToDictionary(keySelector: t => t.Name, elementSelector: t => (Activator.CreateInstance(t) as IMeshGenerator).DefaultJobHandle);

    private List<string> MeshGenerators => _meshBuildingJobs.Keys.ToList();

    [SerializeField, Dropdown("MeshGenerators")]
    private string _meshGenerator;

    [SerializeField]
    private Material[] _materials;

    private Dictionary<string, Material> _materialsLookup => _materials
        .ToDictionary(keySelector: m => m.name, elementSelector: m => m);

    private List<string> Materials => _materialsLookup.Keys.ToList();

    [SerializeField, Dropdown("Materials")]
    private string _material;

    private Mesh _mesh;
    private Vector3[] _vertices, _normals;
    private Vector4[] _tangents;
    private int[] _triangles;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh
        {
            name = "Procedural Mesh"
        };

        _meshFilter.mesh = _mesh;
    }

    void OnValidate() => enabled = true;

    void Update()
    {
        GenerateMesh();
        enabled = false;

        _meshRenderer.material = _materialsLookup[_material];
    }

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

        _vertices = _mesh.vertices;

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
                _normals = _mesh.normals;
                
                Gizmos.color = Color.green;
                Gizmos.DrawRay(position, t.TransformDirection(_normals[i]) * 0.2f);
            }

            if (drawTangents && _mesh.HasVertexAttribute(VertexAttribute.Tangent))
            {
                _tangents = _mesh.tangents;
                
                Gizmos.color = Color.red;
                Gizmos.DrawRay(position, t.TransformDirection(_tangents[i]) * 0.2f);
            }

            if (drawTriangles)
            {
                _triangles = _mesh.triangles;

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

    [SerializeField, Range(1, 50)]
    private int _resolution = 1;

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

        if (_meshGenerator == string.Empty)
        {
            _meshGenerator = MeshGenerators[0];
        }

        _meshBuildingJobs[_meshGenerator](_mesh, meshData, _resolution, default).Complete();

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