using Nothke.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.U2D.Sprites;
using UnityEngine.Rendering;

[RequireComponent(typeof(Image))]
public class UIDie : DiceRotatorBase
{
    public UIDieData dieData;
    private Image _die;

    public override int SidesCount => dieData.SidesTextures.Count;

    [SerializeField]
    private int _seed;
    [SerializeField]
    private int _defaultSide;

    private void Awake()
    {
        UnityEngine.Random.InitState(_seed);
        _die = GetComponent<Image>();
    }

    private void Start()
    {
        RotateDie(_defaultSide);
    }

    public override void RotateDie(int side)
    {
        if (_rotating)
        {
            Debug.Log("Already rotating");
            return;
        }

        _die.sprite = dieData.SidesTextures[side - 1];
    }

    private bool _rotating;
    private float _timeLeft;
    private int _frame;
    private int _processedTargetSide;

    public override void RotateDieAnimated(int side, float animationDuration)
    {
        if (_rotating)
        {
            Debug.Log("Already rotating");
            return;
        }

        _frame = 0;
        _timeLeft = animationDuration;
        _rotating = true;
        _processedTargetSide = side;
    }

    private void Update()
    {
        if (!_rotating)
        {
            return;
        }

        _timeLeft -= Time.deltaTime;

        if (_timeLeft > 0)
        {
            _die.sprite = dieData.AnimationFrames[_frame % dieData.AnimationFrames.Count];
            _frame++;
        }
        else
        {
            _rotating = false;
            RotateDie(_processedTargetSide);
        }
    }
}

[CustomEditor(typeof(UIDie))]
public class UIDieEditor : Editor
{
    private ProceduralDie _die;
    private int _textureSize = 256;
    private bool _showGenerationHelper;

    private string GetPath(string name, string prompt, string badFileMessage, string badFolderMessage, string fileAlreadyExistsMessage)
    {
        var path = EditorUtility.OpenFolderPanel(prompt, "", "");

        if (!path.StartsWith(Application.dataPath))
        {
            throw new ArgumentException(badFileMessage);
        }

        if (!Directory.Exists(path))
        {
            throw new ArgumentException(badFolderMessage);
        }

        if (File.Exists(path + name))
        {
            throw new ArgumentException(fileAlreadyExistsMessage);
        }

        return path;
    }

    private string GetSpritesPath(string dieName) => GetPath(
        dieName + "_sides.png",
        "Select sprites path",
        "Can't generate UIDieData - Sprites directory is not in the project",
        "Can't generate UIDieData - Sprites directory does not exist",
        "Can't generate UIDieData - Sprites are already generated"
    );

    private string GetAnimationPath(string dieName) => GetPath(
        dieName + "_anim.png",
        "Select animation data path",
        "Can't generate UIDieData - Sprites directory is not in the project",
        "Can't generate UIDieData - Animation directory does not exist",
        "Can't generate UIDieData - Animation directory does not exist"
    );

    private string GetDieDataPath(string dieName) => GetPath(
        dieName + "_UIDieData.asset",
        "Select die data path",
        "Can't generate UIDieData - Sprites directory is not in the project",
        "Can't generate UIDieData - UIDieData directory does not exist",
        "Can't generate UIDieData - UIDieData is already generated"
    );

