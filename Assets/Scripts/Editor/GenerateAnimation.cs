using UnityEngine;
using UnityEditor;
using System.IO;
using NetMQ;
using NetMQ.Sockets;
using System.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Animations;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using System.Collections.Generic;

public class GenerateAnimation : EditorWindow
{
    #region Variables
    [Serializable]
    private class JsonMessage
    {
        public string prompt;
        public string model;
        public string output_dir;
        //public bool use_smplify;
        public int iterations;
        public float motion_length;
        public string style;
        public string movement;
        public float gss;
    }

    private string promptText = "Enter the prompt text here";
    //private bool shouldUseSMPLify = false;
    private int iterations = 100;
    //private List<AnimationClip> generatedClips;
    private int selectedModelIndex = 4;
    private int selectedStyleIndex = 0;
    private int selectedMovementIndex = 0;
    private string[] models = new string[] { "MoMask", "GMD", "MDM", "T2M-GPT", "LADiff", "SMooDi", "AttT2M", "Gesticulator" };
    //private string[] convertingOptions = new string[] { "IK Solver", "SMPLify" };
    private string[] styles = new string[100];
    private string[] movementType = new string[] { "Backwards Running", "Backwards Walking", "Forwards Running", "Forwards Walking", "Idling", "Sidestep Running", "Sidestep Walking", "Transitions" };
    private string[] movementTypeConverted = new string[] { "BR", "BW", "FR", "FW", "ID", "SR", "SW", "TR1" };
    private float motion_length = 0.0f;
    private float guidance_scale_style = 0.0f;
    private string pythonPath = "C:/Users/Ciro/AppData/Local/Programs/Python/Python310/python.exe";
    private string outputDir = "";
    private RequestSocket client;
    private bool isGenerating = false;
    private Process pythonServerProcess;
    private string processResponse = "";
    private float startTime = 0f;
    private string elapsedTimeText = "00:00";
    private CancellationTokenSource cancellationTokenSource;
    private bool showPaths = false;
    private List<AnimationClip> generatedClips;
    #endregion

    #region References
    [SerializeField] private Animator animator;
    #endregion

    [MenuItem("AI generation/Generate Animation")]
    public static void ShowWindow()
    {
        GenerateAnimation window = (GenerateAnimation)EditorWindow.GetWindow(typeof(GenerateAnimation));
        window.minSize = new Vector2(400, 600);
        window.maxSize = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        GetWindow<GenerateAnimation>("Generate Animation");
    }

    private void Awake()
    { // Temporary reference to record the animations in the scene
        //TO BE REMOVED
        // animator = FindObjectOfType<Animator>();
        generatedClips = new List<AnimationClip>();
        // initialize the styles array reading the 100style.txt file
        string path = "Assets/Scripts/Editor/100style.txt";
        StreamReader reader = new StreamReader(path);
        for (int i = 0; i < 100; i++)
        {
            styles[i] = reader.ReadLine();
        }
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
        }

