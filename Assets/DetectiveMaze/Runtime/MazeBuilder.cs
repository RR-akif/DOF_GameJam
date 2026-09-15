using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds hand-authored sprite geometry in the XY plane, with normal 2D colliders.</summary>
[DisallowMultipleComponent]
public sealed class MazeBuilder : MonoBehaviour
{
    [SerializeField, Min(0.5f)] private float cellSize = 1f;
    public MazeDetectiveMovement Player { get; private set; }
    public Transform StartDoor { get; private set; }
    public Transform EndDoor { get; private set; }
    public Vector2 SpawnPosition { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    private int layer;
    private readonly Dictionary<Sprite, Material> artMaterials = new Dictionary<Sprite, Material>();
    private PhysicsMaterial2D noFriction;

    public void Build(MazePuzzleController owner, PuzzleInputHandler input, Camera puzzleCamera)
    {
        string[] rows = MazeLayout.CreateRows();
        MazeLayout.Validate(rows);
        Width = rows[0].Length;
        Height = rows.Length;
        layer = LayerMask.NameToLayer(MazePuzzleController.LayerName);
        if (layer < 0) throw new InvalidOperationException("The MazePuzzle layer is missing. Run the Detective Maze setup menu.");
        gameObject.layer = layer;
        // Load every required asset before destroying an existing preview.
        Sprite wall = LoadSprite("Wall");
        Sprite floor = LoadSprite("Floor");
        Sprite lockedDoor = LoadSprite("LockedDoor");
        Sprite openDoor = LoadSprite("OpenDoor");
        Sprite detective = LoadSprite("Detective");
        Sprite light = LoadSprite("LightGlow");
        noFriction = Resources.Load<PhysicsMaterial2D>("DetectiveMaze/MazeNoFriction");
        if (noFriction == null)
            throw new InvalidOperationException("Maze materials are missing. Run Tools > Detective Maze > Build Prefab and Test Scene.");
        artMaterials.Clear();
        foreach (Sprite artwork in new[] { wall, floor, lockedDoor, openDoor, detective, light })
        {
            Material material = Resources.Load<Material>("DetectiveMaze/Materials/" + artwork.name);
            if (material == null || material.mainTexture != artwork.texture)
                throw new InvalidOperationException($"The {artwork.name} sprite material is missing or has the wrong texture. Run Tools > Detective Maze > Build Prefab and Test Scene to repair it.");
            artMaterials.Add(artwork, material);
        }
        ClearGeneratedChildren();
        Transform grid = NewObject("Grid", transform).transform;
        Transform walls = NewObject("Walls", grid).transform;
        Transform floors = NewObject("Floors", grid).transform;
        Transform lighting = NewObject("Lighting", grid).transform;
        Vector2Int startCell = default;

        for (int row = 0; row < Height; row++)
        {
            for (int column = 0; column < Width; column++)
            {
                char cell = rows[row][column];
                Vector3 point = CellLocalPosition(column, row);
                if (cell == '#')
                {
                    GameObject piece = NewSprite($"Wall_{column}_{row}", walls, wall, point,
                        new Vector2(cellSize, cellSize), 10, new Color(0.76f, 0.82f, 0.92f));
                    BoxCollider2D collider = piece.AddComponent<BoxCollider2D>();
                    collider.size = wall.bounds.size;
                    collider.sharedMaterial = noFriction;
                }
                else
                {
                    NewSprite($"Floor_{column}_{row}", floors, floor, point,
                        new Vector2(cellSize, cellSize), 0, new Color(0.70f, 0.75f, 0.83f));
                    if (cell == 'S')
                    {
                        startCell = new Vector2Int(column, row);
                        StartDoor = NewSprite("StartDoor", transform, lockedDoor,
                            point + Vector3.up * (cellSize * 0.23f), Vector2.one * (cellSize * 0.92f), 12, Color.white).transform;
                        SpawnPosition = transform.TransformPoint(point);
                    }
                    if (cell == 'E')
                    {
                        EndDoor = NewSprite("EndDoor", transform, openDoor, point,
                            Vector2.one * (cellSize * 0.95f), 12, Color.white).transform;
                        BoxCollider2D trigger = EndDoor.gameObject.AddComponent<BoxCollider2D>();
                        trigger.size = new Vector2(cellSize * 0.65f / EndDoor.localScale.x,
                            cellSize * 0.65f / EndDoor.localScale.y);
                        trigger.isTrigger = true;
                        EndDoor.gameObject.AddComponent<MazeExitDoor>().Initialize(owner);
                        NewSprite("ExitLight", lighting, light, point, Vector2.one * (cellSize * 2.8f),
                            5, new Color(1f, 0.93f, 0.73f, 0.40f));
                    }
                }
            }
        }

        // Fixed local light pools, hand-placed alongside the fixed layout. No Light2D / URP dependency.
        Vector2Int[] lampCells = { new Vector2Int(5, 3), new Vector2Int(11, 3),
            new Vector2Int(3, 9), new Vector2Int(15, 11) };
        for (int i = 0; i < lampCells.Length; i++)
        {
            Vector2Int cell = lampCells[i];
            if (cell.x >= Width || cell.y >= Height || rows[cell.y][cell.x] == '#') continue;
            Color tint = i == 1 ? new Color(0.48f, 0.70f, 1f, 0.28f) : new Color(1f, 0.90f, 0.70f, 0.27f);
            NewSprite($"LampPool_{i}", lighting, light, CellLocalPosition(cell.x, cell.y),
                Vector2.one * (cellSize * 2.5f), 5, tint);
        }

        GameObject player = NewSprite("DetectivePlayer", transform, detective,
            CellLocalPosition(startCell.x, startCell.y), Vector2.one * (cellSize * 0.72f), 20, Color.white);
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        CircleCollider2D circle = player.AddComponent<CircleCollider2D>();
        circle.radius = cellSize * 0.25f / player.transform.localScale.x;
        circle.sharedMaterial = noFriction;
        Player = player.AddComponent<MazeDetectiveMovement>();
        Player.Initialize(input, owner);
        Player.ResetTo(SpawnPosition);
        GameObject playerLight = NewSprite("DetectiveLight", player.transform, light, Vector3.zero,
            Vector2.one * (cellSize * 3f), 5, new Color(1f, 0.94f, 0.79f, 0.17f));
        // NewSprite sizes in local space; compensate for the detective's sprite scale.
        playerLight.transform.localScale = Vector3.Scale(playerLight.transform.localScale,
            new Vector3(1f / player.transform.localScale.x, 1f / player.transform.localScale.y, 1f));
        FrameCamera(puzzleCamera);
    }

    public void FrameCamera(Camera camera)
    {
        if (camera == null || Width == 0 || Height == 0) return;
        camera.gameObject.layer = layer;
        camera.orthographic = true;
        camera.cullingMask = 1 << layer;
        camera.transform.SetPositionAndRotation(transform.position + Vector3.back * 10f, Quaternion.identity);
        float aspect = camera.targetTexture != null
            ? (float)camera.targetTexture.width / camera.targetTexture.height : camera.aspect;
        camera.aspect = Mathf.Max(0.1f, aspect);
        camera.orthographicSize = Mathf.Max(Height * cellSize * 0.5f,
            Width * cellSize * 0.5f / camera.aspect) * 1.14f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 30f;
    }

    private Vector3 CellLocalPosition(int column, int row)
    {
        return new Vector3((column - (Width - 1) * 0.5f) * cellSize,
            ((Height - 1) * 0.5f - row) * cellSize, 0f);
    }

    public static Sprite LoadSprite(string name)
    {
        Sprite sprite = Resources.Load<Sprite>("DetectiveMaze/Art/" + name);
        if (sprite == null) throw new InvalidOperationException($"Missing sprite {name}. Run the Detective Maze setup menu to import the generated PNGs as sprites.");
        return sprite;
    }

    private GameObject NewObject(string name, Transform parent)
    {
        var child = new GameObject(name);
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child;
    }

    private GameObject NewSprite(string name, Transform parent, Sprite sprite, Vector3 position,
        Vector2 size, int sortingOrder, Color color)
    {
        GameObject child = NewObject(name, parent);
        child.transform.localPosition = position;
        child.transform.localScale = new Vector3(size.x / sprite.bounds.size.x,
            size.y / sprite.bounds.size.y, 1f);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        // Each artwork has its own material and texture. Never reuse a material
        // carrying wall/floor artwork for the detective or either door.
        renderer.sharedMaterial = artMaterials[sprite];
        var properties = new MaterialPropertyBlock();
        properties.SetTexture("_MainTex", sprite.texture);
        renderer.SetPropertyBlock(properties);
        renderer.sortingOrder = sortingOrder;
        renderer.color = color;
        return child;
    }

    private void ClearGeneratedChildren()
    {
        Player = null;
        StartDoor = null;
        EndDoor = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            // Destroy is deferred in Play mode: disable first to prevent one-frame ghost colliders.
            child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
    }
}
