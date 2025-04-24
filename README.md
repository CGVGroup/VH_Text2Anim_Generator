# [Animating Virtual Characters in Unity Using Generative AI: A Prompt-Based Approach](https://webthesis.biblio.polito.it/35302/)

### Passi per riprodurre il lavoro svolto 
   
1. Clona la repo nella cartella desiderata

   ```bash
   git clone https://github.com/ciroanni/MasterThesis
   cd MasterThesis
   ```

2. Apri il progetto in Unity (versione usata Unity 6000.0.28f1)
   
3. Apri il tab AI generation (te lo ritroverai automaticamente)

4. Modifica i working paths affinchè funzionino sul tuo pc. In particolare in python path dovrai mettere l'eseguibile della versione di
   python installata sul tuo pc.
   Ad esempio, un percorso valido potrebbe essere:
    ```
    C:/Users/<nome utente>/AppData/Local/Programs/Python/Python310/python.exe
    ```
5. Tutti i path e i comandi dei modelli sono nel file
   [config.json](https://github.com/ciroanni/MasterThesis/blob/main/Assets/Scripts/PythonScripts/config.json).
   I comandi non bisogna modificarli. Per quanto riguarda i modelli, puoi clonare quelli che trovi forkati nel mio account github e seguire le indicazioni nei readme dei singoli modelli per il download dei pesi e del dataset, poi
   modifica i path presenti nel [config.json](https://github.com/ciroanni/MasterThesis/blob/main/Assets/Scripts/PythonScripts/config.json).

   In particolare:
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

Per qualsiasi dubbio apri una issue qui o contattatemi.

