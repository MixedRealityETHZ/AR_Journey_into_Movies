import cv2
import numpy as np
import itertools
import math

# diverse & evaluation: global triggers and parameters
MIN_DIST_M = 0.20          # space location diversity threshold (m): min distance to used frame >=0.2m could be viewed as diverse frame
MIN_ANGLE_DEG = 10.0       # position diversity threshold (°)： min rotation diff >=10° could be viewed as diverse frame
MIN_USED_PAIRS = 4         # minimum frames to calculate sim3 and return
EPS = 1e-9


def validate_sim3(align, prev_best_rmse=None, scale_min=0.2, scale_max=5.0, min_inliers=4):
    """Validate the Sim3 alignment result.

    align            dict from calculate_world_transform()
    prev_best_median float or None
    scale_min,scale_max   allowed range of scale
    """

    s = align.get("scale", None)
    stats = align.get("stats", {})
    median_err = stats.get("median_err", None)
    rmse = stats.get("rmse", None)
    N_in = stats.get("N_in", None)

    if s is None or rmse is None:
        print("[Sim3] Missing scale or median_err in alignment result.")
        return False

    # 1) scale must be within a safe range
    if not (scale_min < s < scale_max):
        print(f"[Sim3] Reject: scale out of range ({s:.4f}).")
        return False

    # # 2) median error should not be worse than the best so far
    # if prev_best_median is not None and median_err > prev_best_median:
    #     print(f"[Sim3] Reject: median_err worsened ({median_err:.4f} > {prev_best_median:.4f}).")
    #     return False

    # 3） mse should not be suddenly increase
    if prev_best_rmse is not None and rmse > prev_best_rmse * 4.5:
        print(f"[Sim3] Reject: rmse worsened ({rmse:.4f} too big (>>> {prev_best_rmse:.4f}).")
        return False

    # 4) inliners (RANSAC subset) must be enough
    if N_in < min_inliers:
        print(f"[Sim3] Reject: N_in too small ({N_in} < {min_inliers}).")
        return False

    # Passed validation
    return True

# Farthest Point Sampling (FPS)
def fps_select_next_frame(used_positions, pending_positions):
    """
    true FPS:
    - for every frame in pending, calculate its min distance to all used frames
    - selected the biggest min distance frame

    return:
        idx: selected index in pending
        dist: this frame's min-distance
    """
    if len(pending_positions) == 0:
        return None, -1

    used = np.array(used_positions)
    pending = np.array(pending_positions)

    best_idx = None
    best_dist = -1.0

    for i, p in enumerate(pending):
        dists = np.linalg.norm(used - p, axis=1)
        min_dist = float(np.min(dists))
        if min_dist > best_dist:
            best_dist = min_dist
            best_idx = i

    print("Debug: selected frame idx",best_idx)
    print("Debug: ", len(pending))

    return best_idx, best_dist


# BEST Score = 1 / RMSE
def best_score_from_rmse(rmse):
    if rmse <= 0:
        return 1e9
    return 1.0 / float(rmse)


def rot_angle_deg(Ra, Rb):
    """两旋转(3x3)的夹角（度）。"""
    Rerr = Ra @ Rb.T
    c = np.clip((np.trace(Rerr) - 1.0) / 2.0, -1.0, 1.0)
    return float(math.degrees(math.acos(c)))


def is_diverse(new_C, new_R, used_C_list, used_R_list,
               min_dist=MIN_DIST_M, min_ang=MIN_ANGLE_DEG):
    """ whether the new upload frame compared to “used frames” is diverse
       logic: once it is satisfied to dist >=min_dist or rotation angle >=min_ang """
    if len(used_C_list) == 0:
        return True
    dists = [np.linalg.norm(new_C - C) for C in used_C_list]
    angs  = [rot_angle_deg(new_R, R) for R in used_R_list]
    return (min(dists) >= min_dist) or (min(angs) >= min_ang)

# quaternion -> rotation matrix (auto-recognize xyzw / wxyz of COLMAP SfM and ARKit/ARCore)
def quat_to_rot(q):
    q = np.asarray(q, dtype=float)
    assert q.shape[-1] == 4
    def toR_xyzw(qxyzw):
        x, y, z, w = qxyzw
        return np.array([
            [1 - 2*(y*y + z*z), 2*(x*y - z*w),     2*(x*z + y*w)],
            [2*(x*y + z*w),     1 - 2*(x*x + z*z), 2*(y*z - x*w)],
            [2*(x*z - y*w),     2*(y*z + x*w),     1 - 2*(x*x + y*y)]
        ], dtype=float)

    # try 2 sequence but choose the one whose result is det≈+1
    R1 = toR_xyzw(q)                 # if input [x,y,z,w]
    R2 = toR_xyzw(q[[1,2,3,0]])      # if input [w,x,y,z]
    d1, d2 = np.linalg.det(R1), np.linalg.det(R2)
    if abs(d1-1.0) < abs(d2-1.0):
        return R1, "xyzw"
    else:
        return R2, "wxyz"



