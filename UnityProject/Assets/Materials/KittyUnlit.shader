Shader "ARShield/KittyUnlit"
{
    Properties { _MainTex ("Base Color", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target { return fixed4(tex2D(_MainTex, i.uv).rgb * _Color.rgb, 1); }
            ENDCG
        }
    }
}
