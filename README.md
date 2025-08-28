YourProject/
├─ python-tracker/app/
│  ├─ main.py
│  ├─ mediapipe_pose.py
│  ├─ feature_utils.py
│  ├─ smoothing.py
│  ├─ pose_sender.py
│  ├─ person_gate.py
│  ├─ gender.py          ← final (auto-downloads tiny CNN)
│  ├─ gender_net.h5      ← optional offline model file
│  ├─ global_vars.py
│  └─ requirements.txt   ← pinned Python deps
│
└─ UnityProject/
   └─ Assets/
      ├─ Scripts/
      │  ├─ Net/
      │  │  └─ UdpPoseReceiver.cs       ← final
      │  └─ Retarget/
      │     ├─ RandomAvatarSpawner.cs   ← final
      │     ├─ AvatarRetarget.cs
      │     ├─ JointLowPass.cs
      │     └─ PoseMapper.cs
      └─ Resources/
         └─ Avatars/
            ├─ Male/     ← rigged Mixamo prefabs
            └─ Female/   ← rigged Mixamo prefabs




🐍 Python Setup
1. Install Python

Use Python 3.11.x (64-bit) (3.10 also works; avoid 3.12/3.13).

2. Create environment
cd python-tracker/app
py -3.11 -m venv venv
.\venv\Scripts\activate

3. Install requirements
python -m pip install --upgrade pip
pip install -r requirements.txt

4. Run
.\venv\Scripts\activate
python main.py

✅ Requirements (requirements.txt)
numpy==1.26.4
opencv-python==4.10.0.84
mediapipe==0.10.14
tensorflow==2.15.1

Camera tips

If webcam lags, enable MJPG:

cap.set(cv2.CAP_PROP_FOURCC, cv2.VideoWriter_fourcc(*"MJPG"))
cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)

Offline model

If no internet on first run:

Download the tiny CNN once:
simple_CNN.81-0.96.hdf5

Save it as gender_net.h5 next to gender.py.

🎮 Unity Setup
1. Prefabs

Put at least one rigged Mixamo prefab in:

Assets/Resources/Avatars/Male/
Assets/Resources/Avatars/Female/


Or disable useResources in RandomAvatarSpawner and drag prefabs into arrays.

2. Scripts

UdpPoseReceiver.cs

Listens on UDP port 5056.

Spawns avatars on "entered".

Respawns when gender changes.

RandomAvatarSpawner.cs

Loads male/female prefab pools.

Logs pool counts at Play.

Applies pose bones each frame.

PoseMapper.cs / AvatarRetarget.cs

Handles bone mapping and smoothing.

3. Unity version

Unity 2021/2022/2023 LTS — no extra packages needed.

4. Networking

Same PC: Python sends to 127.0.0.1:5056.

Different PCs: set host in Python (pose_sender.py or global_vars.py) to Unity machine’s IP.

Allow inbound UDP 5056 in Windows Firewall.

🔧 Validation Checklist

On each laptop:

Python

pip install -r requirements.txt

Run main.py → Webcam opens, pose draws, console prints FPS and "Gender: male/female".

Unity

Play → Console logs:
[Spawner] Male prefabs: X | Female prefabs: Y

Step into camera → avatar spawns.

Arms/legs move correctly.

Showing female face → avatar respawns from Female pool.

📄 Optional helpers
Start script

start_python.bat (auto-create venv + run):

@echo off
cd /d "%~dp0"
if not exist venv (
  py -3.11 -m venv venv
  call .\venv\Scripts\activate
  python -m pip install --upgrade pip
  pip install -r requirements.txt
) else (
  call .\venv\Scripts\activate
)
python main.py

Freeze exact versions

After install:

pip freeze > requirements.lock.txt


Use requirements.lock.txt to replicate exact same environment:

pip install -r requirements.lock.txt

✅ Known Good Versions

Python: 3.11.x (64-bit)

numpy: 1.26.4

opencv-python: 4.10.0.84

mediapipe: 0.10.14

tensorflow (CPU): 2.15.1

✨ Credits

MediaPipe
 for real-time pose.

oarriaga/face_classification
 tiny gender CNN.

Mixamo
 characters for Unity retarget.

👉 Do you want me to also bundle this README with a ready-made requirements.lock.txt generated from your working laptop, so other machines will get the exact same package builds?

You said:
yes
ChatGPT said:

Perfect 👍

Here’s how you can add a requirements.lock.txt alongside the README.md. This ensures every laptop installs the exact same builds (not just version ranges).

✅ How to generate requirements.lock.txt

On your working laptop (where everything is already running fine):

cd python-tracker/app
.\venv\Scripts\activate
pip freeze > requirements.lock.txt


This will output something like:

absl-py==2.1.0
attrs==23.2.0
flatbuffers==24.3.25
keras==2.15.0
mediapipe==0.10.14
numpy==1.26.4
opencv-python==4.10.0.84
protobuf==4.25.3
scipy==1.13.1
tensorflow==2.15.1
tensorflow-intel==2.15.1
wrapt==1.16.0


(The exact list will depend on what TensorFlow pulled in on your machine — that’s why we freeze it to lock it down.)

✅ How to use on a new laptop

Copy the whole project folder, including:

requirements.txt

requirements.lock.txt

gender_net.h5 (if offline)

On the new laptop:

cd python-tracker/app
py -3.11 -m venv venv
.\venv\Scripts\activate
pip install --upgrade pip
pip install -r requirements.lock.txt


That installs the exact same builds as your working environment.

✅ Update README.md

Add this section (already drafted for you):

### Freeze exact versions
On your main machine:
```bash
pip freeze > requirements.lock.txt


Then on new machines:

pip install -r requirements.lock.txt


This guarantees an identical Python environment across all laptops.


---

👉 Do you want me to **merge the `requirements.lock.txt` section directly into the README.md** I wrote


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

