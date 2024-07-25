using Nothke.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEditor.PackageManager.UI;

public class DiePrerenderer : EditorWindow
{
    [MenuItem("Window/Dicer/DiePrerenderer")]
    public static void ShowWindow()
    {
        DiePrerenderer wnd = GetWindow<DiePrerenderer>();
        wnd.titleContent = new GUIContent("UI Die Prerenderer");
    }

    private string _name = string.Empty;
    
    private ProceduralDie _die;
    private ProceduralDie Die
    {
        get 
        { 
            return _die; 
        }
        set
        {
            if (value == _die)
            {
                return;
            }

            _die = value;
            _name = $"{Die.Generator.GetType().Name}_{Die.DieSize}_{Die.Material.name}";
        }
    }

    private int _textureSize = 256;

    public void OnGUI()
    {
        _name = EditorGUILayout.TextField("Name", _name);
        Die = (ProceduralDie)EditorGUILayout.ObjectField("Procedural die", Die, typeof(ProceduralDie), true);
        _textureSize = EditorGUILayout.IntField("Texture size", _textureSize);

        if (GUILayout.Button("Prerender die") && Application.isEditor)
        {
            GenerateUIDieData(Vector2Int.one * _textureSize);
        }
    }

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
        name: dieName + "_sides.png",
        prompt: "Select sprites path",
        badFileMessage: "Can't generate UIDieData - Sprites directory is not in the project",
        badFolderMessage: "Can't generate UIDieData - Sprites directory does not exist",
        fileAlreadyExistsMessage: "Can't generate UIDieData - Sprites are already generated"
    );

    private string GetAnimationPath(string dieName) => GetPath(
        name: dieName + "_anim.png",
        prompt: "Select animation data path",
        badFileMessage: "Can't generate UIDieData - Sprites directory is not in the project",
        badFolderMessage: "Can't generate UIDieData - Animation directory does not exist",
        fileAlreadyExistsMessage: "Can't generate UIDieData - Animation directory does not exist"
    );

    private string GetDieDataPath(string dieName) => GetPath(
        name: dieName + "_UIDieData.asset",
        prompt: "Select die data path",
        badFileMessage: "Can't generate UIDieData - Sprites directory is not in the project",
        badFolderMessage: "Can't generate UIDieData - UIDieData directory does not exist",
        fileAlreadyExistsMessage: "Can't generate UIDieData - UIDieData is already generated"
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

    private void GenerateSpritesheet(Vector2Int textureSize, string spritesheetPath, string spritesheetName, int rendersCount, Action<int> prepareOnIndex)
    {
        Texture2D[] sides = new Texture2D[rendersCount];
        var initialRotation = Die.transform.rotation;

        for (int index = 0; index < rendersCount; index++)
        {
            prepareOnIndex(index);

            var texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);
            var renderTexture = RenderTexture.GetTemporary(textureSize.x, textureSize.y);

            renderTexture.BeginPerspectiveRendering(60, Vector3.back * 2.5f, Quaternion.identity);

            renderTexture.DrawMesh(Die.Mesh, Die.Material, Matrix4x4.TRS(Vector3.zero, Die.transform.rotation, Vector3.one));

            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            renderTexture.EndRendering();
            sides[index] = texture;

            renderTexture.Release();
        }

        Die.transform.rotation = initialRotation;

        (Texture2D, Vector2Int) sidesSpriteSheet = PackToSpritesheet(sides, textureSize);
        File.WriteAllBytes(spritesheetPath, sidesSpriteSheet.Item1.EncodeToPNG());
        AssetDatabase.Refresh();

        ImportTexturesAsSprites(spritesheetPath, spritesheetName, sides.Length, textureSize, sidesSpriteSheet.Item2);
    }

    private void GenerateSidesSpritesheet(Vector2Int textureSize, string spritesheetPath, string spritesheetName) => GenerateSpritesheet(
        textureSize,
        spritesheetPath,
        spritesheetName,
        Die.Generator.ActualDieSize,
        (int index) =>
        {
            Die.transform.rotation = Quaternion.identity;
            Die.SideRotation(index, Vector3.up, Vector3.back);
        }
    );

    private void GenerateAnimationSpritesheet(Vector2Int textureSize, string spritesheetPath, string spritesheetName) => GenerateSpritesheet(
        textureSize,
        spritesheetPath,
        spritesheetName,
        60,
        (int index) =>
        {
            Die.transform.Rotate(UnityEngine.Random.insideUnitSphere * 180);
        }
    );

    private void GenerateUIDieData(Vector2Int textureSize)
    {
        if (Die == null || !Die.IsValid)
        {
            Debug.LogError("Can't generate UIDieData - Die is invalid");
            return;
        }

        Die.Generate();

        var spritesPath = GetSpritesPath(_name);
        string sidesSpritesheet = $"{spritesPath}/{_name}_sides.png";
        GenerateSidesSpritesheet(textureSize, sidesSpritesheet, $"{_name}_sides");

        var animationPath = GetAnimationPath(_name);
        string animationSpritesheet = $"{animationPath}/{_name}_anim.png";
        GenerateAnimationSpritesheet(textureSize, animationSpritesheet, $"{_name}_anim");

        var dieDataPath = GetDieDataPath(_name);

        var uiDieData = ScriptableObject.CreateInstance<UIDieData>();
        uiDieData.name = _name;

        List<Sprite> LoadSpritesheet(string spritesheetPath) => UnityEditor.AssetDatabase
            .LoadAllAssetsAtPath("Assets" + spritesheetPath.Substring(Application.dataPath.Length))
            .Where(o => o is Sprite)
            .Cast<Sprite>()
            .ToList();

        uiDieData.SidesTextures = LoadSpritesheet(sidesSpritesheet);
        uiDieData.AnimationFrames = LoadSpritesheet(animationSpritesheet);

        AssetDatabase.CreateAsset(uiDieData, $"Assets/{dieDataPath.Substring(Application.dataPath.Length)}/{_name}_UIDieData.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
