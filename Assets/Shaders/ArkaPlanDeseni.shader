// Arka plandaki hareketli desen icin kucuk bir shader.
// Kenney'in siyah-beyaz desenini aliyor, parlak kisimlarini secilen renkte
// yari saydam gosteriyor. Isiktan etkilenmiyor.
Shader "TapStack/ArkaPlanDeseni"
{
    Properties
    {
        _MainTex ("Desen", 2D) = "white" {}
        _Color ("Renk", Color) = (1, 1, 1, 0.1)
    }

    SubShader
    {
        // Gradyan karesi opak ve normal siraya ciziliyor; desenin onun ustune
        // gelmesi icin saydam siraya koyuyoruz.
        Tags { "Queue" = "Transparent-100" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 desen = tex2D(_MainTex, i.uv);
                return fixed4(_Color.rgb, desen.r * _Color.a);
            }
            ENDCG
        }
    }
}
