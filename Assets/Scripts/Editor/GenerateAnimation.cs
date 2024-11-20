using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System;

public class GenerateAnimation : EditorWindow
{
    private string promptText = "Enter the prompt text here";
    private bool shouldUseSMPLify = true;
    private List<AnimationClip> generatedClips;
    private string activateBatPath = "C:\\Users\\Ciro\\anaconda3\\Scripts\\activate.bat";
    private int selectedModelIndex = 0;
    private string[] models = new string[] { "GMD", "MDM", "MoMask" };

    [MenuItem("Window/Generate Animation")]
    public static void ShowWindow()
    {
        GenerateAnimation window = (GenerateAnimation)EditorWindow.GetWindow(typeof(GenerateAnimation));
        window.minSize = new Vector2(400, 600);
        window.maxSize = new Vector2(400, 600);
        GetWindow<GenerateAnimation>("Generate Animation");
    }

    private void OnEnable()
    {
        generatedClips = new List<AnimationClip>();
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

        shouldUseSMPLify = EditorGUILayout.Toggle("Better conversion (may take few more minutes)", shouldUseSMPLify);

        GUILayout.Label("Conda Path", EditorStyles.boldLabel);
        activateBatPath = GUILayout.TextField(activateBatPath);

        GUILayout.Label("Select Model", EditorStyles.boldLabel);
        selectedModelIndex = EditorGUILayout.Popup(selectedModelIndex, models);

        if (GUILayout.Button("Generate"))
        {
            Generate(promptText);
        }
    }

