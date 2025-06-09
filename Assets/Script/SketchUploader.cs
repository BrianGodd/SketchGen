using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dummiesman;
using SFB;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"results\":" + json + "}";
        return JsonUtility.FromJson<Wrapper<T>>(newJson).results;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] results;
    }
}

public class SketchUploader : MonoBehaviour
{
    [Header("API URL")]
    public string apiUrl = "http://127.0.0.1:5000/infer"; // Flask server 的 URL
    public string apiUrl2 = "http://127.0.0.1:5000/infer_selected";
    public string apiUrl3 = "http://127.0.0.1:5000/infer_interpolate";

    [Header("Input Image Path (Absolute or Relative)")]
    public string imagePath = "Assets/sketch_input.png"; // 圖片路徑

    [Header("Optional: Attach Object to Parent")]
    public GameObject loadedObjectParent;

    [Header("sketch rawImage")]
    public RawImage img;

    ExtensionFilter[] extensions = new [] {
        new ExtensionFilter("png", "jpg")
    };

    public List<Texture2D> sketchHistory = new List<Texture2D>();
    public List<GameObject> objHistory = new List<GameObject>();
    public List<GameObject> interpolatePool = new List<GameObject>();
    private List<string> interpolateGMM = new List<string>();
    public int currentIndex = -1;
    private Texture2D lastTexture;

    public DrawController DrawController;
    public ModelController ModelController;
    public GMMLoader GMMLoader;

    public int select1 = -1, select2 = -1;
    public GameObject sketchViewPrefab, nowView;
    public Transform ViewParent;
    public GameObject interpolateParent;
    public InterpolateManager InterpolateManager;

    public ObjExporter ObjExporter;

    void Start()
    {
        
    }

    public void DownloadOBJ()
    {
        string downloadsPath = PathHelper.GetDownloadsPath();
        string filename = "exported_model.obj";
        string fullPath = Path.Combine(downloadsPath, filename);
        ObjExporter.Export(objHistory[currentIndex].transform.GetChild(0).gameObject, fullPath);
    }

    public void InterpolateGen()
    {
        StartCoroutine(GenOBJInter());
    }

    public void ImportInterpolate(int ind)
    {
        StartCoroutine(TurnOBJ(ind));
    }

    public void GenModel()
    {
        if(!GMMLoader.isSelect) StartCoroutine(GenOBJ());
        else StartCoroutine(GenOBJWithSelected());
    }

    public void Undo()
    {
        if (currentIndex > 0)
        {
            objHistory[currentIndex].SetActive(false);
            currentIndex--;
            objHistory[currentIndex].SetActive(true);
            ModelController.model = objHistory[currentIndex];
            img.texture = sketchHistory[currentIndex];
            DrawController.CaptureCurrentImageAsCanvas();
            GMMLoader.SwitchModel(currentIndex);
        }
    }

    public void Redo()
    {
        if (currentIndex < objHistory.Count - 1)
        {
            objHistory[currentIndex].SetActive(false);
            currentIndex++;
            objHistory[currentIndex].SetActive(true);
            ModelController.model = objHistory[currentIndex];
            img.texture = sketchHistory[currentIndex];
            DrawController.CaptureCurrentImageAsCanvas();
            GMMLoader.SwitchModel(currentIndex);
        }
    }

    public void Target(GameObject view, int id)
    {
        nowView.GetComponent<SketchViewController>().ColorChange();
        nowView = view;
        objHistory[currentIndex].SetActive(false);
        currentIndex = id;
        objHistory[currentIndex].SetActive(true);
        ModelController.model = objHistory[currentIndex];
        img.texture = sketchHistory[currentIndex];
        DrawController.CaptureCurrentImageAsCanvas();
        GMMLoader.SwitchModel(currentIndex);
    }

    public void Select(int id)
    {
        if(select1 == id)
        {
            select1 = select2;
            select2 = -1;
        }
        else if(select2 == id)
        {
            select2 = -1;
        }
        else if(select1 == -1) select1 = id;
        else if(select2 == -1)
        {
            select2 = id;
            int t = select1;
            select1 = Mathf.Min(t, select2);
            select2 = Mathf.Max(t, select2);
        }
    }

