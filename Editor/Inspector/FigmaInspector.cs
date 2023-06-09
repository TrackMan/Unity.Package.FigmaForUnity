﻿using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using Trackman;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;

namespace Figma.Inspectors
{
    using Attributes;
    using global;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;

    [CustomEditor(typeof(Figma), true)]
    public class FigmaInspector : Editor
    {
        const string uiDocumentsOnlyIcon = "d_Refresh@2x";
        const string uiDocumentWithImagesIcon = "d_RawImage Icon";
        const string folderIcon = "d_Project";
        const int maxConcurrentRequests = 5;
        static readonly string[] propertiesToCut = new string[] { "componentProperties" };

        #region Fields
        SerializedProperty title;
        SerializedProperty filter;
        SerializedProperty reorder;
        SerializedProperty fontsDirs;

        UIDocument document;
        List<PackageInfo> packages = new();
        #endregion

        #region Properties
        IEnumerable<string> FontsDirs
        {
            get
            {
                foreach (SerializedProperty fontsDir in fontsDirs)
                    yield return fontsDir.stringValue;
            }
        }
        static string PAT
        {
            get => EditorPrefs.GetString("Figma/Editor/PAT", "");
            set => EditorPrefs.SetString("Figma/Editor/PAT", value);
        }
        #endregion

        #region Methods
        void OnEnable()
        {
            async void UpdatePackages()
            {
                ListRequest listRequest = Client.List(true, false);
                while (!listRequest.IsCompleted) await new WaitForUpdate();
                packages.Clear();
                packages.AddRange(listRequest.Result);
            }

            title = serializedObject.FindProperty("title");
            filter = serializedObject.FindProperty("filter");
            reorder = serializedObject.FindProperty("reorder");
            fontsDirs = serializedObject.FindProperty("fontsDirs");

            document = ((MonoBehaviour)target).GetComponent<UIDocument>();
            UpdatePackages();
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnPersonalAccessTokenGUI();
            OnAssetGUI();
            OnFigmaGUI();

            serializedObject.ApplyModifiedProperties();
        }

        void OnPersonalAccessTokenGUI()
        {
            if (PAT.NotNullOrEmpty())
            {
                GUILayout.BeginHorizontal();
                GUI.color = Color.green;
                EditorGUILayout.LabelField("Personal Access Token OK");
                GUI.color = Color.white;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button(new GUIContent("X", "Remove old PAT and enter new PAT"), GUILayout.Width(25), GUILayout.Height(25))) PAT = "";
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            else PAT = EditorGUILayout.TextField("Personal Access Token", PAT);
        }
        void OnAssetGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(title);

            VisualTreeAsset visualTreeAsset = document.visualTreeAsset;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Asset", visualTreeAsset, typeof(VisualTreeAsset), true);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            bool forceUpdate = default;
            bool downloadImages = false;

