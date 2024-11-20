using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using NetMQ;
using NetMQ.Sockets;
using System.Diagnostics;
using System;
using System.Threading.Tasks;


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
    }

    private string promptText = "Enter the prompt text here";
    private bool shouldUseSMPLify = true;
    private List<AnimationClip> generatedClips;
    private int selectedModelIndex = 0;
    private string[] models = new string[] { "GMD", "MDM", "MoMask" };
    private string pythonPath = "C:\\Users\\Ciro\\AppData\\Local\\Programs\\Python\\Python310\\python.exe";
    private string outputDir = "C:\\Users\\Ciro\\Desktop\\UnityProjects\\MasterThesisProject\\Assets\\Resources";
    private RequestSocket client;
    private bool isGenerating = false;
    private Process pythonServerProcess;
    private string processResponse;
    private float startTime = 0f;
    private string elapsedTimeText = "00:00";

    #endregion

    [MenuItem("Window/Generate Animation ZEROMQ")]
    public static void ShowWindow()
    {
        GenerateAnimationZEROMQ window = (GenerateAnimationZEROMQ)EditorWindow.GetWindow(typeof(GenerateAnimationZEROMQ));
        window.minSize = new Vector2(400, 600);
        window.maxSize = new Vector2(400, 600);
        GetWindow<GenerateAnimationZEROMQ>("Generate Animation");
    }

    private void OnEnable()
    {
        generatedClips = new List<AnimationClip>();
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate Animation", EditorStyles.boldLabel);
        GUILayout.Label("Insert prompt:", EditorStyles.boldLabel);
        promptText = GUILayout.TextArea(promptText, GUILayout.Height(100));

        shouldUseSMPLify = EditorGUILayout.Toggle("Use SMPLify", shouldUseSMPLify);

        GUILayout.Label("Python Path", EditorStyles.boldLabel);
        pythonPath = GUILayout.TextField(pythonPath);

        GUILayout.Label("Select Model", EditorStyles.boldLabel);
        selectedModelIndex = EditorGUILayout.Popup(selectedModelIndex, models);

        if (GUILayout.Button("Generate"))
        {
            UsefulShortcuts.ClearConsole();
            StartPythonServer();
            isGenerating = true;
            startTime = Time.realtimeSinceStartup;
            elapsedTimeText = "00:00";
            Task.Run(() => Generate(promptText)).ContinueWith(t => EditorApplication.update += OnTaskCompleted);
            EditorApplication.update += UpdateElapsedTime;
        }

        if (isGenerating)
        {
            GUILayout.BeginHorizontal();
            //align to right side the elapsed time
            GUILayout.FlexibleSpace();
            GUILayout.Label("Elapsed Time: " + elapsedTimeText, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

        }
        else
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Elapsed Time: " + elapsedTimeText, EditorStyles.boldLabel);
            //EditorUtility.ClearProgressBar();
            if (processResponse != "")
            {
                UnityEngine.Debug.Log("Animation generation completed! Details: " + processResponse);
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
            elapsedTimeText = timeSpan.ToString(@"mm\:ss"); // Formatta il tempo in minuti e secondi (mm:ss)
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
            generatedClips.Clear();
            foreach (string file in files)
            {
                GenerateClips(file);
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
        var workingDirectory = Path.GetFullPath("C:\\Users\\Ciro\\Desktop\\UnityProjects\\MasterThesisProject\\Assets\\Scripts\\PythonScripts");
        pythonServerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = pythonPath, // Insert your python path here
                Arguments = @"zeromq_server.py", // Insert your server script here
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
            pythonServerProcess.Kill();
            pythonServerProcess.Dispose();
            pythonServerProcess = null;
        }
    }

    private void Generate(string promptText)
    {
        string selectedModel = models[selectedModelIndex];
        AsyncIO.ForceDotNet.Force();
        client = new RequestSocket("tcp://localhost:5554");

        try
        {
            // Invia il prompt e il modello selezionato al server Python
            var messageObj = new JsonMessage
            {
                prompt = promptText,
                model = selectedModel,
                output_dir = outputDir,
                use_smplify = shouldUseSMPLify
            };

            var message = JsonUtility.ToJson(messageObj);
            UnityEngine.Debug.Log("Generating animation for: " + promptText + ". Please wait...");
            client.SendFrame(message);

            if (client.TryReceiveFrameString(TimeSpan.FromSeconds(350), out string response))
            {
                EditorApplication.delayCall += () => UsefulShortcuts.ClearConsole();
                processResponse = response;
            }
            else
            {
                UnityEngine.Debug.LogError("Timeout or no response from server");
                if (client != null)
                {
                    TerminatePythonServer();
                    client.Close();
                    ((IDisposable)client).Dispose();
                    NetMQConfig.Cleanup();
                }
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
                TerminatePythonServer();
                client.Close();
                ((IDisposable)client).Dispose();
                NetMQConfig.Cleanup();
            }
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
            if (!file.Contains(".anim"))
            {
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
            CreateAnimationClip(assetPath, modelImporter);
        }
        else
        {
            UnityEngine.Debug.LogError("ModelImporter is null for: " + assetPath);
        }
    }

    private void ConfigureModelImporter(ModelImporter modelImporter, string assetPath)
    {
        // Configurazione del model importer per l'animazione
        modelImporter.animationType = ModelImporterAnimationType.Human;

        // Applica le modifiche al modello e aggiorna l'asset
        SerializedObject so = new SerializedObject(modelImporter);
        so.ApplyModifiedProperties();
        AssetDatabase.ImportAsset(modelImporter.assetPath, ImportAssetOptions.ForceUpdate);
    }

    private void CreateAnimationClip(string assetPath, ModelImporter modelImporter)
    {
        AnimationClip origClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(assetPath, typeof(AnimationClip));
        AnimationClip newClip = new AnimationClip();
        EditorUtility.CopySerialized(origClip, newClip);
        AssetDatabase.CreateAsset(newClip, assetPath.Replace(".fbx", "") + ".anim");
        AssetDatabase.Refresh();

        if (generatedClips != null)
        {
            generatedClips.Add(newClip);
        }
    }
}