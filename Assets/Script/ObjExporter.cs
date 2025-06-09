using System;
using System.IO;
using UnityEngine;
using System.Text;
using TMPro;

public static class PathHelper
{
    public static string GetDownloadsPath()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, "Downloads");
#else
        return Application.persistentDataPath;
#endif
    }
}

public class ObjExporter : MonoBehaviour
{
    public GameObject CompleteUI;
    public TextMeshProUGUI Message;

    public void Export(GameObject go, string path)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("No MeshFilter or Mesh found on GameObject.");
            return;
        }

        Mesh mesh = mf.sharedMesh;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# Exported by ObjExporter");
        foreach (Vector3 v in mesh.vertices)
        {
            Vector3 wv = go.transform.TransformPoint(v);
            sb.AppendLine($"v {wv.x} {wv.y} {wv.z}");
        }

        foreach (Vector3 n in mesh.normals)
        {
            Vector3 wn = go.transform.TransformDirection(n);
            sb.AppendLine($"vn {wn.x} {wn.y} {wn.z}");
        }

        foreach (Vector2 uv in mesh.uv)
        {
            sb.AppendLine($"vt {uv.x} {uv.y}");
        }

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                // Wavefront OBJ uses 1-based indexing
                int i1 = triangles[t] + 1;
                int i2 = triangles[t + 1] + 1;
                int i3 = triangles[t + 2] + 1;
                sb.AppendLine($"f {i1}/{i1}/{i1} {i2}/{i2}/{i2} {i3}/{i3}/{i3}");
            }
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"OBJ exported to: {path}");
        CompleteUI.SetActive(true);
        Message.text = $"OBJ exported to: {path}";
    }
}
