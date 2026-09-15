using System.Collections.Generic;
using UnityEngine;

/// <summary>Temporarily isolates only the reserved maze layer; restores it on close.</summary>
internal sealed class MazeWorldIsolation
{
    private readonly Dictionary<Camera, bool> cameraBits = new Dictionary<Camera, bool>();
    private readonly bool[] ignoredBefore = new bool[32];
    private int layer = -1;
    private Camera puzzleCamera;

    public void Acquire(int mazeLayer, Camera camera)
    {
        if (layer >= 0) return;
        layer = mazeLayer;
        puzzleCamera = camera;
        for (int i = 0; i < 32; i++)
        {
            ignoredBefore[i] = Physics2D.GetIgnoreLayerCollision(layer, i);
            Physics2D.IgnoreLayerCollision(layer, i, i != layer);
        }
        RefreshCameras();
    }

    public void RefreshCameras()
    {
        if (layer < 0) return;
        int bit = 1 << layer;
        foreach (Camera camera in Camera.allCameras)
        {
            if (camera == null || camera == puzzleCamera || camera.cameraType != CameraType.Game) continue;
            if (!cameraBits.ContainsKey(camera)) cameraBits.Add(camera, (camera.cullingMask & bit) != 0);
            camera.cullingMask &= ~bit;
        }
    }

    public void Release()
    {
        if (layer < 0) return;
        int bit = 1 << layer;
        foreach (var pair in cameraBits)
        {
            if (pair.Key == null) continue;
            // Restore only our bit, retaining unrelated mask changes made meanwhile.
            pair.Key.cullingMask = pair.Value ? pair.Key.cullingMask | bit : pair.Key.cullingMask & ~bit;
        }
        cameraBits.Clear();
        for (int i = 0; i < 32; i++) Physics2D.IgnoreLayerCollision(layer, i, ignoredBefore[i]);
        layer = -1;
        puzzleCamera = null;
    }
}
