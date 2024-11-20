using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System;
using UnityEditor.Animations;
using System.Net.Sockets;
using System.Text;
using System.Net;

public class GenerateAnimationSocketUDP : EditorWindow
{
    private string promptText = "Enter the prompt text here";
    private bool shouldUseSMPLify = true;
    private List<AnimationClip> generatedClips;
    private AnimationClip selectedClip;
    private PreviewRenderUtility previewRenderUtility;
    private GameObject previewObject;
    private Animator animator;
    private Process pythonServerProcess;

    [MenuItem("Window/Generate Animation Socket UDP")]
    public static void ShowWindow()
    {
        GenerateAnimationSocketUDP window = (GenerateAnimationSocketUDP)EditorWindow.GetWindow(typeof(GenerateAnimationSocketUDP));
        window.minSize = new Vector2(400, 600);
        window.maxSize = new Vector2(400, 600);
        GetWindow<GenerateAnimationSocketUDP>("Generate Animation Socket UDP");
    }

    private void OnEnable()
    {
        generatedClips = new List<AnimationClip>();
        previewRenderUtility = new PreviewRenderUtility();
        previewRenderUtility.cameraFieldOfView = 30f;
    }

    private void OnDisable()
    {
        previewRenderUtility.Cleanup();
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
        StopPythonServer();
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate Animation", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Insert prompt:", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        promptText = GUILayout.TextArea(promptText, GUILayout.Height(100));

        GUILayout.Label("Select the better conversion", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        shouldUseSMPLify = EditorGUILayout.Toggle("use SMPLify", shouldUseSMPLify);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Generate"))
        {
            Generate(promptText);
        }

        GUILayout.Space(20);
        GUILayout.Label("Generated Animations", EditorStyles.boldLabel);

        if (generatedClips != null)
        {
            foreach (var clip in generatedClips)
            {
                if (GUILayout.Button(clip.name))
                {
                    selectedClip = clip;
                    SetupPreview();
                }
            }
        }

        if (selectedClip != null)
        {
            GUILayout.Space(20);
            GUILayout.Label("Selected Animation: " + selectedClip.name, EditorStyles.boldLabel);

            Rect previewRect = GUILayoutUtility.GetRect(400, 400, GUILayout.ExpandWidth(true));
            previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);
            previewRenderUtility.camera.transform.position = new Vector3(0, 1, -5);
            previewRenderUtility.camera.transform.LookAt(Vector3.zero);
            previewRenderUtility.Render();
            previewRenderUtility.EndAndDrawPreview(previewRect);

            if (GUILayout.Button("Save Selected Animation"))
            {
                SaveSelectedClip();
            }
        }
    }

    private void Generate(string promptText = "")
    {
        string output_dir = "C:\\Users\\Ciro\\Desktop\\UnityProjects\\TrySentis\\Assets\\Resources\\GeneratedAnimations";
        GMD(promptText, output_dir);
        if (shouldUseSMPLify)
        {
            ConvertToFbx(promptText, output_dir);
        }
        AssetDatabase.Refresh();

        var new_dir = output_dir.Substring(output_dir.IndexOf("Assets"));
        new_dir = new_dir + "\\" + promptText.Replace(" ", "_");
        UnityEngine.Debug.Log("NEW DIR: " + new_dir);
        string[] files = Directory.GetFiles(new_dir, "*.fbx", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            GenerateClips(file);
        }
        StopPythonServer();
        DeleteAll(new_dir);
    }