    private void Generate(string promptText = "")
    {
        string outputDir = "C:\\Users\\Ciro\\Desktop\\UnityProjects\\MasterThesisProject\\Assets\\Resources";
        string selectedModel = models[selectedModelIndex];
        if (selectedModel == "GMD")
            GMD(promptText, outputDir);
        else if (selectedModel == "MDM")
            MDM(promptText, outputDir);
        else if (selectedModel == "MoMask")
            MoMask(promptText, outputDir);

        if (shouldUseSMPLify)
        {
            ConvertToFbx(promptText, outputDir);
        }
        AssetDatabase.Refresh();

        string newDir = GetNewDirectory(outputDir, promptText);
        string[] files = Directory.GetFiles(newDir, "*.fbx", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            UnityEngine.Debug.Log("Searching for: " + file);
            GenerateClips(file);
        }
        DeleteAll(newDir);
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
                //UnityEngine.Debug.Log(file);
                File.Delete(file);
            }
        }
        foreach (string dir in dirs)
        {
            //UnityEngine.Debug.Log(dir);
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
            ConfigureModelImporter(modelImporter);
            CreateAnimationClip(assetPath, modelImporter);
        }
        else
        {
            UnityEngine.Debug.LogError("ModelImporter is null for: " + assetPath);
        }
    }

    private void ConfigureModelImporter(ModelImporter modelImporter)
    {
        // modelImporter.animationType = ModelImporterAnimationType.Human;
        SerializedObject so = new SerializedObject(modelImporter);
        SerializedProperty humanDescription = so.FindProperty("m_HumanDescription");
        modelImporter.animationType = ModelImporterAnimationType.Human;

        so.ApplyModifiedProperties();
        AssetDatabase.WriteImportSettingsIfDirty(modelImporter.assetPath);
        AssetDatabase.ImportAsset(modelImporter.assetPath, ImportAssetOptions.ForceUpdate);


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
        AssetDatabase.WriteImportSettingsIfDirty(modelImporter.assetPath);
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

    private void GMD(string promptText = "", string outputDir = "")
    {
        UnityEngine.Debug.Log("GMD");
        ExecuteProcess("C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\guided-motion-diffusion", "gmd", promptText, outputDir);
    }

    private void MDM(string promptText = "", string outputDir = "")
    {
        UnityEngine.Debug.Log("MDM");
        ExecuteProcess("C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\motion-diffusion-model", "mdm", promptText, outputDir);
    }

    private void MoMask(string promptText = "", string outputDir = "")
    {
        if (!shouldUseSMPLify)
        {
            UnityEngine.Debug.Log("MoMask");
            ExecuteProcess("C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\momask-codes", "momask", promptText, outputDir);
        }else{
            UnityEngine.Debug.LogError("NOT IMPLEMENTED YET");
        }
    }

    private void ConvertToFbx(string promptText = "", string outputDir = "")
    {
        ExecuteProcess("C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\SMPL-to-FBX", "smpl2fbx", promptText, outputDir);
    }

    private void ExecuteProcess(string workingDirectory, string environment, string promptText, string outputDir)
    {
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

        using (var sw = process.StandardInput)
        {
            if (sw.BaseStream.CanWrite)
            {
                sw.WriteLine(activateBatPath);
                sw.WriteLine($"conda activate {environment}");
                string newDir = promptText.Replace(" ", "_");
                sw.WriteLine($"mkdir {outputDir}\\{newDir}");
                sw.WriteLine(GetPythonCommand(environment, promptText, outputDir, newDir));
                SetPythonConversion(sw, environment, outputDir, newDir);
            }
        }

        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            Console.WriteLine(line);
            //UnityEngine.Debug.Log(line);
        }
    }

    private string GetPythonCommand(string environment, string promptText, string outputDir, string newDir)
    {
        if (environment == "gmd")
        {
            return $"python -m sample.generate --model_path ./save/unet_adazero_xl_x0_abs_proj10_fp16_clipwd_224/model000500000.pt --output_dir {outputDir}\\{newDir} --text_prompt \"{promptText}\"";
        }
        else if (environment == "mdm")
        {
            return $"python -m sample.generate --model_path ./save/humanml_enc_512_50steps/model000750000.pt --text_prompt \"{promptText}\" --output_dir {outputDir}\\{newDir}";
        }
        else if (environment == "momask")
        {
            return $"python gen_t2m.py --gpu_id 0 --ext {outputDir}\\{newDir} --text_prompt \"{promptText}\"";
        }
        else if (environment == "smpl2fbx")
        {
            if (models[selectedModelIndex] == "gmd")
                return $"python Convert.py --input_pkl_base {outputDir}\\{newDir}\\sample00_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base {outputDir}";
            else if (models[selectedModelIndex] == "mdm")
                return $"python Convert.py --input_pkl_base {outputDir}\\{newDir}\\sample00_rep00_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base {outputDir}";
            else if (models[selectedModelIndex] == "momask")
                return $"python Convert.py --input_pkl_base {outputDir}\\{newDir}\\sample00_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base {outputDir}";
        }
        return string.Empty;
    }

    private void SetPythonConversion(StreamWriter sw, string environment, string outputDir, string newDir)
    {
        string inputFilePath = $"{outputDir}\\{newDir}\\results.npy";
        string outputDirPath = $"{outputDir}\\{newDir}";
        string condaActivateBvh2fbx = "conda activate bvh2fbx";
        string bvh2fbxConvertCommand = $"python .\\bvh2fbx\\convert_fbx.py -- ";

        if (environment == "gmd" || environment == "mdm")
        {
            if (!shouldUseSMPLify)
            {
                sw.WriteLine($"python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {outputDirPath}");
                sw.WriteLine(condaActivateBvh2fbx);
                for (int i = 0; i < 3; i++)
                {
                    sw.WriteLine($"{bvh2fbxConvertCommand}{outputDirPath}\\anim{i}.bvh");
                }
            }
            else
            {
                string renderMeshCommand = environment == "gmd" ?
                    $"python -m visualize.render_mesh --input_path {outputDirPath}\\sample00.mp4" :
                    $"python -m visualize.render_mesh --input_path {outputDirPath}\\sample00_rep00.mp4";
                sw.WriteLine(renderMeshCommand);
            }
        }
        else if (environment == "momask")
        {
            if (!shouldUseSMPLify)
            {
                sw.WriteLine($"move {outputDirPath}\\animations\\0\\*.bvh {outputDirPath}");
                sw.WriteLine(condaActivateBvh2fbx);
                sw.WriteLine($"{bvh2fbxConvertCommand}{outputDirPath}\\{newDir}.bvh");
                sw.WriteLine($"{bvh2fbxConvertCommand}{outputDirPath}\\{newDir}_ik.bvh");
            }
            else
            {
                UnityEngine.Debug.LogWarning("NOT IMPLEMENTED YET");
            }
        }
    }
}