using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SketchViewController : MonoBehaviour
{
    public int id;
    public Color selected, unselected;
    public Image Frame, Sketch;
    public GameObject Toggle;

    private SketchUploader SketchUploader;

    // Start is called before the first frame update
    void Start()
    {
        SketchUploader = GameObject.Find("DataManager").GetComponent<SketchUploader>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Toggle.GetComponent<Toggle>().interactable && !Toggle.GetComponent<Toggle>().isOn)
        {
            if(SketchUploader.select1 != -1 && SketchUploader.select2 != -1)
            {
                Toggle.GetComponent<Toggle>().interactable = false;
            }
        }
        else if(SketchUploader.select1 == -1 || SketchUploader.select2 == -1)
        {
            Toggle.GetComponent<Toggle>().interactable = true;
        }
    }

    public void ColorChange()
    {
        Frame.color = (Frame.color == selected)? unselected:selected;
    }

    public void Switch()
    {
        ColorChange();
        SketchUploader.Target(this.gameObject, id);
    }

    public void Choose()
    {
        SketchUploader.Select(id);
    }
}
