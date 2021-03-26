// Copyright 2016 Nibiru. All rights reserved.

Shader "NAR/BackMesh" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
  }
  Category {
   Tags { "Queue"="Background" }
    ZWrite Off
    Lighting Off
    Fog {Mode Off}
    SubShader {
      Pass {
	    //Cull front
        SetTexture [_MainTex] {
          Combine texture
        }
      }
    }
  }
}
