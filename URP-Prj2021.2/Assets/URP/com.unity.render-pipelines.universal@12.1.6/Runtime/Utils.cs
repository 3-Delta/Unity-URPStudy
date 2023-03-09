using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal {

    public class Utils {
        public static bool IsMobile() {
            return GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.SHADER_API_MOBILE);
        }

        public static bool IsSupportBitwise(GraphicsDeviceType type) {
            // GLES2 does not support bitwise operations.
            return type != GraphicsDeviceType.OpenGLES2;
        }

        public static bool IsSceneViewCamera(Camera camera) {
            return camera.cameraType == CameraType.SceneView;
        }

        public static bool IsPreviewCamera(Camera camera) {
            return camera.cameraType == CameraType.Preview;
        }

        public static bool IsGameViewCamera(Camera camera) {
            var cameraType = camera.cameraType;
            return cameraType == CameraType.Game || cameraType == CameraType.VR;
        }

        // 商向上取整
        public static int Ceil(int a, int b) {
            return (a + b - 1) / b;
        }
        
        // 大于等于a的最接近的b的倍数
        public static int CeilNearest(int a, int b) {
            return (a + b - 1) / b * b;
        }

        // 结构体size
        public static int SizeOfStruct<T>() where T : struct {
            return System.Runtime.InteropServices.Marshal.SizeOf<T>();
        }
    }
}
