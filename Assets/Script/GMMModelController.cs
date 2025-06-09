using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GMMModelController : MonoBehaviour
{
    public GameObject model;
    public float moveSpeed = 2, rotateSpeed = 15f;
    public bool isTarget = false, isRight = false;

    private float XAxis, YAxis;
    private Vector3 LastPosition, TargetPosition;
    private Quaternion LastRotation;

    public ModelController ModelController;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        model = ModelController.model;
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
            model.transform.RotateAround(model.transform.position, Vector3.right, YAxis* Mathf.Abs(rotateSpeed) * Time.deltaTime);
            model.transform.RotateAround(model.transform.position, Vector3.up, -1* XAxis* Mathf.Abs(rotateSpeed) * Time.deltaTime);
        }
        else isRight = false;
    }
}
