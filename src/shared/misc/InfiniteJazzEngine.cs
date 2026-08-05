using Godot;
using System;
using System.Collections.Generic;

namespace Asymptote.Shared.World.Audio;

// There is no wrong note in jazz.
public partial class InfiniteJazzEngine : Node
{
    private AudioStreamPlayer audioPlayer;
    private AudioStreamGeneratorPlayback playback;

    private bool isMuted = false; // Mute state flag

    private const float SampleRate = 44100.0f;
    private double bpm = 230; // 220.0 seems fine
    private double sampleTime = 0.0;

    private float phaseBass = 0.0f;
    private float phaseTrumpet = 0.0f;
    private float[] phaseChords = new float[4];

    private float bassEnv = 0.0f;
    private float trumpetEnv = 0.0f;
    private float pwapEnv = 0.0f; // Drives the PWAP filter/pitch scoop
    private float chordEnv = 0.0f;
    private float kickEnv = 0.0f;
    private float snareEnv = 0.0f;
    private float cymbalEnv = 0.0f;

    private float trumpetLowpass1 = 0.0f;
    private float trumpetLowpass2 = 0.0f;
    private float currentTrumpetFreq = 440.0f;
    private float targetTrumpetFreq = 440.0f;
    private float trumpetPitchScoop = 0.0f;
    private float trumpetPan = 0.0f; // Stereo position (-1.0 Left to +1.0 Right)
    private float panAngle = 0.0f;

    private ulong noiseSeed = 987654321;

    // Call and response state machine
    private enum BandState
    {
        TrumpetCall,
        PianoResponse,
        SoloBurst,
        TuttiHit
    }

    private BandState currentState = BandState.TrumpetCall;
    private int measureCounter = 0;

    private int totalSubdivisions = 0;
    private int chordIndex = 0;
    private float currentBassFreq = 110.0f;

    private readonly Random random = new Random();

    // Pitch frequencies (Hz)
    private const float C2 = 65.41f,
        Db2 = 69.30f,
        D2 = 73.42f,
        Eb2 = 77.78f,
        E2 = 82.41f,
        F2 = 87.31f,
        G2 = 98.00f,
        Ab2 = 103.83f,
        A2 = 110.00f,
        Bb2 = 116.54f,
        B2 = 123.47f;

    private const float C4 = 261.63f,
        Db4 = 277.18f,
        D4 = 293.66f,
        Eb4 = 311.13f,
        E4 = 329.63f,
        F4 = 349.23f,
        Gb4 = 369.99f,
        G4 = 392.00f,
        Ab4 = 415.30f,
        A4 = 440.00f,
        Bb4 = 466.16f,
        B4 = 493.88f;

    private const float C5 = 523.25f,
        Db5 = 554.37f,
        D5 = 587.33f,
        Eb5 = 622.25f,
        E5 = 659.25f,
        F5 = 698.46f,
        Gb5 = 739.99f,
        G5 = 783.99f,
        Ab5 = 830.61f,
        A5 = 880.00f,
        Bb5 = 932.33f,
        C6 = 1046.50f;

    private struct ChordDef
    {
        public float Root;
        public float[] Voicing;
        public float[] Scale;
    }

    private readonly List<ChordDef> progression = new List<ChordDef>
    {
        new ChordDef
            { Root = C2, Voicing = new[] { Eb4, G4, Bb4, D5 }, Scale = new[] { C5, Eb5, F5, G5, Bb5, C6 } }, // Cm7
        new ChordDef
            { Root = F2, Voicing = new[] { Eb4, Ab4, C5, F5 }, Scale = new[] { F4, Ab4, Bb4, C5, Eb5, F5 } }, // Fm7
        new ChordDef
            { Root = Bb2, Voicing = new[] { D4, F4, Ab4, C5 }, Scale = new[] { Bb4, C5, D5, Eb5, F5, G5 } }, // Bb7
        new ChordDef
            { Root = Eb2, Voicing = new[] { G4, Bb4, Db5, F5 }, Scale = new[] { Eb4, F4, G4, Bb4, C5, Eb5 } } // EbMaj7
    };

    public override void _Ready()
    {
        var generator = new AudioStreamGenerator
        {
            MixRate = SampleRate,
            BufferLength = 0.05f
        };

        audioPlayer = new AudioStreamPlayer { Stream = generator };
        AddChild(audioPlayer);
        audioPlayer.Play();

        playback = audioPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.M)
        {
            isMuted = !isMuted;
            GetTree().Root.SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (playback == null) return;

        int framesToFill = playback.GetFramesAvailable();
        double samplesPerSubdivision = (SampleRate * 60.0) / (bpm * 3.0); // Triplet swing timing

        for (int i = 0; i < framesToFill; i++)
        {
            sampleTime++;
            if (sampleTime >= samplesPerSubdivision)
            {
                sampleTime -= samplesPerSubdivision;
                OnSubdivisionTick();
            }

            // Output silence while muted, preserving synthesis timing in background
            Vector2 stereoFrame = isMuted ? Vector2.Zero : SynthesizeStereoFrame();
            playback.PushFrame(stereoFrame);
        }
    }

