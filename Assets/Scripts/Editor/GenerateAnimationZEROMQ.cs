using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using NetMQ;
using NetMQ.Sockets;
using System.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.CodeDom;

public class GenerateAnimationZEROMQ : EditorWindow
{
    #region Variables
    [Serializable]
    private class JsonMessage
    {
        public string prompt;
        public string model;
        public string output_dir;
        public bool use_smplify;
        public int iterations;
    }

    private string promptText = "Enter the prompt text here";
    private bool shouldUseSMPLify = false;
    private int iterations = 100;
    //private List<AnimationClip> generatedClips;
    private int selectedModelIndex = 0;
    private int selectedConvertingIndex = 0;
    private string[] models = new string[] { "GMD", "MDM", "MoMask" };
    private string[] convertingOptions = new string[] { "SMPLify", "IK Solver" };
    private string pythonPath = "C:/Users/Ciro/AppData/Local/Programs/Python/Python310/python.exe";
    private string outputDir = "";
    private string pythonServerPath = "C:/Users/Ciro/Desktop/Tesi/MasterThesis/Assets/Scripts/PythonScripts";
    private RequestSocket client;
    private bool isGenerating = false;
    private Process pythonServerProcess;
    private string processResponse = "";
    private float startTime = 0f;
    private string elapsedTimeText = "00:00";
    private CancellationTokenSource cancellationTokenSource;
    private bool showPaths = false;
    #endregion

    [MenuItem("Window/Generate Animation ZEROMQ")]
    public static void ShowWindow()
    {
        GenerateAnimationZEROMQ window = (GenerateAnimationZEROMQ)EditorWindow.GetWindow(typeof(GenerateAnimationZEROMQ));
        window.minSize = new Vector2(400, 600);
        window.maxSize = new Vector2(400, 600);
        GetWindow<GenerateAnimationZEROMQ>("Generate Animation");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate Animation", EditorStyles.boldLabel);
        GUILayout.Label("Insert prompt:", EditorStyles.boldLabel);
        promptText = GUILayout.TextArea(promptText, GUILayout.Height(100));

        showPaths = EditorGUILayout.Foldout(showPaths, "Working Paths");
        if (showPaths)
        {
            // add space
            PathGUI.OpenFileField("Python Path", ref pythonPath);
            PathGUI.OpenFolderField("Server Path", ref pythonServerPath);
        }

        GUILayout.Space(10);
        EditorGUILayout.PrefixLabel("Converting Method", EditorStyles.boldLabel);
        selectedConvertingIndex = EditorGUILayout.Popup(selectedConvertingIndex, convertingOptions);
        if (selectedConvertingIndex == 0)
        {
            shouldUseSMPLify = true;
        }
        else
        {
            shouldUseSMPLify = false;
            EditorGUILayout.PrefixLabel("Iterations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The number of iterations is the number of times the IK solver will be run to convert the animation. The higher the number, the more accurate the animation will be but it will take longer to generate.", MessageType.Info);
            iterations = EditorGUILayout.IntSlider(iterations, 1, 100);
        }

        GUILayout.Space(10);
        GUILayout.Label("Select Model", EditorStyles.boldLabel);
        selectedModelIndex = EditorGUILayout.Popup(selectedModelIndex, models);

        if (isGenerating)
        {
            if (GUILayout.Button("STOP"))
            {
                cancellationTokenSource.Cancel();
                TerminatePythonServer();
                isGenerating = false;
                EditorApplication.update -= UpdateElapsedTime;
                EditorApplication.update -= OnTaskCompleted;
                UnityEngine.Debug.Log("Animation generation stopped!");
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Elapsed Time: " + elapsedTimeText, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Space(5);
            if (GUILayout.Button("Generate"))
            {
                UsefulShortcuts.ClearConsole();
                StartPythonServer();
                isGenerating = true;
                startTime = Time.realtimeSinceStartup;
                elapsedTimeText = "00:00";
                cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => Generate(promptText, cancellationTokenSource.Token)).ContinueWith(t => EditorApplication.update += OnTaskCompleted);
                EditorApplication.update += UpdateElapsedTime;
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Elapsed Time: " + elapsedTimeText, EditorStyles.boldLabel);
            if (processResponse != "")
            {
                UnityEngine.Debug.Log("Animation generation completed with response: " + processResponse);
                processResponse = "";
            }
            GUILayout.EndHorizontal();
        }
    }

    private void UpdateElapsedTime()
    {
        if (isGenerating)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            TimeSpan timeSpan = TimeSpan.FromSeconds(elapsed);
            elapsedTimeText = timeSpan.ToString(@"mm\:ss");
            //Repaint(); // Uncomment this line if the GUI is not updating but it may cause slowdowns
        }
        else
        {
            EditorApplication.update -= UpdateElapsedTime;
        }
    }

