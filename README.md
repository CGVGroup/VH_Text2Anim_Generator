# [Animating Virtual Characters in Unity Using Generative AI: A Prompt-Based Approach](https://webthesis.biblio.polito.it/35302/)
 
### 🔧Come riprodurre il progetto 
   
1. Clona la repository
   Apri il terminale nella cartella dove vuoi salvare il progetto e lancia
   
   ```bash
   git clone https://github.com/ciroanni/MasterThesis
   cd MasterThesis
   ```
   
2. Apri il progetto in Unity (versione usata <strong>Unity 6000.0.28f1</strong>)
   
3. Accedi al tab "AI generation". Una volta aperto il progetto, troverai automaticamente il tab dedicato alla generazione AI.

4. Modifica i working paths affinchè funzionino sul tuo pc.

   In particolare in Python Path dovrai mettere l'eseguibile della versione di python installata sul tuo pc.
   Ad esempio, un percorso (in Windows) valido potrebbe essere:
    ```
    C:/Users/<nome utente>/AppData/Local/Programs/Python/Python310/python.exe
    ```
    
5. Configura i modelli
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

6. Ora puoi usare il tab "AI generation" selezionando il modello. Il risultato (sia in FBX che in .anim) verrà generato nella cartella:
    ```
    Assets/Resources/results
    ```
7. La SampleScene presente in
    ```
    Assets/Scenes
    ```
    è la scena utilizzata per i video del dataset usato nel lavoro di tesi e puoi usarla per visualizzare i risultati. In ogni caso i risultati sono adatti per qualsiasi humanoid.


### Per qualsiasi dubbio, apri una issue su GitHub o contattami direttamente.

