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
            float roundedBoxSDF4(float2 centerPos, float2 size, float4 radii)
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

                float2 q = abs(centerPos) - size + radius;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

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

                // Apply Padding (crop edges)
                float paddingLeft = _Padding.x;
                float paddingRight = _Padding.y;
                float paddingTop = _Padding.z;
                float paddingBottom = _Padding.w;

                // Check if pixel is in padding area
                if (normalizedUV.x < paddingLeft ||
                    normalizedUV.x > (1.0 - paddingRight) ||
                    normalizedUV.y > (1.0 - paddingTop) ||
                    normalizedUV.y < paddingBottom)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // Remap UV to visible area for rounded corner calculation
                float visibleWidth = 1.0 - paddingLeft - paddingRight;
                float visibleHeight = 1.0 - paddingTop - paddingBottom;

                float2 paddedUV;
                paddedUV.x = (normalizedUV.x - paddingLeft) / visibleWidth;
                paddedUV.y = (normalizedUV.y - paddingBottom) / visibleHeight;

                // Convert to centered coordinates (-0.5 to 0.5)
                float2 uv = paddedUV - 0.5;

                // Calculate signed distance
                float2 size = float2(0.5, 0.5);
                float dist = roundedBoxSDF4(uv, size, _CornerRadii);

                // Anti-aliasing
                float delta = fwidth(dist);
                float alpha = 1.0 - smoothstep(-delta, delta, dist);

                // Apply rounded corner alpha
                c.a *= alpha;

                // Premultiply alpha
                c.rgb *= c.a;

                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
