Shader "NumTalk/Checkpoint Gate"
{
    Properties { _Tint("Tint", Color) = (0.25,1,0.8,1) _Activation("Activation", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float _Activation;
            CBUFFER_END
            Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o; }
            half4 frag(Varyings i):SV_Target
            {
                float2 p=(i.uv-0.5)*2;
                float r=length(p);
                float radius=0.72+_Activation*0.22;
                float ring=1-smoothstep(0.015,0.065,abs(r-radius));
                float ripple=pow(saturate(1-abs(r-frac(_Time.y*0.55))*12),2)*0.23;
                float angle=atan2(p.y,p.x);
                float dashes=0.45+0.55*pow(saturate(sin(angle*6+_Time.y*2)),3);
                float pulse=0.7+0.3*sin(_Time.y*3);
                float alpha=(ring*dashes*pulse+ripple)*(1-smoothstep(0.85,1,r))*(1-_Activation);
                return half4(_Tint.rgb*(1+_Activation*2),alpha*_Tint.a);
            }
            ENDHLSL
        }
    }
}
