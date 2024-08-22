using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DynamicAudioEnvironmentTool : EditorWindow
{
    public AudioClip testAudioClip;
    public List<GameObject> noReverbZones = new List<GameObject>(); // List of game objects acting as no-reverb zones

    private Dictionary<string, AudioProfile> audioProfiles = new Dictionary<string, AudioProfile>();
    private string newProfileName = "";
    private AudioProfile currentProfile;
    private bool showReverbZones = true;

    private List<AudioSource> simulationAudioSources = new List<AudioSource>();
    private GameObject simulationParent;
    private GameObject reverbZoneParent;
    private const float gizmoDrawDistance = 50f;

    private bool sceneAnalyzed = false;
    private bool sceneChanged = false;
    private bool isPlayingAudio = false;

    private float volume = 1.0f;
    private float spatialBlend = 1.0f;
    private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    private float minDistance = 1.0f;
    private float maxDistance = 500.0f;

    public enum AmbientSetting
    {
        SmallRoom,
        LargeHall,
        Outdoor,
        Cave,
        Custom
    }
    private AmbientSetting selectedAmbientSetting = AmbientSetting.SmallRoom;

    private Vector2 scrollPos;
    private float clusterDistanceThreshold = 10f;

    // Debug logging options
    private bool logAmbientAreaChanges = true;
    private bool logActiveAmbientSection = true;

    private AmbientSetting lastAppliedSetting;

    [MenuItem("Tools/Dynamic Audio Environment Tool")]
    public static void ShowWindow()
    {
        GetWindow<DynamicAudioEnvironmentTool>("Audio Environment Tool");
    }

    private void OnEnable()
    {
        if (!audioProfiles.ContainsKey("Default"))
        {
            audioProfiles["Default"] = new AudioProfile("Default");
        }
        currentProfile = audioProfiles["Default"];
    }

    private void OnGUI()
    {
        // Scrollable window
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Dynamic Audio Environment Tool", EditorStyles.boldLabel);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Audio Profiles", EditorStyles.largeLabel);

        // Scrollable list of profiles
        GUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));
        foreach (var profileName in audioProfiles.Keys)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(profileName, GUILayout.Height(25)))
            {
                currentProfile = audioProfiles[profileName];
            }

            if (profileName != "Default" && GUILayout.Button("X", GUILayout.Width(20)))
            {
                audioProfiles.Remove(profileName);
                if (currentProfile.name == profileName)
                {
                    currentProfile = audioProfiles["Default"];
                }
                break;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        newProfileName = GUILayout.TextField(newProfileName, GUILayout.Width(200));

        if (GUILayout.Button("Create New Profile", GUILayout.Height(25)))
        {
            CreateNewProfile(newProfileName);
        }

        if (currentProfile != null)
        {
            GUILayout.Label($"Current Profile: {currentProfile.name}", EditorStyles.largeLabel);
            currentProfile.reverbLevel = EditorGUILayout.Slider("Reverb Level", currentProfile.reverbLevel, 0, 1);
            currentProfile.occlusionLevel = EditorGUILayout.Slider("Occlusion Level", currentProfile.occlusionLevel, 0, 1);

            if (GUILayout.Button("Apply Profile", GUILayout.Height(25)))
            {
                ApplyAudioProfile(currentProfile);
            }
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Ambient Settings", EditorStyles.largeLabel);
        selectedAmbientSetting = (AmbientSetting)EditorGUILayout.EnumPopup("Ambient Setting", selectedAmbientSetting);
        ApplyAmbientSetting(selectedAmbientSetting);
        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Reverb Zone Settings", EditorStyles.largeLabel);
        clusterDistanceThreshold = EditorGUILayout.FloatField("Cluster Distance Threshold", clusterDistanceThreshold);
        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        GUILayout.Label("No Reverb Zones", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("Add GameObjects to define no-reverb zones. Reverb zones will not be placed within these areas.", MessageType.Info);
        SerializedObject so = new SerializedObject(this);
        SerializedProperty noReverbZonesProp = so.FindProperty("noReverbZones");
        EditorGUILayout.PropertyField(noReverbZonesProp, true);
        so.ApplyModifiedProperties();
        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Geometry Analysis and Real-Time Simulation", EditorStyles.largeLabel);

        if (sceneAnalyzed)
        {
            if (sceneChanged)
            {
                GUILayout.Label("Scene has changed since the last analysis. Please reanalyze.", EditorStyles.helpBox);
            }
            else
            {
                GUILayout.Label("Scene has been analyzed and reverb zones have been placed.", EditorStyles.helpBox);
            }
        }
        else
        {
            GUILayout.Label("Scene has not been analyzed yet.", EditorStyles.helpBox);
        }

        GUILayout.Label("Test Audio Clip", EditorStyles.largeLabel);
        testAudioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", testAudioClip, typeof(AudioClip), false);

        if (selectedAmbientSetting == AmbientSetting.Custom)
        {
            GUILayout.Label("Custom Audio Settings", EditorStyles.largeLabel);
            volume = EditorGUILayout.Slider("Volume", volume, 0, 1);
            spatialBlend = EditorGUILayout.Slider("Spatial Blend", spatialBlend, 0, 1);
            rolloffMode = (AudioRolloffMode)EditorGUILayout.EnumPopup("Rolloff Mode", rolloffMode);
            minDistance = EditorGUILayout.FloatField("Min Distance", minDistance);
            maxDistance = EditorGUILayout.FloatField("Max Distance", maxDistance);
        }

        if (GUILayout.Button("Analyze Scene Geometry and Place Reverb Zones", GUILayout.Height(30)))
        {
            AnalyzeSceneGeometryAndPlaceReverbZones();
        }

        if (GUILayout.Button("Place Audio Sources", GUILayout.Height(30)))
        {
            PlaceAudioSources();
        }

        if (GUILayout.Button("Remove Audio Sources", GUILayout.Height(30)))
        {
            RemoveAudioSources();
        }

        if (GUILayout.Button(isPlayingAudio ? "Stop Audio" : "Play Audio", GUILayout.Height(30)))
        {
            ToggleAudioPlayback();
        }

        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Debug Logging Options", EditorStyles.largeLabel);
        logAmbientAreaChanges = EditorGUILayout.Toggle("Log Ambient Area Changes", logAmbientAreaChanges);
        logActiveAmbientSection = EditorGUILayout.Toggle("Log Active Ambient Section", logActiveAmbientSection);
        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Visualization Settings", EditorStyles.largeLabel);
        showReverbZones = EditorGUILayout.Toggle("Show Reverb Zones", showReverbZones);
        GUILayout.EndVertical();

        GUILayout.EndScrollView();

        SceneView.RepaintAll();
    }

    private void CreateNewProfile(string profileName)
    {
        if (!string.IsNullOrEmpty(profileName) && !audioProfiles.ContainsKey(profileName))
        {
            AudioProfile newProfile = new AudioProfile(profileName);
            audioProfiles.Add(profileName, newProfile);
            currentProfile = newProfile;
            Debug.Log($"Created new audio profile: {profileName}");
        }
        else
        {
            Debug.LogWarning("Profile name is either empty or already exists.");
        }
    }

    private void ApplyAudioProfile(AudioProfile profile)
    {
        foreach (var reverbZone in FindObjectsOfType<AudioReverbZone>())
        {
            reverbZone.reverbPreset = profile.GetReverbPreset();
        }

        Debug.Log($"Applied audio profile: {profile.name}");
    }

    private void ApplyAmbientSetting(AmbientSetting setting)
    {
        if (logActiveAmbientSection && setting != lastAppliedSetting)
        {
            Debug.Log($"Applying Ambient Setting: {setting}");
            lastAppliedSetting = setting;
        }

        switch (setting)
        {
            case AmbientSetting.SmallRoom:
                volume = 0.8f;
                spatialBlend = 1.0f;
                rolloffMode = AudioRolloffMode.Logarithmic;
                minDistance = 1.0f;
                maxDistance = 20.0f;
                break;
            case AmbientSetting.LargeHall:
                volume = 0.7f;
                spatialBlend = 1.0f;
                rolloffMode = AudioRolloffMode.Linear;
                minDistance = 10.0f;
                maxDistance = 100.0f;
                break;
            case AmbientSetting.Outdoor:
                volume = 0.6f;
                spatialBlend = 0.8f;
                rolloffMode = AudioRolloffMode.Linear;
                minDistance = 5.0f;
                maxDistance = 300.0f;
                break;
            case AmbientSetting.Cave:
                volume = 0.9f;
                spatialBlend = 1.0f;
                rolloffMode = AudioRolloffMode.Custom;
                minDistance = 2.0f;
                maxDistance = 50.0f;
                break;
            case AmbientSetting.Custom:
                break;
        }
    }

    private void AnalyzeSceneGeometryAndPlaceReverbZones()
    {
        sceneAnalyzed = false;
        int totalObjects = FindObjectsOfType<GameObject>().Length;
        int processedObjects = 0;

        // Clear existing reverb zones
        if (reverbZoneParent != null)
        {
            DestroyImmediate(reverbZoneParent);
        }
        reverbZoneParent = new GameObject("ReverbZones");

        foreach (GameObject obj in FindObjectsOfType<GameObject>())
        {
            processedObjects++;
            EditorUtility.DisplayProgressBar("Analyzing Scene Geometry", $"Processing {obj.name} ({processedObjects}/{totalObjects})", (float)processedObjects / totalObjects);

            if (!obj.activeInHierarchy || Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, obj.transform.position) > gizmoDrawDistance)
                continue;

            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null && !IsInsideNoReverbZone(renderer.bounds))
            {
                Vector3 position = obj.transform.position;
                if (IsPositionFarFromClusters(position))
                {
                    CreateReverbZone(position);
                }
            }
        }

        EditorUtility.ClearProgressBar();
        sceneAnalyzed = true;
        sceneChanged = false;
        Debug.Log("Scene geometry analysis complete, reverb zones placed.");
    }

    private bool IsInsideNoReverbZone(Bounds bounds)
    {
        foreach (var zone in noReverbZones)
        {
            if (zone != null && zone.GetComponent<Collider>().bounds.Intersects(bounds))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsPositionFarFromClusters(Vector3 position)
    {
        foreach (Transform cluster in reverbZoneParent.transform)
        {
            if (Vector3.Distance(position, cluster.position) < clusterDistanceThreshold)
            {
                return false;
            }
        }
        return true;
    }

    private void CreateReverbZone(Vector3 position)
    {
        GameObject reverbZone = new GameObject("ReverbZone");
        reverbZone.transform.position = position;
        reverbZone.transform.parent = reverbZoneParent.transform;
        AudioReverbZone audioReverbZone = reverbZone.AddComponent<AudioReverbZone>();

        // Apply the current profile settings
        audioReverbZone.reverbPreset = currentProfile.GetReverbPreset();

        Debug.Log($"Placed reverb zone at {position}");
    }

    private void PlaceAudioSources()
    {
        if (simulationParent != null)
        {
            DestroyImmediate(simulationParent);
        }

        simulationParent = new GameObject("SimulatedAudioSources");

        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null && !IsInsideNoReverbZone(renderer.bounds))
            {
                GameObject audioSourceObject = new GameObject("SimulatedAudioSource");
                audioSourceObject.transform.position = renderer.bounds.center;
                audioSourceObject.transform.parent = simulationParent.transform;

                AudioSource audioSource = audioSourceObject.AddComponent<AudioSource>();
                audioSource.clip = GetSampleAudioClip();
                audioSource.volume = volume;
                audioSource.spatialBlend = spatialBlend;
                audioSource.rolloffMode = rolloffMode;
                audioSource.minDistance = minDistance;
                audioSource.maxDistance = maxDistance;
                audioSource.loop = true;

                simulationAudioSources.Add(audioSource);
            }
        }

        Debug.Log("Placed audio sources in the scene.");
    }

    private void RemoveAudioSources()
    {
        if (simulationParent != null)
        {
            DestroyImmediate(simulationParent);
        }

        simulationAudioSources.Clear();
        Debug.Log("Removed all audio sources from the scene.");
    }

    private void ToggleAudioPlayback()
    {
        if (isPlayingAudio)
        {
            foreach (var audioSource in simulationAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                }
            }

            isPlayingAudio = false;
            Debug.Log("Stopped audio playback.");
        }
        else
        {
            foreach (var audioSource in simulationAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.Play();
                }
            }

            isPlayingAudio = true;
            Debug.Log("Started audio playback.");
        }
    }

    private AudioClip GetSampleAudioClip()
    {
        if (testAudioClip != null)
        {
            return testAudioClip;
        }
        else
        {
            return AudioClip.Create("SampleClip", 44100, 1, 44100, false);
        }
    }

    private void OnDrawGizmos()
    {
        if (showReverbZones && reverbZoneParent != null)
        {
            foreach (Transform reverbZone in reverbZoneParent.transform)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(reverbZone.position, reverbZone.GetComponent<AudioReverbZone>().maxDistance);
            }
        }
    }

    private void OnHierarchyChange()
    {
        sceneChanged = true;
    }
}

public class AudioProfile
{
    public string name;
    public float reverbLevel;
    public float occlusionLevel;

    public AudioProfile(string name)
    {
        this.name = name;
        this.reverbLevel = 0.5f;
        this.occlusionLevel = 0.5f;
    }

    public AudioReverbPreset GetReverbPreset()
    {
        if (reverbLevel < 0.3f)
            return AudioReverbPreset.Room;
        else if (reverbLevel < 0.7f)
            return AudioReverbPreset.Hallway;
        else
            return AudioReverbPreset.Cave;
    }
}