def extract_frames_and_poses(video_path):
    """
    Extract frames from the video and the corresponding poses (mocked for now).
    In the real application, this would extract Tcw (camera pose) for each frame.
    """
    cap = cv2.VideoCapture(video_path)
    frames = []
    poses = []  # Tcw for each frame (mocked)
    frame_id = 0

    while True:
        ret, frame = cap.read()
        if not ret:
            break

        frames.append(frame)

        # For now, we mock poses as random values, in practice you'd get this from your mobile app
        poses.append({
            'rotation': [1.0, 0.0, 0.0, 0.0],  # Identity quaternion
            'translation': [frame_id * 0.1, 0.0, frame_id * 0.2]  # Mock translation
        })

        frame_id += 1

    cap.release()
    return frames, poses


def estimate_sim3_transform(session_points, sfm_points):
    """
    from the frames to estimate similarity by umeyama (Session world vs SfM world)
    input:
        session_points: Nx3 array
        sfm_points: Nx3 array
    output:
        (scale, R, t) or None (if the amount of frames < 3)
    """
    assert session_points.shape == sfm_points.shape, "not enough frames"
    N = session_points.shape[0]
    if N < 3:
        # frame amount not enough, no Sim3 calculation
        return None

    X = session_points
    Y = sfm_points
    Xc = X.mean(0)
    Yc = Y.mean(0)
    Xp = X - Xc
    Yp = Y - Yc

    Sigma = (Yp.T @ Xp) / N
    U, D, Vt = np.linalg.svd(Sigma)
    S = np.eye(3)
    if np.linalg.det(U @ Vt) < 0:
        S[2, 2] = -1.0
    R = U @ S @ Vt

    var_X = (Xp * Xp).sum() / N
    scale = np.trace(np.diag(D) @ S) / var_X
    t = Yc - scale * (R @ Xc)

    return float(scale), R, t


def umeyama_similarity(X, Y, with_scale=True):
    X = np.asarray(X, float); Y = np.asarray(Y, float)
    muX, muY = X.mean(0), Y.mean(0)
    Xc, Yc = X - muX, Y - muY
    Sigma = (Yc.T @ Xc) / len(X)
    U, D, Vt = np.linalg.svd(Sigma)
    S = np.eye(3)
    if np.linalg.det(U @ Vt) < 0:
        S[2, 2] = -1
    R = U @ S @ Vt
    if with_scale:
        varX = (Xc**2).sum() / max(len(X), 1)
        s = np.trace(np.diag(D) @ S) / max(varX, 1e-12)
    else:
        s = 1.0
    U2, _, Vt2 = np.linalg.svd(R); R = U2 @ Vt2
    if np.linalg.det(R) < 0: R[:, -1] *= -1
    t = muY - s * (R @ muX)
    return float(s), R, t


def estimate_sim3_ransac(X, Y, with_scale=True,
                         th_init=None, th_refine=None,
                         max_trials=500, min_inliers=4,
                         s_bounds=(1e-3, 1e3)):
    X = np.asarray(X, float); Y = np.asarray(Y, float)
    N = len(X); assert N >= 3, "需要>=3个对应点"

    if th_init is None:
        scale_Y = np.median(np.linalg.norm(Y - Y.mean(0), axis=1))
        th_init = max(1e-6, 0.05 * (scale_Y if scale_Y > 0 else 1.0))
    if th_refine is None:
        th_refine = th_init * 0.5

    best_inliers, best_num = None, -1
    best_model = (1.0, np.eye(3), np.zeros(3))
    rng = np.random.default_rng()

    for _ in range(max_trials):
        idx = rng.choice(N, size=3, replace=False)
        try:
            s, R, t = umeyama_similarity(X[idx], Y[idx], with_scale=with_scale)
            s = float(np.clip(s, s_bounds[0], s_bounds[1]))
        except Exception:
            continue
        pred = (s * (X @ R.T)) + t
        err = np.linalg.norm(pred - Y, axis=1)
        inliers = err < th_init
        cnt = int(inliers.sum())
        if cnt > best_num:
            best_num = cnt; best_inliers = inliers; best_model = (s, R, t)
            if cnt == N:
                break

    if best_num < min_inliers:
        s, R, t = umeyama_similarity(X, Y, with_scale=with_scale)
        inliers = np.ones(N, bool)
    else:
        inliers = best_inliers
        # inlier re-estimated
        s, R, t = umeyama_similarity(X[inliers], Y[inliers], with_scale=with_scale)
        s = float(np.clip(s, s_bounds[0], s_bounds[1]))
        pred = (s * (X @ R.T)) + t
        err = np.linalg.norm(pred - Y, axis=1)
        inliers = err < th_refine
        s, R, t = umeyama_similarity(X[inliers], Y[inliers], with_scale=with_scale)
        s = float(np.clip(s, s_bounds[0], s_bounds[1]))

    # diagnose
    pred = (s * (X @ R.T)) + t
    err = np.linalg.norm(pred - Y, axis=1)
    inmask = inliers if inliers.any() else np.ones(N, bool)
    stats = {
        "N": int(N),
        "N_in": int(inmask.sum()),
        "th_init": float(th_init),
        "th_refine": float(th_refine),
        "rmse": float(np.sqrt((err[inmask]**2).mean())),
        "median_err": float(np.median(err[inmask])),
        "max_err": float(err[inmask].max()),
        "s": float(s),
        "detR": float(np.linalg.det(R)),
        "orth_err": float(np.linalg.norm(R.T @ R - np.eye(3)))
    }
    th_init = float(stats.get("th_init", 1.0))
    scale_Y = th_init / 0.05 if th_init > 0 else 1.0
    rmse_n = stats["rmse"] / scale_Y
    med_n = stats["median_err"] / scale_Y
    print(f"[sim3] rmse_norm={rmse_n:.2%}, median_norm={med_n:.2%}, N_in={stats.get('N_in')}")
    return s, R, t, inliers, stats


