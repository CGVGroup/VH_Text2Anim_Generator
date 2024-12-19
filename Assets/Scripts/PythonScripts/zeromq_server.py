import zmq
import subprocess
import os
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
# Configure ZeroMQ
context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5554", )  # Port to receive messages from Unity

def execute_model(prompt, model, output_dir, use_smplify, iterations=100, motion_length=1):
    try:
        env_paths = {
            "MDM": "C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\motion-diffusion-model",
            "GMD": "C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\guided-motion-diffusion",
            "MoMask": "C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\momask-codes",
            "T2M-GPT" : "C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\T2M-GPT",
            "LADiff" : "C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\LADiff\\src"
        }
        working_directory = env_paths.get(model, "")
        
        if not working_directory:
            return f"Error: model '{model}' not found."

        new_dir = os.path.join(output_dir, prompt.replace(" ", "_"))
        smplPath = "C:\\Users\\Ciro\\Desktop\\Tesi\\Progetti\\SMPL-to-FBX"
        os.makedirs(new_dir, exist_ok=True)
        newDir = prompt.replace(" ", "_")
        inputFilePath = os.path.join(output_dir, newDir, "results.npy")
        bvh2fbxConvertCommand = "python .\\bvh2fbx\\convert_fbx.py -- "

        command = build_command(model, prompt, new_dir, newDir, inputFilePath, output_dir, use_smplify, smplPath, bvh2fbxConvertCommand, iterations, motion_length)
        
        logging.info(f"Executing command: {command}")
        
        process = subprocess.run(command, shell=True, cwd=working_directory, capture_output=True, text=True)
        
        return f"motion length: {motion_length}" if process.returncode == 0 else process.stderr
    except Exception as e:
        logging.error(f"Error during execution: {str(e)}")
        return f"Error during execution: {str(e)}"

def build_command(model, prompt, new_dir, newDir, inputFilePath, output_dir, use_smplify, smplPath, bvh2fbxConvertCommand, iterations, motion_length):
    if use_smplify:
        return build_smplify_command(model, prompt, new_dir, newDir, output_dir, smplPath, motion_length)
    else:
        return build_standard_command(model, prompt, new_dir, newDir, inputFilePath, output_dir, bvh2fbxConvertCommand, iterations, motion_length)

def build_smplify_command(model, prompt, new_dir, newDir, output_dir, smplPath, motion_length):
    
    if motion_length == 0:
        if model == "GMD":
            return f"conda activate gmd && python -m sample.generate --model_path ./save/unet_adazero_xl_x0_abs_proj10_fp16_clipwd_224/model000500000.pt --output_dir {new_dir} --text_prompt \"{prompt}\" && python -m visualize.render_mesh --input_path {output_dir}\\{newDir}\\sample00.mp4 && cd {smplPath} && conda activate smpl2fbx && python Convert.py --input_pkl_base {output_dir}\\{newDir}\\sample00_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base {output_dir}\\{newDir} --animation_name {newDir}"
        elif model == "MDM":
            return f"conda activate mdm && python -m sample.generate --model_path ./save/humanml_enc_512_50steps/model000750000.pt --text_prompt \"{prompt}\" --output_dir {new_dir} && python -m visualize.render_mesh --input_path {output_dir}\\{newDir}\\sample00_rep00.mp4 && cd {smplPath} && conda activate smpl2fbx && python Convert.py --input_pkl_base {output_dir}\\{newDir}\\sample00_rep00_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base {output_dir}\\{newDir} --animation_name {newDir}"
        elif model == "MoMask" or model == "T2M-GPT":
            logging.info(f"{model} does not support SMPLify-X")
            return f"Error: {model} does not support SMPLify-X"
    else:    
        if model == "GMD":
            return f"conda activate gmd && python -m sample.generate --model_path ./save/unet_adazero_xl_x0_abs_proj10_fp16_clipwd_224/model000500000.pt --output_dir {new_dir} --text_prompt \"{prompt}\" --motion_length {motion_length} && python -m visualize.render_mesh --input_path {output_dir}\\{newDir}\\sample00.mp4 && cd {smplPath} && conda activate smpl2fbx && python Convert.py --input_pkl_base {output_dir}\\{newDir}\\sample00_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base {output_dir}\\{newDir} --animation_name {newDir}"
        elif model == "MDM":
            return f"conda activate mdm && python -m sample.generate --model_path ./save/humanml_enc_512_50steps/model000750000.pt --text_prompt \"{prompt}\" --output_dir {new_dir} --motion_length {motion_length} && python -m visualize.render_mesh --input_path {output_dir}\\{newDir}\\sample00_rep00.mp4 && cd {smplPath} && conda activate smpl2fbx && python Convert.py --input_pkl_base {output_dir}\\{newDir}\\sample00_rep00_smpl_params.npy.pkl --fbx_source_path ./fbx/SMPL_m_unityDoubleBlends_lbs_10_scale5_207_v1.0.0.fbx --output_base {output_dir}\\{newDir} --animation_name {newDir}"
        elif model == "MoMask" or model == "T2M-GPT":
            logging.info(f"{model} does not support SMPLify-X")
            return f"Error: {model} does not support SMPLify-X"

