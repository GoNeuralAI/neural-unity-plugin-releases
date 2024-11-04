void grid_float
(
    float2 _UV, // UV coordinates
    float _GridSize, // grid size
    float _LineWidth, // line width
    out float _Alpha // output fragment alpha
)
{
    float2 uv = (_UV * 1.001) / _GridSize;
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    float2 uvDeriv = float2(length(float2(dx.x, dy.x)), length(float2(dx.y, dy.y)));
    float2 grid = abs(frac(uv)) / uvDeriv;
    float2 ll = abs(grid - 0.5) - 0.5 * _LineWidth / uvDeriv;
    float2 alpha = saturate(min(grid, ll));
    _Alpha = saturate(1.0 - min(alpha.x, alpha.y));
}