        //GUILayout.Space(10);
        //EditorGUILayout.PrefixLabel("Converting Method", EditorStyles.boldLabel);
        //selectedConvertingIndex = EditorGUILayout.Popup(selectedConvertingIndex, convertingOptions);
        // if (convertingOptions[selectedConvertingIndex] == "SMPLify")
        // {
        //     shouldUseSMPLify = true;
        // }
        //else
        //{
        //shouldUseSMPLify = false;
        EditorGUILayout.PrefixLabel("IK solver iterations", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("The number of iterations is the number of times the algorithm will be run to convert the animation. The higher the number, the more accurate the animation will be but it will take longer to generate.", MessageType.Info);
        iterations = EditorGUILayout.IntSlider(iterations, 1, 500);
        //}

        GUILayout.Space(10);
        GUILayout.Label("Select Model", EditorStyles.boldLabel);
        selectedModelIndex = EditorGUILayout.Popup(selectedModelIndex, models);

        GUILayout.Space(10);

        if (models[selectedModelIndex] == "SMooDi")
        {
            // add a popup for choosing the style motion from a txt file named 100style.txt
            EditorGUILayout.HelpBox("You can choose the style of the motion for a style transfer.", MessageType.Info);
            selectedStyleIndex = EditorGUILayout.Popup(selectedStyleIndex, styles);
            selectedMovementIndex = EditorGUILayout.Popup(selectedMovementIndex, movementType);
            EditorGUILayout.HelpBox("You can modify the guidance scale style to achieve a better balance between content preservation and style reflection. 0 means default parameter settings", MessageType.Info);
            guidance_scale_style = EditorGUILayout.Slider("Guidance Scale Style", guidance_scale_style, 0f, 10f);
        }


        if (models[selectedModelIndex] == "GMD")
        {
            EditorGUILayout.HelpBox("The motion length is the duration of the generated animation in seconds. If 0, the animation will have the default length for that model", MessageType.Info);
            motion_length = EditorGUILayout.Slider("Motion Length (sec)", motion_length, 0f, 6f);
        }
        else if (models[selectedModelIndex] == "T2M-GPT")
            EditorGUILayout.HelpBox("You can NOT control the motion length using GPT", MessageType.Info);
        else
        {
            EditorGUILayout.HelpBox("The motion length is the duration of the generated animation in seconds. If 0, the animation will have the default length for that model", MessageType.Info);
            motion_length = EditorGUILayout.Slider("Motion Length (sec)", motion_length, 0f, 9.8f);
        }



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
                    //ConfigureModelImporter(modelImporter, file);
                    GenerateClips(file);
                }
                else
                {
                    UnityEngine.Debug.LogError("ModelImporter is null for: " + file);
                }
            }
            // foreach (AnimationClip clip in generatedClips)
            // {
            //     ApplyAndRecordClip(clip);
            // }
            DeleteAll(newDir);
            generatedClips.Clear();
            isGenerating = false;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.Message);
        }
    }

    private void StartPythonServer()
    {
        string workingDirectory = Path.Combine(Application.dataPath, "Scripts/PythonScripts/");
        pythonServerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = workingDirectory + "server.py",
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


        string path = Path.Combine(outputDir + "\\results", "prompt.txt");
        promptText = promptText.Replace("\n", " ");
        File.WriteAllText(path, promptText);


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
                //use_smplify = shouldUseSMPLify,
                iterations = iterations,
                motion_length = motion_length,
                style = styles[selectedStyleIndex],
                movement = movementTypeConverted[selectedMovementIndex],
                gss = guidance_scale_style,
            };

            var message = JsonUtility.ToJson(messageObj);
            UnityEngine.Debug.Log("Generating animation for: " + promptText + ". Please wait...");
            client.SendFrame(message);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (client.TryReceiveFrameString(TimeSpan.FromSeconds(1), out string response))
                {
                    //EditorApplication.delayCall += () => UsefulShortcuts.ClearConsole();
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
        //return newDir + "\\" + promptText.Replace(" ", "_");
        return newDir + "\\results";
    }

    private void DeleteAll(string path)
    {
        string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        string[] dirs = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            if (!file.Contains(".anim") && !file.Contains(".fbx") && !file.Contains("prompt.txt"))
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

    private void ApplyAndRecordClip(AnimationClip clip)
    {
        animator.runtimeAnimatorController = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Controller.controller");
        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = stateMachine.AddState(clip.name);
        state.motion = clip;
        SetRecordSettings(clip.name);
        AssetDatabase.SaveAssets();

    }

    private void SetRecordSettings(string clipName = "CustomClip") //NOT WORKING
    {
        // Creazione di una sessione di registrazione
        var recorderControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var recorderController = new RecorderController(recorderControllerSettings);

        // Creazione delle impostazioni del recorder
        var movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorderSettings.name = "Video Recorder";
        movieRecorderSettings.Enabled = true;

        // Configura il formato di output
        movieRecorderSettings.OutputFile = "Assets/Recordings/";
        movieRecorderSettings.ImageInputSettings = new GameViewInputSettings(); // Registra dalla Game View
        movieRecorderSettings.FileNameGenerator.FileName = clipName + "_" + models[selectedModelIndex];

        // Configura la risoluzione
        movieRecorderSettings.ImageInputSettings.OutputWidth = 1920;
        movieRecorderSettings.ImageInputSettings.OutputHeight = 1080;

        // Configura il frame rate
        movieRecorderSettings.FrameRate = 30.0f;

        // Aggiungi le impostazioni al controller
        recorderControllerSettings.AddRecorderSettings(movieRecorderSettings);
        recorderControllerSettings.SetRecordModeToManual();
        RecorderWindow recorderWindow = EditorWindow.GetWindow<RecorderWindow>();
        recorderWindow.Show();
        recorderWindow.SetRecorderControllerSettings(recorderControllerSettings);

        // Avvia la registrazione (se richiesto)
        recorderWindow.StartRecording();
        UnityEngine.Debug.Log("Recording started...");
    }

    bool AnimatorIsPlaying()
    {
        return animator.GetCurrentAnimatorStateInfo(0).length >
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }

    private void ConfigureModelImporter(ModelImporter modelImporter, string assetPath)
    {
        modelImporter.animationType = ModelImporterAnimationType.Human;
        if (models[selectedModelIndex] == "MoMask")
        {
            modelImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            modelImporter.sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/FBXs/MoMask.fbx");
        }
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
        generatedClips.Add(newClip);
        AssetDatabase.Refresh();
    }
}