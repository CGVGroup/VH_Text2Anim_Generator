import socket
import subprocess
import os

# Configurazione del server UDP
HOST = '127.0.0.1'  # localhost
PORT = 65432       # Porta arbitraria per il server

def run_command(commands):
    # Configura il processo con Conda e il modello di generazione
    process = subprocess.Popen(
        " && ".join(commands), shell=True,
        stdout=subprocess.PIPE, stderr=subprocess.PIPE
    )
    stdout, stderr = process.communicate()
    return stdout, stderr

# Avvio del server
with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
    s.bind((HOST, PORT))
    print(f"Server in ascolto su {HOST}:{PORT}")
    
    while True:
        data, addr = s.recvfrom(4096)  # ricevi i comandi
        print(f"Connesso a {addr}")
        data = data.decode()
        
        # Esegui i comandi ricevuti
        commands = data.split(";")  # Comandi separati da ";"
        stdout, stderr = run_command(commands)
        
        # Invia l'output di ritorno al client
        response = stdout.decode() if stdout else stderr.decode()
        s.sendto(response.encode(), addr)