    private (Texture2D, Vector2Int) PackToSpritesheet(Texture2D[] textures, Vector2Int textureSize)
    {
        int maxTexturesPerSide = 16384 / textureSize.x;

        var cols = Math.Min(maxTexturesPerSide, textures.Length);
        var rows = (int)Math.Ceiling((float)textures.Length / cols);

        var spriteSheetTexture = new Texture2D(textureSize.x * cols, textureSize.y * rows, TextureFormat.RGBA32, false);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                if (index >= textures.Length)
                {
                    break;
                }

                Graphics.CopyTexture(
                    textures[index], 0, 0, 
                    0, 0, textureSize.x, textureSize.y, 
                    spriteSheetTexture, 0, 0,
                    textureSize.x * col, textureSize.y * row
                );
            }
        }

        return (spriteSheetTexture, new Vector2Int(cols, rows));
    }

    private void ImportTexturesAsSprites(string path, string name, int count, Vector2Int textureSize, Vector2Int cells)
    {
        var fullPath = "Assets" + path.Substring(Application.dataPath.Length);
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(fullPath);

        importer.isReadable = true;
        importer.textureType = TextureImporterType.Sprite;

        TextureImporterSettings importerSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(importerSettings);
        importerSettings.spriteExtrude = 0;
        importerSettings.spriteGenerateFallbackPhysicsShape = false;
        importerSettings.spriteMeshType = SpriteMeshType.FullRect;
        importerSettings.spriteMode = (int)SpriteImportMode.Multiple;
        importer.SetTextureSettings(importerSettings);

        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.maxTextureSize = 16384;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;

        List<SpriteMetaData> spriteMetaDatas = new List<SpriteMetaData>();

        for (int row = 0; row < cells.y; row++)
        {
            for (int col = 0; col < cells.x; col++)
            {
                int index = row * cells.x + col;
                if (index >= count)
                {
                    break;
                }

                SpriteMetaData spriteMetaData = new SpriteMetaData
                {
                    name = name + "_" + index,
                    rect = new Rect(textureSize.x * col, textureSize.y * row, textureSize.x, textureSize.y),
                    alignment = 0,
                    pivot = new Vector2(0f, 0f)
                };

                spriteMetaDatas.Add(spriteMetaData);
            }
        }

        importer.spritesheet = spriteMetaDatas.ToArray();

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh();
    }

    private string GenerateSidesSpritesheet(Vector2Int textureSize, string spritesheetPath, string spritesheetName)
    {
        Texture2D[] sides = new Texture2D[_die.Generator.ActualDieSize];

        var initialRotation = _die.transform.rotation;

        for (int side = 0; side < _die.Generator.ActualDieSize; side++)
        {
            _die.transform.rotation = Quaternion.identity;
            _die.SideRotation(side, Vector3.up, Vector3.back);

            var texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);
            var renderTexture = RenderTexture.GetTemporary(textureSize.x, textureSize.y);

            renderTexture.BeginPerspectiveRendering(60, Vector3.back * 2.5f, Quaternion.identity);

            renderTexture.DrawMesh(_die.Mesh, _die.Material, Matrix4x4.TRS(Vector3.zero, _die.transform.rotation, Vector3.one));

            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            renderTexture.EndRendering();
            sides[side] = texture;

            renderTexture.Release();
        }

        _die.transform.rotation = initialRotation;

        (Texture2D, Vector2Int) sidesSpriteSheet = PackToSpritesheet(sides, textureSize);
        File.WriteAllBytes(spritesheetPath, sidesSpriteSheet.Item1.EncodeToPNG());
        AssetDatabase.Refresh();

        ImportTexturesAsSprites(spritesheetPath, spritesheetName, sides.Length, textureSize, sidesSpriteSheet.Item2);

        return spritesheetPath;
    }

    private string GenerateAnimationSpritesheet(Vector2Int textureSize, string spritesheetPath, string spritesheetName)
    {
        Texture2D[] frames = new Texture2D[60];

        var initialRotation = _die.transform.rotation;

        for (int frame = 0; frame < 60; frame++)
        {
            _die.transform.Rotate(UnityEngine.Random.insideUnitSphere * 180);

            var texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);
            var renderTexture = RenderTexture.GetTemporary(textureSize.x, textureSize.y);

            renderTexture.BeginPerspectiveRendering(60, _die.transform.position + Vector3.back * 2.5f, Quaternion.identity);
            renderTexture.DrawMesh(_die.Mesh, _die.Material, Matrix4x4.TRS(_die.transform.position, _die.transform.rotation, Vector3.one));

            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            renderTexture.EndRendering();
            frames[frame] = texture;

            renderTexture.Release();
        }

        _die.transform.rotation = initialRotation;

        (Texture2D, Vector2Int) animationSpriteSheet = PackToSpritesheet(frames, textureSize);
        File.WriteAllBytes(spritesheetPath, animationSpriteSheet.Item1.EncodeToPNG());
        AssetDatabase.Refresh();

        ImportTexturesAsSprites(spritesheetPath, spritesheetName, frames.Length, textureSize, animationSpriteSheet.Item2);

        return spritesheetPath;
    }

    private void GenerateUIDieData(Vector2Int textureSize)
    {
        // Validate procedural die
        if (_die == null || !_die.IsValid)
        {
            Debug.LogError("Can't generate UIDieData - Die is invalid");
            return;
        }

        // Generate name for a die
        // <Shape>_<DieSize>_<Material>
        var name = $"{_die.Generator.GetType().Name}_{_die.DieSize}_{_die.Material.name}";

        _die.Generate();

        var spritesPath = GetSpritesPath(name);
        string sidesSpritesheet = GenerateSidesSpritesheet(textureSize, $"{spritesPath}/{name}_sides.png", $"{name}_sides");

        var animationPath = GetAnimationPath(name);
        string animationSpritesheet = GenerateAnimationSpritesheet(textureSize, $"{animationPath}/{name}_anim.png", $"{name}_anim");

        var dieDataPath = GetDieDataPath(name);
        
        var uiDieData = ScriptableObject.CreateInstance<UIDieData>();
        uiDieData.name = name;

        uiDieData.SidesTextures = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets" + sidesSpritesheet.Substring(Application.dataPath.Length))
            .Where(o => o is Sprite)
            .Cast<Sprite>()
            .ToList();

        uiDieData.AnimationFrames = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets" + animationSpritesheet.Substring(Application.dataPath.Length))
            .Where(o => o is Sprite)
            .Cast<Sprite>()
            .ToList();

        AssetDatabase.CreateAsset(uiDieData, $"Assets/{dieDataPath.Substring(Application.dataPath.Length)}/{name}_UIDieData.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        (target as UIDie).dieData = uiDieData;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var uiDie = target as UIDie;

        EditorGUILayout.Space(20f);

        _showGenerationHelper = EditorGUILayout.Foldout(_showGenerationHelper, "Generate UIDieData");
        if (_showGenerationHelper)
        {
            _die = (ProceduralDie)EditorGUILayout.ObjectField("Procedural die", _die, typeof(ProceduralDie), true);
            _textureSize = EditorGUILayout.IntField("Texture size", _textureSize);

            if (GUILayout.Button("Prerender die") && Application.isEditor)
            {
                GenerateUIDieData(Vector2Int.one * _textureSize);
            }
        }
    }
}
