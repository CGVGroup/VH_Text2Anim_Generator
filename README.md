### ⚙️ Requirements

- **Unity 6000.0.28f1** installed.
- **Python** installed (recommended >= 3.10).
- **CUDA-compatible NVIDIA GPU**.  
  A GPU with CUDA support is required to run the motion generation models.

### 🔧 Instructions

1. Clone the repository
   Open your terminal in the desired folder and run:
   
   ```bash
   git clone https://github.com/ciroanni/MasterThesis
   cd MasterThesis
   ```
   
2. Open the project in Unity (version <strong>Unity 6000.0.28f1</strong>)
   
3. Open the "AI generation" tab. The AI generation interface will appear automatically once the project is loaded.
   <p>
    <img src="Assets/Screenshots/tab_ai.png" />
   </p>
   <p>
    <img src="Assets/Screenshots/open_tab_ai.png" />
   </p>

4. Make sure to adjust the paths to match your local setup.
   
   Specifically, set the correct Python executable in the Python Path field. Example (on Windows):
    ```
    C:/Users/<user name>/AppData/Local/Programs/Python/Python310/python.exe
    ```
   <p>
    <img src="Assets/Screenshots/python_path.png" width="439" height="503" />
   </p> 
5. Configure models.
   All commands and paths are located in
   [config.json](https://github.com/ciroanni/MasterThesis/blob/main/Assets/Scripts/PythonScripts/config.json).
   - <strong> Do not modify the commands</strong>.
   - To use the models:
      - Clone the required models from my GitHub forks.
      - Follow each model's instructions in their repository to download the pretrained weights and datasets ([T2M-GPT](https://github.com/Mael-zys/T2M-GPT), [MDM](https://github.com/GuyTevet/motion-diffusion-model), and [LADiff](https://github.com/AlessioSam/LADiff)).
      - Then update the paths in [config.json](https://github.com/ciroanni/MasterThesis/blob/main/Assets/Scripts/PythonScripts/config.json) accordingly.
      - **Note**: running the models requires a CUDA-capable GPU.
    

   Supported models (so far):
   ### LADiff
   ```bash
   git clone -b master https://github.com/ciroanni/LADiff
   ```
   ### MDM
   ```bash
   git clone -b master https://github.com/ciroanni/motion-diffusion-model
   ```
   ### T2M-GPT
   ```bash
   git clone https://github.com/ciroanni/T2M-GPT
   ```

   If the folder "bvh2fbx" is empty or nonexistent after the clone command, run this (in the model folder):
   ```bash
   mkdir bvh2fbx #if there is no folder 
   git clone https://github.com/ciroanni/bvh2fbx
   ```

   If the folder "Motion" is empty or nonexistent after the clone command, run this (in the model folder):
   ```bash
   mkdir Motion #if there is no folder
   git clone https://github.com/ciroanni/Motion
   ```

7. Now you can use the “AI generation” tab by selecting the model. The result (both in FBX and .anim) will be generated in the folder:
    ```
    Assets/Resources/results
    ```
8. The SampleScene in
    ```
    Assets/Scenes
    ```
    is the scene used for the dataset videos used in the thesis work, and you can use it to visualize the results. In any case, the results are suitable for any humanoid rig.



