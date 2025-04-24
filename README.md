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
   I comandi non bisogna modificarli. Per quanta riguarda i percorsi dei vari
   modelli, bisogna prima seguire le indicazioni che trovi sui github dei
   singoli modelli in quanto non posso fornire i vari dataset/pesi dei modelli.

   Nello specifico puoi clonare i modelli che trovi forkati nel mio account
   github e seguire le indicazioni per il download dei pesi e del dataset, poi
   modifica i path presenti nel [config.json](https://github.com/ciroanni/MasterThesis/blob/main/Assets/Scripts/PythonScripts/config.json).

