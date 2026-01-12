import os
os.environ["KMP_DUPLICATE_LIB_OK"] = "TRUE"
os.environ["OMP_NUM_THREADS"] = "1"

import json
import socket
import time
from queue import Queue
from threading import Thread

import numpy as np
from flask import Flask, request, jsonify
from scipy.spatial.transform import Rotation as Rotate

from utils import (
    calculate_world_transform,
    fps_select_next_frame,
    best_score_from_rmse,
    validate_sim3,
)
from query_sfm_pose import query_sfm_pose

# ============================================================
# Configuration
# ============================================================

app = Flask(__name__)

BASE_DIR = os.path.dirname(os.path.abspath(__file__)) 
UPLOAD_FOLDER = os.path.join(BASE_DIR, "upload_test")
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

FILM_SCENE_FRAME_ROOT = r"E:\mixed_reality\mocker_v5\film_scene_frame"


MAX_QUEUE = 50
MIN_USED = 4

# Coordinate system correction matrices
Rfix_z90 = Rotate.from_euler("z", 90, degrees=True).as_matrix()
Rfix_x180 = Rotate.from_euler("x", 180, degrees=True).as_matrix()
D_FLIP = np.diag([1, 1, -1])

# ============================================================
# Global state
# ============================================================

GLOBAL_STATE = {
    "film_pose_session": None,
    "film_focal_length": None,
    "best_score": None,
    "best_rmse": None,
}

FILM_POSE = None
CURRENT_SFM_MODEL = None

# Frames waiting for SfM processing (session pose only)
raw_frames = []  
# Frames already used for Sim3 alignment
used_positions = []      
used_rotations = []      
used_sfm_positions = []  
used_sfm_rotations = [] 

# Upload queue
frame_queue = Queue(maxsize=MAX_QUEUE)


# ============================================================
# Flask endpoint
# ============================================================

@app.route("/upload", methods=["POST"]) 
def upload():
    global FILM_POSE, CURRENT_SFM_MODEL

    # Save uploaded image
    img = request.files["image"]
    name = os.path.basename(img.filename)
    path = os.path.join(UPLOAD_FOLDER, name)
    img.save(path)

    # Parse upload metadata
    meta = json.loads(request.form["meta_json"])
    movie = meta.get("movieName", None)
    scene = meta.get("sceneName", None)
    frame = meta.get("frameId", None)                       
    from_album = meta.get("isFromAlbum", False)
 
    # Load film pose and SfM model once 
    if FILM_POSE is None:
        pose_json_path = os.path.join(
            FILM_SCENE_FRAME_ROOT,
            movie,
            scene,
            f"{frame}.json"
        ) 

        with open(pose_json_path, "r", encoding="utf-8") as f:
            FILM_POSE = json.load(f)

        if CURRENT_SFM_MODEL is None and movie and scene:
            sfm_path = os.path.join(
                FILM_SCENE_FRAME_ROOT, movie, scene, "sfm"
            )
            CURRENT_SFM_MODEL = sfm_path

    # Push frame to processing queue
    try:
        frame_queue.put_nowait({
            "image_path": path,
            "image_name": name,
            "meta": meta,
        })
    except Exception:
        return jsonify({"success": False, "reason": "queue full"}), 429

    # Return current best estimate if available
    pose = GLOBAL_STATE["film_pose_session"]
    if pose is None:
        return jsonify({
            'success': False,
            'reason': 'please keep scanning',
        }), 202
    
    else:
        C_session = np.array(pose["translation"])
        quat_session = np.array(pose["rotation_xyzw"])

        print(f"[result] C_session = {np.round(C_session,3).tolist()}, quat = {np.round(quat_session,3).tolist()}")

        return jsonify({
            "success": True,
            "session_pose": {
                "translation": C_session.tolist(),
                "rotation_xyzw": quat_session.tolist()
            },
            "film_focal_length": GLOBAL_STATE["film_focal_length"]
        }), 200



# ============================================================
# Worker 1: Session pose preprocessing
# ============================================================

def worker_loop():                
    """
    Continuously convert uploaded frames into session-space poses.

    """
    while True:
        frame = frame_queue.get()
        try:
            process_frame_into_raw_pool(frame)
        except Exception as e:
            print("[worker error]", e)
        finally:
            frame_queue.task_done()


