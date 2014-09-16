Shader "IslandShader"
{
	Properties 
	{
_Ramp("_Ramp", 2D) = "black" {}
_ZeroPos("_ZeroPos", Vector) = (0,0,0,0)
_CliffColor("_CliffColor", Color) = (1,1,1,1)
_Scale("_Scale", Float) = 1

	}
	
	SubShader 
	{
		Tags
		{
"Queue"="Geometry"
"IgnoreProjector"="False"
"RenderType"="Opaque"

		}

		
Cull Back
ZWrite On
ZTest LEqual
ColorMask RGBA
LOD 100
Fog{
}


		CGPROGRAM
#pragma surface surf BlinnPhongEditor  vertex:vert
#pragma target 2.0


sampler2D _Ramp;
float4 _ZeroPos;
float4 _CliffColor;
float _Scale;

			struct EditorSurfaceOutput {
				half3 Albedo;
				half3 Normal;
				half3 Emission;
				half3 Gloss;
				half Specular;
				half Alpha;
				half4 Custom;
			};
			
			inline half4 LightingBlinnPhongEditor_PrePass (EditorSurfaceOutput s, half4 light)
			{
half3 spec = light.a * s.Gloss;
half4 c;
c.rgb = (s.Albedo * light.rgb + light.rgb * spec);
c.a = s.Alpha;
return c;

			}

			inline half4 LightingBlinnPhongEditor (EditorSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
			{
				half3 h = normalize (lightDir + viewDir);
				
				half diff = max (0, dot ( lightDir, s.Normal ));
				
				float nh = max (0, dot (s.Normal, h));
				float spec = pow (nh, s.Specular*128.0);
				
				half4 res;
				res.rgb = _LightColor0.rgb * diff;
				res.w = spec * Luminance (_LightColor0.rgb);
				res *= atten * 2.0;

				return LightingBlinnPhongEditor_PrePass( s, res );
			}
			
			struct Input {
				float3 sWorldNormal;
float3 worldPos;
float4 fullMeshUV2;

			};

			void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
float4 VertexOutputMaster0_0_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_1_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_2_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_3_NoInput = float4(0,0,0,0);

o.sWorldNormal = mul((float3x3)_Object2World, SCALED_NORMAL);
o.fullMeshUV2 = v.texcoord1;

			}
			

			void surf (Input IN, inout EditorSurfaceOutput o) {
				o.Normal = float3(0.0,0.0,1.0);
				o.Alpha = 1.0;
				o.Albedo = 0.0;
				o.Emission = 0.0;
				o.Gloss = 0.0;
				o.Specular = 0.0;
				o.Custom = 0.0;
				
float4 Swizzle1=float4(float4( IN.sWorldNormal.x, IN.sWorldNormal.y,IN.sWorldNormal.z,1.0 ).y, float4( IN.sWorldNormal.x, IN.sWorldNormal.y,IN.sWorldNormal.z,1.0 ).y, float4( IN.sWorldNormal.x, IN.sWorldNormal.y,IN.sWorldNormal.z,1.0 ).y, float4( IN.sWorldNormal.x, IN.sWorldNormal.y,IN.sWorldNormal.z,1.0 ).y);
float4 Subtract1=float4( 1.0, 1.0, 1.0, 1.0 ) - Swizzle1;
float4 Multiply1=Subtract1 * _CliffColor;
float4 Divide0=float4( 1.0, 1.0, 1.0, 1.0 ) / _Scale.xxxx;
float4 Multiply3=float4( 10,10,10,10 ) * Divide0;
float4 Subtract0=float4( IN.worldPos.x, IN.worldPos.y,IN.worldPos.z,1.0 ) - _ZeroPos;
float4 Swizzle0=float4(Subtract0.y, Subtract0.y, Subtract0.y, Subtract0.y);
float4 Multiply0=Multiply3 * Swizzle0;
float4 Swizzle3=float4((IN.fullMeshUV2).x, (IN.fullMeshUV2).x, (IN.fullMeshUV2).x, (IN.fullMeshUV2).x);
float4 Multiply4=float4( 0.05,0.05,0.05,0.05 ) * Swizzle3;
float4 Add1=Multiply0 + Multiply4;
float4 Swizzle2=float4((IN.fullMeshUV2).y, (IN.fullMeshUV2).y, (IN.fullMeshUV2).y, (IN.fullMeshUV2).y);
float4 Assemble0_2_NoInput = float4(0,0,0,0);
float4 Assemble0_3_NoInput = float4(0,0,0,0);
float4 Assemble0=float4(Add1.x, Swizzle2.y, Assemble0_2_NoInput.z, Assemble0_3_NoInput.w);
float4 Tex2D0=tex2D(_Ramp,Assemble0.xy);
float4 Multiply2=Swizzle1 * Tex2D0;
float4 Add0=Multiply1 + Multiply2;
float4 Master0_1_NoInput = float4(0,0,1,1);
float4 Master0_2_NoInput = float4(0,0,0,0);
float4 Master0_3_NoInput = float4(0,0,0,0);
float4 Master0_4_NoInput = float4(0,0,0,0);
float4 Master0_5_NoInput = float4(1,1,1,1);
float4 Master0_7_NoInput = float4(0,0,0,0);
float4 Master0_6_NoInput = float4(1,1,1,1);
o.Albedo = Add0;

				o.Normal = normalize(o.Normal);
			}
		ENDCG
	}
	Fallback "Diffuse"
}