            if (GUILayout.Button(new GUIContent("Update UI", EditorGUIUtility.IconContent(uiDocumentsOnlyIcon).image), GUILayout.Height(20)) ||
                (downloadImages = GUILayout.Button(new GUIContent("Update UI & Images", EditorGUIUtility.IconContent(uiDocumentWithImagesIcon).image), GUILayout.Width(184), GUILayout.Height(20))) ||
                (forceUpdate = GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture(folderIcon)), GUILayout.Width(36))))
                Update(forceUpdate ? EditorUtility.DisplayDialog("Figma Updater", "Do you want to update images as well?", "Yes", "No") : downloadImages);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            void Update(bool downloadImages)
            {
                string assetPath = forceUpdate ? default : AssetDatabase.GetAssetPath(visualTreeAsset);
                string folder;
                string relativeFolder;

                if (assetPath.NullOrEmpty())
                {
                    assetPath = EditorUtility.SaveFilePanel("Save VisualTreeAsset", Application.dataPath, document.name, "uxml");
                    if (Path.GetFullPath(assetPath).StartsWith(Path.GetFullPath(Application.dataPath)))
                    {
                    }
                    else
                    {
                        PackageInfo packageInfo = packages.Find(x => Path.GetFullPath(assetPath).StartsWith($"{Path.GetFullPath(x.resolvedPath)}\\"));
                        assetPath = $"{packageInfo.assetPath}/{Path.GetFullPath(assetPath).Replace(Path.GetFullPath(packageInfo.resolvedPath), "")}";
                    }
                }

                if (assetPath.StartsWith("Packages"))
                {
                    PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetPath);
                    folder = $"{packageInfo.resolvedPath}{Path.GetDirectoryName(assetPath.Replace(packageInfo.assetPath, ""))}";
                    relativeFolder = Path.GetDirectoryName(assetPath);
                }
                else
                {
                    folder = Path.GetDirectoryName(assetPath);
                    relativeFolder = Path.GetRelativePath(Directory.GetCurrentDirectory(), folder);
                }

                if (folder.NotNullOrEmpty())
                {
                    UpdateTitle(document, (Figma)target, title.stringValue, folder, relativeFolder, Event.current.modifiers == EventModifiers.Control, downloadImages);
                }
            }
        }
        void OnFigmaGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(reorder, new GUIContent("De-root and Re-order Hierarchy"));
            EditorGUILayout.PropertyField(filter, new GUIContent("Filter by Path"));
            EditorGUILayout.PropertyField(fontsDirs, new GUIContent("Additional Fonts Directories"));
            EditorGUI.BeginDisabledGroup(true);

            if (document && document.visualTreeAsset)
            {
                foreach (MonoBehaviour element in document.GetComponentsInChildren<IRootElement>())
                {
                    Type elementType = element.GetType();
                    UxmlAttribute uxml = elementType.GetCustomAttribute<UxmlAttribute>();
                    if (uxml is not null)
                    {
                        EditorGUILayout.ObjectField(new GUIContent(uxml.Root), element, typeof(MonoBehaviour), true);
                        foreach (string root in uxml.Preserve)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PrefixLabel(root);
                            EditorGUILayout.LabelField($"Preserved by {elementType.Name}");
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Support Methods
        async void UpdateTitle(UIDocument document, Figma figma, string title, string folder, string relativeFolder, bool systemCopyBuffer, bool downloadImages)
        {
            if (!Directory.Exists(Path.Combine(folder, "Images"))) Directory.CreateDirectory(Path.Combine(folder, "Images"));
            if (!Directory.Exists(Path.Combine(folder, "Elements"))) Directory.CreateDirectory(Path.Combine(folder, "Elements"));

            string processName = $"Figma Update {figma.name}{(downloadImages ? " & Images" : string.Empty)}";

            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            int progress = Progress.Start(processName, default, Progress.Options.Managed);
            Progress.RegisterCancelCallback(progress, () =>
                    {
                        cancellationToken.Cancel();
                        return true;
                    });
            try
            {
                await UpdateTitleAsync(document, figma, progress, title, folder, relativeFolder, systemCopyBuffer, downloadImages, cancellationToken.Token);
            }
            catch (Exception exception)
            {
                Progress.Finish(progress, Progress.Status.Failed);

                if (!exception.Message.Contains("404") || (exception is not OperationCanceledException)) throw;
                Debug.LogException(exception);
            }
            finally
            {
                Progress.UnregisterCancelCallback(progress);
                NodeMetadata.Clear(document);
                cancellationToken.Dispose();
            }
        }
        async Task UpdateTitleAsync(UIDocument document, Figma figma, int progress, string title, string folder, string relativeFolder, bool systemCopyBuffer, bool downloadImages, CancellationToken token)
        {
            string remapsFilename = $"{folder}/remaps_{figma.name}.json";
            Dictionary<string, string> remaps = File.Exists(remapsFilename) ? JsonUtility.FromJson<Dictionary<string, string>>(File.ReadAllText(remapsFilename)) : new();
            Func<bool> cleanup = default;

            string CutJson(string json, string propertyToCut)
            {
                while (json.IndexOf(propertyToCut) > 0)
                {
                    bool touched = false;
                    int startIndex = json.IndexOf(propertyToCut);
                    int counter = 0;

                    for (int i = startIndex; i > 0; --i)
                    {
                        if (json[i] == ',')
                        {
                            startIndex = i;
                            break;
                        }
                    }
                    for (int i = startIndex; i < json.Length; ++i)
                    {
                        if (json[i] == '{')
                        {
                            touched = true;
                            ++counter;
                        }
                        else if (json[i] == '}') --counter;

                        if (touched && counter == 0)
                        {
                            json = json.Remove(startIndex, i - startIndex + 1);
                            break;
                        }
                    }
                }

                return json;
            }

            string GetFontPath(string name, string extension)
            {
                string localFontsPath = $"Fonts/{name}.{extension}";
                if (File.Exists(FileUtil.GetPhysicalPath($"{relativeFolder}/{localFontsPath}")))
                {
                    return localFontsPath;
                }

                foreach (string fontsDir in FontsDirs)
                {
                    string projectFontPath = $"{fontsDir}/{name}.{extension}";
                    if (File.Exists(FileUtil.GetPhysicalPath(projectFontPath)))
                    {
                        return $"/{projectFontPath}";
                    }
                }
                return default;
            }

            #region GetAssetPath, GetAssetSize
            (bool valid, string path) GetAssetPath(string name, string extension)
            {
                switch (extension)
                {
                    case "otf":
                    case "ttf":
                        string fontPath = GetFontPath(name, extension);
                        return (fontPath.NotNullOrEmpty(), $"{fontPath}");
                    case "asset":
                        string fontAssetPath = GetFontPath(name, extension);
                        return (fontAssetPath.NotNullOrEmpty(), $"{Path.GetDirectoryName(fontAssetPath)}/{name} SDF.{extension}");

                    case "png":
                    case "svg":
                        remaps.TryGetValue(name, out string mappedName);
                        string filename = $"Images/{mappedName ?? name}.{extension}";
                        return (File.Exists(Path.Combine(folder, filename)), filename);

                    default:
                        throw new NotSupportedException();
                }
            }
            (bool valid, int width, int height) GetAssetSize(string name, string extension)
            {
                (bool valid, string path) = GetAssetPath(name, extension);
                switch (extension)
                {
                    case "png":
                        if (valid)
                        {
                            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(Path.Combine(relativeFolder, path));
                            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                            return (valid, width, height);
                        }
                        else return (valid, -1, -1);

                    case "svg":
                        if (valid)
                        {
                            SVGImporter importer = (SVGImporter)AssetImporter.GetAtPath(Path.Combine(relativeFolder, path));
                            UnityEngine.Object vectorImage = AssetDatabase.LoadMainAssetAtPath(Path.Combine(relativeFolder, path));

                            if (vectorImage.GetType().GetField("size", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo fieldInfo)
                            {
                                Vector2 size = (Vector2)fieldInfo.GetValue(vectorImage);
                                return (valid, Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y));
                            }

                            return (valid, importer.TextureWidth == -1 ? -1 : importer.TextureWidth, importer.TextureHeight == -1 ? -1 : importer.TextureHeight);
                        }
                        else return (valid, -1, -1);

                    default:
                        throw new NotSupportedException();
                }
            }
            #endregion

            #region Getting Figma's data
            const string figmaTmpJson = "Temp/FigmaUI.json";
            Dictionary<string, string> headers = new() { { "X-FIGMA-TOKEN", PAT } };
            string json = default;

            Progress.Report(progress, 1, 4, "Downloading nodes");

            if (File.Exists(figmaTmpJson))
            {
                json = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(figmaTmpJson, token));
                Debug.LogWarning($"[FigmaInspector] Figma geometry data is loaded from {figmaTmpJson} file! If it is not intended, remove the file from the project!");
            }
            else
            {
                json = Encoding.UTF8.GetString(await $"https://api.figma.com/v1/files/{title}?geometry=paths".HttpGetAsync(headers, cancellationToken: token));
            }

            if (systemCopyBuffer) GUIUtility.systemCopyBuffer = json;
            if (!File.Exists(figmaTmpJson)) File.WriteAllText(figmaTmpJson, json);

            foreach (string propertyToCut in propertiesToCut) json = CutJson(json, propertyToCut);
            #endregion

            Files files = JsonUtility.FromJson<Files>(json);
            files.document.SetParentRecursively();

            MonoBehaviour[] elements = figma.GetComponentsInChildren<IRootElement>().Cast<MonoBehaviour>().ToArray();
            NodeMetadata.Initialize(document, figma, elements, files.document);
            FigmaParser parser = new FigmaParser(files.document, files.styles, GetAssetPath, GetAssetSize);

            Progress.Report(progress, 2, 4, "Downloading missing nodes");

            #region AddMissingComponents
            if (parser.MissingComponents.Count > 0)
            {
                Nodes nodes = JsonUtility.FromJson<Nodes>(await $"https://api.figma.com/v1/files/{title}/nodes?ids={string.Join(",", parser.MissingComponents.Distinct())}".HttpGetAsync(headers, cancellationToken: token));
                foreach (Nodes.Document value in nodes.nodes.Values.Where(value => value is not null))
                {
                    value.document.parent = files.document;
                    value.document.SetParentRecursively();
                    parser.AddMissingComponent(value.document, value.styles);
                }
            }
            #endregion

            #region Downloading images
            Progress.Report(progress, 3, 4, "Downloading images");
            if (downloadImages)
            {
                List<string> importPng = new();
                List<string> importGradient = new();
                List<string> requiredImages = new();
                List<(string path, int width, int height)> importSvg = new();

                void AddPngImport(string _, string path) => importPng.Add(path);
                void SaveRemaps(Dictionary<string, string> dictionary) => File.WriteAllText(remapsFilename, JsonUtility.ToJson(dictionary, prettyPrint: true));
                Func<KeyValuePair<string, string>, Task> DownloadMethodFor(string extension, Action<string, string> addForImport)
                {
                    HttpClient http = new();
                    foreach (KeyValuePair<string, string> header in headers) http.DefaultRequestHeaders.Add(header.Key, header.Value);

                    async Task GetAsync(KeyValuePair<string, string> urlByNodeID)
                    {
                        string nodeID = urlByNodeID.Key;
                        string url = urlByNodeID.Value;
                        (bool fileExists, string _) = GetAssetPath(nodeID, extension);

                        token.ThrowIfCancellationRequested();

                        Progress.SetStepLabel(progress, $"{url}");

                        if (fileExists && remaps.TryGetValue(nodeID, out string etag))
                            http.DefaultRequestHeaders.Add("If-None-Match", $"\"{etag}\"");
                        else
                            http.DefaultRequestHeaders.Remove("If-None-Match");

                        HttpResponseMessage response = await http.GetAsync(url, token);

                        if (response.Headers.TryGetValues("ETag", out IEnumerable<string> values))
                            remaps[nodeID] = values.First().Trim('"');

                        (bool _, string assetPath) = GetAssetPath(nodeID, extension);
                        string relativePath = Path.Combine(relativeFolder, assetPath).Replace('\\', '/');

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            File.WriteAllBytes(relativePath, await response.Content.ReadAsByteArrayAsync());
                            addForImport(nodeID, relativePath);
                        }

                        requiredImages.Add(relativePath);
                    }
                    return GetAsync;
                }

                string imagesPath = Path.Combine(folder, "Images");
                Directory.CreateDirectory(imagesPath);
                IEnumerable<string> existingPngs = Directory.GetFiles(imagesPath, "*.png").Select(x => x.Replace('\\', '/'));
                IEnumerable<string> existingSvg = Directory.GetFiles(imagesPath, "*.svg").Select(x => x.Replace('\\', '/'));

                Task fillsSyncTask = Task.CompletedTask;
                Task svgToPngSyncTask = Task.CompletedTask;
                Task svgSyncTask = Task.CompletedTask;

                bool Cleanup()
                {
                    Progress.SetDescription(progress, "Remove dangling images");

                    IEnumerable<string> existingImages = existingPngs.Concat(existingSvg);

                    foreach (string filename in existingImages.Select(Path.GetFileName).Except(requiredImages.Select(Path.GetFileName)))
                    {
                        string fullPath = Path.Combine(imagesPath, filename);

                        Debug.Log($"[FigmaInspector] Removing dangling file {fullPath}");

                        File.Delete(fullPath);
                        File.Delete($"{fullPath}.meta");

                        string value = Path.GetFileNameWithoutExtension(filename);
                        foreach (KeyValuePair<string, string> pair in remaps.Where(v => v.Value == value).ToArray())
                        {
                            remaps.Remove(pair.Key, out string _);
                        }
                    }

                    SaveRemaps(remaps);
                    return true;
                }

                cleanup = Cleanup;

                #region WriteImageFillNodes
                if (parser.ImageFillNodes.Any(x => x.ShouldDownload(UxmlDownloadImages.ImageFills)))
                {
                    Progress.SetDescription(progress, "Downloading image fills");

                    byte[] bytes = await $"https://api.figma.com/v1/files/{title}/images".HttpGetAsync(headers, cancellationToken: token);
                    Files.Images filesImages = JsonUtility.FromJson<Files.Images>(bytes);

                    IEnumerable<string> imageRefs = parser.ImageFillNodes.Where(x => x.ShouldDownload(UxmlDownloadImages.ImageFills)).Cast<GeometryMixin>().Select(y => y.fills.OfType<ImagePaint>().First().imageRef);
                    IEnumerable<KeyValuePair<string, string>> images = filesImages.meta.images.Where(item => imageRefs.Contains(item.Key));

                    fillsSyncTask = images.ForEachParallelAsync(maxConcurrentRequests, DownloadMethodFor("png", AddPngImport), token);
                }
                #endregion

                #region WritePngNodes
                if (parser.PngNodes.Any(x => x.ShouldDownload(UxmlDownloadImages.RenderAsPng)))
                {
                    Progress.SetDescription(progress, "Downloading png images");
                    int i = 0;
                    IEnumerable<IGrouping<int, string>> items = parser.PngNodes.Where(x => x.ShouldDownload(UxmlDownloadImages.RenderAsPng)).Select(y => y.id).GroupBy(_ => i++ / 100);
                    Task<byte[]>[] tasks = items.Select((group) => $"https://api.figma.com/v1/images/{title}?ids={string.Join(",", group)}&format=png".HttpGetAsync(headers, cancellationToken: token)).ToArray();
                    await Task.WhenAll(tasks);
                    IEnumerable<KeyValuePair<string, string>> images = tasks.SelectMany(t => JsonUtility.FromJson<Images>(t.Result).images);
                    svgToPngSyncTask = images.ForEachParallelAsync(maxConcurrentRequests, DownloadMethodFor("png", AddPngImport), token);
                }
                #endregion

                #region WriteSvgNodes
                if (parser.SvgNodes.Any(x => x.ShouldDownload(UxmlDownloadImages.RenderAsSvg)))
                {
                    void AddSvgImport(string id, string path)
                    {
                        BaseNode node = parser.SvgNodes.Find(x => x.id == id);
                        LayoutMixin layout = node as LayoutMixin;
                        importSvg.Add((path, (int)layout.absoluteBoundingBox.width, (int)layout.absoluteBoundingBox.height));
                    }

                    Progress.SetDescription(progress, "Downloading svg images");
                    int i = 0;

                    IEnumerable<IGrouping<int, BaseNode>> nodesGroups = parser.SvgNodes.Where(x => x.ShouldDownload(UxmlDownloadImages.RenderAsSvg)).GroupBy(_ => i++ / 100);
                    Task<byte[]>[] tasks = nodesGroups.Select((nodes) => $"https://api.figma.com/v1/images/{title}?ids={string.Join(",", nodes.Select(x => x.id))}&format=svg".HttpGetAsync(headers, cancellationToken: token)).ToArray();
                    await Task.WhenAll(tasks);
                    IEnumerable<KeyValuePair<string, string>> images = tasks.SelectMany(t => JsonUtility.FromJson<Images>(t.Result).images);
                    svgSyncTask = images.ForEachParallelAsync(maxConcurrentRequests, DownloadMethodFor("svg", AddSvgImport), token);
                }
                #endregion

                try
                {
                    AssetDatabase.StartAssetEditing();
                    await Task.WhenAll(new[] { fillsSyncTask, svgToPngSyncTask, svgSyncTask });

                    Progress.SetDescription(progress, "Write Gradients");
                    foreach (KeyValuePair<string, GradientPaint> keyValue in parser.Gradients)
                    {
                        CultureInfo defaultCulture = CultureInfo.GetCultureInfo("en-US");
                        GradientPaint gradient = keyValue.Value;
                        XmlWriter writer = XmlWriter.Create(Path.Combine(folder, $"{GetAssetPath(keyValue.Key, "svg").path}"), new XmlWriterSettings() { Indent = true, NewLineOnAttributes = true, IndentChars = "    " });
                        writer.WriteStartElement("svg");
                        {
                            writer.WriteStartElement("defs");
                            {
                                switch (gradient.type)
                                {
                                    case PaintType.GRADIENT_LINEAR:
                                        writer.WriteStartElement("linearGradient");
                                        writer.WriteAttributeString("id", "gradient");
                                        for (int i = 0; i < Mathf.Max(gradient.gradientHandlePositions.Length, 2); ++i)
                                        {
                                            writer.WriteAttributeString($"x{i + 1}", $"{gradient.gradientHandlePositions[i].x.ToString("F2", defaultCulture)}");
                                            writer.WriteAttributeString($"y{i + 1}", $"{gradient.gradientHandlePositions[i].y.ToString("F2", defaultCulture)}");
                                        }
                                        break;

                                    case PaintType.GRADIENT_RADIAL:
                                    case PaintType.GRADIENT_DIAMOND:
                                        writer.WriteStartElement("radialGradient");
                                        writer.WriteAttributeString("id", "gradient");
                                        writer.WriteAttributeString("fx", $"{gradient.gradientHandlePositions[0].x.ToString("F2", defaultCulture)}");
                                        writer.WriteAttributeString("fy", $"{gradient.gradientHandlePositions[0].y.ToString("F2", defaultCulture)}");
                                        writer.WriteAttributeString("cx", $"{gradient.gradientHandlePositions[0].x.ToString("F2", defaultCulture)}");
                                        writer.WriteAttributeString("cy", $"{gradient.gradientHandlePositions[0].y.ToString("F2", defaultCulture)}");

                                        float radius = Vector2.Distance(new Vector2((float)gradient.gradientHandlePositions[1].x, (float)gradient.gradientHandlePositions[1].y), new Vector2((float)gradient.gradientHandlePositions[0].x, (float)gradient.gradientHandlePositions[0].y));
                                        writer.WriteAttributeString("r", $"{radius.ToString("F2", defaultCulture)}");
                                        break;

                                    default:
                                        throw new NotSupportedException();
                                }

                                foreach (ColorStop stop in gradient.gradientStops)
                                {
                                    writer.WriteStartElement("stop");
                                    writer.WriteAttributeString("offset", $"{stop.position.ToString("F2", defaultCulture)}");
                                    writer.WriteAttributeString("style", $"stop-color:rgb({(byte)(stop.color.r * 255)},{(byte)(stop.color.g * 255)},{(byte)(stop.color.b * 255)});stop-opacity:{stop.color.a.ToString("F2", defaultCulture)}");
                                    writer.WriteEndElement();
                                }

                                writer.WriteEndElement();
                            }

                            writer.WriteEndElement();

                            writer.WriteStartElement("rect");
                            writer.WriteAttributeString("width", "100");
                            writer.WriteAttributeString("height", "100");
                            writer.WriteAttributeString("fill", "url(#gradient)");
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                        writer.Close();

                        string relativePath = Path.Combine(relativeFolder, $"{GetAssetPath(keyValue.Key, "svg").path}").Replace('\\', '/');

                        importGradient.Add(relativePath);
                        requiredImages.Add(relativePath);
                    }
                    #endregion

                }
                finally
                {
                    SaveRemaps(remaps);
                    Progress.SetStepLabel(progress, "");
                    AssetDatabase.StopAssetEditing();
                }

                #region WriteGradients
                AssetDatabase.ImportAsset(Path.Combine(relativeFolder, "Images"), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);

                Progress.SetDescription(progress, "Importing png...");
                foreach (string relativePath in importPng)
                {
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(relativePath);
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.mipmapEnabled = false;
                    EditorUtility.SetDirty(importer);
                }

                Progress.SetDescription(progress, "Importing svg...");
                foreach ((string path, int width, int height) value in importSvg)
                {
                    SVGImporter importer = (SVGImporter)AssetImporter.GetAtPath(value.path);
#if VECTOR_GRAPHICS_RASTER
                    importer.SvgType = SVGType.Texture2D;
                    importer.KeepTextureAspectRatio = false;
                    importer.TextureWidth = Mathf.CeilToInt(value.width);
                    importer.TextureHeight = Mathf.CeilToInt(value.height);
                    importer.SampleCount = 8;
#else
                    importer.SvgType = SVGType.UIToolkit;
#endif
                    EditorUtility.SetDirty(importer);
                }

                Progress.SetDescription(progress, "Importing gradients...");
                foreach (string path in importGradient)
                {
                    SVGImporter importer = (SVGImporter)AssetImporter.GetAtPath(path);
                    importer.SvgType = SVGType.UIToolkit;
                    EditorUtility.SetDirty(importer);
                }

                AssetDatabase.ImportAsset(Path.Combine(relativeFolder, "Images"), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
            }
            #endregion

            try
            {
                AssetDatabase.StartAssetEditing();
                Progress.Report(progress, 4, 4, "Updating uss/uxml files");

                foreach (string path in Directory.GetFiles(Path.Combine(folder, "Elements"), "*.uxml")) File.Delete(path);

                parser.Run();
                parser.Write(folder, figma.name);

                cleanup?.Invoke();
                File.Delete(figmaTmpJson);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Progress.Finish(progress);
            Debug.Log($"Figma Update {figma.name} OK");
        }
        #endregion
    }
}