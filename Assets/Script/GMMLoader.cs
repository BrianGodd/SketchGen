using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Globalization;

public class GMMLoader : MonoBehaviour
{
    public Camera initCamera, gmmCamera;
    public GameObject canvas1, canvas2;
    public GameObject pointPrefab;
    public Transform pointParent;

    public List<List<GameObject>> gmmPoints = new List<List<GameObject>>();
    public List<bool> selected = new List<bool>();

    public bool isSelect = false;
    public int nowIndex = 0;
    public ModelController ModelController;
    private Vector3 lastPos;
    private Quaternion lastRot;

    void Start()
    {
        
    }

    public void LoadGMMPoints(string gmmText)
    {
        //gmmPoints.Clear();
        if(isSelect)
        {
            isSelect = false;
            foreach(GameObject gmm in gmmPoints[nowIndex]) gmm.SetActive(isSelect);
        }

        gmmPoints.Add(new List<GameObject>());
        selected.Clear();

        string[] lines = gmmText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        

        string[] numbers = lines[1].Split(new[] { ' ', '\t', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (numbers.Length % 3 != 0)
        {
            Debug.LogWarning("第二行數字數量應為3的倍數");
            return;
        }

        for (int i = 0; i < numbers.Length; i += 3)
        {
            float x = float.Parse(numbers[i]);
            float y = float.Parse(numbers[i + 1]);
            float z = float.Parse(numbers[i + 2]);
            Vector3 pos = new Vector3(x, y, z);

            GameObject gmm = Instantiate(pointPrefab);
            gmm.GetComponent<GMMController>().renderCamera = gmmCamera;
            gmm.transform.parent = pointParent;
            gmm.transform.localPosition = pos;
            gmm.transform.localRotation = Quaternion.identity;
            gmm.name = $"GMM_{i / 3}";
            gmm.layer = 6;
            gmmPoints[^1].Add(gmm);
            selected.Add(false);
            gmm.SetActive(false);
        }
        nowIndex = gmmPoints.Count-1;
    }

    public void SwitchModel(int ind)
    {
        isSelect = false;
        foreach(GameObject gmm in gmmPoints[nowIndex]) gmm.SetActive(isSelect);
        nowIndex = ind;
    }

    public void SwitchGMM()
    {
        isSelect = !isSelect;
        foreach(GameObject gmm in gmmPoints[nowIndex]) gmm.SetActive(isSelect);
        if(isSelect) SwitchScene();
    }

    public void SwitchScene()
    {
        if(canvas1.active)
        {
            lastPos = ModelController.model.transform.position;
            lastRot = ModelController.model.transform.rotation;
        }
        else
        {
            ModelController.model.transform.position = lastPos;
            ModelController.model.transform.rotation = lastRot;
        }
        canvas1.SetActive(!canvas1.active);
        canvas2.SetActive(!canvas2.active);
        initCamera.gameObject.SetActive(!initCamera.gameObject.active);
        gmmCamera.gameObject.SetActive(!gmmCamera.gameObject.active);
    }

    public void ChangeSelected(GameObject gmm)
    {
        int ind = 0;
        for(int i = 0;i < gmmPoints[nowIndex].Count;i++)
        {
            if(gmmPoints[nowIndex][i] == gmm)
            {
                ind = i;
                break;
            }
        }
        selected[ind] = !selected[ind];
    }
}

