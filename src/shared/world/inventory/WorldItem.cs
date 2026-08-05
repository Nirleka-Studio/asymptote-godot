using System;
using System.Linq;
using Asymptote.Shared.World.Entity;
using Asymptote.Shared.World.Entity.Component;
using Asymptote.Shared.World.Entity.Component.Inventory;
using Asymptote.Shared.World.Entity.Player;
using Asymptote.Shared.World.Level.Scene;
using Godot;

namespace Asymptote.Shared.World.Inventory;

/// <summary>
/// Class <c>WorldItem</c> represents a physical ItemStack sitting in the Scene
/// that can be picked up.
/// </summary>
public partial class WorldItem : RigidBody3D, IEntity
{
    public ItemStack stack { get; private set; }

    private Scene scene;
    private Vector3 lastReportedPosition;

    private AudioStreamPlayer3D audioPlayer;
    private AudioStreamGenerator audioGenerator;
    private Vector3 previousVelocity = Vector3.Zero;

    [ExportGroup("Audio Settings")] [Export]
    public float volumeDb = -10.0f;

    [Export] public float collisionVolume = 1.0f;
    [Export] public StringName audioBus = "Master";

    // Mortar whistle
    [ExportGroup("Mortar Whistle")] [Export]
    public float fallVelocityThreshold = 8.0f;

    [Export] public float minWhistleFreq = 202.0f;
    [Export] public float maxWhistleFreq = 276.0f;
    private float whistlePhase = 0.0f;

    // Impact parameters
    [ExportGroup("Impact Sound")] [Export] public float minImpactVelocity = 0.5f;
    [Export] public float maxImpactVelocity = 15.0f;
    [Export] public float sampleRate = 44100.0f;
    [Export] public float basePitchHz = 380.0f; // High enough for punch, low enough to avoid ear bleed

    public string uuid { get; set; }
    public int instId { get; set; }
    public event Action<IEntity> onPositionChanged;
    public event Action<IEntity> onRemovedFromWorld;

    private IEntity.RemovalReason? removalReason;

    public string getUuid() => uuid;

    public Vector3 getPosition() => GlobalPosition;

    public Rid getRid() => GetRid();

    public void setScene(Scene scene)
    {
        this.scene = scene;
    }

    public bool isInScene()
    {
        return scene != null;
    }

    public void update(double deltaTime, double currentTime)
    {
        if (this.getPosition().Y < this.scene.minY)
        {
            ((IEntity)this).remove(IEntity.RemovalReason.BELOW_WORLD);
        }
    }

    public void setStack(ItemStack givenStack)
    {
        this.stack = givenStack;
        refreshVisual();
    }

    public override void _Ready()
    {
        refreshVisual();
        setupCollisionAudio();
    }

    private void setupCollisionAudio()
    {
        ContactMonitor = true;
        MaxContactsReported = 4;
        BodyEntered += OnBodyEntered;

        audioGenerator = new AudioStreamGenerator
        {
            MixRate = sampleRate,
            BufferLength = 0.1f
        };

        audioPlayer = new AudioStreamPlayer3D
        {
            Stream = audioGenerator,
            Autoplay = false,
            VolumeDb = volumeDb,
            Bus = audioBus
        };

        AddChild(audioPlayer);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GlobalPosition != lastReportedPosition)
        {
            lastReportedPosition = GlobalPosition;
            onPositionChanged?.Invoke(this);
        }

        // Handle falling mortar whistle
        float speed = LinearVelocity.Length();
        if (speed >= fallVelocityThreshold)
        {
            SynthesizeMortarWhistle(speed);
        }

