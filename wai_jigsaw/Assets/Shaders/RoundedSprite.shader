Shader "Custom/RoundedSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Rounded Corners)]
        _CornerRadius ("Corner Radius (default)", Range(0, 0.5)) = 0.1
        [HideInInspector] _CornerRadii ("Corner Radii (TL, TR, BL, BR)", Vector) = (0.1, 0.1, 0.1, 0.1)

        [Header(Debug)]
        _DebugMode ("Debug Mode (0=Off, 1=RawUV, 2=NormalizedUV)", Range(0, 2)) = 0

        [Header(UV Normalization)]
        [HideInInspector] _UVRect ("UV Rect (xy=min, zw=max)", Vector) = (0, 0, 1, 1)

        [Header(Sprite Settings)]
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)

        // Stencil (for masking support)
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

            // Properties
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            float4 _Flip;
            float _CornerRadius;
            float4 _CornerRadii;  // x=TL, y=TR, z=BL, w=BR
            float4 _UVRect;  // xy = UV min, zw = UV max
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

            /// <summary>
            /// Signed Distance Function for a rounded box with 4 individual corner radii
            /// centerPosition: -0.5~0.5 range (centered)
            /// size: half size (0.5, 0.5)
            /// radii: x=TopLeft, y=TopRight, z=BottomLeft, w=BottomRight
            /// </summary>
            float roundedBoxSDF4(float2 centerPosition, float2 size, float4 radii)
            {
                // Determine which corner's radius to use based on quadrant
                // centerPosition: negative x = left, positive x = right
                //                 positive y = top, negative y = bottom
                float radius;
                if (centerPosition.x < 0.0)
                {
                    // Left side
                    radius = (centerPosition.y > 0.0) ? radii.x : radii.z; // TL or BL
                }
                else
                {
                    // Right side
                    radius = (centerPosition.y > 0.0) ? radii.y : radii.w; // TR or BR
                }

                // Standard rounded box SDF with selected radius
                float2 q = abs(centerPosition) - size + radius;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                // Sample the texture
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // Normalize UV to local 0~1 range for this sprite piece
                // _UVRect: xy = min UV, zw = max UV (in original texture space)
                float2 uvRange = _UVRect.zw - _UVRect.xy;
                float2 normalizedUV = (IN.texcoord - _UVRect.xy) / uvRange;

                // Debug mode 1: show raw UV as color
                // 각 조각마다 다른 색상 = UV가 원본 텍스처 기준
                if (_DebugMode > 0.5 && _DebugMode < 1.5)
                {
                    return fixed4(IN.texcoord.x, IN.texcoord.y, 0, 1);
                }

                // Debug mode 2: show normalized UV as color
                // 모든 조각이 동일한 그라데이션 = 정규화 성공
                // (좌하단=검정, 우상단=노랑)
                if (_DebugMode > 1.5)
                {
                    return fixed4(normalizedUV.x, normalizedUV.y, 0, 1);
                }

                // Convert normalized UV from 0~1 to -0.5~0.5 (centered)
                float2 uv = normalizedUV - 0.5;

                // Size is 0.5 (half of the 0~1 UV space)
                float2 size = float2(0.5, 0.5);

                // Calculate the signed distance with 4 individual corner radii
                float dist = roundedBoxSDF4(uv, size, _CornerRadii);

                // Apply anti-aliasing using fwidth for smooth edges
                float delta = fwidth(dist);
                float alpha = 1.0 - smoothstep(-delta, delta, dist);

                // Apply the rounded corner alpha
                c.a *= alpha;

                // Premultiply alpha for correct blending
                c.rgb *= c.a;

                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
