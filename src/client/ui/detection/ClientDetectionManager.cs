using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Asymptote.Client.UI.Detection;

// Audio tones maths are made by Claude
// I only combined the logic
public partial class ClientDetectionManager : Node
{
    public static ClientDetectionManager Instance { get; private set; }

    private readonly Dictionary<string, float> _clientTrackers = new();

    private const int MixRate = 22050;
    private const float BufferLength = 0.05f;
    private const float MinFreq = 180f;
    private const float MaxFreq = 520f;
    private const float MaxVolume = 0.12f;
    private const float TremoloDepth = 0.3f;
    private const float LowPassCoeff = 0.3f;

    private const float PanicStart = 0.5f;

    private const float CaughtThreshold = 0.97f;
    private const float CaughtRearm = 0.2f;
    private const float CaughtVolume = 0.35f;
    private const float CaughtDuration = 0.5f;

    private const string CaughtBusName = "CaughtFX";

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float UserVolume { get; set; } = 0.7f;

    private static float Coeff(float seconds) => 1f - Mathf.Exp(-1f / (seconds * MixRate));
    private static readonly float FreqCoeff = Coeff(0.15f);
    private static readonly float AttackCoeff = Coeff(0.25f);
    private static readonly float ReleaseCoeff = Coeff(0.6f);
    private static readonly float PanicCoeff = Coeff(0.35f);

    private static readonly float[] CaughtBaseHz = { 220f, 233f, 311f };

    private AudioStreamPlayer _audioPlayer;
    private AudioStreamGeneratorPlayback _playback;

    private AudioStreamPlayer _caughtPlayer;
    private AudioStreamGeneratorPlayback _caughtPlayback;

    private float _phase;
    private float _phaseChorus;
    private float _phaseTritone;
    private float _phaseDetune;
    private float _tremPhase;
    private float _vibPhase;
    private float _currentFreq = MinFreq;
    private float _currentVolume;
    private float _lpState;

    private float _panic;
    private float _prevPanic;
    private float _beatTime;

    private uint _noiseState = 22222u;
    private float _noiseLp;
    private float _breathLp;

    private float _stingerTime = -1f;
    private float _stingerPhase;

