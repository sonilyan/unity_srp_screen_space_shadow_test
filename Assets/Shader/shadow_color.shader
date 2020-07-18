Shader "sonil/shadow_color"
{
	SubShader
	{
		Pass
		{
			Tags { "RenderPipeline" = "shadow_color" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
			};

			sampler2D _ScreenSpaceShadowMap;
			sampler2D _CameraDepth;
			float _ShadowResolution;
			float4x4 _m;
			
			float4 world_color(float3 wpos)
			{
				if(wpos.y > 0) {
					if(wpos.x > 0)
						return float4(1,0,0,0);
					return float4(0,1,0,0);
				}
				return float4(0,0,1,0);
			}

			float pcf_color(float z,float2 xy)
			{
				float4 tmp = tex2D(_ScreenSpaceShadowMap,xy);
				float depth = DecodeFloatRGBA(tmp);
								
				if(z > depth-0.005)
					return 0;
				return 0.5;
			}

			//use tex2Dproj( u, v, z, p ) get 2x2PCF+BF
			float4 cal_wpos(float3 wpos)
			{
				float4 spos = mul(_m,float4(wpos.xyz,1));

				float3 ndc = spos.xyz / spos.w;
				ndc = (ndc + 1) / 2;//[-1,1]=>[0,1]
				ndc.z = 1-ndc.z;//[0,1]=>[1,0]

				if(ndc.x > 1 || ndc.x < 0 || ndc.y > 1 || ndc.y < 0)
					return fixed4(0,0,0,0);

				float offset = 1 / _ShadowResolution;
				float color = pcf_color(ndc.z, ndc.xy);
				//return color;

				color += pcf_color(ndc.z, float2(ndc.x + offset, ndc.y));
				color += pcf_color(ndc.z, float2(ndc.x, ndc.y + offset));
				color += pcf_color(ndc.z, float2(ndc.x - offset, ndc.y));
				color += pcf_color(ndc.z, float2(ndc.x, ndc.y - offset));
				color += pcf_color(ndc.z, float2(ndc.x + offset, ndc.y - offset));
				color += pcf_color(ndc.z, float2(ndc.x - offset, ndc.y + offset));
				color += pcf_color(ndc.z, float2(ndc.x + offset, ndc.y + offset));
				color += pcf_color(ndc.z, float2(ndc.x - offset, ndc.y - offset));
				color /= 9;

				return color;
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv.xy = v.uv;
				o.uv.zw = o.pos.xy / o.pos.w;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			    //i.pos = i.pos / i.pos.w;
			    //return i.pos.y;//i.pos的值和vert里的不一样，说明插值方式不一样
			
				//return (i.uv.z +  1)/2;   pos.xy /pos.w => [-1,1]
				//return (i.uv.w +  1)/2;   pos.xy /pos.w => [-1,1]
				float4 tmp = tex2D(_CameraDepth,i.uv.xy);
				//return tmp; //uv的值是正确的
				if(tmp.z == 0)
				{
					return fixed4(0,0,0,0);
				}
				
				float depth = DecodeFloatRGBA(tmp);
				
				//if(depth == 0.25) return fixed4(0,1,0,0);
				//if(depth == 0.5) return fixed4(0,0,1,0);
	
				depth = 1 - depth;//[1,0]=>[0,1]

				float4 cpos = float4(i.uv.zw, depth * 2 - 1, 1.0);//[0,1]*2-1=>[-1,1]
				cpos.y = -cpos.y;

				float4 vpos = mul(unity_CameraInvProjection, cpos);//转到camera view
				vpos = vpos / vpos.w;
				vpos.z = -vpos.z;//camera view为右手坐标系

				float4 wpos = mul(unity_CameraToWorld,vpos);//转到world

				//return world_color(wpos.xyz / wpos.w);
				return cal_wpos(wpos.xyz);
			}
			ENDCG
		}
	}
}

