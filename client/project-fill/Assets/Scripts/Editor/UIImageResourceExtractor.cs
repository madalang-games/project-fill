using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class UIImageResourceExtractor : EditorWindow
{
    const string DefaultOutputFolder = "Assets/Sprites/Extracted";
    const int PreviewSize = 96;

    readonly List<SourceImage> sourceImages = new();
    readonly List<ExtractedResource> resources = new();
    string outputFolder = DefaultOutputFolder;
    string baseFileName = "ui_resource";
    string status = "No image added.";
    Vector2 windowScroll;
    int alphaThreshold = 8;
    int minPixelArea = 16;
    int padding = 1;
    int pixelsPerUnit = 100;

    sealed class SourceImage
    {
        public Texture2D Texture { get; set; }
        public string Path { get; set; }
        public string DisplayName { get; set; }
        public bool External { get; set; }
        public int Order { get; set; }
        public int ResourceCount { get; set; }
    }

    sealed class ExtractedResource
    {
        public SourceImage Source { get; set; }
        public RectInt Bounds { get; set; }
        public Texture2D Texture { get; set; }
        public int PixelArea { get; set; }
    }

    [MenuItem("Tools/UI/Image Resource Extractor")]
    public static void Open()
    {
        GetWindow<UIImageResourceExtractor>("UI Resource Extractor");
    }

    void OnDisable()
    {
        ClearAll();
    }

    void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
        try
        {
            EditorGUILayout.LabelField("Sources", EditorStyles.boldLabel);
            DrawAddButtons();
            DrawDropArea();
            DrawSourceList();

            using (new EditorGUI.DisabledScope(sourceImages.Count == 0))
            {
                if (GUILayout.Button($"Parse {sourceImages.Count} Image(s)"))
                    ParseAllSources();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Parsing", EditorStyles.boldLabel);
            alphaThreshold = EditorGUILayout.IntSlider("Alpha Threshold", alphaThreshold, 0, 255);
            minPixelArea = EditorGUILayout.IntField("Min Pixel Area", Mathf.Max(1, minPixelArea));
            padding = EditorGUILayout.IntSlider("Padding", padding, 0, 16);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                outputFolder = EditorGUILayout.TextField("Folder", outputFolder);
                if (GUILayout.Button("Select", GUILayout.Width(72f)))
                    SelectOutputFolder();
            }

            baseFileName = EditorGUILayout.TextField("Base File Name", baseFileName);
            pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", Mathf.Max(1, pixelsPerUnit));

            using (new EditorGUI.DisabledScope(resources.Count == 0))
            {
                if (GUILayout.Button($"Save {resources.Count} Sprite(s)"))
                    SaveResources();
            }

            EditorGUILayout.HelpBox(status, MessageType.Info);
            DrawPreview();
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    void DrawAddButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Selected Assets"))
                AddSelectedTextureAssets();

            if (GUILayout.Button("Add PNG/JPG"))
                AddImageFile();

            if (GUILayout.Button("Add Folder Images"))
                AddFolderImages();

            using (new EditorGUI.DisabledScope(sourceImages.Count == 0))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(72f)))
                    ClearAll();
            }
        }
    }

    void DrawDropArea()
    {
        Rect rect = GUILayoutUtility.GetRect(0f, 56f, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "Drag Texture assets, PNG/JPG files, or folders here");

        Event current = Event.current;
        if (!rect.Contains(current.mousePosition))
            return;

        if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (current.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            int added = AddDraggedItems();
            status = added == 0 ? "No supported images were added." : $"Added {added} image(s). Click Parse.";
        }

        current.Use();
    }

    void DrawSourceList()
    {
        if (sourceImages.Count == 0)
            return;

        for (int i = 0; i < sourceImages.Count; i++)
        {
            SourceImage source = sourceImages[i];
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Rect previewRect = GUILayoutUtility.GetRect(48f, 48f, GUILayout.Width(48f), GUILayout.Height(48f));
                EditorGUI.DrawTextureTransparent(previewRect, source.Texture, ScaleMode.ScaleToFit);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(source.DisplayName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{source.Texture.width}x{source.Texture.height} / parsed: {source.ResourceCount}");
                }

                if (GUILayout.Button("Remove", GUILayout.Width(72f)))
                {
                    RemoveSourceAt(i);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    void AddSelectedTextureAssets()
    {
        int added = 0;
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            if (selected is Texture2D texture)
                added += AddSource(texture, AssetDatabase.GetAssetPath(texture), false) ? 1 : 0;
        }

        status = added == 0 ? "No selected Texture2D assets were added." : $"Added {added} image(s). Click Parse.";
    }

    void AddImageFile()
    {
        string path = EditorUtility.OpenFilePanel("Select UI image", Application.dataPath, "png,jpg,jpeg");
        if (string.IsNullOrEmpty(path))
            return;

        status = AddImagePath(path) ? "Added 1 image. Click Parse." : "Failed to add image.";
    }

    void AddFolderImages()
    {
        string path = EditorUtility.OpenFolderPanel("Select image folder", Application.dataPath, string.Empty);
        if (string.IsNullOrEmpty(path))
            return;

        int added = AddImageFolder(path);
        status = added == 0 ? "No supported images were added." : $"Added {added} image(s). Click Parse.";
    }

    int AddDraggedItems()
    {
        int added = 0;
        foreach (UnityEngine.Object reference in DragAndDrop.objectReferences)
        {
            if (reference is Texture2D texture)
                added += AddSource(texture, AssetDatabase.GetAssetPath(texture), false) ? 1 : 0;
        }

        foreach (string path in DragAndDrop.paths)
        {
            if (Directory.Exists(path))
                added += AddImageFolder(path);
            else
                added += AddImagePath(path) ? 1 : 0;
        }

        return added;
    }

    bool AddImagePath(string path)
    {
        if (!IsSupportedImagePath(path))
            return false;

        string assetPath = ToProjectAssetPath(path);
        if (!string.IsNullOrEmpty(assetPath))
        {
            Texture2D assetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (assetTexture != null)
                return AddSource(assetTexture, assetPath, false);
        }

        byte[] bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = Path.GetFileNameWithoutExtension(path),
            hideFlags = HideFlags.HideAndDontSave
        };

        if (!ImageConversion.LoadImage(texture, bytes))
        {
            DestroyImmediate(texture);
            return false;
        }

        return AddSource(texture, path, true);
    }

    int AddImageFolder(string path)
    {
        int added = 0;
        foreach (string filePath in Directory.GetFiles(path))
            added += AddImagePath(filePath) ? 1 : 0;

        return added;
    }

    bool AddSource(Texture2D texture, string path, bool external)
    {
        if (texture == null)
            return false;

        for (int i = 0; i < sourceImages.Count; i++)
        {
            SourceImage existing = sourceImages[i];
            bool sameSource = string.IsNullOrEmpty(path) || string.IsNullOrEmpty(existing.Path)
                ? existing.Texture == texture
                : string.Equals(NormalizePath(existing.Path), NormalizePath(path), StringComparison.OrdinalIgnoreCase);

            if (!sameSource)
                continue;

            if (external)
                DestroyImmediate(texture);
            return false;
        }

        sourceImages.Add(new SourceImage
        {
            Texture = texture,
            Path = path ?? string.Empty,
            DisplayName = string.IsNullOrEmpty(path) ? texture.name : Path.GetFileNameWithoutExtension(path),
            External = external,
            Order = sourceImages.Count
        });
        ClearExtractedResources();
        return true;
    }

    void ParseAllSources()
    {
        ClearExtractedResources();
        int total = 0;

        foreach (SourceImage source in sourceImages)
        {
            Texture2D readable = CreateReadableCopy(source.Texture);
            readable.name = $"{source.Texture.name}_readable";
            readable.hideFlags = HideFlags.HideAndDontSave;

            List<ExtractedResource> extracted = ExtractResources(readable, source);
            extracted.Sort(CompareResourceOrder);
            source.ResourceCount = extracted.Count;
            resources.AddRange(extracted);
            total += extracted.Count;

            DestroyImmediate(readable);
        }

        status = total == 0
            ? "No resource elements found. Lower Alpha Threshold or Min Pixel Area, then Parse again."
            : $"Parsed {total} resource element(s) from {sourceImages.Count} image(s).";
    }

    void DrawPreview()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Parsed Sprites", EditorStyles.boldLabel);

        if (resources.Count == 0)
            return;

        for (int i = 0; i < resources.Count; i++)
        {
            ExtractedResource resource = resources[i];
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
                EditorGUI.DrawTextureTransparent(previewRect, resource.Texture, ScaleMode.ScaleToFit);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField($"{SanitizeFileName(baseFileName)}_{i + 1}.png", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Source: {resource.Source.DisplayName}");
                    EditorGUILayout.LabelField($"Bounds: x={resource.Bounds.x}, y={resource.Bounds.y}, w={resource.Bounds.width}, h={resource.Bounds.height}");
                    EditorGUILayout.LabelField($"Pixels: {resource.PixelArea}");
                }
            }
        }
    }

    void SaveResources()
    {
        string cleanBaseName = SanitizeFileName(baseFileName);
        if (string.IsNullOrEmpty(cleanBaseName))
        {
            status = "Base File Name is empty or invalid.";
            return;
        }

        string absoluteFolder = ResolveOutputFolder(outputFolder);
        if (string.IsNullOrEmpty(absoluteFolder))
        {
            status = "Output folder is invalid.";
            return;
        }

        Directory.CreateDirectory(absoluteFolder);
        var importedAssetPaths = new List<string>();

        for (int i = 0; i < resources.Count; i++)
        {
            string fileName = $"{cleanBaseName}_{i + 1}.png";
            string absolutePath = Path.Combine(absoluteFolder, fileName);
            File.WriteAllBytes(absolutePath, resources[i].Texture.EncodeToPNG());

            string assetPath = ToProjectAssetPath(absolutePath);
            if (!string.IsNullOrEmpty(assetPath))
                importedAssetPaths.Add(assetPath);
        }

        AssetDatabase.Refresh();
        foreach (string assetPath in importedAssetPaths)
            ConfigurePngAsSprite(assetPath, pixelsPerUnit);

        status = $"Saved {resources.Count} sprite file(s) to {NormalizePath(absoluteFolder)}.";
    }

    static Texture2D CreateReadableCopy(Texture2D texture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(texture, renderTexture);
        RenderTexture.active = renderTexture;

        var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
        copy.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        return copy;
    }

    List<ExtractedResource> ExtractResources(Texture2D texture, SourceImage source)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];
        var extracted = new List<ExtractedResource>();
        var queue = new Queue<int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (visited[index] || pixels[index].a <= alphaThreshold)
                    continue;

                RectInt bounds = FloodFillComponent(x, y, width, height, pixels, visited, queue, out int area);
                if (area < minPixelArea)
                    continue;

                bounds = AddPadding(bounds, width, height, padding);
                extracted.Add(new ExtractedResource
                {
                    Source = source,
                    Bounds = bounds,
                    PixelArea = area,
                    Texture = CropTexture(texture, pixels, bounds)
                });
            }
        }

        return extracted;
    }

    RectInt FloodFillComponent(int startX, int startY, int width, int height, Color32[] pixels, bool[] visited, Queue<int> queue, out int area)
    {
        int minX = startX, maxX = startX, minY = startY, maxY = startY;
        area = 0;

        queue.Clear();
        EnqueueOpaque(startX, startY, width, pixels, visited, queue);

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;
            area++;
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);

            if (x > 0) TryEnqueueOpaque(x - 1, y, width, pixels, visited, queue);
            if (x < width - 1) TryEnqueueOpaque(x + 1, y, width, pixels, visited, queue);
            if (y > 0) TryEnqueueOpaque(x, y - 1, width, pixels, visited, queue);
            if (y < height - 1) TryEnqueueOpaque(x, y + 1, width, pixels, visited, queue);
        }

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    void TryEnqueueOpaque(int x, int y, int width, Color32[] pixels, bool[] visited, Queue<int> queue)
    {
        int index = y * width + x;
        if (visited[index] || pixels[index].a <= alphaThreshold) return;
        EnqueueOpaque(x, y, width, pixels, visited, queue);
    }

    static void EnqueueOpaque(int x, int y, int width, Color32[] pixels, bool[] visited, Queue<int> queue)
    {
        int index = y * width + x;
        visited[index] = true;
        queue.Enqueue(index);
    }

    static RectInt AddPadding(RectInt bounds, int width, int height, int amount)
    {
        int xMin = Mathf.Max(0, bounds.xMin - amount);
        int yMin = Mathf.Max(0, bounds.yMin - amount);
        int xMax = Mathf.Min(width, bounds.xMax + amount);
        int yMax = Mathf.Min(height, bounds.yMax + amount);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    static Texture2D CropTexture(Texture2D source, Color32[] sourcePixels, RectInt bounds)
    {
        var texture = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false)
        {
            name = $"{source.name}_{bounds.x}_{bounds.y}",
            hideFlags = HideFlags.HideAndDontSave
        };

        var cropped = new Color32[bounds.width * bounds.height];
        for (int y = 0; y < bounds.height; y++)
        {
            int sourceOffset = (bounds.y + y) * source.width + bounds.x;
            int targetOffset = y * bounds.width;
            Array.Copy(sourcePixels, sourceOffset, cropped, targetOffset, bounds.width);
        }

        texture.SetPixels32(cropped);
        texture.Apply();
        return texture;
    }

    static int CompareResourceOrder(ExtractedResource left, ExtractedResource right)
    {
        int sourceCompare = left.Source.Order.CompareTo(right.Source.Order);
        if (sourceCompare != 0) return sourceCompare;
        int topCompare = right.Bounds.yMax.CompareTo(left.Bounds.yMax);
        return topCompare != 0 ? topCompare : left.Bounds.xMin.CompareTo(right.Bounds.xMin);
    }

    static void ConfigurePngAsSprite(string assetPath, int ppu)
    {
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    void SelectOutputFolder()
    {
        string initialFolder = ResolveOutputFolder(outputFolder);
        if (string.IsNullOrEmpty(initialFolder) || !Directory.Exists(initialFolder))
            initialFolder = Application.dataPath;

        string selected = EditorUtility.OpenFolderPanel("Select output folder", initialFolder, string.Empty);
        if (string.IsNullOrEmpty(selected)) return;

        outputFolder = ToProjectAssetPath(selected);
        if (string.IsNullOrEmpty(outputFolder))
            outputFolder = NormalizePath(selected);
    }

    void RemoveSourceAt(int index)
    {
        SourceImage source = sourceImages[index];
        if (source.External && source.Texture != null)
            DestroyImmediate(source.Texture);

        sourceImages.RemoveAt(index);
        ClearExtractedResources();
        status = sourceImages.Count == 0 ? "No image added." : $"Removed source. {sourceImages.Count} image(s) remain.";
    }

    void ClearAll()
    {
        ClearExtractedResources();
        for (int i = 0; i < sourceImages.Count; i++)
        {
            if (sourceImages[i].External && sourceImages[i].Texture != null)
                DestroyImmediate(sourceImages[i].Texture);
        }

        sourceImages.Clear();
        status = "No image added.";
    }

    void ClearExtractedResources()
    {
        for (int i = 0; i < resources.Count; i++)
        {
            if (resources[i].Texture != null)
                DestroyImmediate(resources[i].Texture);
        }

        resources.Clear();
        for (int i = 0; i < sourceImages.Count; i++)
            sourceImages[i].ResourceCount = 0;
    }

    static bool IsSupportedImagePath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
    }

    static string ResolveOutputFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return string.Empty;
        string normalized = folder.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalized)) return Path.GetFullPath(normalized);
        return Path.GetFullPath(Path.Combine(GetProjectRoot(), normalized));
    }

    static string ToProjectAssetPath(string absolutePath)
    {
        string projectRoot = NormalizePath(GetProjectRoot());
        string normalizedPath = NormalizePath(Path.GetFullPath(absolutePath));
        if (!normalizedPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        return normalizedPath.Substring(projectRoot.Length + 1);
    }

    static string GetProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    static string NormalizePath(string path) => path.Replace('\\', '/');

    static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;
        string cleanName = fileName.Trim();
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            cleanName = cleanName.Replace(invalidChar.ToString(), string.Empty);
        return cleanName;
    }
}
