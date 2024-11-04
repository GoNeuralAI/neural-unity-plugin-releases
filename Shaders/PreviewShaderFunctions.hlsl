// Fresnel-Schlick approximation
float3 fresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

float2 longLatToUv(float3 longLat)
{
    return float2((1 + atan2(longLat.x, -longLat.y) / 3.14159265) / 2, acos(longLat.z) / 3.14159265);
}

void ibl_float(
    float3 _Albedo, // albedo color
    float _Roughness, // roughness value
    float _Metallic, // metallic value
    float3 _Normal, // normal vector
    float _AmbientOcclusion, // ambient occlusion value
    float3 _CameraPosition, // camera world position
    float3 _WorldPosition, // world position of the shaded point
    float3 _WorldNormal, // world normal vector of the shaded point
    float3 _WorldTangent, // world tangent vector of the shaded point
    float3 _WorldBitangent, // world bitangent vector of the shaded point
    UnityTexture2D _CubemapDiffuse, // diffuse irradiance cubemap
    UnityTextureCube _CubemapSpecular, // specular radiance cubemap
    UnityTexture2D _BRDFLUT, // BRDF LUT texture
    out float3 _FragmentColor // output fragment color
)
{
    // Calculate the view vector
    float3 V = normalize(_CameraPosition - _WorldPosition);

    // Calculate TBN matrix
    float3x3 TBN = float3x3(normalize(_WorldTangent), normalize(_WorldBitangent), normalize(_WorldNormal));

    // Convert normal from [0, 1] to [-1, 1] range
    float3 N = 2.0 * _Normal - 1.0;

    // Transform normal from tangent space to world space
    N = normalize(mul(TBN, _Normal));

    // Calculate reflection vector
    float3 R = reflect(-V, N);

    // Linearize the roughness
    _Roughness *= _Roughness;
    
    // Calculate diffuse and specular colors
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), _Albedo, _Metallic);

    // Calculate reflectance at normal incidence using the Fresnel equation
    float3 kS = lerp(fresnelSchlick(max(dot(N, V), 0.0), F0), float3(0.0, 0.0, 0.0), _Roughness);
    float3 kD = (float3(1.0, 1.0, 1.0) - kS) * (1.0 - _Metallic);

    // Sample diffuse irradiance
    float2 uv = longLatToUv(_Normal);
    float3 irradiance = SAMPLE_TEXTURE2D_LOD(_CubemapDiffuse, sampler_CubemapDiffuse, uv, 8).rgb;
    
    // Calculate the diffuse color
    float3 diffuse = kD * irradiance * _Albedo;

    // Sample specular radiance
    float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(_CubemapSpecular, sampler_CubemapSpecular, R, _Roughness).rgb;

    // Sample the BRDF LUT texture
    float2 brdf = SAMPLE_TEXTURE2D(_BRDFLUT, sampler_BRDFLUT, float2(max(dot(N, V), 0.0), _Roughness)).rg;

    // Calculate the specular color
    float3 specular = prefilteredColor * (kS * brdf.x + brdf.y) * (1.0 - _Roughness);

    // Combine diffuse and specular contributions and apply ambient occlusion
    _FragmentColor = (diffuse + specular) * _AmbientOcclusion;
}
