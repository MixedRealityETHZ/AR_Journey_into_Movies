# AR Journey to Movie

## 1. Motivation & Project Description

**Abstract:**  Visiting real-world locations where iconic movie scenes were filmed offers a unique connection between cinematic storytelling and physical space. However, accurately
aligning original film frames with the real environment remains challenging due to scale ambiguity, viewpoint mismatch, and environmental changes over time.
**AR Journey to Movie** is an augmented reality (AR) system that enables users to recreate cinematic shots at their original filming locations using a mobile device.
By integrating server-side Structure-from-Motion (SfM) models with real-time mobile AR sessions, the system guides users to the correct camera viewpoint and overlays
original movie frames directly onto the live camera view. This allows users not only to stand at the original filming position, but also to capture photos and videos with
precise cinematic alignment.

The project focuses on robust camera localization, efficient server-side processing, and intuitive AR guidance, demonstrating how computer vision and mobile AR can be combined to bridge film history and physical experience.

You can find the project **paper** here: [[link](https://github.com/MixedRealityETHZ/AR_Journey_into_Movies/blob/main/MR_report.pdf)]  
You can find the project **demo video** here: [[link](https://drive.google.com/file/d/1Oq_z8GhvoA68HsbQnFUXR84OpYLmLJHK/view?pli=1)]

This project was developed as part of the **Mixed Reality** (Fall Semester 2025) course at ETH Zurich.

---

## 2. Project Organization

This repository contains two main components: a mobile AR application and a server-side localization backend. Their responsibilities and interactions are summarized below.

### 2.1 Mobile AR Application (Unity)

- Built with **Unity (C#)** and mobile AR frameworks (ARKit / ARCore)
- Handles user interaction, camera tracking, and AR session management
- Continuously captures camera frames and AR poses during scanning
- Uploads selected frames to the server asynchronously
- Visualizes navigation guidance and overlays original movie frames onto the live camera view
- Allows users to apply visual filters and capture cinematic photos

### 2.2 Server-side Localization Backend (Python)

- Implemented in **Python**
- Manages pre-built **SfM reconstructions** of filming locations
- Performs image retrieval and camera localization using a modified **HLoc pipeline**
- Estimates camera poses in the SfM coordinate frame
- Computes a **similarity transformation (sRt)** to align the SfM world with the mobile AR session
- Returns refined movie frames AR session poses to the mobile client for accurate AR alignment

---

## 3. Core Application Flow

1. The user selects a predefined movie scene.
2. The mobile AR application guides the user to specific location by Google Maps and leads the user to scan the surrounding environment.
3. Camera frames and AR poses are uploaded to the server in the background.
4. The server localizes selected frames in the SfM model.
5. A similarity transformation aligns the SfM coordinate system with the AR session.
6. The original movie frame is overlaid onto the live camera view.
7. The user captures photos with optional functions (filters, movie characters).

![Core Flow](core_flow.png)

---

## 4. Repository Structure
```text
AR-Journey-to-Movie/
├── .vscode/                         # VSCode workspace settings
├── Assets/                          # Unity project assets (scenes, scripts, prefabs, UI, etc.)
├── Packages/                        # Unity package dependencies
├── ProjectSettings/                 # Unity project settings
│
├── Server/                          # Python-based localization server (modified HLoc)
│   ├── Hierarchical-Localization/   # Vendored HLoc codebase (with project-specific modifications)
│   ├── query_sfm_pose.py            # Single-image SfM pose query entry (HLoc-based)
│   ├── server_main.py               # Server entry point (handles requests / threading / pipeline orchestration)
│   └── utils.py                     # Shared utilities (FPS, conversions and other helpers)
│
├── core_flow.png                    # Workflow figure used in documentation/poster/README
├── UpgradeLog.htm                   
├── MR poster.pdf                    # Course poster 
├── MR_Midterm_Pre.pdf               # Midterm presentation slides
├── MR_Proposal_CAI_TANG_PANG_HUANG.pdf
├── MR_report.pdf                    # Final report
├── README.md
└── .gitignore
```

## 5. Reproduction Guide

### 5.1 Overview

This project consists of two tightly coupled components:

- **Unity Client**: a mobile AR application responsible for user interaction, frame acquisition, scanning, guiding, and photo capture.
- **Python Server**: a localization backend based on a **modified HLoc pipeline**, responsible for querying camera poses in pre-built SfM models and returning alignment results in real-time ARKit session.

The Unity application is designed to work **in conjunction with the server**.  
Without connecting to the server via an HTTPS endpoint, the core localization and alignment workflow cannot be executed.

---

### 5.2 Required SfM Data

The server relies on **pre-built Structure-from-Motion (SfM) reconstructions**.  
Currently, two real-world scenes are modeled and supported for the app:

- **ETH HG EO-Nord (indoor)**
- **ETH CAB main entrance (outdoor)**

Due to the large size of the SfM data, these reconstructions are **not included directly in the GitHub repository**.

Please download the SfM models from Polybox:

- **SfM models download**:  
  https://polybox.ethz.ch/index.php/s/i2cYKanDZ9AnMow

After downloading, place the SfM folders into the directory expected by the server (as referenced in `server_main.py` and `query_sfm_pose.py`).

---

### 5.3 Running the Server (Python Backend)

1. Create and activate a Python environment with the required dependencies:
   - PyTorch
   - HLoc and its dependencies
   - COLMAP / `pycolmap`

2. Ensure the downloaded SfM models are accessible to the server code.

3. Start the localization server:
   ```bash
   python Server/server_main.py
   
4. Once running, the server will:
   - receive query frames from the Unity client,
   - perform SfM-based camera localization using the modified HLoc pipeline,
   - return pose and alignment results to the client.


### 5.4 Running the Unity Client

1. Open the Unity project in Unity Hub.
2. Configure the server HTTPS URL in the Unity client settings.
3. Build and deploy the application to a supported mobile AR device.
4. Follow the in-app workflow as shown in the demo video.

### 5.5 Notes
  - The Unity client requires an active server connection for smooth operation.
  - The directory Server/Hierarchical-Localization/ contains a vendored version of HLoc with project-specific modifications.
  - Without the downloaded SfM data, the server will not be able to perform localization.