        previousVelocity = LinearVelocity;
    }

    private void SynthesizeMortarWhistle(float currentSpeed)
    {
        if (!audioPlayer.Playing)
        {
            audioPlayer.Play();
        }

        var playback = audioPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        if (playback == null) return;

        // Scale whistle pitch and volume with current falling speed
        float speedRatio = Mathf.Clamp((currentSpeed - fallVelocityThreshold) / maxImpactVelocity, 0.0f, 1.0f);
        float currentFreq = Mathf.Lerp(minWhistleFreq, maxWhistleFreq, speedRatio);

        // Scaled lower amplitude range
        float amplitude = Mathf.Lerp(0.02f, 0.2f, speedRatio) * collisionVolume;

        float phaseIncrement = (currentFreq * Mathf.Tau) / sampleRate;
        int framesToPush = playback.GetFramesAvailable();

        for (int i = 0; i < framesToPush; i++)
        {
            float sampleVal = Mathf.Sin(whistlePhase) * amplitude;
            var frame = new Vector2(sampleVal, sampleVal);

            playback.PushFrame(frame);
            whistlePhase = Mathf.PosMod(whistlePhase + phaseIncrement, Mathf.Tau);
        }
    }

    private void OnBodyEntered(Node body)
    {
        float speed = previousVelocity.Length();
        if (speed < minImpactVelocity) return;

        float t = Mathf.Clamp((speed - minImpactVelocity) / (maxImpactVelocity - minImpactVelocity), 0.0f, 1.0f);

        float frequency = Mathf.Lerp(basePitchHz, basePitchHz * 2.0f, t);

        // Reduced peak amplitude from (0.3f..0.9f) down to (0.05f..0.35f), then multiplied by collisionVolume
        float amplitude = Mathf.Lerp(0.05f, 0.35f, t) * collisionVolume;

        GenerateImpactTone(frequency, amplitude);
    }

    private void GenerateImpactTone(float frequency, float maxAmplitude)
    {
        if (!audioPlayer.Playing)
        {
            audioPlayer.Play();
        }

        var playback = audioPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        if (playback == null) return;

        int targetFrames = (int)(sampleRate * 0.08f);
        int availableFrames = playback.GetFramesAvailable();
        int framesToGenerate = Mathf.Min(targetFrames, availableFrames);

        float phase = 0.0f;

        for (int i = 0; i < framesToGenerate; i++)
        {
            float progress = (float)i / framesToGenerate;

            float envelope = Mathf.Exp(-progress * 9.0f) * maxAmplitude;

            float currentFreq = frequency * (1.0f + 0.4f * (1.0f - progress));
            float phaseIncrement = (currentFreq * Mathf.Tau) / sampleRate;

            float rawSample = Mathf.Sin(phase) * envelope;
            float cleanSample = Mathf.Clamp(rawSample, -0.85f, 0.85f);

            var frame = new Vector2(cleanSample, cleanSample);

            playback.PushFrame(frame);

            phase = Mathf.PosMod(phase + phaseIncrement, Mathf.Tau);
        }
    }

    private void refreshVisual()
    {
        if (stack == null) return;
        // TODO: ItemPresentation.worldModel here once that resource exists
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestPickup()
    {
        if (!Multiplayer.IsServer()) return;

        int senderId = Multiplayer.GetRemoteSenderId();
        var requester = GetTree().GetNodesInGroup("players")
            .OfType<PlayerEntity>()
            .FirstOrDefault(p => p.GetMultiplayerAuthority() == senderId);

        if (requester == null) return;

        var inventory = requester.getComponent<InventoryComponent>();
        tryPickup(inventory.main);
    }

    public bool tryPickup(InventoryContainer destination)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PushWarning("WorldItem.tryPickup called on a non-authoritative peer");
            return false;
        }

        if (!destination.addItem(stack))
        {
            return false;
        }

        QueueFree();
        return true;
    }

    #region Life

    public void setRemoved(IEntity.RemovalReason reason)
    {
        if (this.removalReason == null)
        {
            this.removalReason = reason;
        }

        onRemovedFromWorld?.Invoke(this);
        this.onRemoval(reason);
    }

    private void onRemoval(IEntity.RemovalReason reason)
    {
        if (reason != IEntity.RemovalReason.EXIT_TREE)
        {
            this.QueueFree();
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        ((IEntity)this).remove(IEntity.RemovalReason.EXIT_TREE);
    }

    #endregion
}