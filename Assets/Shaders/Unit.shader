// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Unit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _EmitColor ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        //LOD 0

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float4 opjectPos;
        };


        void vert (inout appdata_full v, out Input data) 
        {
            data.opjectPos = v.vertex;

            //data.opjectPos = mul (unity_ObjectToWorld, v.vertex);
            //data.opjectPos = mul(unity_WorldToObject, data.opjectPos);
		}


        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _EmitColor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float t = step(sin(IN.opjectPos.y), 0.5);
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;
            o.Albedo = lerp(0, c.rgb, t);
            // Metallic and smoothness come from slider variables
            o.Metallic = lerp(0, _Metallic, t);
            o.Smoothness = lerp(0, _Glossiness, t);
            o.Alpha = c.a;
            o.Emission = lerp(_EmitColor, 0, t);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
