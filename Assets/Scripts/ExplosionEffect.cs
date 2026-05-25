using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    private const int PieceCount = 14;

    private readonly SpriteRenderer[] pieces = new SpriteRenderer[PieceCount];
    private readonly Vector3[] directions = new Vector3[PieceCount];
    private readonly float[] speeds = new float[PieceCount];
    private readonly float[] rotations = new float[PieceCount];

    private float duration = 0.48f;
    private float timer;
    private float size = 1f;
    private bool playerExplosion;

    public static void SpawnEnemy(Vector3 position, float radius)
    {
        ExplosionEffect effect = Create(position, Mathf.Max(0.7f, radius * 1.8f), false);
        GameSfxPlayer.PlayEnemyExplosion(position);
    }

    public static void SpawnPlayer(Vector3 position)
    {
        ExplosionEffect effect = Create(position, 1.65f, true);
        effect.duration = 0.78f;
        GameSfxPlayer.PlayPlayerExplosion(position);
    }

    private static ExplosionEffect Create(Vector3 position, float size, bool playerExplosion)
    {
        GameObject effectObject = new GameObject(playerExplosion ? "Player Explosion" : "Enemy Explosion");
        effectObject.transform.position = position;
        ExplosionEffect effect = effectObject.AddComponent<ExplosionEffect>();
        effect.size = size;
        effect.playerExplosion = playerExplosion;
        effect.Build();
        return effect;
    }

    private void Build()
    {
        Sprite pixel = SquareSpriteFactory.Pixel();
        Color core = playerExplosion ? new Color(0.25f, 0.92f, 1f) : new Color(1f, 0.16f, 0.16f);
        Color hot = playerExplosion ? new Color(1f, 1f, 1f) : new Color(1f, 0.86f, 0.26f);
        Color smoke = playerExplosion ? new Color(0.36f, 0.22f, 0.74f) : new Color(0.28f, 0.08f, 0.1f);

        for (int i = 0; i < PieceCount; i++)
        {
            GameObject piece = new GameObject("Burst Piece");
            piece.transform.SetParent(transform, false);
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.sortingOrder = playerExplosion ? 16 : 14;
            renderer.color = i % 3 == 0 ? hot : (i % 3 == 1 ? core : smoke);
            pieces[i] = renderer;

            float angle = i * Mathf.PI * 2f / PieceCount + Hash01(i, 7) * 0.35f;
            directions[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            speeds[i] = Mathf.Lerp(1.8f, 5.8f, Hash01(i, 19)) * size;
            rotations[i] = Mathf.Lerp(-360f, 360f, Hash01(i, 31));
        }
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float ease = 1f - Mathf.Pow(1f - t, 3f);
        float alpha = 1f - t;

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null)
            {
                continue;
            }

            Transform pieceTransform = pieces[i].transform;
            pieceTransform.localPosition = directions[i] * speeds[i] * ease;
            pieceTransform.Rotate(0f, 0f, rotations[i] * Time.unscaledDeltaTime);

            float scale = Mathf.Lerp(size * 0.28f, size * 0.04f, t) * (i % 3 == 0 ? 1.25f : 1f);
            pieceTransform.localScale = new Vector3(scale, scale, 1f);

            Color color = pieces[i].color;
            color.a = alpha;
            pieces[i].color = color;
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private static float Hash01(int x, int salt)
    {
        unchecked
        {
            int hash = x * 73856093 ^ salt * 19349663;
            hash = (hash << 13) ^ hash;
            int value = hash * (hash * hash * 15731 + 789221) + 1376312589;
            return (value & 0x7fffffff) / 2147483647f;
        }
    }
}