    private void OnSubdivisionTick()
    {
        totalSubdivisions++;
        int tripletStep = totalSubdivisions % 3;
        int beat = (totalSubdivisions / 3) % 4;
        int measure = (totalSubdivisions / 12);

        if (beat == 0 && tripletStep == 0)
        {
            chordIndex = measure % progression.Count;

            if (measure != measureCounter)
            {
                measureCounter = measure;
                if (measureCounter % 2 == 0)
                {
                    // Rotate Band State: Call -> Response -> Solo Burst -> Tutti PWAP
                    currentState = (BandState)((measureCounter / 2) % 4);
                }
            }
        }

        var activeChord = progression[chordIndex];

        // drums
        if (tripletStep == 0 || tripletStep == 2) cymbalEnv = (tripletStep == 2) ? 0.35f : 0.7f;
        if (tripletStep == 0 && (beat == 1 || beat == 3)) snareEnv = 0.85f;
        else if (random.NextDouble() < 0.2) snareEnv = 0.25f; // Ghost snare
        if (tripletStep == 0 && (beat == 0 || (beat == 2 && random.NextDouble() < 0.4))) kickEnv = 0.9f;

        // bass
        if (tripletStep == 0)
        {
            bassEnv = 1.0f;
            currentBassFreq = CalculateWalkingBass(beat, activeChord);
        }

        // call and response
        switch (currentState)
        {
            case BandState.TrumpetCall:
                // Trumpet plays a short 2-bar motif; piano stays subtle
                if (tripletStep != 1 && random.NextDouble() < 0.7)
                {
                    targetTrumpetFreq = PickScaleNote(activeChord);
                    trumpetEnv = 1.0f;
                    trumpetPan = -0.3f; // Call comes slightly from Left

                    // 25% chance to hit a "PWAP" accent
                    if (random.NextDouble() < 0.25) TriggerPwap();
                }

                if (beat == 0 && tripletStep == 0) chordEnv = 0.5f; // Light comping
                break;

            case BandState.PianoResponse:
                // Trumpet rests; Piano responds with aggressive chord comping
                trumpetEnv = 0.0f;
                if ((beat == 0 && tripletStep == 2) || (beat == 2 && tripletStep == 1) || random.NextDouble() < 0.4)
                {
                    chordEnv = 0.9f;
                }

                break;

            case BandState.SoloBurst:
                // Trumpet improvs high fast bursts, sweeping hard Left to Right
                if (random.NextDouble() < 0.85)
                {
                    targetTrumpetFreq = PickScaleNote(activeChord);
                    trumpetEnv = 1.0f;

                    // Pan sweep left to right
                    panAngle += 0.25f;
                    trumpetPan = Mathf.Sin(panAngle) * 0.85f;

                    if (tripletStep == 0 && random.NextDouble() < 0.35) TriggerPwap();
                }

                if (beat % 2 == 1 && tripletStep == 0) chordEnv = 0.4f;
                break;

            case BandState.TuttiHit:
                // Band plays together; Trumpet starts with a huge PWAP fall/scoop
                if (beat == 0 && tripletStep == 0)
                {
                    targetTrumpetFreq = activeChord.Scale[activeChord.Scale.Length - 1]; // High note
                    trumpetEnv = 1.0f;
                    TriggerPwap();
                    chordEnv = 1.0f;
                    snareEnv = 1.0f;
                    kickEnv = 1.0f;
                    trumpetPan = 0.0f; // Center stage hit
                }
                else if (tripletStep != 1 && random.NextDouble() < 0.5)
                {
                    targetTrumpetFreq = PickScaleNote(activeChord);
                    trumpetEnv = 0.85f;
                }

                break;
        }
    }

    private void TriggerPwap()
    {
        pwapEnv = 1.0f;
        trumpetPitchScoop = -80.0f; // Start pitch ~1-2 semitones lower for the scoop
    }