def process_frame_into_raw_pool(frame):
    """
    Convert raw session pose into a unified coordinate system
    and store it for later selection.
    """
    meta = frame["meta"]

    q_xyzw = np.array(meta["rotation_xyzw"], float)
    C_sess = np.array(meta["translation_m"], float)

    # Right-handed coordinate correction
    R_sess = Rotate.from_quat(q_xyzw).as_matrix()
    R_sess = D_FLIP @ R_sess @ D_FLIP
    C_sess = D_FLIP @ C_sess

    # Camera orientation normalization
    R_sess = R_sess @ Rfix_z90 @ Rfix_x180

    raw_frames.append({
        "image_name": frame["image_name"],
        "image_path": frame["image_path"],
        "C_sess": C_sess,
        "R_sess": R_sess,
    })


# ============================================================
# Worker 2: FPS selection, SfM localization, Sim3 alignment
# ============================================================

def algo_loop():
    while True:
        time.sleep(0.1)  

        if not raw_frames:
            continue

        raw_positions = [f["C_sess"] for f in raw_frames]

        # Initialize with the first frame
        if len(used_positions) == 0:
            f = raw_frames.pop(0)
            add_frame_to_used_with_sfm(f)
            continue

        # FPS selection
        idx, dist = fps_select_next_frame(used_positions, raw_positions)
        if idx is None:
            continue

        f = raw_frames.pop(idx) 
        add_frame_to_used_with_sfm(f)

        if len(used_positions) < MIN_USED:
            continue

        # Estimate Sim3 transform
        align = calculate_world_transform(
            used_positions, used_sfm_positions, allow_scale=True
        )
        stats = align.get("stats", {})
        rmse = stats.get("rmse", None)
        if rmse is None:
            continue

        if not validate_sim3(align, GLOBAL_STATE["best_rmse"]):
            continue

        GLOBAL_STATE["best_rmse"] = rmse

        # Transform film pose into session space
        s = align["scale"]
        R_sim3 = np.array(align["rotation_matrix"])
        t = np.array(align["translation"])

        C_sfm_film = np.array(FILM_POSE["translation"])
        R_sfm_film = Rotate.from_quat(np.array(FILM_POSE["rotation"])).as_matrix()

        C_sess_film = (1.0 / s) * (R_sim3.T @ (C_sfm_film - t))

        R_sess_film = R_sim3.T @ R_sfm_film
        R_sess_film = R_sess_film @ Rfix_x180.T @ Rfix_z90.T

        C_sess_film = D_FLIP @ C_sess_film
        R_sess_film = D_FLIP @ R_sess_film @ D_FLIP

        quat_sess = Rotate.from_matrix(R_sess_film).as_quat().tolist()

        new_pose = {
            "translation": C_sess_film.tolist(),
            "rotation_xyzw": quat_sess,
        }

        score = best_score_from_rmse(rmse)
        if GLOBAL_STATE["best_score"] is None or score > GLOBAL_STATE["best_score"]:
            GLOBAL_STATE["best_score"] = score
            GLOBAL_STATE["film_pose_session"] = new_pose
            print("Film pose updated, RMSE =", rmse)


def add_frame_to_used_with_sfm(frame_dict):
    """
    Run SfM localization for a selected frame and store it.
    
    """
    C_sess = frame_dict["C_sess"]
    R_sess = frame_dict["R_sess"]

    pose_sfm = query_sfm_pose(
        images=UPLOAD_FOLDER,
        query=frame_dict["image_name"],
        model_file=CURRENT_SFM_MODEL,
    )
    q_sfm = np.array(pose_sfm["rotation"])
    C_sfm = np.array(pose_sfm["translation"])
    R_sfm = Rotate.from_quat(q_sfm).as_matrix()

    f_est = pose_sfm["camera_params"]["f"]
    if GLOBAL_STATE["film_focal_length"] is None:
            GLOBAL_STATE["film_focal_length"] = float(f_est)

    used_positions.append(C_sess)
    used_rotations.append(R_sess)
    used_sfm_positions.append(C_sfm)
    used_sfm_rotations.append(R_sfm)

def clear_upload_folder():
    if os.path.isdir(UPLOAD_FOLDER):
        for filename in os.listdir(UPLOAD_FOLDER):
            file_path = os.path.join(UPLOAD_FOLDER, filename)
            if os.path.isfile(file_path) or os.path.islink(file_path):
                os.unlink(file_path)
            elif os.path.isdir(file_path):
                shutil.rmtree(file_path)
            

# ============================================================
# Server entry point
# ============================================================
if __name__ == "__main__":
    hostname = socket.gethostname()
    local_ip = socket.gethostbyname(hostname)
    clear_upload_folder() # clear upload history
    print(f"[server] running at http://{local_ip}:5000")

    Thread(target=worker_loop, daemon=True).start()
    Thread(target=algo_loop, daemon=True).start()

    app.run(host="0.0.0.0", port=5000, debug=False, threaded=False)
