// AlchemistSDF.cginc — SDF primitives, operations, noise & utilities for Mergistry shaders
// All functions work in 2D (float2 p) centered at (0,0).

// ── Global uniforms (set via Shader.SetGlobal* from ShaderGlobalController) ──
float  _GameTime;          // elapsed time with pause/slowmo control
float2 _ScreenShake;       // current screenshake offset
float4 _GlobalFlash;       // xyz=color, w=intensity

// ── Primitives ───────────────────────────────────────────────────────────────

// Filled circle: negative inside, positive outside
float sdCircle(float2 p, float r)
{
    return length(p) - r;
}

// Axis-aligned box: b = half-extents
float sdBox(float2 p, float2 b)
{
    float2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// Rounded box: b = half-extents, r = corner radius
float sdRoundBox(float2 p, float2 b, float r)
{
    float2 d = abs(p) - b + r;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - r;
}

// Line segment (returns distance, use - thickness for thick line)
float sdSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

// Approximate ellipse via scaled circle
float sdEllipse(float2 p, float rx, float ry)
{
    float2 q = p / float2(rx, ry);
    return (length(q) - 1.0) * min(rx, ry);
}

// Equilateral-style upward triangle with half-height h, half-width w
float sdTriangle(float2 p, float w, float h)
{
    p.x = abs(p.x);
    float2 a = float2(0, h);
    float2 b = float2(w, -h);
    float2 c = float2(-w, -h);
    float2 e0 = b - a; float2 v0 = p - a;
    float2 e1 = c - b; float2 v1 = p - b;
    float2 e2 = a - c; float2 v2 = p - c;
    float2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
    float2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
    float2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
    float s = sign(e0.x * e2.y - e0.y * e2.x);
    float2 d2 = min(min(
        float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
        float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
        float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
    return -sqrt(d2.x) * sign(d2.y);
}

// Regular hexagon
float sdHexagon(float2 p, float r)
{
    const float2 k = float2(-0.866025404, 0.5);
    p = abs(p);
    p -= 2.0 * min(dot(k, p), 0.0) * k;
    p -= float2(clamp(p.x, -k.y * r, k.y * r), r);
    return length(p) * sign(p.y);
}

// Ring: radius r, width w (annular circle)
float sdRing(float2 p, float r, float w)
{
    return abs(length(p) - r) - w;
}

// ── Boolean ops ──────────────────────────────────────────────────────────────

float opUnion(float d1, float d2) { return min(d1, d2); }
float opSubtraction(float d1, float d2) { return max(-d1, d2); }
float opIntersection(float d1, float d2) { return max(d1, d2); }

// Smooth union (metaball-style), k = blend radius
float opSmoothUnion(float d1, float d2, float k)
{
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) - k * h * (1.0 - h);
}

// Rotate UV by angle radians
float2 opRotate(float2 p, float angle)
{
    float c = cos(angle); float s = sin(angle);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// ── Edge / fill helpers ───────────────────────────────────────────────────────

// Smooth alpha from SDF value (negative = inside)
float sdfAlpha(float d, float aaWidth)
{
    return 1.0 - smoothstep(-aaWidth, aaWidth, d);
}

// Alias
float fill(float d, float smoothness)
{
    return sdfAlpha(d, smoothness);
}

// Stroke (outline) of thickness t
float stroke(float d, float t, float smoothness)
{
    return sdfAlpha(abs(d) - t, smoothness);
}

// Additive glow: bright near surface, falls off with distance
float glow(float d, float intensity, float falloff)
{
    d = max(d, 0.001);
    return intensity / pow(d, falloff);
}

// ── Hash & Noise ─────────────────────────────────────────────────────────────

float hash21(float2 p)
{
    p = frac(p * float2(127.1, 311.7));
    p += dot(p, p + 19.19);
    return frac(p.x * p.y);
}

float2 hash22(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)),
               dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453123);
}

float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f); // smoothstep
    float a = hash21(i);
    float b = hash21(i + float2(1,0));
    float c = hash21(i + float2(0,1));
    float d = hash21(i + float2(1,1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float gradientNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float2 ga = hash22(i)             * 2.0 - 1.0;
    float2 gb = hash22(i + float2(1,0)) * 2.0 - 1.0;
    float2 gc = hash22(i + float2(0,1)) * 2.0 - 1.0;
    float2 gd = hash22(i + float2(1,1)) * 2.0 - 1.0;
    float va = dot(ga, f);
    float vb = dot(gb, f - float2(1,0));
    float vc = dot(gc, f - float2(0,1));
    float vd = dot(gd, f - float2(1,1));
    return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y) * 0.5 + 0.5;
}

// FBM: 2–4 octaves of value noise
float fbm(float2 p, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float2 pp = p;
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        if (i >= octaves) break;
        value += valueNoise(pp) * amplitude;
        pp *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

// Voronoi: returns float2(minDist, cellID)
float2 voronoi(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float minDist = 8.0;
    float cellID  = 0.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 pt       = hash22(i + neighbor);
            float2 diff     = neighbor + pt - f;
            float  dist     = length(diff);
            if (dist < minDist)
            {
                minDist = dist;
                cellID  = hash21(i + neighbor);
            }
        }
    }
    return float2(minDist, cellID);
}

// Domain warp: distort UV by noise
float2 domainWarp(float2 p, float strength)
{
    float ox = valueNoise(p + float2(0.0, 0.0)) - 0.5;
    float oy = valueNoise(p + float2(5.2, 1.3)) - 0.5;
    return p + float2(ox, oy) * strength;
}

// ── Colour utilities ──────────────────────────────────────────────────────────

float3 hsvToRgb(float h, float s, float v)
{
    float3 rgb = clamp(abs(fmod(h * 6.0 + float3(0,4,2), 6.0) - 3.0) - 1.0, 0.0, 1.0);
    return v * lerp(float3(1,1,1), rgb, s);
}

// Smooth pulsation
float pulse(float t, float frequency, float sharpness)
{
    return pow(sin(t * frequency) * 0.5 + 0.5, sharpness);
}
