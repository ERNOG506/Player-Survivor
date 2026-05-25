using System.Collections.Generic;
using UnityEngine;

public class ProceduralArena : MonoBehaviour
{
    public Transform target;
    public float chunkSize = 8f;
    public int visibleRadius = 3;
    public int propsPerChunk = 7;

    private readonly Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastCenter = new Vector2Int(int.MinValue, int.MinValue);

    private void Start()
    {
        if (target == null && SurvivorGameManager.Instance != null)
        {
            target = SurvivorGameManager.Instance.PlayerTransform;
        }

        RefreshChunks(force: true);
    }

    private void Update()
    {
        if (target == null && SurvivorGameManager.Instance != null)
        {
            target = SurvivorGameManager.Instance.PlayerTransform;
        }

        RefreshChunks(force: false);
    }

    private void RefreshChunks(bool force)
    {
        if (target == null || chunkSize <= 0f)
        {
            return;
        }

        Vector2Int center = WorldToChunk(target.position);
        if (!force && center == lastCenter)
        {
            return;
        }

        lastCenter = center;
        HashSet<Vector2Int> needed = new HashSet<Vector2Int>();
        for (int y = -visibleRadius; y <= visibleRadius; y++)
        {
            for (int x = -visibleRadius; x <= visibleRadius; x++)
            {
                Vector2Int coord = new Vector2Int(center.x + x, center.y + y);
                needed.Add(coord);
                if (!chunks.ContainsKey(coord))
                {
                    chunks.Add(coord, CreateChunk(coord));
                }
            }
        }

        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (Vector2Int coord in chunks.Keys)
        {
            if (!needed.Contains(coord))
            {
                toRemove.Add(coord);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            GameObject chunk = chunks[toRemove[i]];
            chunks.Remove(toRemove[i]);
            if (chunk != null)
            {
                Destroy(chunk);
            }
        }
    }

    private GameObject CreateChunk(Vector2Int coord)
    {
        GameObject chunk = new GameObject($"Terrain Chunk {coord.x},{coord.y}");
        chunk.transform.SetParent(transform, false);
        chunk.transform.position = ChunkCenter(coord);

        CreateBlock(chunk.transform, "Floor", Vector3.zero, new Vector3(chunkSize, chunkSize, 1f), FloorColor(coord), -30);
        CreateGrid(chunk.transform, coord);
        CreateProps(chunk.transform, coord);
        return chunk;
    }

    private void CreateGrid(Transform parent, Vector2Int coord)
    {
        Color lineColor = new Color(0.13f, 0.18f, 0.23f, 0.32f);
        for (int i = -1; i <= 1; i++)
        {
            float offset = i * chunkSize / 3f;
            CreateBlock(parent, "Grid V", new Vector3(offset, 0f, 0f), new Vector3(0.025f, chunkSize, 1f), lineColor, -28);
            CreateBlock(parent, "Grid H", new Vector3(0f, offset, 0f), new Vector3(chunkSize, 0.025f, 1f), lineColor, -28);
        }

        Color seamColor = new Color(0.08f, 0.45f, 0.42f, 0.18f + Hash01(coord.x, coord.y, 5) * 0.12f);
        CreateBlock(parent, "Seam Top", new Vector3(0f, chunkSize * 0.5f, 0f), new Vector3(chunkSize, 0.035f, 1f), seamColor, -27);
        CreateBlock(parent, "Seam Right", new Vector3(chunkSize * 0.5f, 0f, 0f), new Vector3(0.035f, chunkSize, 1f), seamColor, -27);
    }

    private void CreateProps(Transform parent, Vector2Int coord)
    {
        for (int i = 0; i < propsPerChunk; i++)
        {
            float roll = Hash01(coord.x, coord.y, i);
            if (roll < 0.34f)
            {
                continue;
            }

            float x = Mathf.Lerp(-chunkSize * 0.42f, chunkSize * 0.42f, Hash01(coord.x, coord.y, i + 17));
            float y = Mathf.Lerp(-chunkSize * 0.42f, chunkSize * 0.42f, Hash01(coord.x, coord.y, i + 43));
            float w = Mathf.Lerp(0.18f, 0.72f, Hash01(coord.x, coord.y, i + 71));
            float h = Mathf.Lerp(0.12f, 0.44f, Hash01(coord.x, coord.y, i + 97));
            Color color = PropColor(Hash01(coord.x, coord.y, i + 131));
            CreateBlock(parent, "Terrain Detail", new Vector3(x, y, 0f), new Vector3(w, h, 1f), color, -24);
        }
    }

    private void CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color, int order)
    {
        GameObject block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = scale;

        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = SquareSpriteFactory.Pixel();
        renderer.color = color;
        renderer.sortingOrder = order;
    }

    private Vector2Int WorldToChunk(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.y / chunkSize));
    }

    private Vector3 ChunkCenter(Vector2Int coord)
    {
        return new Vector3((coord.x + 0.5f) * chunkSize, (coord.y + 0.5f) * chunkSize, 0f);
    }

    private static Color FloorColor(Vector2Int coord)
    {
        float n = Hash01(coord.x, coord.y, 0);
        return Color.Lerp(new Color(0.045f, 0.052f, 0.072f), new Color(0.06f, 0.075f, 0.095f), n);
    }

    private static Color PropColor(float value)
    {
        if (value < 0.33f)
        {
            return new Color(0.08f, 0.34f, 0.31f, 0.42f);
        }

        if (value < 0.66f)
        {
            return new Color(0.22f, 0.16f, 0.36f, 0.34f);
        }

        return new Color(0.36f, 0.28f, 0.1f, 0.28f);
    }

    private static float Hash01(int x, int y, int salt)
    {
        unchecked
        {
            int hash = x * 73856093 ^ y * 19349663 ^ salt * 83492791;
            hash = (hash << 13) ^ hash;
            int value = hash * (hash * hash * 15731 + 789221) + 1376312589;
            return (value & 0x7fffffff) / 2147483647f;
        }
    }
}