    private void DeleteAll(string path)
    {
        string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        string[] dirs = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            if (!file.Contains(".anim"))
            {
                UnityEngine.Debug.Log(file);
                File.Delete(file);
            }
        }
        foreach (string dir in dirs)
        {
            UnityEngine.Debug.Log(dir);
            if (!Directory.Exists(dir))
            {
                continue;
            }
            Directory.Delete(dir, true);
        }
        AssetDatabase.Refresh();
    }

    private void GenerateClips(string assetPath)
    {
        ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;

        if (modelImporter != null)
        {
            modelImporter.animationType = ModelImporterAnimationType.Human;

            SerializedObject so = new SerializedObject(modelImporter);
            SerializedProperty humanDescription = so.FindProperty("m_HumanDescription");

            // Forza la mappatura delle ossa delle mani
            ForceHandBoneMapping(humanDescription, "LeftHand", "L_Elbow_end");
            ForceHandBoneMapping(humanDescription, "RightHand", "R_Elbow_end");

            so.ApplyModifiedProperties();
            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Configurazione delle clip di animazione
            ModelImporterClipAnimation[] clipAnimations = modelImporter.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clipAnimation in clipAnimations)
            {
                clipAnimation.loopTime = true;
                clipAnimation.lockRootRotation = true;
                clipAnimation.lockRootHeightY = true;
                clipAnimation.keepOriginalPositionY = true;
                clipAnimation.keepOriginalOrientation = true;
            }
            modelImporter.clipAnimations = clipAnimations;
            so.ApplyModifiedProperties();
            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Creazione della nuova AnimationClip
            AnimationClip orig_clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(assetPath, typeof(AnimationClip));
            AnimationClip new_clip = new AnimationClip();
            EditorUtility.CopySerialized(orig_clip, new_clip);
            AssetDatabase.CreateAsset(new_clip, assetPath.Replace(".fbx", "") + ".anim");
            AssetDatabase.Refresh();

            if (generatedClips != null)
            {
                generatedClips.Add(new_clip);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("ModelImporter is null for: " + assetPath);
        }
    }

    private void ForceHandBoneMapping(SerializedProperty humanDescription, string handBoneName, string customBoneName)
    {
        SerializedProperty humanBones = humanDescription.FindPropertyRelative("m_Human");
        //if there is no LeftHand in humanBones, add it
        bool found = false;
        foreach (SerializedProperty bone in humanBones)
        {
            if (bone.FindPropertyRelative("m_HumanName").stringValue == handBoneName)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            UnityEngine.Debug.Log("Adding " + handBoneName + " to humanBones");
            humanBones.InsertArrayElementAtIndex(humanBones.arraySize);
            SerializedProperty newBone = humanBones.GetArrayElementAtIndex(humanBones.arraySize - 1);
            UnityEngine.Debug.Log("New bone: " + newBone.FindPropertyRelative("m_HumanName").stringValue);
            newBone.FindPropertyRelative("m_HumanName").stringValue = handBoneName;
            newBone.FindPropertyRelative("m_BoneName").stringValue = customBoneName;
        }
    }

    private void SetupPreview()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }

        previewObject = new GameObject("Preview Object");
        animator = previewObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = AnimatorController.CreateAnimatorControllerAtPathWithClip("Assets/Temp.controller", selectedClip);

        previewRenderUtility.AddSingleGO(previewObject);
        previewRenderUtility.camera.transform.position = new Vector3(0, 1, -5);
        previewRenderUtility.camera.transform.LookAt(previewObject.transform);

        // Start the animation
        animator.Play(selectedClip.name);
    }

    private void SaveSelectedClip()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Animation Clip", selectedClip.name, "anim", "Please enter a file name to save the animation clip to");
        if (path.Length != 0)
        {
            AssetDatabase.CreateAsset(selectedClip, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void StartPythonServer()
    {
        var workingDirectory = Path.GetFullPath("C:\\Users\\Ciro\\Desktop\\UnityProjects\\TrySentis\\Assets\\PythonScripts");
        pythonServerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = @"C:\\Users\\Ciro\\AppData\\Local\\Programs\\Python\\Python310\\python.exe", // Insert your python path here
                Arguments = @"serverUDP.py", // Insert your server script here
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            }
        };

        pythonServerProcess.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log("Server: " + args.Data);
        pythonServerProcess.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError("Server Error: " + args.Data);

        pythonServerProcess.Start();
        pythonServerProcess.BeginOutputReadLine();
        pythonServerProcess.BeginErrorReadLine();
    }

    private void StopPythonServer()
    {
        if (pythonServerProcess != null && !pythonServerProcess.HasExited)
        {
            pythonServerProcess.Kill();
            pythonServerProcess.Dispose();
        }
    }

    private void GMD(string promptText = "", string output_dir = "")
    {
        // Avvia il server se non è già attivo
        StartPythonServer();
        //Thread.Sleep(1000);
        // Configurazione del server UDP
        string server = "127.0.0.1";
        int port = 65432;

        using (UdpClient client = new UdpClient())
        {
            client.Connect(server, port);

            // Definisci la directory e i comandi specifici
            string new_dir = promptText.Replace(" ", "_");
            string workingDirectory = "C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\guided-motion-diffusion"; // here you can specify the working directory of your model

            string commands = $@"
                            cd {workingDirectory};
                            C:\\Users\\Ciro\\anaconda3\\Scripts\\activate.bat;
                            conda activate gmd;
                            mkdir {output_dir}\\{new_dir};
                            python -m sample.generate --model_path ./save/unet_adazero_xl_x0_abs_proj10_fp16_clipwd_224/model000500000.pt --output_dir {output_dir}\\{new_dir} --text_prompt ""{promptText}""";

            if (!shouldUseSMPLify)
            {
                commands += $@";
                            python .\\smpl2bvh.py --input_file {output_dir}\\{new_dir}\\results.npy --output_dir {output_dir}\\{new_dir};
                            conda activate bvh2fbx;
                            python .\\bvh2fbx\\convert_fbx.py -- {output_dir}\\{new_dir}\\anim0.bvh;
                            python .\\bvh2fbx\\convert_fbx.py -- {output_dir}\\{new_dir}\\anim1.bvh;
                            python .\\bvh2fbx\\convert_fbx.py -- {output_dir}\\{new_dir}\\anim2.bvh";
            }
            else
            {
                commands += $@";
                            python -m visualize.render_mesh --input_path {output_dir}\\{new_dir}\\sample00.mp4";
            }

            // Invio comandi al server
            byte[] data = Encoding.UTF8.GetBytes(commands.Replace("\n", "").Replace("    ", ""));
            client.Send(data, data.Length);

            // Ricezione della risposta dal server
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
            byte[] buffer = client.Receive(ref remoteEndPoint);
            string response = Encoding.UTF8.GetString(buffer);

            UnityEngine.Debug.Log("Risposta del server: " + response);
        }
    }

    private void ConvertToFbx(string promptText = "", string output_dir = "")
    {
        // Set working directory and create process
        var workingDirectory = Path.GetFullPath("C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\SMPL-to-FBX");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory
            }
        };
        process.Start();
        // Pass multiple commands to cmd.exe
        using (var sw = process.StandardInput)
        {
            if (sw.BaseStream.CanWrite)
            {
                // Vital to activate Anaconda
                sw.WriteLine("C:\\Users\\Ciro\\anaconda3\\Scripts\\activate.bat");
                // Activate your environment
                sw.WriteLine("conda activate smpl2fbx");
                // run your script. You can also pass in arguments
                //create new dir for the output with the name of the prompt
                string new_dir = promptText.Replace(" ", "_");
                sw.WriteLine("python Convert.py --input_pkl_base " + output_dir + "\\" + new_dir + "\\sample00_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base " + output_dir);
                //sw.WriteLine("python Convert.py --input_pkl_base " + output_dir + "\\" + new_dir + "\\sample01_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base " + output_dir + " --animation_name " + new_dir + "\\" + new_dir);
                //sw.WriteLine("python Convert.py --input_pkl_base " + output_dir + "\\" + new_dir + "\\sample02_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base " + output_dir);
            }
        }

        // read multiple output lines
        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            Console.WriteLine(line);
            //print the output of the process
            UnityEngine.Debug.Log(line);
        }
    }

    private void MDM(string promptText = "", string output_dir = "")
    {
        // Set working directory and create process
        var workingDirectory = Path.GetFullPath("C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\motion-diffusion-model");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory
            }
        };
        process.Start();
        // Pass multiple commands to cmd.exe
        using (var sw = process.StandardInput)
        {
            if (sw.BaseStream.CanWrite)
            {
                // Vital to activate Anaconda
                sw.WriteLine("C:\\Users\\Ciro\\anaconda3\\Scripts\\activate.bat");
                // Activate your environment
                sw.WriteLine("conda activate mdm");
                // run your script. You can also pass in arguments
                //create new dir for the output with the name of the prompt
                string new_dir = promptText.Replace(" ", "_");
                sw.WriteLine("mkdir " + output_dir + "\\" + new_dir);
                sw.WriteLine("python -m sample.generate --model_path ./save/humanml_enc_512_50steps/model000750000.pt --text_prompt \"" + promptText + "\" --output_dir " + output_dir + "\\" + new_dir);
                if (!shouldUseSMPLify)
                {
                    sw.WriteLine("python .\\smpl2bvh.py --input_file " + output_dir + "\\" + new_dir + "\\results.npy --output_dir " + output_dir + "\\" + new_dir);
                    sw.WriteLine("conda activate bvh2fbx");
                    sw.WriteLine("python .\\bvh2fbx\\convert_fbx.py -- " + output_dir + "\\" + new_dir + "\\anim0.bvh");
                    sw.WriteLine("python .\\bvh2fbx\\convert_fbx.py -- " + output_dir + "\\" + new_dir + "\\anim1.bvh");
                    sw.WriteLine("python .\\bvh2fbx\\convert_fbx.py -- " + output_dir + "\\" + new_dir + "\\anim2.bvh");
                }
                else
                {
                    sw.WriteLine("python -m visualize.render_mesh --input_path " + output_dir + "\\" + new_dir + "\\sample00_rep00.mp4");
                    //sw.WriteLine("python -m visualize.render_mesh --input_path " + output_dir + "\\" + new_dir + "\\sample01.mp4");
                    //sw.WriteLine("python -m visualize.render_mesh --input_path " + output_dir + "\\" + new_dir + "\\sample02.mp4");
                }
                //sw.WriteLine("python -m visualize.render_mesh --input_path " + output_dir + "\\" + new_dir + "\\sample00_rep00.mp4");
                //sw.WriteLine("python -m visualize.render_mesh --input_path " + output_dir + "\\" + new_dir + "\\sample00_rep01.mp4");
                //sw.WriteLine("python -m visualize.render_mesh --input_path " + output_dir + "\\" + new_dir + "\\sample00_rep02.mp4");
            }
        }

        // read multiple output lines
        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            Console.WriteLine(line);
            //print the output of the process
            UnityEngine.Debug.Log(line);
        }
    }
}