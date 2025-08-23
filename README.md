ReadyPersonTo3D — Integrated project (Python tracker + Unity skeleton)
================================================================

What you get in this ZIP:
- python-tracker/: OpenCV + MediaPipe tracker that detects people, computes normalized bone directions, and sends JSON UDP packets to Unity (port 5056). Uses DeepFace for gender classification.
- unity-avatar/: Unity project skeleton with scripts to receive UDP, spawn random male/female avatars from Resources, and retarget pose vectors to Humanoid rigs in real time.
- Assets/Resources/Avatars/Male and Female directories (empty) — put your Mixamo FBX files here.

----------------------------------------------------------------
Step-by-step setup (Windows 10/11) — Python tracker (CPU)
----------------------------------------------------------------
1) Install Python 3.10.x (very important). Avoid 3.11+ for MediaPipe compatibility.
2) Open a command prompt:
   cd path\to\ReadyPersonTo3D\python-tracker
   py -3.10 -m venv env
   env\Scripts\activate
   python -m pip install --upgrade pip wheel setuptools
   pip install -r requirements.txt
3) Run the tracker:
   python -m app.main
   - A camera window will open. When you stand in front of it, the tracker will detect you, estimate gender, and send UDP JSON to 127.0.0.1:5056.

Optional: GPU
- MediaPipe runs fine on CPU. If you want DeepFace/TensorFlow GPU, install compatible tensorflow and NVIDIA drivers (advanced).

----------------------------------------------------------------
Step-by-step setup — Unity side
----------------------------------------------------------------
1) Open Unity Hub → New Project → 3D (Core). Name it (e.g., unity-avatar) OR use the provided unity-avatar folder in this ZIP by copying Assets folder contents into your Unity project.
2) In the Project window, make sure you can see these scripts under Assets/Scripts/Net, Retarget, Spawner.
3) Put your avatars (Mixamo FBX files) into these folders inside your Unity project:
   Assets/Resources/Avatars/Male/
   Assets/Resources/Avatars/Female/
   (These are present in this ZIP; just drop your FBX files into them)
4) For each FBX you imported:
   - Click the FBX in Project window → Inspector → Rig tab → Animation Type: Humanoid → Apply.
5) Create a new empty GameObject in your scene called 'SceneRoot' (or similar).
   - Add 'RandomAvatarSpawner' component to it.
   - Optionally create a child Empty called 'SpawnRoot' and assign it to SpawnRoot field on the spawner.
   - Adjust SpawnPosition and SpawnEuler (set Euler Y to 180 to face camera).
6) Create another empty GameObject called 'Network' and add 'UdpPoseReceiver' to it.
7) Add 'UnityMainThreadDispatcher' to an empty GameObject (or it will be auto-created at runtime).
8) Press Play in Unity. Then run the Python tracker. When you step in front of camera:
   - Python will detect gender and send JSON to Unity.
   - Unity will spawn a random avatar from the corresponding gender folder and apply pose updates.
9) If avatars don't spawn: check Unity Console for errors. Also ensure Windows Firewall allows Unity and port 5056 inbound UDP.

----------------------------------------------------------------
Quick testing without avatars
----------------------------------------------------------------
- If no avatars are present, Unity will warn in the Console. You can create a simple placeholder by right-clicking in Hierarchy -> 3D Object -> Capsule, turn it into a Prefab and move that prefab into Assets/Resources/Avatars/Male (or Female) for quick testing.

----------------------------------------------------------------
Troubleshooting & Tips
----------------------------------------------------------------
- mediapipe installation issues: use Python 3.10. Check pip errors and upgrade pip if necessary.
- DeepFace downloads model weights on first run (requires internet) — expect a large one-time download.
- If avatar limbs are twisted, tweak PoseMapper.AimBone or adjust bone axes for your rig. Each rig may need small calibration offsets.
- For production, consider using a proper JSON library on the Unity side (e.g., Newtonsoft.Json package) for robust parsing.
- If UDP messages are not received, ensure both Python and Unity run on same machine and port 5056 is free. Check Windows firewall settings.

----------------------------------------------------------------
File layout inside ZIP:
ReadyPersonTo3D/
  python-tracker/
    requirements.txt
    app/
      main.py
      mediapipe_pose.py
      gender.py
      feature_utils.py
      smoothing.py
      person_gate.py
      pose_sender.py
  unity-avatar/
    Assets/
      Scripts/
        Net/UdpPoseReceiver.cs
        Retarget/PoseMapper.cs
        Retarget/AvatarRetarget.cs
        Retarget/JointLowPass.cs
        Spawner/RandomAvatarSpawner.cs
        UnityMainThreadDispatcher.cs
      Resources/
        Avatars/
          Male/  (drop male FBX prefabs here)
          Female/ (drop female FBX prefabs here)

----------------------------------------------------------------
Ready-to-run ZIP:
- Download the ZIP provided below, unzip to a folder on your machine.
- Follow the Python and Unity steps above to run both sides.

----------------------------------------------------------------
Notes:
- Avatars are NOT included due to licensing/size — add your Mixamo exports into the Resources folders.
- If you want, send me one FBX or a screenshot of your rig’s bone names and I can suggest per-rig calibration settings for PoseMapper.