    public void SpawnSketchView(Texture2D img)
    {
        GameObject sketch_view = Instantiate(sketchViewPrefab);
        sketch_view.transform.parent = ViewParent;
        if(nowView != null) nowView.GetComponent<SketchViewController>().ColorChange();
        sketch_view.GetComponent<SketchViewController>().ColorChange();
        Sprite sketchSprite = Sprite.Create(
            img,
            new Rect(0, 0, img.width, img.height),
            new Vector2(0.5f, 0.5f)
        );
        sketch_view.GetComponent<SketchViewController>().Sketch.sprite = sketchSprite;
        sketch_view.GetComponent<SketchViewController>().id = currentIndex;
        nowView = sketch_view;
    }

    IEnumerator TurnOBJ(int ind)
    {
        GameObject loadedObj = interpolatePool[ind];
        loadedObj.name = "GeneratedOBJ";

        if (loadedObjectParent != null)
        {
            loadedObj.transform.SetParent(loadedObjectParent.transform, false);
        }

        loadedObj.transform.position = new Vector3(2, 0, 0);
        if(objHistory.Count > 0) loadedObj.transform.rotation = objHistory[currentIndex].transform.rotation;
        loadedObj.transform.GetChild(0).gameObject.layer = 6;
        ModelController.model = loadedObj;
        GMMLoader.pointParent = loadedObj.transform.GetChild(0);

        objHistory.Add(loadedObj);
        lastTexture = img.texture as Texture2D;
        Texture2D copy = new Texture2D(lastTexture.width, lastTexture.height, lastTexture.format, false);
        copy.SetPixels(lastTexture.GetPixels());
        copy.Apply();
        sketchHistory.Add(copy);
        
        foreach(Transform objt in interpolateParent.transform) Destroy(objt.gameObject);

        currentIndex = objHistory.Count - 1;

        Debug.Log("OBJ loaded into scene.");

        GMMLoader.LoadGMMPoints(interpolateGMM[ind]);

        SpawnSketchView(lastTexture);

        yield return new WaitForSeconds(0.1f);

        objHistory[currentIndex].SetActive(true);   

        yield return null;
    }

    IEnumerator GenOBJ()
    {
        // 讀取圖片
        byte[] imageData = ConvertRawImageToBytes(img);

        // 建立表單並加上圖片
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageData, "sketch.png", "image/png");

