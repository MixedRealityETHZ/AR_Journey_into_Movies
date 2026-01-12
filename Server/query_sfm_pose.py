import os
import torch
import numpy as np
from pathlib import Path
import shutil
import pycolmap
from scipy.spatial.transform import Rotation as R
import h5py
import torch
from PIL import Image

from hloc import extract_features, extract_features_single, match_features
from hloc.localize_sfm import QueryLocalizer, pose_from_cluster


# Global SfM cache (loaded once and reused across queries)
GLOBAL_SFM_CACHE = { 
    "init": False,
    "models": {},
    "references": None,
}


# Feature extraction and matching configuration
feat_conf = {
    "output": "feats-disk",
    "preprocessing": {"grayscale": False, "resize_max": 1600},
    "model": {"name": "disk", "max_keypoints": 5000},
    "loader": {"batch_size": 1, "num_workers": 0, "shuffle": False},
}
matcher_conf = {
    "output": "matches-disk-lightglue",
    "model": {"name": "lightglue", "features": "disk"},
    "loader": {"batch_size": 1, "num_workers": 0, "shuffle": False},
}

TOPK=10 # the number of pre-retrival


# Global descriptor utilities
def load_disk_global_db(h5_path):
    """
    Load precomputed global DISK descriptors for database images.

    """
    with h5py.File(h5_path, "r") as f:
        names = [n.decode() for n in f["names"][()]]   
        feats = f["descriptors"][()]                  
    return names, feats

def compute_query_disk_global(feats_h5_path, query_rel_path):
    """
    Compute a global descriptor for a query image
    using mean pooling over DISK local descriptors.

    """
    with h5py.File(feats_h5_path, "r") as f:
        desc = f[query_rel_path]["descriptors"][()]   
        desc = desc.astype(np.float32).T              
        g = desc.mean(axis=0)
        g = g / np.linalg.norm(g)
        return g   


# SfM model loading (cached)
def load_sfm_model_once(model_root):
    """
    Load a SfM reconstruction only once
    and cache it in memory for subsequent queries.

    """
    model_root = str(Path(model_root).resolve())

    if model_root in GLOBAL_SFM_CACHE["models"]:
        return GLOBAL_SFM_CACHE["models"][model_root]["model"], \
               GLOBAL_SFM_CACHE["models"][model_root]["references"]

    sfm_path = Path(model_root) / "sfm"
    model = pycolmap.Reconstruction(sfm_path)

    references = [model.images[i].name for i in model.reg_image_ids()]

    GLOBAL_SFM_CACHE["models"][model_root] = {
        "model": model,
        "references": references
    }

    print(f"Loaded {len(references)} registered DB images into RAM.")

    return model, references

# Retrieval
def retrieve_topk_disk(db_names, db_global_feats, query_global, k):
    """
    Retrieve top-K database images using cosine similarity.

    """
    sims = db_global_feats @ query_global     
    idx = np.argsort(-sims)[:k]
    topk = [f"db/{db_names[i]}" for i in idx]  
    return topk

# Main localization pipeline
def query_sfm_pose(images, query, model_file):
    """
    Localize a query image in an SfM model using:
      - DISK features
      - Global descriptor retrieval (Top-K)
      - LightGlue matching
      - COLMAP PnP + RANSAC
    """
    
    images = Path(images)
    query_rel = str(Path(query).as_posix())

    base_dir = Path(model_file)

    feats_h5 = base_dir / f"{feat_conf['output']}.h5"
    matches_h5 = base_dir / f"{matcher_conf['output']}.h5"

    model, references = load_sfm_model_once(base_dir) 

    disk_global_h5 = base_dir / "disk_global.h5" # load gloabal feature file
    db_names, db_feats = load_disk_global_db(disk_global_h5)


    extract_features_single.main(
        feat_conf, image_dir=images, image_list=[query_rel], export_dir=base_dir
    ) # extract disk feature for each single image

    q_feat = compute_query_disk_global(feats_h5, query_rel)
    topk_refs = retrieve_topk_disk(db_names, db_feats, q_feat, k=TOPK) # retriving top-k database images in sfm for matching

    loc_pairs = base_dir / "pairs-loc.txt"

    with open(loc_pairs, "w", encoding="utf-8") as f:
        for r in topk_refs:
            f.write(f"{query_rel} {r}\n")

    match_features.main(
        matcher_conf,
        pairs=loc_pairs,
        features=feats_h5,
        matches=matches_h5,
        overwrite=True,
    ) # matching query image features with top-k database images
 
    try:
        camera = pycolmap.infer_camera_from_image(str(images / query_rel))
    except Exception:
        with Image.open(images / query_rel) as im:
            w, h = im.size
        fx = 1.2 * max(w, h)
        fy = fx
        cx, cy = w / 2.0, h / 2.0
        camera = pycolmap.Camera(
            model="SIMPLE_RADIAL", width=w, height=h, params=[fx, cx, cy, 0.0]
        )

    valid_topk = [
        r for r in topk_refs
        if model.find_image_with_name(r).image_id != -1
    ]
    ref_ids = [model.find_image_with_name(r).image_id for r in valid_topk]

    conf = {
        "estimation": {
            "estimate_focal_length": True, # estimated focal length
            "ransac": {"max_error": 12.0}},
        "refinement": {
            "refine_focal_length": True,
            "refine_extra_params": True
        },
    }
    localizer = QueryLocalizer(model, conf)

    ret, log = pose_from_cluster(
        localizer, query_rel, camera, ref_ids, feats_h5, matches_h5
    )
    if ret is None or "cam_from_world" not in ret:
        raise ValueError(f"Localization failed for {query_rel}")
    print(f"[localize] inliers={ret.get('num_inliers', -1)}, reproj_err={ret.get('reproj_error', -1):.2f}px")

    T_cw = ret["cam_from_world"]
    R_cw = T_cw.rotation.matrix()
    t_cw = np.array(T_cw.translation)
    R_wc = R_cw.T
    C_w = -R_cw.T @ t_cw
    quat_wc = R.from_matrix(R_wc).as_quat()

    if matches_h5.exists():
        matches_h5.unlink()
    if loc_pairs.exists():
        loc_pairs.unlink()

    f, cx, cy, k = ret["camera"].params # camera parameters = [f, cx, cy, k] 

    return {
        "rotation": quat_wc.tolist(), 
        "translation": C_w.tolist(), 
        "camera_params": {
        "f": float(f),
        "cx": float(cx),
        "cy": float(cy),
        "k": float(k)},
        }
    




