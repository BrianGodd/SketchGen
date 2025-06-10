from flask import Flask, request, jsonify, Response
from flask_cors import CORS
import numpy as np
import cv2
import uuid
import os
import sys

# 加入 SENS 的根路徑
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from run import main as run_main
from run import update_zh as run_update
from constants import ASSETS_ROOT

app = Flask(__name__)
CORS(app)

@app.route("/infer", methods=["POST"])
def infer():
    if 'image' not in request.files:
        return jsonify({"error": "No image uploaded"}), 400

    file = request.files['image']
    file_bytes = np.frombuffer(file.read(), np.uint8)
    img = cv2.imdecode(file_bytes, cv2.IMREAD_GRAYSCALE)

    if img is None:
        return jsonify({"error": "Invalid image format"}), 400

    # 儲存暫存圖片
    uid = str(uuid.uuid4())
    tmp_dir = os.path.join(ASSETS_ROOT, "tmp")
    os.makedirs(tmp_dir, exist_ok=True)
    tmp_image_path = os.path.join(tmp_dir, f"render.png")
    cv2.imwrite(tmp_image_path, img)

    try:
        # 呼叫推論主程式
        run_main(tmp_image_path, to_save=True)
    except Exception as e:
        print("error:", str(e))
        return jsonify({"error": f"Inference failed: {str(e)}"}), 501

    # 讀取 .obj 純文字內容
    obj_path = os.path.join(ASSETS_ROOT, "output", "mesh_res.obj")
    if not os.path.exists(obj_path):
        return jsonify({"error": "Output mesh not found"}), 502

    with open(obj_path, "r", encoding="utf-8") as f:
        obj_text = f.read()

    # 讀取 gmm.txt（你可能是存成 JSON 或 CSV 格式）
    gmm_path = os.path.join(ASSETS_ROOT, "output", "gmm.txt")
    if not os.path.exists(gmm_path):
        return jsonify({"error": "Output GMM not found"}), 503

    with open(gmm_path, "r", encoding="utf-8") as f:
        gmm_text = f.read()

    return jsonify({
        "mesh_obj": obj_text,
        "gmm": gmm_text
    })

@app.route("/infer_selected", methods=["POST"])
def infer_selected():
    if 'image' not in request.files or 'selected' not in request.form:
        return jsonify({"error": "Missing 'image' or 'selected' file"}), 401

    # === 讀取 image ===
    image_file = request.files['image']
    image_bytes = np.frombuffer(image_file.read(), np.uint8)
    img = cv2.imdecode(image_bytes, cv2.IMREAD_GRAYSCALE)

    if img is None:
        return jsonify({"error": "Invalid image format"}), 402

    # === 暫存 image ===
    uid = str(uuid.uuid4())
    tmp_dir = os.path.join(ASSETS_ROOT, "tmp")
    os.makedirs(tmp_dir, exist_ok=True)
    tmp_image_path = os.path.join(tmp_dir, f"render.png")
    cv2.imwrite(tmp_image_path, img)

    # === 讀取 selected 字串欄位 ===
    selected_str = request.form.get("selected")  # 從 form 欄位而不是 file 讀取
    if not selected_str:
        return jsonify({"error": "Missing 'selected' field"}), 403

    try:
        # 將 "0,1,1,0" 轉為布林 list，再轉成 numpy array
        selected_list = [bool(int(x)) for x in selected_str.strip().split(",")]
        selected_array = np.array(selected_list, dtype=bool)
    except Exception as e:
        return jsonify({"error": f"Invalid selected array format: {str(e)}"}), 404

    try:
        # 呼叫推論主程式
        inputs = (tmp_image_path, selected_array)
        run_main(inputs, to_save=True)
    except Exception as e:
        print("error:", str(e))
        return jsonify({"error": f"Inference failed: {str(e)}"}), 501

    # === 回傳推論結果 ===
    obj_path = os.path.join(ASSETS_ROOT, "output", "mesh_res.obj")
    gmm_path = os.path.join(ASSETS_ROOT, "output", "gmm.txt")

    if not os.path.exists(obj_path):
        return jsonify({"error": "mesh_res.obj not found"}), 502
    if not os.path.exists(gmm_path):
        return jsonify({"error": "gmm.txt not found"}), 503

    with open(obj_path, "r", encoding="utf-8") as f:
        obj_text = f.read()
    with open(gmm_path, "r", encoding="utf-8") as f:
        gmm_text = f.read()

    return jsonify({
        "mesh_obj": obj_text,
        "gmm": gmm_text
    })

@app.route("/infer_interpolate", methods=["POST"])
def infer_interpolate():
    if 'image1' not in request.files or 'image2' not in request.files:
        return jsonify({"error": "Both image1 and image2 are required"}), 400

    file1 = request.files['image1']
    file2 = request.files['image2']
    img1 = cv2.imdecode(np.frombuffer(file1.read(), np.uint8), cv2.IMREAD_GRAYSCALE)
    img2 = cv2.imdecode(np.frombuffer(file2.read(), np.uint8), cv2.IMREAD_GRAYSCALE)

    if img1 is None or img2 is None:
        return jsonify({"error": "One or both images are invalid"}), 400

    tmp_dir = os.path.join(ASSETS_ROOT, "tmp")
    interpolate_dir = os.path.join(ASSETS_ROOT, "interpolate")
    os.makedirs(tmp_dir, exist_ok=True)
    os.makedirs(interpolate_dir, exist_ok=True)

    tmp_path1 = os.path.join(tmp_dir, "render1.png")
    tmp_path2 = os.path.join(tmp_dir, "render2.png")
    cv2.imwrite(tmp_path1, img1)
    cv2.imwrite(tmp_path2, img2)

    try:
        # 執行插值推論
        inputs = (tmp_path1, tmp_path2)
        run_main(inputs, to_save=True)
    except Exception as e:
        print("error:", str(e))
        return jsonify({"error": f"Inference failed: {str(e)}"}), 501

    results = []
    for i in range(6):  # 預設六組結果
        obj_path = os.path.join(interpolate_dir, f"mesh_res_{i}.obj")
        gmm_path = os.path.join(interpolate_dir, f"gmm_{i}.txt")

        if not os.path.exists(obj_path) or not os.path.exists(gmm_path):
            return jsonify({"error": f"Missing output at index {i}"}), 502

        with open(obj_path, "r", encoding="utf-8") as f:
            obj_text = f.read()
        with open(gmm_path, "r", encoding="utf-8") as f:
            gmm_text = f.read()

        results.append({
            "mesh_obj": obj_text,
            "gmm": gmm_text
        })

    return jsonify(results)

@app.route("/update_index", methods=["POST"])
def update_index():
    data = request.get_json()
    if not data or "index" not in data:
        return jsonify({"error": "Missing index parameter"}), 400

    index = data["index"]
    try:
        run_update(index)
    except FileNotFoundError as e:
        return jsonify({"error": str(e)}), 404
    except Exception as e:
        return jsonify({"error": f"Unexpected error: {str(e)}"}), 500

    return jsonify({"message": f"zh_0.npy updated to zh_{index}.npy"}), 200

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