        // 發送 POST 請求
        UnityWebRequest request = UnityWebRequest.Post(apiUrl, form);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Upload failed: " + request.error);
            yield break;
        }

        // 取得回傳的 .obj 純文字
        string json = request.downloadHandler.text;

        var response = JsonUtility.FromJson<APIResponse>(json);

        string objText = response.mesh_obj;
        string gmm = response.gmm;
        Debug.Log("gmm: \n" + gmm);

        // 將文字轉為 MemoryStream 傳給 Dummiesman 解析器
        using (var textStream = new MemoryStream(Encoding.UTF8.GetBytes(objText)))
        {
            //if(loadedObjectParent.transform.childCount > 1) Destroy(loadedObjectParent.transform.GetChild(1).gameObject);

            GameObject loadedObj = new OBJLoader().Load(textStream);
            loadedObj.name = "GeneratedOBJ";

            if (loadedObjectParent != null)
            {
                loadedObj.transform.SetParent(loadedObjectParent.transform, false);
            }

            loadedObj.transform.position = new Vector3(2, 0, 0);
            if(objHistory.Count > 0) loadedObj.transform.rotation = objHistory[currentIndex].transform.rotation;
            loadedObj.transform.GetChild(0).gameObject.layer = 6;
            ModelController.model = loadedObj;
            GMMLoader.pointParent = loadedObj.transform.GetChild(0);

            objHistory.Add(loadedObj);
            lastTexture = img.texture as Texture2D;
            Texture2D copy = new Texture2D(lastTexture.width, lastTexture.height, lastTexture.format, false);
            copy.SetPixels(lastTexture.GetPixels());
            copy.Apply();
            sketchHistory.Add(copy);
            
            if (currentIndex >= 0 && currentIndex < objHistory.Count)
            {
                objHistory[currentIndex].SetActive(false);
            }

            currentIndex = objHistory.Count - 1;

            Debug.Log("OBJ loaded into scene.");

            GMMLoader.LoadGMMPoints(gmm);

            SpawnSketchView(lastTexture);
        }
    }

    IEnumerator GenOBJWithSelected()
    {
        // 讀取圖片
        byte[] imageData = ConvertRawImageToBytes(img);

        string selectedString = string.Join(",", GMMLoader.selected.ConvertAll(b => b ? "1" : "0"));

        // 建立表單並加上圖片
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageData, "sketch.png", "image/png");
        form.AddField("selected", selectedString);

        // 發送 POST 請求
        UnityWebRequest request = UnityWebRequest.Post(apiUrl2, form);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Upload failed: " + request.error);
            yield break;
        }

        // 取得回傳的 .obj 純文字
        string json = request.downloadHandler.text;

        var response = JsonUtility.FromJson<APIResponse>(json);

        string objText = response.mesh_obj;
        string gmm = response.gmm;
        Debug.Log("gmm: \n" + gmm);

        // 將文字轉為 MemoryStream 傳給 Dummiesman 解析器
        using (var textStream = new MemoryStream(Encoding.UTF8.GetBytes(objText)))
        {
            //if(loadedObjectParent.transform.childCount > 1) Destroy(loadedObjectParent.transform.GetChild(1).gameObject);

            GameObject loadedObj = new OBJLoader().Load(textStream);
            loadedObj.name = "GeneratedOBJ";

            if (loadedObjectParent != null)
            {
                loadedObj.transform.SetParent(loadedObjectParent.transform, false);
            }

            loadedObj.transform.position = new Vector3(2, 0, 0);
            if(objHistory.Count > 0) loadedObj.transform.rotation = objHistory[currentIndex].transform.rotation;
            loadedObj.transform.GetChild(0).gameObject.layer = 6;
            ModelController.model = loadedObj;
            GMMLoader.pointParent = loadedObj.transform.GetChild(0);

            objHistory.Add(loadedObj);
            lastTexture = img.texture as Texture2D;
            Texture2D copy = new Texture2D(lastTexture.width, lastTexture.height, lastTexture.format, false);
            copy.SetPixels(lastTexture.GetPixels());
            copy.Apply();
            sketchHistory.Add(copy);
            
            if (currentIndex >= 0 && currentIndex < objHistory.Count)
            {
                objHistory[currentIndex].SetActive(false);
            }

            currentIndex = objHistory.Count - 1;

            Debug.Log("OBJ loaded into scene.");

            GMMLoader.LoadGMMPoints(gmm);

            SpawnSketchView(lastTexture);
        }
    }

    IEnumerator GenOBJInter()
    {
        interpolatePool.Clear();
        interpolateGMM.Clear();
        
        // 讀取圖片
        byte[] imageData1 = ConvertTextureToBytes(sketchHistory[select1]);
        byte[] imageData2 = ConvertTextureToBytes(sketchHistory[select2]);

        // 建立表單並加上圖片
        WWWForm form = new WWWForm();
        form.AddBinaryData("image1", imageData1, "sketch1.png", "image/png");
        form.AddBinaryData("image2", imageData2, "sketch2.png", "image/png");

        // 發送 POST 請求
        UnityWebRequest request = UnityWebRequest.Post(apiUrl3, form);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Upload failed: " + request.error);
            yield break;
        }

        // 取得回傳的 .obj 純文字
        string json = request.downloadHandler.text;

        APIResponse[] results = JsonHelper.FromJson<APIResponse>(json);

        for (int i = 0; i < results.Length; i++)
        {
            var result = results[i];

            using (var textStream = new MemoryStream(Encoding.UTF8.GetBytes(result.mesh_obj)))
            {
                GameObject loadedObj = new OBJLoader().Load(textStream);
                loadedObj.name = $"Interpolated_{i}";

                if (interpolateParent != null)
                    loadedObj.transform.SetParent(interpolateParent.transform, false);
                    
                loadedObj.transform.position = new Vector3(2, 0, 0);
                loadedObj.transform.rotation = objHistory[currentIndex].transform.rotation;
                loadedObj.transform.GetChild(0).gameObject.layer = 6;
                loadedObj.SetActive(false);
                interpolatePool.Add(loadedObj);
            }
            interpolateGMM.Add(result.gmm);
        }
        objHistory[currentIndex].SetActive(false);
        interpolatePool[^1].SetActive(true);
        InterpolateManager.Keep();
    }

    [System.Serializable]
    public class APIResponse
    {
        public string mesh_obj;
        public string gmm;
    }

    byte[] ConvertRawImageToBytes(RawImage rawImg)
    {
        Texture2D tex = rawImg.texture as Texture2D;
        if (tex == null)
        {
            Debug.LogError("RawImage texture is not a Texture2D!");
            return null;
        }

        byte[] bytes = tex.EncodeToPNG();
        return bytes;
    }

    byte[] ConvertTextureToBytes(Texture2D img)
    {
        byte[] bytes = img.EncodeToPNG();
        return bytes;
    }
}

