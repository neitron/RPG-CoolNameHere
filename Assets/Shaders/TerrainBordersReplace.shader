

Shader "Hidden/TerrainBordersReplace"
{
    Properties
    {
    }
    SubShader
    {
        Tags 
        { 
            "TerrainBorders"="True" 
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            #include "HexMetrix.cginc"
            #include "HexCellData.cginc"

            //struct appdata
            //{
            //    float4 vertex : POSITION;
            //};

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float borderedCell : TEXCOORD0;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                float4 cell0 = GetCellData(v, 0);
			    float4 cell1 = GetCellData(v, 1);
			    float4 cell2 = GetCellData(v, 2);

                cell0.y *= 255;
                cell1.y *= 255;
                cell2.y *= 255;

                float v0 = (int)cell0.y & 15;
                float v1 = (int)cell1.y & 15;
                float v2 = (int)cell2.y & 15;

                float isCellOwnersAreSame = v0 == v1 && v1 == v2;

                v0 = step(1, v0);
                v1 = step(1, v1);
                v2 = step(1, v2);
            
                float isHasOwner = (v0 * v.color.x + v1 * v.color.y + v2 * v.color.z);
                isCellOwnersAreSame = isCellOwnersAreSame * isHasOwner;
                o.borderedCell = isCellOwnersAreSame;

                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                return i.borderedCell;
            }
            ENDCG  
		}
    }
}
