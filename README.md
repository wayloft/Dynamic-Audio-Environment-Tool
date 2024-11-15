# Dynamic Audio Environment Tool for Unity

The **Dynamic Audio Environment Tool** is a Unity Editor extension designed to create immersive audio experiences by dynamically placing and managing audio sources, reverb zones, and occlusion effects in your scenes. This tool analyzes scene geometry and lets you fine-tune audio environments for different scenarios.

## Features

- **Audio Profiles**  
  Create, save, and manage audio profiles for quick reverb and occlusion adjustments.

- **Dynamic Reverb Zones**  
  Automatically place reverb zones based on scene geometry and remove zones overlapping no-reverb areas.

- **Customizable Audio Settings**  
  Adjust volume, spatial blend, rolloff modes, and distance thresholds for precise audio control.

- **Ambient Settings**  
  Predefined settings for Small Rooms, Large Halls, Outdoor environments, and more, with an option for custom audio configurations.

- **Scene Analysis**  
  Analyze scene geometry to optimize audio placements and remove unnecessary sources or zones.

- **Visualization Tools**  
  Enable or disable reverb zone visualizations to improve scene understanding.

- **No-Reverb Zones**  
  Define areas where reverb zones should be excluded using GameObjects as boundaries.

- **Real-Time Simulation**  
  Place and remove audio sources dynamically, and toggle playback for testing audio setups.

- **Debug Logging**  
  Optional debug logs for tracking ambient area changes and active audio sections.

## Installation

1. Download the script file: [DynamicAudioEnvironmentTool.cs](./DynamicAudioEnvironmentTool.cs).
2. Place the script in the `Editor` folder of your Unity project.  
   If the folder doesn't exist, create it.

## How to Use

1. Open the tool by navigating to `Tools > Dynamic Audio Environment Tool` in the Unity Editor.
2. Use the intuitive interface to:
   - Analyze your scene geometry and place reverb zones.
   - Adjust ambient settings or create custom configurations.
   - Add GameObjects to define no-reverb zones.
   - Simulate and test audio environments with real-time audio source placement and playback.
3. Manage and apply audio profiles to quickly configure reverb and occlusion levels.

## Key Functionalities

### Audio Profiles
- **Create New Profile**: Save reverb and occlusion settings for reuse.
- **Apply Profile**: Instantly apply saved profiles to all reverb zones in your scene.

### Ambient Settings
- Predefined settings for common environments:
  - **Small Room**
  - **Large Hall**
  - **Outdoor**
  - **Cave**
  - **Custom**

### No-Reverb Zones
- Add GameObjects as boundaries to exclude reverb zones within specific areas.

### Scene Analysis
- Automatically place reverb zones based on scene geometry.
- Remove reverb zones overlapping with no-reverb areas.

### Custom Audio Settings
- Fine-tune volume, spatial blend, rolloff modes, and distance thresholds.

### Real-Time Simulation
- Place or remove audio sources dynamically.
- Play or stop audio clips in real-time to test your setup.

### Debug Logging
- Enable or disable debug logs for ambient area changes and active audio sections.

## Demo Video

Check out this tool in action on YouTube:  
[![Dynamic Audio Environment Tool Demo](https://img.youtube.com/vi/IG22kWah_oM/maxresdefault.jpg)](https://www.youtube.com/watch?v=IG22kWah_oM)

Click the thumbnail to watch the video demonstration.

## License
This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.
