Shader "Custom/RoundedFrame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Frame Settings)]
        _FrameThickness ("Frame Thickness (UV ratio)", Range(0, 0.5)) = 0.05

        [Header(Directional Hide)]
        [HideInInspector] _HideDirections ("Hide Directions (Top, Bottom, Left, Right)", Vector) = (0, 0, 0, 0)

        [Header(Rounded Corners)]
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.1
        [HideInInspector] _CornerRadii ("Corner Radii (TL, TR, BL, BR)", Vector) = (0.1, 0.1, 0.1, 0.1)

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
            float _FrameThickness;
            float4 _HideDirections;  // (Top, Bottom, Left, Right) - 1이면 해당 방향 숨김

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

            // Signed Distance Function for rounded box
            float roundedBoxSDF(float2 centerPos, float2 size, float4 radii)
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
                // UV를 중심 기준 좌표로 변환 (-0.5 ~ 0.5)
                float2 uv = IN.texcoord - 0.5;

                // 방향별 숨김 체크 (Top, Bottom, Left, Right)
                // _HideDirections: x=Top, y=Bottom, z=Left, w=Right
                float hideTop = _HideDirections.x;
                float hideBottom = _HideDirections.y;
                float hideLeft = _HideDirections.z;
                float hideRight = _HideDirections.w;

                // 현재 픽셀이 어느 영역(edge/corner)에 있는지 확인
                // 프레임 영역인지 확인
                float inTopEdge = (uv.y > (0.5 - _FrameThickness)) ? 1.0 : 0.0;
                float inBottomEdge = (uv.y < (-0.5 + _FrameThickness)) ? 1.0 : 0.0;
                float inLeftEdge = (uv.x < (-0.5 + _FrameThickness)) ? 1.0 : 0.0;
                float inRightEdge = (uv.x > (0.5 - _FrameThickness)) ? 1.0 : 0.0;

                // 모서리 영역 판정 (두 변이 겹치는 영역)
                float inTopLeft = inTopEdge * inLeftEdge;
                float inTopRight = inTopEdge * inRightEdge;
                float inBottomLeft = inBottomEdge * inLeftEdge;
                float inBottomRight = inBottomEdge * inRightEdge;

                // 순수 변 영역 판정 (모서리 제외)
                float isPureTop = inTopEdge * (1.0 - inLeftEdge) * (1.0 - inRightEdge);
                float isPureBottom = inBottomEdge * (1.0 - inLeftEdge) * (1.0 - inRightEdge);
                float isPureLeft = inLeftEdge * (1.0 - inTopEdge) * (1.0 - inBottomEdge);
                float isPureRight = inRightEdge * (1.0 - inTopEdge) * (1.0 - inBottomEdge);

                // 숨김 판정:
                // - 순수 변 영역: 해당 방향만 숨김이면 숨김
                // - 모서리 영역: 인접한 두 방향 모두 숨김이면 숨김
                float hideByPureEdge = (isPureTop * hideTop) +
                                       (isPureBottom * hideBottom) +
                                       (isPureLeft * hideLeft) +
                                       (isPureRight * hideRight);

                float hideByCorner = (inTopLeft * hideTop * hideLeft) +
                                     (inTopRight * hideTop * hideRight) +
                                     (inBottomLeft * hideBottom * hideLeft) +
                                     (inBottomRight * hideBottom * hideRight);

                float directionHide = hideByPureEdge + hideByCorner;
                if (directionHide > 0.5)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 외곽 SDF (전체 프레임 경계)
                float2 outerSize = float2(0.5, 0.5);
                float outerDist = roundedBoxSDF(uv, outerSize, _CornerRadii);

                // 내부 SDF (투명하게 할 영역)
                float2 innerSize = float2(0.5 - _FrameThickness, 0.5 - _FrameThickness);

                // 내부 모서리 반경도 조정 (프레임 두께만큼 줄임)
                float4 innerRadii = max(_CornerRadii - _FrameThickness, 0.0);
                float innerDist = roundedBoxSDF(uv, innerSize, innerRadii);

                // 안티앨리어싱
                float outerDelta = fwidth(outerDist);
                float innerDelta = fwidth(innerDist);

                // 외곽 알파 (바깥은 투명)
                float outerAlpha = 1.0 - smoothstep(-outerDelta, outerDelta, outerDist);

                // 내부 알파 (안쪽도 투명)
                float innerAlpha = smoothstep(-innerDelta, innerDelta, innerDist);

                // 프레임 알파 = 외곽 안쪽 AND 내부 바깥쪽
                float frameAlpha = outerAlpha * innerAlpha;

                // 최종 색상
                fixed4 c = IN.color;
                c.a *= frameAlpha;

                // Premultiply alpha
                c.rgb *= c.a;

                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
