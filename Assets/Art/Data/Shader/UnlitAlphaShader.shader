Shader  "Custom/UnlitAlpha"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans. (Alpha)", 2D) = "white" { }
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf NoLighting alpha:fade novertexlights nometa noforwardadd
		#include "UnityCG.cginc"
		struct Input
		{
			float2 uv_MainTex;
		};

		half4 _Color;
		sampler2D _MainTex;

		half4 LightingNoLighting(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
		{
			return half4(s.Albedo, s.Alpha);
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			half4 tex = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = tex.rgb * _Color.rgb;
			o.Alpha = _Color.a * tex.a;
		}

		ENDCG
	}
}