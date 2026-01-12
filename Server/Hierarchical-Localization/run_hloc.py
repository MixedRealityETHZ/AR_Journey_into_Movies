import numpy as np
from pathlib import Path
from hloc import (
    extract_features,
    match_features,
    reconstruction,
    visualization,
    pairs_from_exhaustive,
)
from hloc.visualization import plot_images, read_image
from hloc.utils import viz_3d

# setup
images = Path(r"E:\mixed_reality\CABImage_data1024") 
outputs = Path("outputs/demo_CAB1024_aliked/")
sfm_pairs = outputs / "pairs-sfm.txt" 
loc_pairs = outputs / "pairs-loc.txt" 
sfm_dir = outputs / "sfm"
features = outputs / "features.h5" 
matches = outputs / "matches.h5"
# feature_conf = extract_features.confs["superpoint_aachen"]
# matcher_conf = match_features.confs["superglue"] 
feature_conf = extract_features.confs["aliked-n16"] 
matcher_conf = match_features.confs["aliked+lightglue"] 

def Rename(img_dir):
    for p in img_dir.iterdir():
        if p.suffix == ".JPG":
            p.rename(p.with_suffix(".jpg"))

def main():
    # Rename(images/"mapping/")

    # # 3D mapping
    references = [p.relative_to(images).as_posix() for p in (images / "mapping/").iterdir()] 
    print(len(references), "mapping images")
    plot_images([read_image(images / r) for r in references], dpi=25) 

    extract_features.main(
        feature_conf, images, image_list=references, feature_path=features
    )
    pairs_from_exhaustive.main(sfm_pairs, image_list=references)
    match_features.main(matcher_conf, sfm_pairs, features=features, matches=matches);
    model = reconstruction.main(
        sfm_dir, images, sfm_pairs, features, matches, image_list=references
    )

    # 可视化sfm部分
    fig = viz_3d.init_figure() 
    viz_3d.plot_reconstruction(
        fig, model, color="rgba(255,0,0,0.5)", name="mapping", points_rgb=True
    )
    fig.show()
    fig.write_html("reconstruction.html")
    visualization.visualize_sfm_2d(model, images, color_by="visibility", n=2)

   
    # Localization
    query = "query/frame_1761291674613.png"
    # plot_images([read_image(images / query)], dpi=75)

    extract_features.main(
        feature_conf, images, image_list=[query], feature_path=features, overwrite=True
    ) 
    pairs_from_exhaustive.main(loc_pairs, image_list=[query], ref_list=references) 
    match_features.main(
        matcher_conf, loc_pairs, features=features, matches=matches, overwrite=True
    ); 

    import pycolmap
    from hloc.localize_sfm import QueryLocalizer, pose_from_cluster 

    camera = pycolmap.infer_camera_from_image(images / query)
    # for r in references:
    #     img = model.find_image_with_name(r)
    #     if img is None:
    #         print(f"[WARNING] Reference image not found in model: {r}")
    #     else:
    #         print(f"[OK] Found {r}, id={img.image_id}")

    ref_ids = [model.find_image_with_name(r).image_id for r in references] 
    conf = {
        "estimation": {"ransac": {"max_error": 12}},
        "refinement": {"refine_focal_length": True, "refine_extra_params": True}, 
    } 
    localizer = QueryLocalizer(model, conf) 
    ret, log = pose_from_cluster(localizer, query, camera, ref_ids, features, matches) 
    print(f'found {ret["num_inliers"]}/{len(ret["inlier_mask"])} inlier correspondences.') 
    visualization.visualize_loc_from_log(images, query, log, model)

    # 可视化四棱锥部分
    viz_3d.plot_camera_colmap(
        fig, ret["cam_from_world"], ret["camera"], color="rgba(0,255,0,0.5)", name=query, fill=True,
        text=f"inliers: {ret['num_inliers']} / {ret['inlier_mask'].shape[0]}"
    )
    # visualize 2D-3D correspodences
    inl_3d = np.array(
        [model.points3D[pid].xyz for pid in np.array(log["points3D_ids"])[ret["inlier_mask"]]]
    )
    viz_3d.plot_points(fig, inl_3d, color="lime", ps=1, name=query)
    fig.show()
    fig.write_html("localization.html")

    



if __name__ == "__main__":
    main()