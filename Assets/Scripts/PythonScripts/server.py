import random
import shutil
import zmq
import subprocess
import os
import logging
import json

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

CONFIG_FILE = "config.json"

def load_config():
    """Carica la configurazione da un file JSON."""
    if os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, "r") as f:
            return json.load(f)
    else:
        default_config = {
            "model_paths": {},
            "model_commands": {}
        }
        save_config(default_config)
        return default_config

def save_config(config):
    """Salva la configurazione nel file JSON."""
    with open(CONFIG_FILE, "w") as f:
        json.dump(config, f, indent=4)

def get_model_path(model_name):
    """Ottiene il percorso della directory di un modello dalla configurazione."""
    config = load_config()
    return config["model_paths"].get(model_name, "")

def get_model_command(model_name, motion_length):
    """Ottiene il comando per un modello dalla configurazione."""
    config = load_config()
    if motion_length == 0:
        return config["model_commands_noLength"].get(model_name, "")
    else:
        return config["model_commands"].get(model_name, "")

def random_file_selector(input_folder, output_folder, prefix, suffix):
    """
    Seleziona randomicamente un file con prefisso e suffisso specificati, lo copia
    nella cartella di destinazione (che viene svuotata prima).

    :param input_folder: Cartella contenente i file di input.
    :param output_folder: Cartella di destinazione.
    :param prefix: Prefisso del file.
    :param suffix: Suffisso del file.
    """
    # Svuota la cartella di output
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)
    else:
        for file in os.listdir(output_folder):
            file_path = os.path.join(output_folder, file)
            if os.path.isfile(file_path) or os.path.islink(file_path):
                os.unlink(file_path)

    
    # Cerca i file con prefisso e suffisso specificati
    matching_files = [
        file for file in os.listdir(input_folder)
        if file.startswith(f"{prefix}_{suffix}_")
    ]
    
    if not matching_files:
        print("Nessun file corrispondente trovato.")
        return f"No file found with prefix '{prefix}' and suffix '{suffix}'."

    # Seleziona un file randomicamente
    selected_file = random.choice(matching_files)
    
    # Copia il file selezionato nella cartella di output
    source_path = os.path.join(input_folder, selected_file)
    destination_path = os.path.join(output_folder, selected_file)
    shutil.copy(source_path, destination_path)

def execute_model(prompt, model, output_dir, style, movement, gss, iterations=100, motion_length=1):
    try:
        working_directory = get_model_path(model)
        if not working_directory:
            return f"Error: Model '{model}' path not found in configuration."
        
        result_dir = os.path.join(output_dir, "results")
        os.makedirs(result_dir, exist_ok=True)
        
        command = get_model_command(model, motion_length)
        if not command:
            return f"Error: No command defined for model '{model}'."
        
        if model == "SMooDi":
            random_file_selector(working_directory+"\\100style", working_directory+"\\test_motion", style, movement)
    
        command = command.format(prompt=prompt, output_dir=result_dir, gss=gss, iterations=iterations, motion_length=int(motion_length))
        
        logging.info(f"Executing command: {command}")
        process = subprocess.run(command, shell=True, cwd=working_directory, capture_output=True, text=True)
        
        return "Success" if process.returncode == 0 else process.stderr
    except Exception as e:
        logging.error(f"Error during execution: {str(e)}")
        return f"Error: {str(e)}"

# Configurazione ZeroMQ
context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5554")

logging.info("Server started and waiting for requests...")

while True:
    try:
        message = socket.recv_json()
        
        prompt = message.get("prompt", "")
        model = message.get("model", "GMD")
        output_dir = message.get("output_dir", "output")
        iterations = message.get("iterations", 1)
        motion_length = message.get("motion_length", 1)
        style = message.get("style", "Aeroplane")
        movement = message.get("movement", "BR")
        gss = message.get("gss", 1.5)
        
        result = execute_model(prompt, model, output_dir, style, movement, gss, iterations, motion_length)
        socket.send_string(result)
        
    except zmq.ZMQError as e:
        logging.error(f"ZMQ Error: {str(e)}")
    except Exception as e:
        logging.error(f"Unexpected error: {str(e)}")
