// AlchemistSDF.cginc — basic SDF primitives for Mergistry shaders
// All functions work in 2D (float2 p) centered at (0,0).

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
    // Simple barycentric test via crossing product
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

// ── Boolean ops ──────────────────────────────────────────────────────────────

float opUnion(float d1, float d2) { return min(d1, d2); }
float opSubtraction(float d1, float d2) { return max(-d1, d2); }
float opIntersection(float d1, float d2) { return max(d1, d2); }

// ── Edge ─────────────────────────────────────────────────────────────────────

// Smooth alpha from SDF value (negative = inside)
float sdfAlpha(float d, float aaWidth)
{
    return 1.0 - smoothstep(-aaWidth, aaWidth, d);
}