    private void OnTaskCompleted()
    {
        EditorApplication.update -= OnTaskCompleted;
        EditorApplication.update -= UpdateElapsedTime;
        try
        {
            AssetDatabase.Refresh();

            string newDir = GetNewDirectory(outputDir, promptText);
            string[] files = Directory.GetFiles(newDir, "*.fbx", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath(file) as ModelImporter;
                if (modelImporter != null)
                {
                    ConfigureModelImporter(modelImporter, file);
                }
                else
                {
                    UnityEngine.Debug.LogError("ModelImporter is null for: " + file);
                }
            }
            DeleteAll(newDir);
            isGenerating = false;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.Message);
        }
    }

    private void StartPythonServer()
    {
        var workingDirectory = pythonServerPath;
        UnityEngine.Debug.Log("Working directory: " + workingDirectory);
        pythonServerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = @"zeromq_server.py",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            }
        };

        pythonServerProcess.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log("Server: " + args.Data);
        pythonServerProcess.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogWarning("Server Warning: " + args.Data);

        pythonServerProcess.Start();
        pythonServerProcess.BeginOutputReadLine();
        pythonServerProcess.BeginErrorReadLine();
    }

    private void TerminatePythonServer()
    {
        if (pythonServerProcess != null && !pythonServerProcess.HasExited)
        {
            UnityEngine.Debug.Log("Terminating Python server...");
            pythonServerProcess.Kill();
            pythonServerProcess.Dispose();
            pythonServerProcess = null;
        }
    }

    private void Generate(string promptText, CancellationToken cancellationToken)
    {
        //generatedClips = new List<AnimationClip>();
        outputDir = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(outputDir))
        {
            outputDir = Directory.CreateDirectory(outputDir).FullName;
            UnityEngine.Debug.Log("Resources directory created: " + outputDir);
        }
        string selectedModel = models[selectedModelIndex];
        AsyncIO.ForceDotNet.Force();
        client = new RequestSocket("tcp://localhost:5554");

        try
        {
            var messageObj = new JsonMessage
            {
                prompt = promptText,
                model = selectedModel,
                output_dir = outputDir,
                use_smplify = shouldUseSMPLify,
                iterations = iterations
            };

            var message = JsonUtility.ToJson(messageObj);
            UnityEngine.Debug.Log("Generating animation for: " + promptText + ". Please wait...");
            client.SendFrame(message);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (client.TryReceiveFrameString(TimeSpan.FromSeconds(1), out string response))
                {
                    EditorApplication.delayCall += () => UsefulShortcuts.ClearConsole();
                    processResponse = response;
                    break;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                UnityEngine.Debug.LogWarning("Animation generation was cancelled.");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Exception: " + ex.Message);
        }
        finally
        {
            if (client != null)
            {
                client.Close();
                ((IDisposable)client).Dispose();
                NetMQConfig.Cleanup();
            }
            TerminatePythonServer();
        }
    }

    private string GetNewDirectory(string outputDir, string promptText)
    {
        string newDir = outputDir.Substring(outputDir.IndexOf("Assets"));
        return newDir + "\\" + promptText.Replace(" ", "_");
    }

    private void DeleteAll(string path)
    {
        string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        string[] dirs = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            if (!file.Contains(".anim") && !file.Contains(".fbx"))
            {
                UnityEngine.Debug.Log("Deleting file: " + file);
                File.Delete(file);
            }
        }
        foreach (string dir in dirs)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
        AssetDatabase.Refresh();
    }

    private void GenerateClips(string assetPath)
    {
        ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (modelImporter != null)
        {
            ConfigureModelImporter(modelImporter, assetPath);
            //CreateAnimationClip(assetPath, modelImporter);
        }
        else
        {
            UnityEngine.Debug.LogError("ModelImporter is null for: " + assetPath);
        }
    }

    private void ConfigureModelImporter(ModelImporter modelImporter, string assetPath)
    {
        modelImporter.animationType = ModelImporterAnimationType.Human;

        SerializedObject so = new SerializedObject(modelImporter);
        so.ApplyModifiedProperties();
        AssetDatabase.ImportAsset(modelImporter.assetPath, ImportAssetOptions.ForceUpdate);
    }

    // private void CreateAnimationClip(string assetPath, ModelImporter modelImporter)
    // {
    //     AnimationClip origClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(assetPath, typeof(AnimationClip));
    //     AnimationClip newClip = new AnimationClip();
    //     EditorUtility.CopySerialized(origClip, newClip);
    //     AssetDatabase.CreateAsset(newClip, assetPath.Replace(".fbx", "") + ".anim");
    //     AssetDatabase.Refresh();

    //     if (generatedClips != null)
    //     {
    //         generatedClips.Add(newClip);
    //     }
    // }
}