using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DrawController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public RawImage targetImage;
    public int brushSize = 4, eraseSize = 5;
    public Color drawColor = Color.black;

    private Texture2D drawTexture;
    private RectTransform rectTransform;
    private bool isDrawing = false, isErase = false;
    private Vector2? lastDrawPos = null;

    public Slider BrushSlider, EraseSlider;
    public TextMeshProUGUI brushTXT, eraseTXT;

    public FunctionManager FunctionManager;

    void Start()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<RawImage>();
        }

        rectTransform = targetImage.rectTransform;

        drawTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        FillWhite(drawTexture);
        drawTexture.Apply();
        targetImage.texture = drawTexture;
    }

    void Update()
    {
        brushSize = (int)(BrushSlider.value);
        eraseSize = (int)(EraseSlider.value);
        
        brushTXT.text = brushSize.ToString();
        eraseTXT.text = eraseSize.ToString();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDrawing = true;
        Draw(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;
        lastDrawPos = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDrawing)
        {
            Draw(eventData.position);
        }
    }

    void Draw(Vector2 screenPos)
    {
        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPos, null, out localPos))
        {
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            float px = (localPos.x + width / 2f) / width * drawTexture.width;
            float py = (localPos.y + height / 2f) / height * drawTexture.height;

            Vector2 current = new Vector2(px, py);

            if (Input.GetMouseButton(1)) // 右鍵
            {
                isErase = true;
                drawColor = Color.white; // 橡皮擦
            }
            else
            {
                isErase = false;
                drawColor = Color.black; // 畫筆（可改成 UI 選色器顏色）
            }

            if (lastDrawPos != null)
            {
                DrawLine(lastDrawPos.Value, current);
            }
            else
            {
                DrawCircle((int)px, (int)py);
            }

            lastDrawPos = current;
            drawTexture.Apply();
        }
    }

    void DrawCircle(int cx, int cy)
    {
        int ss = (isErase)? eraseSize:brushSize;
        for (int x = -ss; x <= ss; x++)
        {
            for (int y = -ss; y <= ss; y++)
            {
                if (x * x + y * y <= ss * ss)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && py >= 0 && px < drawTexture.width && py < drawTexture.height)
                    {
                        drawTexture.SetPixel(px, py, drawColor);
                    }
                }
            }
        }
    }

    void DrawLine(Vector2 from, Vector2 to)
    {
        int steps = (int)Vector2.Distance(from, to);
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = (int)Mathf.Lerp(from.x, to.x, t);
            int y = (int)Mathf.Lerp(from.y, to.y, t);
            DrawCircle(x, y);
        }
    }

    void FillWhite(Texture2D tex)
    {
        Color[] colors = new Color[tex.width * tex.height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        tex.SetPixels(colors);
    }

    public void Clear()
    {
        FillWhite(drawTexture);
        drawTexture.Apply();
    }

    public Texture2D GetDrawnTexture()
    {
        return drawTexture;
    }

    public void CaptureCurrentImageAsCanvas()
    {
        Texture tex = targetImage.texture;
        Texture2D newTex;

        if (tex is Texture2D tex2D)
        {
            newTex = new Texture2D(tex2D.width, tex2D.height, TextureFormat.RGBA32, false);
            newTex.SetPixels(tex2D.GetPixels());
            newTex.Apply();
        }
        else
        {
            Debug.LogWarning("Target texture is not Texture2D. Creating new blank canvas.");
            newTex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            FillWhite(newTex);
            newTex.Apply();
        }

        // newTex = FunctionManager.Sketchify_Import(newTex);

        // 指定為新的畫布
        drawTexture = newTex;
        targetImage.texture = drawTexture;

        Debug.Log("Image captured and set as drawable canvas.");
    }

}
