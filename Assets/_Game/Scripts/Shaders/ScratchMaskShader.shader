Shader "Applegrapple/Mask"
{
    Properties
    {
        _MainTex ("Main", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "black" {}
        _Offset ("Offset", Vector) = (0, 0, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _ShadowDistance ("Shadow Distance", Range(0, 20)) = 10
        _ShadowSoftness ("Shadow Softness", Range(1, 20)) = 5
        _ShadowAngle ("Shadow Angle", Range(0, 360)) = 215
        _ShadowOffset ("Shadow Offset", Range(0, 10)) = 5
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
            uniform sampler2D _MainTex;
            uniform sampler2D _MaskTex;
            uniform float4 _MainTex_ST;
            uniform float4 _MaskTex_ST;
            uniform float4 _MaskTex_TexelSize;
            uniform float4 _Offset;
            uniform float4 _ShadowColor;
            uniform float _ShadowDistance;
            uniform float _ShadowSoftness;
            uniform float _ShadowAngle;
            uniform float _ShadowOffset;
            
            struct app2vert
            {
                float4 position: POSITION;
                half4 color: COLOR;
                float2 texcoord: TEXCOORD0;
            };
            
            struct vert2frag
            {
                float4 position: SV_POSITION;
                half4 color: COLOR;
                float2 texcoord: TEXCOORD0;
            };
            
            vert2frag vert(app2vert input)
            {
                vert2frag output;
                output.position = UnityObjectToClipPos(input.position);
                output.color = input.color;
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                return output;
            }
            
            float sampleMaskWithShadow(sampler2D maskTex, float2 uv)
            {
                return tex2D(maskTex, uv).r;
            }
            
            float4 frag(vert2frag input) : COLOR
            {
                float2 texcoord = float2(_Offset.x + input.texcoord.x * _Offset.z, 
                                       _Offset.y + input.texcoord.y * _Offset.w);
                float4 main_color = tex2D(_MainTex, texcoord);
                float mask = tex2D(_MaskTex, input.texcoord).r;
                float angleRad = radians(_ShadowAngle);
                float2 shadowDir = float2(cos(angleRad), sin(angleRad));
                float shadow = 0;
                float weightSum = 0;
                int samples = 12;
                for(int i = 0; i < samples; i++)
                {
                    float t = (float)i / (float)(samples - 1);
                    float dist = _ShadowOffset + t * _ShadowDistance;
                    float2 offset = shadowDir * dist * _MaskTex_TexelSize.xy;
                    float weight = 1.0 - (t * _ShadowSoftness / samples);
                    
                    shadow += sampleMaskWithShadow(_MaskTex, input.texcoord + offset) * weight;
                    weightSum += weight;
                }
                shadow /= weightSum;
                float shadowMask = saturate(shadow - mask);
                float4 finalColor = main_color;
                finalColor.rgb = lerp(finalColor.rgb, _ShadowColor.rgb, shadowMask * _ShadowColor.a);
                finalColor.a *= input.color.a * (1.0 - mask);
                
                return finalColor;
            }
            ENDCG
        }
    }
}
