Shader "Custom/RoundedSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Rounded Corners)]
        _CornerRadius ("Corner Radius (default)", Range(0, 0.5)) = 0.1
        [HideInInspector] _CornerRadii ("Corner Radii (TL, TR, BL, BR)", Vector) = (0.1, 0.1, 0.1, 0.1)

        [Header(Padding)]
        [HideInInspector] _Padding ("Padding (Left, Right, Top, Bottom)", Vector) = (0, 0, 0, 0)

        [Header(Debug)]
        _DebugMode ("Debug Mode (0=Off, 1=RawUV, 2=NormalizedUV)", Range(0, 2)) = 0

        [Header(UV Normalization)]
        [HideInInspector] _UVRect ("UV Rect (xy=min, zw=max)", Vector) = (0, 0, 1, 1)

        [Header(Sprite Settings)]
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            float4 _Flip;
            float _CornerRadius;
            float4 _CornerRadii;
            float4 _Padding;
            float4 _UVRect;
            float _DebugMode;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            // Signed Distance Function for rounded box with 4 corner radii
            // Returns distance and selected radius
            float roundedBoxSDF4(float2 centerPos, float2 size, float4 radii, out float selectedRadius)
            {
                float radius;
                if (centerPos.x < 0.0)
                {
                    radius = (centerPos.y > 0.0) ? radii.x : radii.z;
                }
                else
                {
                    radius = (centerPos.y > 0.0) ? radii.y : radii.w;
                }

                selectedRadius = radius;

                float2 q = abs(centerPos) - size + radius;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                // Normalize UV to local 0-1 range for this sprite piece
                float2 uvRange = _UVRect.zw - _UVRect.xy;
                float2 normalizedUV = (IN.texcoord - _UVRect.xy) / uvRange;

                // Debug mode 1: show raw UV
                if (_DebugMode > 0.5 && _DebugMode < 1.5)
                {
                    return fixed4(IN.texcoord.x, IN.texcoord.y, 0, 1);
                }

                // Debug mode 2: show normalized UV
                if (_DebugMode > 1.5)
                {
                    return fixed4(normalizedUV.x, normalizedUV.y, 0, 1);
                }

                // Apply Padding (crop edges or extend image)
                // 양수 패딩: 이미지 자르기 (간격 표현)
                // 음수 패딩: 이미지 확장 (겹침 표현, 경계선 제거용) - 가장자리 픽셀 반복
                float paddingLeft = _Padding.x;
                float paddingRight = _Padding.y;
                float paddingTop = _Padding.z;
                float paddingBottom = _Padding.w;

                // 양수 패딩일 때만 자르기 (음수면 확장이므로 자르지 않음)
                if ((paddingLeft > 0 && normalizedUV.x < paddingLeft) ||
                    (paddingRight > 0 && normalizedUV.x > (1.0 - paddingRight)) ||
                    (paddingTop > 0 && normalizedUV.y > (1.0 - paddingTop)) ||
                    (paddingBottom > 0 && normalizedUV.y < paddingBottom))
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 텍스처 샘플링
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // 양수 패딩만 적용 (음수 패딩은 이미지 확장으로 처리됨)
                float effectiveLeft = max(paddingLeft, 0.0);
                float effectiveRight = max(paddingRight, 0.0);
                float effectiveTop = max(paddingTop, 0.0);
                float effectiveBottom = max(paddingBottom, 0.0);

                float visibleWidth = 1.0 - effectiveLeft - effectiveRight;
                float visibleHeight = 1.0 - effectiveTop - effectiveBottom;

                float2 paddedUV;
                paddedUV.x = (normalizedUV.x - effectiveLeft) / visibleWidth;
                paddedUV.y = (normalizedUV.y - effectiveBottom) / visibleHeight;

                // Convert to centered coordinates (-0.5 to 0.5)
                float2 uv = paddedUV - 0.5;

                // Check if all corner radii are zero (no rounding needed)
                float maxRadius = max(max(_CornerRadii.x, _CornerRadii.y), max(_CornerRadii.z, _CornerRadii.w));

                if (maxRadius > 0.001)
                {
                    // Calculate signed distance with selected radius
                    float2 size = float2(0.5, 0.5);
                    float selectedRadius;
                    float dist = roundedBoxSDF4(uv, size, _CornerRadii, selectedRadius);

                    // Only apply rounding/clipping if this corner has radius > 0
                    // 반경이 0인 모서리는 클리핑하지 않음 (정사각형 유지)
                    if (selectedRadius > 0.001)
                    {
                        float delta = fwidth(dist);
                        float alpha = 1.0 - smoothstep(-delta, delta, dist);
                        c.a *= alpha;
                    }
                    // else: 반경 0인 모서리는 클리핑 없이 그대로 표시 (직선 가장자리)
                }
                // If all radii are 0, skip rounding entirely (full square)

                // Premultiply alpha
                c.rgb *= c.a;

                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
