using UnityEngine;

public class GMMController : MonoBehaviour
{
    public Material selectedMat, unselectedMat;
    public Camera renderCamera;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private MeshRenderer meshRenderer;

    private static GMMController lastHovered;
    public bool isSelected = false;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        // 將滑鼠螢幕座標轉為 Ray（使用 renderCamera）
        if(!renderCamera.gameObject.active) return;

        Ray ray = renderCamera.ScreenPointToRay(Input.mousePosition);
        // Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);
        RaycastHit hit;

        bool isHit = Physics.Raycast(ray, out hit) && hit.transform == transform;

        // 懸停效果
        if (isHit)
        {
            // Debug.Log(this.gameObject.name);
            if (lastHovered != this)
            {
                if (lastHovered != null)
                    lastHovered.ResetScale();

                lastHovered = this;
                targetScale = originalScale * 2f;
            }

            // 點擊切換材質
            if (Input.GetMouseButtonDown(0))
            {
                ChangeMaterial();
            }
        }
        else
        {
            if (lastHovered == this)
            {
                ResetScale();
                lastHovered = null;
            }
        }

        // 平滑縮放
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
    }

    void ResetScale()
    {
        targetScale = originalScale;
    }

    void ChangeMaterial()
    {
        isSelected = !isSelected;
        if (meshRenderer != null)
        {
            meshRenderer.material = (isSelected) ? selectedMat : unselectedMat;
        }

        GameObject.Find("DataManager").GetComponent<GMMLoader>().ChangeSelected(this.gameObject);
    }
}
