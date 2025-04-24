using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Animancer;
using UnityEditor.Recorder;
using UnityEditor;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using System;

[RequireComponent(typeof(Animator))]
public class RecordClip : MonoBehaviour
{
    [SerializeField] private AnimancerComponent _Animancer;
    [SerializeField] private AnimationClip[] clips;

    public enum Scenario
    {
        All,
        Speaking,
        Standing,
        Walking,
        Waving,
    }
    public enum Emotion
    {
        All,
        Anger,
        Disgust,
        Fear,
        Happiness,
        Sadness,
        Surprise,
    }
    public enum Model
    {
        LADiff,
        MDM,
        T2MGPT,
        Muse
    }

    public Model model;
    public Scenario scenario;
    public Emotion emotion;
    private readonly string[] scenarios = { "Speaking", "Standing", "Walking", "Waving" };
    private readonly string[] emotions = { "Anger", "Disgust", "Fear", "Happiness", "Sadness", "Surprise" };
    private int _scenarioIndex = 0;
    private int _emotionIndex = 0;
    public bool IsPlayingAnimation { get; private set; }
    private RecorderWindow _recorderWindow;
    private bool isRecordingAllEmotions = true;
    private bool isRecordingAllScenarios = true;

    private void Awake()
    {
        _recorderWindow = GetRecorderWindow();
        InitializeRecorder();
    }

    private void InitializeRecorder()
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var recorder = CreateRecorderSettings();
        controllerSettings.AddRecorderSettings(recorder);
        controllerSettings.ExitPlayMode = false;
        _recorderWindow.SetRecorderControllerSettings(controllerSettings);
    }

    private MovieRecorderSettings CreateRecorderSettings()
    {
        var recorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        recorder.name = "My Video Recorder";
        recorder.Enabled = true;
        recorder.AudioInputSettings.PreserveAudio = true;
        recorder.OutputFile = GetOutputFileName();
        recorder.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1280,
            OutputHeight = 720,
        };
        return recorder;
    }

    private string GetOutputFileName()
    {
        return $"C:\\Users\\Ciro\\Desktop\\Tesi\\MasterThesis\\Assets\\Recordings\\{model}_{emotions[_emotionIndex]}_{scenarios[_scenarioIndex]}";
    }

    private void Start()
    {
        _Animancer = GetComponent<AnimancerComponent>();
        IsPlayingAnimation = false;
        if (emotion != Emotion.All)
        {
            isRecordingAllEmotions = false;
            _emotionIndex = Array.IndexOf(emotions, emotion.ToString());
            Debug.Log("Emozione: " + emotion.ToString() + "at " + _emotionIndex);
        }

        if (scenario != Scenario.All)
        {
            isRecordingAllScenarios = false;
            _scenarioIndex = Array.IndexOf(scenarios, scenario.ToString());
            Debug.Log("Scenario: " + scenario.ToString() + "at " + _scenarioIndex);
        }
    }

    private void OnAnimationEnd()
    {
        _recorderWindow.StopRecording();
        if (isRecordingAllEmotions)
            _emotionIndex++;
        else
            _emotionIndex = emotions.Length;
        IsPlayingAnimation = false;
    }

    private void Update()
    {
        if (!IsPlayingAnimation)
        {
            if (_emotionIndex >= emotions.Length)
            {
                if (isRecordingAllScenarios)
                {
                    _scenarioIndex++;
                    if (emotion != Emotion.All)
                        _emotionIndex = Array.IndexOf(emotions, emotion.ToString());
                    else
                        _emotionIndex = 0;
                }
                else
                    _scenarioIndex = scenarios.Length;


                if (_scenarioIndex >= scenarios.Length)
                {
                    Debug.Log("Stop Unity Play Mode");
                    EditorApplication.isPlaying = false;
                    return;
                }
            }

            PlayNextAnimation();
        }
    }

    private void PlayNextAnimation()
    {

        clips = Resources.LoadAll<AnimationClip>($"{model}_BodyLanguage\\{scenarios[_scenarioIndex]}\\{emotions[_emotionIndex]}");
        if (clips.Length == 0)
        {
            Debug.LogWarning("No animation clips found for the current scenario and emotion.");
            return;
        }

        IsPlayingAnimation = true;
        UpdateRecorderSettings();
        _recorderWindow.StartRecording();
        AnimancerState state = _Animancer.Play(clips[0]);
        if (model != Model.Muse)
        {
            state.ApplyAnimatorIK = true;
            state.ApplyFootIK = true;
        }else
        {
            state.ApplyAnimatorIK = false;
            state.ApplyFootIK = false;
        }
        state.Events.OnEnd = OnAnimationEnd;
    }

    private void UpdateRecorderSettings()
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var recorder = CreateRecorderSettings();
        controllerSettings.AddRecorderSettings(recorder);
        controllerSettings.ExitPlayMode = false;
        _recorderWindow.SetRecorderControllerSettings(controllerSettings);
    }

    private RecorderWindow GetRecorderWindow()
    {
        return (RecorderWindow)EditorWindow.GetWindow(typeof(RecorderWindow));
    }
}