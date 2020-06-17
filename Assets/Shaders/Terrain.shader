Shader "Custom/Terrain"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "white" {}
        _GridTex ("Grid Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specular ("Specular", Color) = (0.2, 0.2, 0.2)
		_BackgroundColor ("Background Color", Color) = (0,0,0)
        [Toggle(SHOW_MAP_DATA)] _ShowMapData ("Show Map Data", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows vertex:vert
        #pragma target 3.5
        
        #pragma multi_compile _ GRID_ON
        #pragma multi_compile _ HEX_MAP_EDIT_MODE
        #pragma shader_feature SHOW_MAP_DATA

        #include "HexMetrix.cginc"
        #include "HexCellData.cginc"

        UNITY_DECLARE_TEX2DARRAY(_MainTex);

        struct Input
        {
			float4 color : COLOR;
            float3 worldPos;
            float3 terrain;
            float4 visibility;
            float4 owner;
            float ownerHue;
            #if defined(SHOW_MAP_DATA)
                float mapData;
            #endif
        };



        float3 HSVToRGB( float3 c )
		{
			float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
			float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
			return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
		}


        void vert (inout appdata_full v, out Input data) 
        {
			UNITY_INITIALIZE_OUTPUT(Input, data);
			
            float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
			float4 cell2 = GetCellData(v, 2);

            cell0.y *= 255;
            cell1.y *= 255;
            cell2.y *= 255;

			data.terrain.x = cell0.w;
			data.terrain.y = cell1.w;
			data.terrain.z = cell2.w;

            data.visibility.x = cell0.x;
			data.visibility.y = cell1.x;
			data.visibility.z = cell2.x;
            data.visibility.xyz = lerp(0.25, 1, data.visibility);

            float v0 = ((int)cell0.y >> 4 & 15) / 15.0f;
            float v1 = ((int)cell1.y >> 4 & 15) / 15.0f;
            float v2 = ((int)cell2.y >> 4 & 15) / 15.0f;

            data.visibility.w = v0 * v.color.x + v1 * v.color.y + v2 * v.color.z;
            
            v0 = (int)cell0.y & 15;
            v1 = (int)cell1.y & 15;
            v2 = (int)cell2.y & 15;

            float3 color0 = HSVToRGB( float3( v0 / 15.0f, 1.0f, 1.0f) ); 
            float3 color1 = HSVToRGB( float3( v1 / 15.0f, 1.0f, 1.0f) ); 
            float3 color2 = HSVToRGB( float3( v2 / 15.0f, 1.0f, 1.0f) ); 

            data.ownerHue = floor(v0 * v.color.x + v1 * v.color.y + v2 * v.color.z);
            data.owner.rgb = 
                (color0 * v.color.x + color1 * v.color.y + color2 * v.color.z);
			
            v0 = step(1, v0);
            v1 = step(1, v1);
            v2 = step(1, v2);
            
            data.owner.w = (v0 * v.color.x + v1 * v.color.y + v2 * v.color.z);
            data.owner.rgb *= data.owner.w;
            #if defined(SHOW_MAP_DATA)
                data.mapData = cell0.z * v.color.x + cell1.z * v.color.y + cell2.z * v.color.z;
            #endif
		}

        sampler2D _GridTex;
        half _Glossiness;
        fixed4 _Color;
        fixed3 _Specular;
        half3 _BackgroundColor;


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)


        float4 GetTerrainColor (Input IN, int index) 
        {
			float3 uvw = float3(IN.worldPos.xz * (2 * TILING_SCALE), IN.terrain[index]);
			float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);
			return c * (IN.color[index] * IN.visibility[index]);
		}


        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
			fixed4 c =
				GetTerrainColor(IN, 0) +
				GetTerrainColor(IN, 1) +
				GetTerrainColor(IN, 2);
			
            fixed4 grid = 1;
            #if defined(GRID_ON)
                float2 gridUV = IN.worldPos.xz;
			    gridUV.x *= 1 / (4 * 8.66025404);
			    gridUV.y *= 1 / (2 * 15.0);
                grid = tex2D(_GridTex, gridUV);
            #endif

            float explored = IN.visibility.w;
            float ownerBorder = IN.owner.w * 2;
            ownerBorder = smoothstep(0.8, 0.85, ownerBorder);

            float cellGrid = abs(IN.color.x - 0.5f) * abs(IN.color.y - 0.5f) * abs(IN.color.z - 0.5f);
            cellGrid = smoothstep(0.01f, 0.03f, cellGrid);
            
            float i;
            float j = modf(IN.ownerHue, i);
			o.Albedo = lerp(c.rgb * grid * _Color * explored, IN.owner.rgb, saturate((1 - abs(j - 0.5) * 2) * (1 - cellGrid) * ownerBorder * 10000));
            #if defined(SHOW_MAP_DATA)
                o.Albedo = IN.mapData * grid;
            #endif

            o.Smoothness = _Glossiness;
            o.Specular = _Specular * explored;
            o.Occlusion = explored;
            o.Emission = _BackgroundColor * (1 - explored);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