def build_standard_command(model, prompt, new_dir, newDir, inputFilePath, output_dir, bvh2fbxConvertCommand, iterations, motion_length=1):
    
    if motion_length == 0:
        if model == "GMD":
            return f"conda activate gmd && python -m sample.generate --model_path ./save/unet_adazero_xl_x0_abs_proj10_fp16_clipwd_224/model000500000.pt --output_dir {new_dir} --text_prompt \"{prompt}\" && python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {output_dir}\\{newDir} --iterations {iterations} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_0.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_1.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_2.bvh"
        elif model == "MDM":
            return f"conda activate mdm && python -m sample.generate --model_path ./save/humanml_enc_512_50steps/model000750000.pt --text_prompt \"{prompt}\" --output_dir {new_dir} && python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {output_dir}\\{newDir} --iterations {iterations} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_0.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_1.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_2.bvh"
        elif model == "T2M-GPT":
            return f"conda activate t2mgpt && python generate.py --output_dir {new_dir} --text_prompt \"{prompt}\" && python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {output_dir}\\{newDir} --iterations {iterations} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_0.bvh"
        elif model == "MoMask":
            return f"conda activate momask && python gen_t2m.py --gpu_id 0 --ext {new_dir} --text_prompt \"{prompt}\" --iterations {iterations} && move {output_dir}\\{newDir}\\animations\\0\\*.bvh {output_dir}\\{newDir} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\{newDir}.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\{newDir}_ik.bvh"
        elif model == "LADiff":
            return f"conda activate ladiff && python .\demo.py --prompt \"{prompt}\" --out_dir {new_dir} && conda activate gmd && python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {output_dir}\\{newDir} --iterations {iterations} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_0.bvh"
    else:
        if model == "GMD":
            return f"conda activate gmd && python -m sample.generate --model_path ./save/unet_adazero_xl_x0_abs_proj10_fp16_clipwd_224/model000500000.pt --output_dir {new_dir} --text_prompt \"{prompt}\" --motion_length {motion_length} && python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {output_dir}\\{newDir} --iterations {iterations} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_0.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_1.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_2.bvh"
        elif model == "MDM":
            return f"conda activate mdm && python -m sample.generate --model_path ./save/humanml_enc_512_50steps/model000750000.pt --text_prompt \"{prompt}\" --output_dir {new_dir} --motion_length {motion_length} && python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {output_dir}\\{newDir} --iterations {iterations} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_0.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_1.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_2.bvh"
        elif model == "T2M-GPT":
            return f"conda activate t2mgpt && python generate.py --output_dir {new_dir} --text_prompt \"{prompt}\" && python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {output_dir}\\{newDir} --iterations {iterations} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_0.bvh"
        elif model == "MoMask":
            return f"conda activate momask && python gen_t2m.py --gpu_id 0 --ext {new_dir} --text_prompt \"{prompt}\" --iterations {iterations}  --motion_length {int(motion_length)} && move {output_dir}\\{newDir}\\animations\\0\\*.bvh {output_dir}\\{newDir} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\{newDir}.bvh && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\{newDir}_ik.bvh"
        elif model == "LADiff":
            return f"conda activate ladiff && python .\demo.py --prompt \"{prompt}\" --length {int(motion_length)} --out_dir {new_dir} && conda activate gmd && python .\\smpl2bvh.py --input_file {inputFilePath} --output_dir {output_dir}\\{newDir} --iterations {iterations} && conda activate bvh2fbx && {bvh2fbxConvertCommand}{output_dir}\\{newDir}\\anim_0.bvh"

while True:
    try:
        # Receive the message from Unity
        message = socket.recv_json()
        prompt = message.get("prompt", "")
        model = message.get("model", "GMD")
        output_dir = message.get("output_dir", "default_output_path")
        use_smplify = message.get("use_smplify", False)
        iterations = message.get("iterations", 1)
        motion_length = message.get("motion_length", 1)

        # logging.info(f"Received request: model={model}, prompt={prompt}, output_dir={output_dir}, use_smplify={use_smplify}")

        # Execute the model with the specified parameters
        result = execute_model(prompt, model, output_dir, use_smplify, iterations, motion_length=motion_length)
        
        # Send the result back to Unity
        socket.send_string(result)
        # logging.info(f"Sent response: {result}")
    except zmq.ZMQError as e:
        logging.error(f"ZMQ Error: {str(e)}")
    except Exception as e:
        logging.error(f"Unexpected error: {str(e)}")