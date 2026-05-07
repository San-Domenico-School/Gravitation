using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates procedural crack textures and URP-compatible transparent emissive materials.
/// No external assets required — runs entirely at runtime.
/// </summary>
public static class CrackVisuals
{
    private static readonly int BaseMapID    = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexID    = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorID  = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID      = Shader.PropertyToID("_Color");
    private static readonly int SurfaceID    = Shader.PropertyToID("_Surface");
    private static readonly int BlendID      = Shader.PropertyToID("_Blend");
    private static readonly int AlphaClipID  = Shader.PropertyToID("_AlphaClip");
    private static readonly int SrcBlendID   = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendID   = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteID     = Shader.PropertyToID("_ZWrite");

    public static Texture2D GenerateCrackTexture(int size = 256, int branches = 7, int seed = 0)
    {
        var rng = new System.Random(seed == 0 ? Random.Range(1, int.MaxValue) : seed);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int b = 0; b < branches; b++)
        {
            float angle = (b / (float)branches) * Mathf.PI * 2f + RandRange(rng, -0.4f, 0.4f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float length = size * RandRange(rng, 0.32f, 0.46f);
            Vector2 end = center + dir * length;
            DrawJaggedLine(pixels, size, center, end, rng, 3, 1f);

            int subBranches = rng.Next(1, 3);
            for (int s = 0; s < subBranches; s++)
            {
                float t = RandRange(rng, 0.35f, 0.85f);
                Vector2 origin = Vector2.Lerp(center, end, t);
                float subAngle = angle + RandRange(rng, -1.2f, 1.2f);
                Vector2 subDir = new Vector2(Mathf.Cos(subAngle), Mathf.Sin(subAngle));
                float subLength = length * RandRange(rng, 0.25f, 0.5f);
                DrawJaggedLine(pixels, size, origin, origin + subDir * subLength, rng, 2, 0.7f);
            }
        }

        DrawSoftHotspot(pixels, size, center, size * 0.06f, 1f);

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return tex;
    }

    public static Material CreateGlowMaterial(Color glowColor, Texture2D crackTexture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        ApplyTexture(mat, crackTexture);
        ApplyColor(mat, glowColor);
        ConfigureTransparent(mat);
        return mat;
    }

    public static void ApplyColor(Material mat, Color color)
    {
        if (mat.HasProperty(BaseColorID)) mat.SetColor(BaseColorID, color);
        if (mat.HasProperty(ColorID))     mat.SetColor(ColorID, color);
    }

    private static void ApplyTexture(Material mat, Texture2D tex)
    {
        if (mat.HasProperty(BaseMapID)) mat.SetTexture(BaseMapID, tex);
        if (mat.HasProperty(MainTexID)) mat.SetTexture(MainTexID, tex);
    }

    private static void ConfigureTransparent(Material mat)
    {
        if (mat.HasProperty(SurfaceID))   mat.SetFloat(SurfaceID, 1f);   // 1 = Transparent
        if (mat.HasProperty(BlendID))     mat.SetFloat(BlendID, 0f);     // 0 = Alpha
        if (mat.HasProperty(AlphaClipID)) mat.SetFloat(AlphaClipID, 0f);
        if (mat.HasProperty(SrcBlendID))  mat.SetFloat(SrcBlendID, (float)BlendMode.SrcAlpha);
        if (mat.HasProperty(DstBlendID))  mat.SetFloat(DstBlendID, (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty(ZWriteID))    mat.SetFloat(ZWriteID, 0f);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void DrawJaggedLine(Color[] pixels, int size, Vector2 from, Vector2 to, System.Random rng, int thickness, float intensity)
    {
        int segments = Mathf.Clamp(Mathf.RoundToInt((to - from).magnitude / 6f), 4, 24);
        Vector2 prev = from;
        Vector2 dir = (to - from).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 onLine = Vector2.Lerp(from, to, t);
            float jitter = RandRange(rng, -3f, 3f) * (1f - Mathf.Abs(t - 0.5f) * 0.6f);
            Vector2 next = onLine + normal * jitter;
            DrawLine(pixels, size, prev, next, thickness, intensity);
            prev = next;
        }
    }

    private static void DrawLine(Color[] pixels, int size, Vector2 a, Vector2 b, int thickness, float intensity)
    {
        int x0 = Mathf.RoundToInt(a.x);
        int y0 = Mathf.RoundToInt(a.y);
        int x1 = Mathf.RoundToInt(b.x);
        int y1 = Mathf.RoundToInt(b.y);

        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            for (int oy = -thickness; oy <= thickness; oy++)
            {
                for (int ox = -thickness; ox <= thickness; ox++)
                {
                    int px = x0 + ox;
                    int py = y0 + oy;
                    if (px < 0 || py < 0 || px >= size || py >= size) continue;
                    float dist = Mathf.Sqrt(ox * ox + oy * oy);
                    if (dist > thickness) continue;
                    float falloff = 1f - (dist / (thickness + 1f));
                    float a01 = falloff * intensity;
                    int idx = py * size + px;
                    Color existing = pixels[idx];
                    float merged = Mathf.Min(1f, existing.a + a01);
                    pixels[idx] = new Color(1f, 1f, 1f, merged);
                }
            }

            if (x0 == x1 && y0 == y1) break;
            int e2 = err << 1;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 <  dx) { err += dx; y0 += sy; }
        }
    }

    private static void DrawSoftHotspot(Color[] pixels, int size, Vector2 center, float radius, float intensity)
    {
        int cx = Mathf.RoundToInt(center.x);
        int cy = Mathf.RoundToInt(center.y);
        int r = Mathf.CeilToInt(radius);
        for (int oy = -r; oy <= r; oy++)
        {
            for (int ox = -r; ox <= r; ox++)
            {
                int px = cx + ox;
                int py = cy + oy;
                if (px < 0 || py < 0 || px >= size || py >= size) continue;
                float dist = Mathf.Sqrt(ox * ox + oy * oy);
                if (dist > radius) continue;
                float falloff = 1f - (dist / radius);
                falloff = falloff * falloff;
                float a01 = falloff * intensity;
                int idx = py * size + px;
                Color existing = pixels[idx];
                float merged = Mathf.Min(1f, existing.a + a01);
                pixels[idx] = new Color(1f, 1f, 1f, merged);
            }
        }
    }

    private static float RandRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