    private bool _caughtArmed = true;
    private float _caughtTime = -1f;
    private float _caughtThumpPhase;
    private readonly float[] _caughtPhases = new float[3];
    private float _crackLp;

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }

        Instance = this;

        var generator = new AudioStreamGenerator { MixRate = MixRate, BufferLength = BufferLength };
        _audioPlayer = new AudioStreamPlayer { Stream = generator };
        AddChild(_audioPlayer);
        _audioPlayer.Play();
        _playback = (AudioStreamGeneratorPlayback)_audioPlayer.GetStreamPlayback();

        int busIdx = AudioServer.GetBusIndex(CaughtBusName);
        if (busIdx < 0)
        {
            busIdx = AudioServer.BusCount;
            AudioServer.AddBus(busIdx);
            AudioServer.SetBusName(busIdx, CaughtBusName);
            AudioServer.SetBusSend(busIdx, "Master");
            AudioServer.AddBusEffect(busIdx, new AudioEffectReverb
            {
                RoomSize = 0.85f,
                Damping = 0.45f,
                Spread = 1f,
                Dry = 1f,
                Wet = 0.4f,
                PredelayMsec = 30f
            });
        }

        var caughtGen = new AudioStreamGenerator { MixRate = MixRate, BufferLength = BufferLength };
        _caughtPlayer = new AudioStreamPlayer { Stream = caughtGen, Bus = CaughtBusName };
        AddChild(_caughtPlayer);
        _caughtPlayer.Play();
        _caughtPlayback = (AudioStreamGeneratorPlayback)_caughtPlayer.GetStreamPlayback();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
            int idx = AudioServer.GetBusIndex(CaughtBusName);
            if (idx >= 0) AudioServer.RemoveBus(idx);
        }
    }

    public override void _Process(double delta)
    {
        CleanupStaleTrackers((float)delta);
        FillAudioBuffer();
    }

    public static void UpdateClientMeter(string uuid, float detectionValue)
    {
        if (Instance == null) return;

        if (detectionValue <= 0.001f)
            Instance._clientTrackers.Remove(uuid);
        else
            Instance._clientTrackers[uuid] = detectionValue;
    }

    private void CleanupStaleTrackers(float delta)
    {
        foreach (var key in _clientTrackers.Keys.ToList())
        {
            _clientTrackers[key] -= 0.5f * delta;
            if (_clientTrackers[key] <= 0f)
                _clientTrackers.Remove(key);
        }
    }

    private float Noise()
    {
        _noiseState ^= _noiseState << 13;
        _noiseState ^= _noiseState >> 17;
        _noiseState ^= _noiseState << 5;
        return (_noiseState / (float)uint.MaxValue) * 2f - 1f;
    }

    private static float Harm(float p) =>
        (Mathf.Sin(p) + 0.5f * Mathf.Sin(2f * p) + 0.3f * Mathf.Sin(3f * p) + 0.15f * Mathf.Sin(4f * p)) * 0.55f;

    private void FillAudioBuffer()
    {
        if (_playback == null || _caughtPlayback == null) return;

        float maxDetection = _clientTrackers.Count > 0 ? _clientTrackers.Values.Max() : 0f;
        bool active = maxDetection > 0.05f;

        if (_caughtArmed && maxDetection >= CaughtThreshold)
        {
            _caughtArmed = false;
            _caughtTime = 0f;
            _caughtThumpPhase = 0f;
            for (int k = 0; k < 3; k++) _caughtPhases[k] = 0f;

            _currentVolume = 0f;
            _panic = 0f;
            _prevPanic = 0f;
            _lpState = 0f;
            _stingerTime = -1f;

            _audioPlayer.Stop();
            _audioPlayer.Play();
            _playback = (AudioStreamGeneratorPlayback)_audioPlayer.GetStreamPlayback();
        }
        else if (!_caughtArmed && maxDetection < CaughtRearm)
        {
            _caughtArmed = true;
        }

        float targetFreq = Mathf.Lerp(MinFreq, MaxFreq, maxDetection);
        float targetVolume = active ? Mathf.Lerp(0.15f, 1f, maxDetection) : 0f;
        float targetPanic = Mathf.SmoothStep(PanicStart, 1f, maxDetection);

        if (!_caughtArmed)
        {
            targetVolume = 0f;
            targetPanic = 0f;
        }

        float tremRate = Mathf.Lerp(2f, 7f, maxDetection) + _panic * 5f;
        float bpm = Mathf.Lerp(90f, 160f, _panic);
        float beatPeriod = 60f / bpm;

        int frames = Mathf.Min(_playback.GetFramesAvailable(), _caughtPlayback.GetFramesAvailable());
        for (int i = 0; i < frames; i++)
        {
            if (!_caughtArmed)
            {
                _currentVolume = 0f;
                _panic = 0f;
                _prevPanic = 0f;
                _stingerTime = -1f;
            }

            _currentFreq += (targetFreq - _currentFreq) * FreqCoeff;
            _panic += (targetPanic - _panic) * PanicCoeff;

            float volCoeff = targetVolume > _currentVolume ? AttackCoeff : ReleaseCoeff;
            _currentVolume += (targetVolume - _currentVolume) * volCoeff;

            if (_prevPanic < 0.5f && _panic >= 0.5f)
            {
                _stingerTime = 0f;
                _stingerPhase = 0f;
            }

            _prevPanic = _panic;

            _tremPhase += Mathf.Tau * tremRate / MixRate;
            if (_tremPhase > Mathf.Tau) _tremPhase -= Mathf.Tau;
            float depth = Mathf.Lerp(TremoloDepth, 0.5f, _panic);
            float trem = 1f - depth * (0.5f + 0.5f * Mathf.Sin(_tremPhase));

            _vibPhase += Mathf.Tau * 5.5f / MixRate;
            if (_vibPhase > Mathf.Tau) _vibPhase -= Mathf.Tau;
            float vib = 1f + Mathf.Lerp(0.004f, 0.012f, _panic) * Mathf.Sin(_vibPhase);
            float f = _currentFreq * vib;

            float baseTone = (Harm(_phase) + Harm(_phaseChorus)) * 0.5f;
            float tension = (Harm(_phaseTritone) * 0.5f + Harm(_phaseDetune) * 0.5f) * _panic;
            float tone = baseTone * (1f - 0.3f * _panic) + tension;

            _breathLp += 0.15f * (Noise() - _breathLp);
            tone += _breathLp * 0.25f;

            float drive = 1f + 2.5f * _panic;
            tone = Mathf.Tanh(tone * drive) / Mathf.Sqrt(drive);

            tone *= _currentVolume * _currentVolume * trem;

            _beatTime += 1f / MixRate;
            if (_beatTime >= beatPeriod) _beatTime -= beatPeriod;
            float lub = Mathf.Exp(-_beatTime * 22f) * Mathf.Sin(Mathf.Tau * 55f * _beatTime);
            float t2 = _beatTime - 0.2f;
            float dub = t2 > 0f ? 0.7f * Mathf.Exp(-t2 * 22f) * Mathf.Sin(Mathf.Tau * 48f * t2) : 0f;
            float heart = (lub + dub) * (1f + 0.4f * Mathf.Sin(Mathf.Tau * 110f * _beatTime)) * _panic * 0.9f;

            float noiseCoeff = Mathf.Lerp(0.03f, 0.45f, _panic);
            _noiseLp += noiseCoeff * (Noise() - _noiseLp);
            float riser = _noiseLp * _panic * _panic * 0.5f;

            float stinger = 0f;

            float caught = 0f;
            if (_caughtTime >= 0f)
            {
                float t = _caughtTime;
                float atk = Mathf.Min(t / 0.003f, 1f);

                float thumpF = Mathf.Lerp(150f, 45f, Mathf.Min(t / 0.08f, 1f));
                float thump = Mathf.Sin(_caughtThumpPhase) * Mathf.Exp(-t * 16f);
                _caughtThumpPhase += Mathf.Tau * thumpF / MixRate;

                float cluster = 0f;
                for (int k = 0; k < 3; k++)
                {
                    cluster += Mathf.Tanh(Mathf.Sin(_caughtPhases[k]) * 4f);
                    _caughtPhases[k] += Mathf.Tau * CaughtBaseHz[k] / MixRate;
                }

                cluster = cluster / 3f * Mathf.Exp(-t * 14f);

                float n = Noise();
                _crackLp += 0.2f * (n - _crackLp);
                float crack = (n - _crackLp) * Mathf.Exp(-t * 28f);

                caught = (thump * 0.9f + cluster * 0.5f + crack * 0.6f) * atk;

                _caughtTime += 1f / MixRate;
                if (_caughtTime > CaughtDuration) _caughtTime = -1f;
            }

            float sustained = (tone + heart + riser + stinger) * MaxVolume * UserVolume;
            sustained = Mathf.Tanh(sustained * 1.5f) / 1.5f;

            float lp = Mathf.Lerp(LowPassCoeff, 0.45f, _panic);
            _lpState += lp * (sustained - _lpState);

            float caughtOut = Mathf.Tanh(caught * CaughtVolume * UserVolume * 1.2f) / 1.2f;

            _playback.PushFrame(new Vector2(_lpState, _lpState));
            _caughtPlayback.PushFrame(new Vector2(caughtOut, caughtOut));

            _phase += Mathf.Tau * f / MixRate;
            _phaseChorus += Mathf.Tau * f * 1.008f / MixRate;
            _phaseTritone += Mathf.Tau * f * 1.414f / MixRate;
            _phaseDetune += Mathf.Tau * f * 1.02f / MixRate;
            if (_phase > Mathf.Tau) _phase -= Mathf.Tau;
            if (_phaseChorus > Mathf.Tau) _phaseChorus -= Mathf.Tau;
            if (_phaseTritone > Mathf.Tau) _phaseTritone -= Mathf.Tau;
            if (_phaseDetune > Mathf.Tau) _phaseDetune -= Mathf.Tau;
        }
    }
}