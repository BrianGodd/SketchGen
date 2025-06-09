using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Events;
using TMPro;

public class FunctionManager : MonoBehaviour
{
    public RenderTexture renderTexture;
    public RawImage targetRawImage;
    public float Sketchify_threshold = 0.2f;
    public int Sketchify_dilation = 3;
    public UnityEvent custom_event;
    public Light EnvLight;
    public Slider ThresholdSlider, DilationSlider, LightSlider;
    public TextMeshProUGUI thresholdTXT, dilationTXT;

    void Start()
    {
        
    }

    void Update()
    {
        Sketchify_threshold = ThresholdSlider.value;
        Sketchify_dilation = (int)(DilationSlider.value);
        thresholdTXT.text = Sketchify_threshold.ToString("F2");
        dilationTXT.text = Sketchify_dilation.ToString();
        EnvLight.intensity = LightSlider.value;
    }

    public void RenderToSketch()
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentRT;

        Texture2D sketch = Sketchify(tex);

        byte[] pngBytes = sketch.EncodeToPNG();
        string path = Application.dataPath + "/texture/sketch_idl.png";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, pngBytes);

        targetRawImage.texture = sketch;
        custom_event?.Invoke();
    }

    public Texture2D Sketchify_Import(Texture2D input)
    {
        return Sketchify(input);
    }

    Texture2D Sketchify(Texture2D input)
    {
        int width = input.width;
        int height = input.height;
        Texture2D output = new Texture2D(width, height, TextureFormat.RGB24, false);

        Color[] src = input.GetPixels();
        Color[] dst = new Color[src.Length];
        bool[] edgeMask = new bool[width * height];

        float threshold = Sketchify_threshold; // 可調整，越低越容易被視為邊緣（敏感度高）

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int i = y * width + x;

                float gx =
                    -1 * src[(y - 1) * width + (x - 1)].grayscale +
                    1 * src[(y - 1) * width + (x + 1)].grayscale +
                    -2 * src[y * width + (x - 1)].grayscale +
                    2 * src[y * width + (x + 1)].grayscale +
                    -1 * src[(y + 1) * width + (x - 1)].grayscale +
                    1 * src[(y + 1) * width + (x + 1)].grayscale;

                float gy =
                    -1 * src[(y - 1) * width + (x - 1)].grayscale +
                    -2 * src[(y - 1) * width + x].grayscale +
                    -1 * src[(y - 1) * width + (x + 1)].grayscale +
                    1 * src[(y + 1) * width + (x - 1)].grayscale +
                    2 * src[(y + 1) * width + x].grayscale +
                    1 * src[(y + 1) * width + (x + 1)].grayscale;

                float edge = Mathf.Sqrt(gx * gx + gy * gy);

                // 只留下高於閾值的為黑線，其餘為白
                dst[i] = edge > threshold ? Color.black : Color.white;
                edgeMask[i] = edge > threshold;
            }
        }

        // Dilation
        bool[] dilated = new bool[width * height];
        int radius = Sketchify_dilation;

        for (int y = radius; y < height - radius; y++)
        {
            for (int x = radius; x < width - radius; x++)
            {
                int i = y * width + x;
                if (!edgeMask[i]) continue;

                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        int ni = ny * width + nx;
                        dilated[ni] = true;
                    }
                }
            }
        }

        for (int i = 0; i < dilated.Length; i++)
        {
            output.SetPixel(i % width, i / width, dilated[i] ? Color.black : Color.white);
        }
        output.Apply();
        return output;
    }


}
