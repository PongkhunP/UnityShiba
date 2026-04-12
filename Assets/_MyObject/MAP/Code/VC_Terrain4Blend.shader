Shader "Custom/VC_Terrain4Blend"
{
    Properties
    {
        _Tex0 ("Texture 0 (R)", 2D) = "white" {}
        _Tex1 ("Texture 1 (G)", 2D) = "white" {}
        _Tex2 ("Texture 2 (B)", 2D) = "white" {}
        _Tex3 ("Texture 3 (A)", 2D) = "white" {}

        _Tiling ("Tiling", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _Tex0;
        sampler2D _Tex1;
        sampler2D _Tex2;
        sampler2D _Tex3;
        float _Tiling;

        struct Input
        {
            float2 uv_Tex0;
            float4 color : COLOR;   // รับ Vertex Color
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_Tex0 * _Tiling;

            float4 c0 = tex2D(_Tex0, uv);
            float4 c1 = tex2D(_Tex1, uv);
            float4 c2 = tex2D(_Tex2, uv);
            float4 c3 = tex2D(_Tex3, uv);

            // น้ำหนักจาก Vertex Color (R,G,B,A)
            float4 w = saturate(IN.color);

            // ป้องกันกรณีไม่มีสีเลย
            float sum = w.r + w.g + w.b + w.a;
            if (sum < 0.0001) sum = 1;
            w /= sum;

            float4 col =
                c0 * w.r +
                c1 * w.g +
                c2 * w.b +
                c3 * w.a;

            o.Albedo = col.rgb;
            o.Alpha  = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
