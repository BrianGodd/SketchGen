using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ModelController : MonoBehaviour
{
    public GameObject model;
    public float moveSpeed = 2, rotateSpeed = 15f;
    public bool isTarget = false, isRight = false;
    public bool xAble = false, yAble = true;
    public Image XButton, YButton;
    public Color AbleColor, UnableColor;
    private float XAxis, YAxis;
    private Vector3 LastPosition, TargetPosition;
    private Quaternion LastRotation;

    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;
    public GameObject targetUI;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(model == null || !IsPointerOverTargetUI()) return;

        bool mouseCenter = Input.GetMouseButton(2); //滑鼠中鍵
        bool mouseRight = Input.GetMouseButton(1);  //滑鼠右鍵

        if(mouseCenter)
        {  
            if(!isTarget)
            {
                LastPosition = model.transform.localPosition;
                TargetPosition = Input.mousePosition;
                isTarget = true;
            }
            model.transform.localPosition =  Vector3.Lerp(model.transform.localPosition, LastPosition + 0.0075f*(Input.mousePosition - TargetPosition), Mathf.Abs(moveSpeed) * Time.deltaTime);
        }
        else
        {
            if(isTarget) isTarget = false;
        }

        if(mouseRight)
        {  
            if(!isRight)
            {
                LastRotation = model.transform.rotation;
                TargetPosition = Input.mousePosition;
                isRight = true;
            }
            XAxis = Input.mousePosition.x - TargetPosition.x;
            YAxis = Input.mousePosition.y - TargetPosition.y;
            TargetPosition = Input.mousePosition;
            if(xAble) model.transform.RotateAround(model.transform.position, Vector3.right, YAxis* Mathf.Abs(rotateSpeed) * Time.deltaTime);
            if(yAble) model.transform.RotateAround(model.transform.position, Vector3.up, -1* XAxis* Mathf.Abs(rotateSpeed) * Time.deltaTime);
        }
        else isRight = false;
    }

    public bool IsPointerOverTargetUI()
    {
        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == targetUI)
                return true;
        }

        return false;
    }

    public void SwitchX()
    {
        xAble = !xAble;
        XButton.color = (xAble)? AbleColor:UnableColor;
    }

    public void SwitchY()
    {
        yAble = !yAble;
        YButton.color = (yAble)? AbleColor:UnableColor;
    }
}