    private Vector2 SynthesizeStereoFrame()
    {
        var activeChord = progression[chordIndex];

        // Envelope Decay Rates
        bassEnv *= 0.9991f;
        trumpetEnv *= 0.9982f;
        pwapEnv *= 0.994f; // Fast PWAP filter decay
        chordEnv *= 0.998f;
        kickEnv *= 0.994f;
        snareEnv *= 0.991f;
        cymbalEnv *= 0.993f;

        // Smooth pitch glide & PWAP scoop recovery
        trumpetPitchScoop = Mathf.Lerp(trumpetPitchScoop, 0.0f, 0.03f);
        currentTrumpetFreq = Mathf.Lerp(currentTrumpetFreq, targetTrumpetFreq + trumpetPitchScoop, 0.012f);

        // BRASS / TRUMPETS
        float vibrato = Mathf.Sin(phaseTrumpet * 0.008f) * 6.0f;
        float trumpetInc = ((currentTrumpetFreq + vibrato) * Mathf.Tau) / SampleRate;
        phaseTrumpet = Mathf.PosMod(phaseTrumpet + trumpetInc, Mathf.Tau);

        // Raw Sawtooth + Square blend for cutting brass timbre
        float saw = (phaseTrumpet / Mathf.Pi) - 1.0f;
        float pulse = phaseTrumpet < Mathf.Pi ? 0.8f : -0.8f;
        float rawBrass = (saw * 0.6f) + (pulse * 0.4f);

        // Envelope-driven Cutoff
        float baseCutoff = 0.08f + (trumpetEnv * 0.30f) + (pwapEnv * 0.55f);

        // 2-Pole Resonant Filter Simulation
        trumpetLowpass1 += baseCutoff * (rawBrass - trumpetLowpass1);
        trumpetLowpass2 += baseCutoff * (trumpetLowpass1 - trumpetLowpass2);

        // Boosted Trumpet Gain (1.10 base vs old 0.55)
        float trumpetGain = 1.10f + (pwapEnv * 0.40f);
        float trumpetSignal = trumpetLowpass2 * trumpetEnv * trumpetGain;

        // Walking bass
        float bassInc = (currentBassFreq * Mathf.Tau) / SampleRate;
        phaseBass = Mathf.PosMod(phaseBass + bassInc, Mathf.Tau);
        // Reduced bass level from 0.45f to 0.18f so it stops squashing the mix
        float bassSignal = (Mathf.Sin(phaseBass) + ((phaseBass / Mathf.Pi) - 1.0f) * 0.3f) * bassEnv * 0.18f;

        // Piano chords
        float chordSignal = 0.0f;
        for (int ch = 0; ch < 4; ch++)
        {
            float chordInc = (activeChord.Voicing[ch] * Mathf.Tau) / SampleRate;
            phaseChords[ch] = Mathf.PosMod(phaseChords[ch] + chordInc, Mathf.Tau);
            chordSignal += (Mathf.Sin(phaseChords[ch]) + 0.2f * Mathf.Sin(phaseChords[ch] * 2.0f)) * 0.1f;
        }

        chordSignal *= chordEnv * 0.8f;

        // Drums
        noiseSeed = (noiseSeed * 1664525u + 1013904223u);
        float whiteNoise = ((float)(noiseSeed & 0x00FFFFFF) / 0x00FFFFFF) * 2.0f - 1.0f;

        // Tamed Kick sub-bass from 0.55f to 0.25f
        float kickSignal = Mathf.Sin(kickEnv * kickEnv * 0.6f) * kickEnv * 0.25f;
        float snareSignal = whiteNoise * snareEnv * 0.28f;
        float cymbalSignal = whiteNoise * cymbalEnv * 0.14f;

        // Panning
        // Frying pan
        // Cooking
        float trumpetL = trumpetSignal * (0.5f - 0.5f * trumpetPan);
        float trumpetR = trumpetSignal * (0.5f + 0.5f * trumpetPan);

        float leftMix = trumpetL + bassSignal + kickSignal + (snareSignal * 0.8f) + (chordSignal * 0.7f) +
                        (cymbalSignal * 0.3f);
        float rightMix = trumpetR + bassSignal + kickSignal + (snareSignal * 0.8f) + (chordSignal * 0.3f) +
                         (cymbalSignal * 0.7f);

        // Clean headroom saturator
        return new Vector2(Mathf.Tanh(leftMix * 1.0f), Mathf.Tanh(rightMix * 1.0f));
    }

    private float CalculateWalkingBass(int beat, ChordDef chord)
    {
        return beat switch
        {
            0 => chord.Root,
            1 => chord.Root * 1.25f,
            2 => chord.Root * 1.5f,
            3 => progression[(chordIndex + 1) % progression.Count].Root *
                 (random.NextDouble() > 0.5 ? 1.05946f : 0.94387f),
            _ => chord.Root
        };
    }

    private float PickScaleNote(ChordDef chord) => chord.Scale[random.Next(0, chord.Scale.Length)];
}