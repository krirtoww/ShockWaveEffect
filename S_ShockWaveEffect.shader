Shader "Custom/Shockwave"
{
    Properties
    {
        _CaptureTex ("Capture Texture", 2D) = "white" {}
        _CullingTex ("Culling Texture", 2D) = "white" {}
        _UpdateTime ("Update Time", Range(0, 1)) = 0
        _Width ("Ring Width", Range(0.001, 1)) = 0.1
        _DistortionStrength ("Distortion Strength", Range(0,1)) = 0.05
        _LightColor ("Shockwave Light Color", Color) = (1,1,1,1)
        _Smooth ("Smooth Ring",float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _CaptureTex;
            sampler2D _CullingTex;
            float _UpdateTime;
            float _Smooth;
            float _Width;
            float _DistortionStrength;
            float4 _LightColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv      : TEXCOORD0;
                float4 vertex  : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 获得和Sprite中心的距离
                float2 centerUV = (uv - 0.5) * 2.0;
                float radius = length(centerUV);

                // 圆环外圈半径和内圈半径
                float outer = _UpdateTime;
                float inner = max(0.0, outer - _Width*(1-_UpdateTime));
                // 其实_UpdateTime就是外圈半径，因为脚本里是控制这个参数来控制特效进度，所以命名为这个。
                // *（1 - _UpdateTime)是为了让特效接近结束时圆环的宽度逐渐变为0，而不是动画结束直接消失，比较突兀。


                // 圆环内外圈的中心位置
                float halfWidth = (outer - inner) * 0.5;
                float ringCenter = (outer + inner) * 0.5;

                // 归一化半径差，[-1, 1] 区间
                // 计算和圆环内外圈中心的距离进行平滑过渡
                float norm = abs(radius - ringCenter) / halfWidth;

                // 最终权重：当 norm=0 时为1，norm=1时为0，中间平滑过渡
                // 软边（平滑过渡）
                float soft = smoothstep(1.0, 0.0, norm);

                // 硬边（全 1 或 0）
                float hard = step(norm, 1.0);

                // 根据_Smooth混合
                float ringWeight = lerp(hard, soft, _Smooth);

                // 计算冲击波扭曲的UV偏移方向
                float2 dir = centerUV == 0 ? float2(0, 0) : normalize(centerUV);

                // 根据强度等参数计算uv偏移量
                float2 offsetUV = uv + dir * (ringWeight * _DistortionStrength);

                // 用偏移后的uv采样
                fixed4 captureCol = tex2D(_CaptureTex, offsetUV);

                // 采样不需要应用冲击波的层的图像。
                fixed4 cullCol = tex2D(_CullingTex, uv);
                float cullAlpha = cullCol.a;
                // 如果有culling层的图像则显示正常图像，没有则是扭曲后的图像
                fixed4 baseCol = lerp(captureCol, cullCol, cullAlpha);

                // 叠加光晕颜色
                fixed4 lightColor = _LightColor * (_LightColor.a * _DistortionStrength * ringWeight);
                fixed4 finalCol = baseCol + lightColor;

                // 使得冲击波圆环以外的区域的不透明度为0
                // 不加这个的话其他冲击波会被Sprite阻挡
                finalCol.a *= ringWeight ;
                return finalCol;
            }
            ENDCG
        }
    }
}
