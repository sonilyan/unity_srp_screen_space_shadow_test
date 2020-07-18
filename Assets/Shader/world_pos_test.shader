Shader "sonil/world_pos_test"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags { "LightMode" = "sonil_light_base" }
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			fixed4 world_color(float3 wpos)
			{
				if(wpos.y > 0) {
					if(wpos.x > 0)
						return fixed4(1,0,0,0);
					return fixed4(0,1,0,0);
				}
				return fixed4(0,0,1,0);
			}
			
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = mul(UNITY_MATRIX_P,mul(UNITY_MATRIX_MV,v.vertex));//use this for framedebug
				o.uv = o.uv / o.uv.w;
				//o.uv = o.pos; //pos和uv的插值方式不一样
				//o.uv = o.uv / o.uv.w;
				//o.uv.y = -o.uv.y;
				//o.uv.z = (1 - o.uv.z) * 2 - 1;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			    i.pos = i.uv;
			    //i.pos = i.pos / i.pos.w;//归一化，经过投影矩阵后w都不为1；
			    i.pos.z = (1 - i.pos.z) * 2 - 1 ;//[1,0]=>[0,1] =>[0,2]=>[-1,1]
			    i.pos.y = -i.pos.y;
			    float4 tmp = mul(unity_CameraInvProjection,i.pos);//unity_CameraInvProjection为open gl的矩阵定义
			    tmp = tmp / tmp.w;
			    tmp.z = -tmp.z;//左右手坐标系转换
			    float4 tmp2 = mul(unity_CameraToWorld,tmp);
				return world_color(tmp2.xyz/tmp2.w);
			}
			ENDCG
		}
	}
	
	Fallback "sonil/base"
}

