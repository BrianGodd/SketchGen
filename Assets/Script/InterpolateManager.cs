using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InterpolateManager : MonoBehaviour
{
    public enum state {init, choose, loading, demo}
    public state nowState = state.choose;
    public SketchUploader SketchUploader;
    public FunctionManager FunctionManager;
    public GameObject ChooseUI, LoadingUI, DemoUI, Mask;
    public GameObject ChooseOK;
    public Transform ViewParent;
    public Slider interpolateSlider;
    public Button interpolate;
    public int interpolateIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(nowState == state.choose && SketchUploader.select1 != -1 && SketchUploader.select2 != -1)
            ChooseOK.SetActive(true);
        else 
            ChooseOK.SetActive(false);
        
        if(nowState == state.init && SketchUploader.objHistory.Count < 2) interpolate.interactable = false;
        else interpolate.interactable = true;
    }

    public void Keep()
    {
        switch(nowState)
        {
            case state.init:
                nowState = state.choose;
                ChooseUI.SetActive(true);
                foreach(Transform objt in ViewParent)
                    objt.GetComponent<SketchViewController>().Toggle.SetActive(true);
                break;
            case state.choose:
                nowState = state.loading;
                Mask.SetActive(true);
                ChooseUI.SetActive(false);
                foreach(Transform objt in ViewParent)
                    objt.GetComponent<SketchViewController>().Toggle.SetActive(false);
                LoadingUI.SetActive(true);
                SketchUploader.InterpolateGen();
                break;
            case state.loading:
                LoadingUI.SetActive(false);
                DemoUI.SetActive(true);
                break;
        }
    }

    public void ChangeInterpolate()
    {
        int ind = 0;
        interpolateIndex = (int)interpolateSlider.value;
        foreach(GameObject obj in SketchUploader.interpolatePool)
        {
            if(ind == interpolateIndex) obj.SetActive(true);
            else obj.SetActive(false);
            ind++;
        }
    }

    public void Import()
    {
        nowState = state.init;
        DemoUI.SetActive(false);
        FunctionManager.RenderToSketch();
        SketchUploader.ImportInterpolate(interpolateIndex);
        Mask.SetActive(false);
        interpolateSlider.value = 5;
    }
}
