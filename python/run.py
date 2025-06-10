from custom_types import *
from constants import ASSETS_ROOT
import options
from utils import files_utils
from ui_sketch import sketch_inference
from data_loaders import augment_clipcenter
import shutil


def get_mesh(spaghetti, zh):
    # out_z, gmms = spaghetti.model.occ_former.forward_mid(zh.unsqueeze(0).unsqueeze(0)) 
    out_z, gmms = spaghetti.model.occ_former.forward_mid([zh]) 
    out_z = spaghetti.model.merge_zh_step_a(out_z, gmms)
    out_z, _ = spaghetti.model.affine_transformer.forward_with_attention(out_z[0][:].unsqueeze(0))
    mesh = spaghetti.get_mesh(out_z[0], 256, None)
    if mesh is None:
        return None, None
    gmm = gmms
    return gmm[0], mesh

def sketch2mesh(path: str, model):
    sketch = files_utils.load_image(path)
    sketch = augment_clipcenter.augment_cropped_square(sketch, 256)
    gmm, mesh, zh_0 = model.sketch2mesh(sketch, get_zh=True)
    return gmm, mesh, zh_0, sketch

def sketch2mesh_partial(path: str, model, selected, zh_last):
    sketch = files_utils.load_image(path)
    sketch = augment_clipcenter.augment_cropped_square(sketch, 256)
    gmm, mesh, zh_0 = model.sketch2mesh_partial(sketch, selected, zh_last, get_zh=True)
    return gmm, mesh, zh_0, sketch

def update_zh(index: int):
    source_folder = f"{ASSETS_ROOT}/output/"
    target_folder = f"{ASSETS_ROOT}/interpolate/"
    source_path = f"{target_folder}/zh_{index}.npy"
    target_path = f"{source_folder}/zh_0.npy"

    if not os.path.exists(source_path):
        raise FileNotFoundError(f"[update_zh] Latent code file not found: {source_path}")

    shutil.copyfile(source_path, target_path)
    print(f"[update_zh] Updated zh_0.npy to interpolation index {index}.")

def main(inputs, to_save=True):
    print(inputs)
    opt = options.SketchOptions(tag="chairs", spaghetti_tag="chairs_sym_hard")
    model = sketch_inference.SketchInference(opt)

    folder = f"{ASSETS_ROOT}/output/"
    folder_interpolate = f"{ASSETS_ROOT}/interpolate/"
    
    if isinstance(inputs, str):
        input_path = inputs
        gmm, mesh, zh_0, sketch = sketch2mesh(input_path, model)

    elif isinstance(inputs, tuple) and len(inputs) == 2 and all(isinstance(item, str) for item in inputs):
        ######### interpolation #########
        gmm_a, mesh_a, zh_0_a, sketch_a = sketch2mesh(inputs[0], model)
        files_utils.export_mesh(mesh_a, f"{folder_interpolate}/mesh_res_0") 
        files_utils.export_gmm(gmm_a, 0, f"{folder_interpolate}/gmm_0")
        files_utils.save_np(zh_0_a, f"{folder_interpolate}/zh_0")
        gmm_b, mesh_b, zh_0_b, sketch_b = sketch2mesh(inputs[1], model)
        files_utils.export_mesh(mesh_b, f"{folder_interpolate}/mesh_res_6") 
        files_utils.export_gmm(gmm_b, 0, f"{folder_interpolate}/gmm_6")
        files_utils.save_np(zh_0_b, f"{folder_interpolate}/zh_6")
        # 指定插值的 alpha 值 (0 <= alpha <= 1)

        for i in range(0,5):
            alpha = 0.2 * (i+1)
            interpolated = zh_0_a * (1 - alpha) + zh_0_b * alpha
            # print(zh_0_a.dtype)
            # print(interpolated.dtype)

            gmm_final, mesh_final = get_mesh(model.spaghetti, interpolated)
            files_utils.export_mesh(mesh_final, f"{folder_interpolate}/mesh_res_{i+1}")  
            files_utils.export_gmm(gmm_final, 0, f"{folder_interpolate}/gmm_{i+1}")
            files_utils.save_np(interpolated, f"{folder_interpolate}/zh_{i+1}")

        to_save = False
        ##################
    else:
        input_path = inputs[0]
        selected_array = inputs[1]
        print(f"path:{input_path}, bool:{selected_array}")

        # 載入之前儲存的 zh_0
        zh_last_np = np.load(f"{folder}/zh_0.npy")
        zh_last = torch.from_numpy(zh_last_np).float().to(model.device)
        
        gmm, mesh, zh_0, sketch = sketch2mesh_partial(input_path, model, selected_array, zh_last)


    # print(f"zh shape: {zh_0.shape}")

    if to_save:
        filename_gmm = f'{folder}/gmm'
        filename_mesh = f'{folder}/mesh_res'
        filename_zh_0 = f'{folder}/zh_0'
        filename_input_sketch = f'{folder}/cropped_sketch.png'
        files_utils.export_gmm(gmm, 0, filename_gmm)
        files_utils.export_mesh(mesh, filename_mesh)
        files_utils.save_image(sketch, filename_input_sketch)
        files_utils.save_np(zh_0, filename_zh_0)
        print("Done - Saved files to", folder)



if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument('--input1', type=str, required=True)
    parser.add_argument('--input2', type=str, required=True)
    parser.add_argument('--to_save', type=bool, default=True)
    args = parser.parse_args()
    main((args.input1, args.input2), args.to_save)