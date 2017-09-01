Shader "Lines/Colored Blended" { 
                SubShader { Pass {  
                    Blend SrcAlpha OneMinusSrcAlpha  
                    ZWrite Off Cull Off Fog { Mode Off } 
                    BindChannels { 
                      Bind "Vertex", vertex Bind "Color", color } 
                } } }