Shader "Hidden/GaussianBlur" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurRadius ("Blur Radius", Range(0, 50)) = 10
    }
    
    CGINCLUDE
    #include "UnityCG.cginc"
    
    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    float _BlurRadius;
    
    struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };
    
    struct v2f {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };
    
    v2f vert (appdata v) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        
        // DX/OpenGL UV修正
        #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0)
                o.uv.y = 1.0 - o.uv.y;
        #endif
        return o;
    }
    
    float GaussianWeight(float x, float sigma) {
        const float SQRT_2PI = 2.50662827463;
        return (1.0 / (sigma * SQRT_2PI)) * exp(-(x*x)/(2.0*sigma*sigma));
    }
    
    float4 GaussianBlurPass(float2 uv, float2 direction) {
        float sigma = max(0.01, _BlurRadius * 0.35 + 0.18);
        float weightSum = 0;
        float4 color = 0;
        float radius = ceil(_BlurRadius);
        
        for(float i = -radius; i <= radius; i++) {
            float weight = GaussianWeight(i, sigma);
            float2 offset = direction * i * _MainTex_TexelSize.xy;
            
            // 精确边缘处理
            float2 sampleUV = clamp(uv + offset, float2(0.001, 0.001), float2(0.999, 0.999));
            color += tex2D(_MainTex, sampleUV) * weight;
            weightSum += weight;
        }
        
        return color / weightSum;
    }
    
    float4 frag_horizontal (v2f i) : SV_Target {
        return GaussianBlurPass(i.uv, float2(1, 0));
    }
    
    float4 frag_vertical (v2f i) : SV_Target {
        return GaussianBlurPass(i.uv, float2(0, 1));
    }
    ENDCG
    
    SubShader {
        Cull Off ZWrite Off ZTest Always
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_horizontal
            ENDCG
        }
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_vertical
            ENDCG
        }
    }
}