def calculate_world_transform(session_pts, sfm_pts, allow_scale=True):
    """
    session_pts: [(x,y,z), ...]  # session (m)
    sfm_pts:     [(x,y,z), ...]  # sfm reconstructed to an arbitrary scale
    return: T(4x4) to store (sR+t), and seperated s, R, t
    """
    X = np.asarray(session_pts, float)
    Y = np.asarray(sfm_pts, float)

    s, R, t, inliers, stats = estimate_sim3_ransac(
        X, Y, with_scale=allow_scale,
        th_init=0.5, th_refine=0.25,
        max_trials=1000, min_inliers=4,
        s_bounds=(1e-3, 1e3)
    )

    T = np.eye(4)
    T[:3, :3] = s * R
    T[:3,  3] = t

    return {
        "T_session_to_sfm": T,
        "scale": float(s),
        "rotation_matrix": R.tolist(),
        "translation": t.tolist(),
        "inliers": [int(i) for i in np.where(inliers)[0].tolist()],
        "stats": stats
    }


def sfm_to_session_pose(t_sfm, Rwc_sfm, s, R_sim3, t):
    """
    transform movie frame's SfM coordinate (Rwc_sfm, t_sfm) to Session(ARKit)：
      ARKit ≈ (1/s) * R_sim3^T * (SfM - t)
      Rwc_session ≈ R_sim3^T * Rwc_sfm

    parameters:
      t_sfm:   (3,)  movie frame's camera center C_sfm in SfM world
      Rwc_sfm: (3,3) movie frame's Rwc in SfM world
      s: Umeyama calcualted scale
      R_sim3:  (3,3) Umeyama calculated rotation
      t:       (3,)  Umeyama calculated translation

    return:
      C_session: (3,)   movie frame's camera center in Session(ARKit)
      Rwc_session: (3,3) movie frame's Rwc in Session(ARKit)
    """
    t_sfm   = np.asarray(t_sfm,   dtype=float).reshape(3)
    Rwc_sfm = np.asarray(Rwc_sfm, dtype=float).reshape(3,3)
    R_sim3  = np.asarray(R_sim3,  dtype=float).reshape(3,3)
    t       = np.asarray(t,       dtype=float).reshape(3)
    s       = float(s)

    Rwc_session = R_sim3.T @ Rwc_sfm
    C_session   = (1.0 / s) * (R_sim3.T @ (t_sfm - t))
    return C_session, Rwc_session


def transform_query_to_session_pose(query_pose_in_sfm, T_session_to_sfm):
    """
    Transform the query pose from SfM coordinates to the session coordinate system.
    Excute only when T_session_to_sfm is not None
    """
    if T_session_to_sfm is None:
        raise ValueError("Not enough correspondences to transform query pose")


    # R_sfm = quat_to_rot(query_pose_in_sfm["rotation"])
    R_sfm, quat_format = quat_to_rot(query_pose_in_sfm["rotation"])
    print(f"[transform] using quat format: {quat_format}")
    t_sfm = np.array(query_pose_in_sfm["translation"]).reshape(3,)

    T_sfm_query = np.eye(4)
    T_sfm_query[:3, :3] = R_sfm
    T_sfm_query[:3, 3] = t_sfm

    R_scaled = T_session_to_sfm[:3, :3]
    s = np.cbrt(np.linalg.det(R_scaled))
    R_transform = R_scaled / s
    t_transform = T_session_to_sfm[:3, 3]

    R_inv = R_transform.T
    t_inv = -(1.0 / s) * (R_inv @ t_transform)

    T_sfm_to_session = np.eye(4)
    T_sfm_to_session[:3, :3] = (1.0 / s) * R_inv
    T_sfm_to_session[:3, 3] = t_inv

    T_session_query = T_sfm_to_session @ T_sfm_query

    R_session = T_session_query[:3, :3]
    t_session = T_session_query[:3, 3]

    return {
        "rotation_matrix": R_session.tolist(),
        "translation": t_session.tolist(),
        "scale": s
    }
