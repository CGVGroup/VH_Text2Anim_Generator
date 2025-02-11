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
    [SerializeField] private string[] model = {"LADiff", "MDM", "T2M-GPT"};
    public bool IsPlayingAnimation { get; private set; }
    private int _index = 0;
    private RecorderWindow _recorderWindow;
    private void Awake()
    {
        _recorderWindow = GetRecorderWindow();
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var recorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        recorder.name = "My Video Recorder";
        recorder.Enabled = true;
        recorder.AudioInputSettings.PreserveAudio = true;
        recorder.OutputFile = "C:\\Users\\Ciro\\Desktop\\Tesi\\MasterThesis\\Assets\\Recordings\\" + model + "_<Take>";
        recorder.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1280,
            OutputHeight = 720,
        };

        controllerSettings.AddRecorderSettings(recorder);
        controllerSettings.ExitPlayMode = false;
        _recorderWindow.SetRecorderControllerSettings(controllerSettings);
    }
    private void Start()
    {
        clips = Resources.LoadAll<AnimationClip>(model + "_BodyLanguage");
        Debug.Log("clips.Length: " + clips.Length);
        _Animancer = GetComponent<AnimancerComponent>();
        IsPlayingAnimation = true;
        _recorderWindow.StartRecording();
        AnimancerState state = _Animancer.Play(clips[_index]);
        state.ApplyAnimatorIK = true;
        state.ApplyFootIK = true;
        state.Events.OnEnd = OnAnimationEnd;
    }
    private void OnAnimationEnd()
    {
        _recorderWindow.StopRecording();
        IsPlayingAnimation = false;
    }

    private void Update()
    {
        if (IsPlayingAnimation == false)
        {
            _index++;
            if (_index >= clips.Length)
            {
                // stop unity play mode
                Debug.Log("Finito");

                UnityEditor.EditorApplication.isPlaying = false;
            }
            else
            {
                IsPlayingAnimation = true;
                _recorderWindow.StartRecording();
                AnimancerState state = _Animancer.Play(clips[_index]);
                state.ApplyAnimatorIK = true;
                state.ApplyFootIK = true;
                state.Events.OnEnd = OnAnimationEnd;
            }
        }
    }

    private RecorderWindow GetRecorderWindow()
    {
        return (RecorderWindow)EditorWindow.GetWindow(typeof(RecorderWindow));
    }
}