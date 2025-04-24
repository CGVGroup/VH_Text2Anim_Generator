# [Animating Virtual Characters in Unity Using Generative AI: A Prompt-Based Approach](https://webthesis.biblio.polito.it/35302/)
 
### 🔧Come riprodurre il progetto 
   
1. Clona la repository
   Apri il terminale nella cartella dove vuoi salvare il progetto e lancia
   
   ```bash
   git clone https://github.com/ciroanni/MasterThesis
   cd MasterThesis
   ```
   
2. Apri il progetto in Unity (versione usata <strong>Unity 6000.0.28f1</strong>)
   
4. Accedi al tab "AI generation"
   Una volta aperto il progetto, troverai automaticamente il tab dedicato alla generazione AI.

5. Modifica i working paths affinchè funzionino sul tuo pc.

   In particolare in Python Path dovrai mettere l'eseguibile della versione di python installata sul tuo pc.
   Ad esempio, un percorso (in Windows) valido potrebbe essere:
    ```
    C:/Users/<nome utente>/AppData/Local/Programs/Python/Python310/python.exe
    ```
    
6. Configura i modelli
   Tutti i path e i comandi dei modelli sono nel file
   [config.json](https://github.com/ciroanni/MasterThesis/blob/main/Assets/Scripts/PythonScripts/config.json).
   - I comandi non bisogna modificarli.
   - Per utilizzare i modelli, puoi clonare i fork presenti nel mio profilo GitHub.
   - Segui le istruzioni nei README dei singoli repository per scaricare pesi e dataset.
   - Poi aggiorna i path nel in [config.json](https://github.com/ciroanni/MasterThesis/blob/main/Assets/Scripts/PythonScripts/config.json) base alla tua configurazione locale.
   

   Modelli supportati al momento:
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

Per qualsiasi dubbio, apri una issue su GitHub o contattami direttamente.

