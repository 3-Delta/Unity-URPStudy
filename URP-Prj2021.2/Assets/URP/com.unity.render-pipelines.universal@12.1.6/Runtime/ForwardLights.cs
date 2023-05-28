using Unity.Collections;
using UnityEngine.PlayerLoop;
using Unity.Jobs;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

namespace UnityEngine.Rendering.SelfUniversal.Internal
{
    /// <summary>
    /// Computes and submits lighting data to the GPU.
    /// </summary>
    public class ForwardLights
    {
        const string k_SetupLightConstants = "Setup Light Constants";
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_SetupLightConstants);
        MixedLightingSetup m_MixedLightingSetup;
        
        static partial class LightConstantBuffer
        {
            public static int _MainLightPositionPID;   // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightColorPID;      // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightOcclusionProbesChannelPID;    // Deferred?
            public static int _MainLightLayerMaskPID;
        }

        static partial class LightConstantBuffer {
            public static int _AdditionalLightsCountPID;
            
            // 结构体方式
            public static int  _AdditionalLightsBufferPID; // Shader.PropertyToID("_AdditionalLightsBuffer");
            public static int  _AdditionalLightsIndicesPID; // Shader.PropertyToID("_AdditionalLightsIndices");
            
            // 正常方式
            public static int _AdditionalLightsPositionPID;
            public static int _AdditionalLightsColorPID;
            public static int _AdditionalLightsAttenuationPID;
            public static int _AdditionalLightsSpotDirPID;
            public static int _AdditionalLightOcclusionProbeChannelPID;
            public static int _AdditionalLightsLayerMasksPID;
        }
        
        int m_DirectionalLightCount;
        
        Vector4[] m_AdditionalLightPositions;
        Vector4[] m_AdditionalLightColors;
        Vector4[] m_AdditionalLightAttenuations;
        Vector4[] m_AdditionalLightSpotDirections;
        Vector4[] m_AdditionalLightOcclusionProbeChannels;
        float[] m_AdditionalLightsLayerMasks;  // Unity has no support for binding uint arrays. We will use asuint() in the shader instead.

        // 其实就是是否可以使用ComputeBuffer传递数据给GPU，因为普通的同时通过cmd.SetGlobalVectorArray传递，比较低效
        // 可以传递自定义结构体
        bool m_UseStructuredBuffer;
        // custer渲染，tile渲染
        bool m_UseClusteredRendering;
        
        int m_ActualTileWidth;
        int2 m_TileResolution;
        int m_RequestedTileWidth;
        float m_ZBinFactor;
        int m_ZBinOffset;

        JobHandle m_CullingHandle;
        
        NativeArray<ZBin> m_ZBinsNA;
        NativeArray<uint> m_TileLightMasksNA;

        ComputeBuffer m_ZBinCompBuffer; // float4
        ComputeBuffer m_TileCompBuffer; // float4

        private LightCookieManager m_LightCookieManager;

        internal struct InitParams
        {
            public LightCookieManager lightCookieManager;
            public bool clusteredRendering;
            public int tileSize;

            static internal InitParams GetDefault()
            {
                InitParams p;
                {
                    var settings = LightCookieManager.Settings.GetDefault();
                    var asset = UniversalRenderPipeline.asset;
                    if (asset)
                    {
                        settings.atlas.format = asset.additionalLightsCookieFormat;
                        settings.atlas.resolution = asset.additionalLightsCookieResolution;
                    }

                    p.lightCookieManager = new LightCookieManager(ref settings);
                    p.clusteredRendering = false;
                    p.tileSize = 32;
                }
                return p;
            }
        }

        public ForwardLights() : this(InitParams.GetDefault()) { }

        internal ForwardLights(InitParams initParams)
        {
            if (initParams.clusteredRendering) {
                Assert.IsTrue(math.ispow2(initParams.tileSize));
            }
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;
            m_UseClusteredRendering = initParams.clusteredRendering;

            LightConstantBuffer._MainLightPositionPID = Shader.PropertyToID("_MainLightPosition");
            LightConstantBuffer._MainLightColorPID = Shader.PropertyToID("_MainLightColor");
            LightConstantBuffer._MainLightOcclusionProbesChannelPID = Shader.PropertyToID("_MainLightOcclusionProbes");
            LightConstantBuffer._MainLightLayerMaskPID = Shader.PropertyToID("_MainLightLayerMask");
            
            LightConstantBuffer._AdditionalLightsCountPID = Shader.PropertyToID("_AdditionalLightsCount");

            if (m_UseStructuredBuffer)
            {
                LightConstantBuffer._AdditionalLightsBufferPID = Shader.PropertyToID("_AdditionalLightsBuffer");
                LightConstantBuffer._AdditionalLightsIndicesPID = Shader.PropertyToID("_AdditionalLightsIndices");
            }
            else
            {
                LightConstantBuffer._AdditionalLightsPositionPID = Shader.PropertyToID("_AdditionalLightsPosition");
                LightConstantBuffer._AdditionalLightsColorPID = Shader.PropertyToID("_AdditionalLightsColor");
                LightConstantBuffer._AdditionalLightsAttenuationPID = Shader.PropertyToID("_AdditionalLightsAttenuation");
                LightConstantBuffer._AdditionalLightsSpotDirPID = Shader.PropertyToID("_AdditionalLightsSpotDir");
                LightConstantBuffer._AdditionalLightOcclusionProbeChannelPID = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");
                LightConstantBuffer._AdditionalLightsLayerMasksPID = Shader.PropertyToID("_AdditionalLightsLayerMasks");

                int maxLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
                m_AdditionalLightPositions = new Vector4[maxLights];
                m_AdditionalLightColors = new Vector4[maxLights];
                m_AdditionalLightAttenuations = new Vector4[maxLights];
                m_AdditionalLightSpotDirections = new Vector4[maxLights];
                m_AdditionalLightOcclusionProbeChannels = new Vector4[maxLights];
                m_AdditionalLightsLayerMasks = new float[maxLights];
            }

            m_LightCookieManager = initParams.lightCookieManager;

            if (m_UseClusteredRendering)
            {
                this.m_ZBinCompBuffer = new ComputeBuffer(UniversalRenderPipeline.maxZBins / 4, UnsafeUtility.SizeOf<float4>(), ComputeBufferType.Constant, ComputeBufferMode.Dynamic);
                this.m_TileCompBuffer = new ComputeBuffer(UniversalRenderPipeline.maxTileVec4s, UnsafeUtility.SizeOf<float4>(), ComputeBufferType.Constant, ComputeBufferMode.Dynamic);
                m_RequestedTileWidth = initParams.tileSize;
            }
        }

        // 先于Setup执行
        internal void ProcessLights(ref RenderingData renderingData)
        {
            if (m_UseClusteredRendering) {
                var camera = renderingData.cameraData.camera;
                var screenResolution = math.int2(renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);

                // https://zhuanlan.zhihu.com/p/489839605
                // CullResults的VisibleLights 是根据光源距离相机的远近进行排序的
                var lightCount = renderingData.lightData.visibleLights.Length;
                var lightOffset = 0;
                while (lightOffset < lightCount && renderingData.lightData.visibleLights[lightOffset].lightType == LightType.Directional) {
                    // 计算平行光的数量
                    lightOffset++;
                }

                // todo 这段代码有问题吧？通过查看urp14的源码，发现的确存在问题
                if (lightOffset == lightCount) {
                    lightOffset = 0;
                }
                
                lightCount -= lightOffset;
                
                // 非mainLight的平行光数量
                this.m_DirectionalLightCount = lightOffset;
                if (renderingData.lightData.mainLightIndex != -1) {
                    this.m_DirectionalLightCount -= 1;
                }
                
                // 从这里可以得出：visibleLights是按照平行光在前，其他光源在后的方式排序的
                // cullResult的非平行光
                var visibleUnDirLights = renderingData.lightData.visibleLights.GetSubArray(lightOffset, lightCount);
                var lightsPerTile = UniversalRenderPipeline.lightsPerTile;
                var wordsPerTile = lightsPerTile / 32;

                // m_RequestedTileWidth是配置的tileSize
                m_ActualTileWidth = m_RequestedTileWidth >> 1;
                do
                {
                    m_ActualTileWidth = m_ActualTileWidth << 1;
                    // 商 向上取整
                    m_TileResolution = (screenResolution + m_ActualTileWidth - 1) / m_ActualTileWidth;
                }
                while ((m_TileResolution.x * m_TileResolution.y * wordsPerTile) > (UniversalRenderPipeline.maxTileVec4s * 4));
                
                // URP-Prj2021.2\Assets\URP\com.unity.render-pipelines.universal@12.1.6\Runtime\Tangent-Fov.png
                var fovHalfHeight = math.tan(math.radians(camera.fieldOfView * 0.5f));
                // TODO: Make this work with VR
                var aspect = (float)screenResolution.x / (float)screenResolution.y;
                // 将近裁剪面距离当做1，那么此时halfHeight的一半就是fovHalfHeight
                var fovHalfWidth = fovHalfHeight * aspect;

                /*
                    相机视锥体的Z范围是由相机的近裁剪面和远裁剪面决定的，它们之间的差值可以用简单的减法运算来计算。
                    但是，这个差值并不一定是最优的分割间隔，因为相机视锥体的深度分布是非线性的，物体在Z方向上的分布也可能是非线性的。
                    因此，将Z范围均分成若干层，并不一定能够得到最优的渲染效果。为了更好地适应这种非线性的分布，这里采用平方根间隔来划分Z范围。
                    平方根间隔能够更好地适应深度分布的非线性性，以及物体在Z方向上的非均匀分布。而math.sqrt函数正是用来计算平方根的，
                    因此在这段代码中进行了math.sqrt计算。
                */
                var unLinearZDiff = math.sqrt(camera.farClipPlane) - math.sqrt(camera.nearClipPlane);
                // 每个z单元分几块
                var maxZFactor = (float)UniversalRenderPipeline.maxZBins / unLinearZDiff;
                m_ZBinFactor = maxZFactor;
                m_ZBinOffset = (int)(math.sqrt(camera.nearClipPlane) * m_ZBinFactor);
                
                // 视锥体区间 z分块(4096)
                var binCount = (int)(math.sqrt(camera.farClipPlane) * m_ZBinFactor) - m_ZBinOffset;
                // Must be a multiple of 4 to be able to alias to vec4
                binCount = ((binCount + 3) / 4) * 4;
                binCount = math.min(UniversalRenderPipeline.maxZBins, binCount);
                
// Debug.LogError("screenResolution:" + screenResolution + "  m_ActualTileWidth:" + m_ActualTileWidth + "  m_TileResolution:" + m_TileResolution + "  m_ZBinFactor:" + m_ZBinFactor + "  m_ZBinOffset:" + m_ZBinOffset + "  binCount:" + binCount);
                
                this.m_ZBinsNA = new NativeArray<ZBin>(binCount, Allocator.TempJob);
                Assert.AreEqual(UnsafeUtility.SizeOf<uint>(), UnsafeUtility.SizeOf<ZBin>());

                // 此时lightCount是非平行光数量
                
                using var minMaxZsNA = new NativeArray<LightMinMaxZ>(lightCount, Allocator.TempJob);
                // We allocate double array length because the sorting algorithm needs swap space to work in.
                using var midZsNA = new NativeArray<float>(lightCount * 2, Allocator.TempJob);

                Matrix4x4 worldToViewMatrix = renderingData.cameraData.GetViewMatrix();
                var minMaxZJob = new MinMaxZJob
                {
                    worldToViewMatrix = worldToViewMatrix,
                    lights = visibleUnDirLights,
                    minMaxZs = minMaxZsNA,
                    midZs = midZsNA
                };
                // Innerloop batch count of 32 is not special, just a handwavy amount to not have too much scheduling overhead nor too little parallelism.
                var minMaxZJobHandle = minMaxZJob.ScheduleParallel(lightCount, 32, new JobHandle());

                // Allocator.TempJob存在4帧
                // We allocate double array length because the sorting algorithm needs swap space to work in.
                using var indicesNA = new NativeArray<int>(lightCount * 2, Allocator.TempJob);
                // 基数排序
                var zSortJob = new RadixSortJob
                {
                    // Floats can be sorted bitwise with no special handling if positive floats only
                    // keys在基数排序之后无用了
                    keys = midZsNA.Reinterpret<uint>(),
                    indices = indicesNA
                };
                var zSortJobHandle = zSortJob.Schedule(minMaxZJobHandle);

                #region 按照z重新排序
                var reorderedLightsNA = new NativeArray<VisibleLight>(lightCount, Allocator.TempJob);
                var reorderedMinMaxZsNA = new NativeArray<LightMinMaxZ>(lightCount, Allocator.TempJob);

                var reorderLightsJob = new ReorderJob<VisibleLight> {
                    indices = indicesNA, input = visibleUnDirLights, output = reorderedLightsNA
                };
                var reorderLightsJobHandle = reorderLightsJob.ScheduleParallel(lightCount, 32, zSortJobHandle);

                var reorderMinMaxZsJob = new ReorderJob<LightMinMaxZ> {
                    indices = indicesNA, input = minMaxZsNA, output = reorderedMinMaxZsNA
                };
                var reorderMinMaxZsJobHandle = reorderMinMaxZsJob.ScheduleParallel(lightCount, 32, zSortJobHandle);

                var reorderHandle = JobHandle.CombineDependencies(
                    reorderLightsJobHandle,
                    reorderMinMaxZsJobHandle
                );

                // JobHandle.ScheduleBatchedJobs：当你想要你的job开始执行时，可以调用这个函数flush调度的batch。不flush batch会导致调度延迟到主线程等待batch执行结果时才触发执行
                JobHandle.ScheduleBatchedJobs();
                #endregion

                LightExtractionJob lightExtractionJob;
                lightExtractionJob.orderedLights = reorderedLightsNA;
                var lightTypesNA = lightExtractionJob.lightTypesNA = new NativeArray<LightType>(lightCount, Allocator.TempJob);
                var radiusesNA = lightExtractionJob.radiusesNA = new NativeArray<float>(lightCount, Allocator.TempJob);
                var directionsNA = lightExtractionJob.directionsWSNA = new NativeArray<float3>(lightCount, Allocator.TempJob);
                var positionsNA = lightExtractionJob.positionsWSNA = new NativeArray<float3>(lightCount, Allocator.TempJob);
                var coneRadiusesNA = lightExtractionJob.coneRadiusesNA = new NativeArray<float>(lightCount, Allocator.TempJob);
                
                var lightExtractionJobHandle = lightExtractionJob.ScheduleParallel(lightCount, 32, reorderHandle);

                var zBinningJob = new ZBinningJob
                {
                    bins = this.m_ZBinsNA,
                    minMaxZs = reorderedMinMaxZsNA,
                    binOffset = m_ZBinOffset,
                    zFactor = m_ZBinFactor
                };
                var zBinningJobHandle = zBinningJob.ScheduleParallel((binCount + ZBinningJob.batchCount - 1) / ZBinningJob.batchCount, 1, reorderHandle);
                reorderedMinMaxZsNA.Dispose(zBinningJobHandle);

                // Must be a multiple of 4 to be able to alias to vec4
                var lightMasksLength = (((wordsPerTile) * m_TileResolution + 3) / 4) * 4;
                var horizontalLightMasksNA = new NativeArray<uint>(lightMasksLength.y, Allocator.TempJob);
                var verticalLightMasksNA = new NativeArray<uint>(lightMasksLength.x, Allocator.TempJob);

                // Vertical slices along the x-axis
                var verticalJob = new SliceCullingJob
                {
                    scale = (float)m_ActualTileWidth / (float)screenResolution.x,
                    viewOrigin = camera.transform.position,
                    viewForward = camera.transform.forward,
                    viewRight = camera.transform.right * fovHalfWidth,
                    viewUp = camera.transform.up * fovHalfHeight,
                    lightTypes = lightTypesNA,
                    radiuses = radiusesNA,
                    directions = directionsNA,
                    positions = positionsNA,
                    coneRadiuses = coneRadiusesNA,
                    lightsPerTile = lightsPerTile,
                    sliceLightMasks = verticalLightMasksNA
                };
                
                var verticalJobHandle = verticalJob.ScheduleParallel(m_TileResolution.x, 1, lightExtractionJobHandle);

                // Horizontal slices along the y-axis
                var horizontalJob = verticalJob;
                horizontalJob.scale = (float)m_ActualTileWidth / (float)screenResolution.y;
                horizontalJob.viewRight = camera.transform.up * fovHalfHeight;
                horizontalJob.viewUp = -camera.transform.right * fovHalfWidth;
                horizontalJob.sliceLightMasks = horizontalLightMasksNA;
                
                var horizontalJobHandle = horizontalJob.ScheduleParallel(m_TileResolution.y, 1, lightExtractionJobHandle);

                var slicesHandle = JobHandle.CombineDependencies(horizontalJobHandle, verticalJobHandle);

                this.m_TileLightMasksNA = new NativeArray<uint>(((m_TileResolution.x * m_TileResolution.y * (wordsPerTile) + 3) / 4) * 4, Allocator.TempJob);
                var sliceCombineJob = new SliceCombineJob
                {
                    tileResolution = m_TileResolution,
                    wordsPerTile = wordsPerTile,
                    sliceLightMasksH = horizontalLightMasksNA,
                    sliceLightMasksV = verticalLightMasksNA,
                    lightMasks = this.m_TileLightMasksNA
                };
                var sliceCombineHandle = sliceCombineJob.ScheduleParallel(m_TileResolution.y, 1, slicesHandle);

                m_CullingHandle = JobHandle.CombineDependencies(sliceCombineHandle, zBinningJobHandle);

                reorderHandle.Complete();
                NativeArray<VisibleLight>.Copy(reorderedLightsNA, 0, renderingData.lightData.visibleLights, lightOffset, lightCount);

                var tempBias = new NativeArray<Vector4>(lightCount, Allocator.Temp);
                var tempResolution = new NativeArray<int>(lightCount, Allocator.Temp);
                var tempIndices = new NativeArray<int>(lightCount, Allocator.Temp);

                for (var i = 0; i < lightCount; i++)
                {
                    tempBias[indicesNA[i]] = renderingData.shadowData.bias[lightOffset + i];
                    tempResolution[indicesNA[i]] = renderingData.shadowData.resolution[lightOffset + i];
                    tempIndices[indicesNA[i]] = lightOffset + i;
                }

                for (var i = 0; i < lightCount; i++)
                {
                    renderingData.shadowData.bias[i + lightOffset] = tempBias[i];
                    renderingData.shadowData.resolution[i + lightOffset] = tempResolution[i];
                    renderingData.lightData.originalIndices[i + lightOffset] = tempIndices[i];
                }

                tempBias.Dispose();
                tempResolution.Dispose();
                tempIndices.Dispose();

                lightTypesNA.Dispose(m_CullingHandle);
                radiusesNA.Dispose(m_CullingHandle);
                directionsNA.Dispose(m_CullingHandle);
                positionsNA.Dispose(m_CullingHandle);
                coneRadiusesNA.Dispose(m_CullingHandle);
                reorderedLightsNA.Dispose(m_CullingHandle);
                horizontalLightMasksNA.Dispose(m_CullingHandle);
                verticalLightMasksNA.Dispose(m_CullingHandle);
                JobHandle.ScheduleBatchedJobs();
            }
        }

        public void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            /*
                lightData.supportsAdditionalLights = settings.additionalLightsRenderingMode != LightRenderingMode.Disabled;
                lightData.shadeAdditionalLightsPerVertex = settings.additionalLightsRenderingMode == LightRenderingMode.PerVertex;
                lightData.visibleLights = visibleLights;
                lightData.supportsMixedLighting = settings.supportsMixedLighting;
                lightData.reflectionProbeBlending = settings.reflectionProbeBlending;
                lightData.reflectionProbeBoxProjection = settings.reflectionProbeBoxProjection;
                lightData.supportsLightLayers = RenderingUtils.SupportsLightLayers(SystemInfo.graphicsDeviceType) && settings.supportsLightLayers;
            */
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            // 附加光源是否是逐顶点光照, 逐顶点光照在这里影响shader
            bool additionalLightsPerVertex = renderingData.lightData.shadeAdditionalLightsPerVertex;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(null, m_ProfilingSampler))
            {
                var useClusteredRendering = m_UseClusteredRendering;
                if (useClusteredRendering)
                {
                    // 确保光源裁切完成，主要是填充m_ZBinsNA和m_TileLightMasksNA
                    // 然后下面传递给GPU
                    m_CullingHandle.Complete();

                    this.m_ZBinCompBuffer.SetData(this.m_ZBinsNA.Reinterpret<float4>(UnsafeUtility.SizeOf<ZBin>()), 0, 0, this.m_ZBinsNA.Length / 4);
                    this.m_TileCompBuffer.SetData(this.m_TileLightMasksNA.Reinterpret<float4>(UnsafeUtility.SizeOf<uint>()), 0, 0, this.m_TileLightMasksNA.Length / 4);

                    cmd.SetGlobalInteger("_AdditionalLightsDirectionalCount", this.m_DirectionalLightCount);
                    cmd.SetGlobalInteger("_AdditionalLightsZBinOffset", m_ZBinOffset);
                    cmd.SetGlobalFloat("_AdditionalLightsZBinScale", m_ZBinFactor);
                    cmd.SetGlobalVector("_AdditionalLightsTileScale", renderingData.cameraData.pixelRect.size / (float)m_ActualTileWidth);
                    cmd.SetGlobalInteger("_AdditionalLightsTileCountX", m_TileResolution.x);

                    cmd.SetGlobalConstantBuffer(this.m_ZBinCompBuffer, "AdditionalLightsZBins", 0, this.m_ZBinsNA.Length * 4);
                    cmd.SetGlobalConstantBuffer(this.m_TileCompBuffer, "AdditionalLightsTiles", 0, this.m_TileLightMasksNA.Length * 4);

                    this.m_ZBinsNA.Dispose();
                    this.m_TileLightMasksNA.Dispose();
                }

                SetupShaderLightConstants(cmd, ref renderingData);

                bool lightCountCheck = (renderingData.cameraData.renderer.stripAdditionalLightOffVariants && renderingData.lightData.supportsAdditionalLights) || additionalLightsCount > 0;
                // 附加光源 顶点光照
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex, lightCountCheck && additionalLightsPerVertex && !useClusteredRendering);
                // 附加光源 像素光照
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel, lightCountCheck && !additionalLightsPerVertex && !useClusteredRendering);
                // tile渲染
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ClusteredRendering, useClusteredRendering);

                bool isShadowMask = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.ShadowMask;
                bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
                bool isSubtractive = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightmapShadowMixing, isSubtractive || isShadowMaskAlways);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ShadowsShadowMask, isShadowMask);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, isSubtractive); // Backward compatibility

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ReflectionProbeBlending, renderingData.lightData.reflectionProbeBlending);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ReflectionProbeBoxProjection, renderingData.lightData.reflectionProbeBoxProjection);

                bool lightLayers = renderingData.lightData.supportsLightLayers;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightLayers, lightLayers);

                m_LightCookieManager.Setup(context, cmd, ref renderingData.lightData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        internal void Cleanup()
        {
            if (m_UseClusteredRendering)
            {
                this.m_ZBinCompBuffer.Dispose();
                this.m_TileCompBuffer.Dispose();
            }
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel, out uint lightLayerMask)
        {
            UniversalRenderPipeline.InitializeLightConstants_Common(lights, lightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionProbeChannel);
            lightLayerMask = 0;

            // When no orderedLights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            Light light = lightData.light;

            if (light == null)
                return;

            if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                lightData.light.shadows != LightShadows.None &&
                m_MixedLightingSetup == MixedLightingSetup.None)
            {
                switch (light.bakingOutput.mixedLightingMode)
                {
                    case MixedLightingMode.Subtractive:
                        m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                        break;
                    case MixedLightingMode.Shadowmask:
                        m_MixedLightingSetup = MixedLightingSetup.ShadowMask;
                        break;
                }
            }

            var additionalLightData = light.GetUniversalAdditionalLightData();
            lightLayerMask = (uint)additionalLightData.lightLayerMask;
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Universal pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref renderingData.lightData);
            SetupAdditionalLightConstants(cmd, ref renderingData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
            uint lightLayerMask;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel, out lightLayerMask);

            cmd.SetGlobalVector(LightConstantBuffer._MainLightPositionPID, lightPos);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightColorPID, lightColor);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightOcclusionProbesChannelPID, lightOcclusionChannel);
            cmd.SetGlobalInt(LightConstantBuffer._MainLightLayerMaskPID, (int)lightLayerMask);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref LightData lightData = ref renderingData.lightData;
            var cullResults = renderingData.cullResults;
            var lights = lightData.visibleLights;
            int maxAdditionalLightsCount = UniversalRenderPipeline.maxVisibleAdditionalLights;
            int additionalLightsCount = SetupPerObjectLightIndices(cullResults, ref lightData);
            if (additionalLightsCount > 0)
            {
                if (m_UseStructuredBuffer)
                {
                    NativeArray<ShaderInput.LightData> additionalLightsData = new NativeArray<ShaderInput.LightData>(additionalLightsCount, Allocator.Temp);
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        VisibleLight light = lights[i];
                        if (lightData.mainLightIndex != i)
                        {
                            ShaderInput.LightData data;
                            InitializeLightConstants(lights, i,
                                out data.position, out data.color, out data.attenuation,
                                out data.spotDirection, out data.occlusionProbeChannels,
                                out data.layerMask);
                            additionalLightsData[lightIter] = data;
                            lightIter++;
                        }
                    }

                    var lightDataBuffer = ShaderData.instance.GetLightDataBuffer(additionalLightsCount);
                    lightDataBuffer.SetData(additionalLightsData);

                    int lightIndices = cullResults.lightAndReflectionProbeIndexCount;
                    var lightIndicesBuffer = ShaderData.instance.GetLightIndicesBuffer(lightIndices);

                    cmd.SetGlobalBuffer(LightConstantBuffer._AdditionalLightsBufferPID, lightDataBuffer);
                    cmd.SetGlobalBuffer(LightConstantBuffer._AdditionalLightsIndicesPID, lightIndicesBuffer);

                    additionalLightsData.Dispose();
                }
                else
                {
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        VisibleLight light = lights[i];
                        if (lightData.mainLightIndex != i)
                        {
                            uint lightLayerMask;
                            InitializeLightConstants(lights, i, out m_AdditionalLightPositions[lightIter],
                                out m_AdditionalLightColors[lightIter],
                                out m_AdditionalLightAttenuations[lightIter],
                                out m_AdditionalLightSpotDirections[lightIter],
                                out m_AdditionalLightOcclusionProbeChannels[lightIter],
                                out lightLayerMask);
                            m_AdditionalLightsLayerMasks[lightIter] = Unity.Mathematics.math.asfloat(lightLayerMask);
                            lightIter++;
                        }
                    }

                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsPositionPID, m_AdditionalLightPositions);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsColorPID, m_AdditionalLightColors);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsAttenuationPID, m_AdditionalLightAttenuations);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsSpotDirPID, m_AdditionalLightSpotDirections);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightOcclusionProbeChannelPID, m_AdditionalLightOcclusionProbeChannels);
                    cmd.SetGlobalFloatArray(LightConstantBuffer._AdditionalLightsLayerMasksPID, m_AdditionalLightsLayerMasks);
                }

                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCountPID, new Vector4(lightData.maxPerObjectAdditionalLightsCount,
                    0.0f, 0.0f, 0.0f));
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCountPID, Vector4.zero);
            }
        }

        int SetupPerObjectLightIndices(CullingResults cullResults, ref LightData lightData)
        {
            if (lightData.additionalLightsCount == 0)
                return lightData.additionalLightsCount;

            var visibleLights = lightData.visibleLights;
            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
            int globalDirectionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional orderedLights from the perobject light indices
            // Pipeline handles main light globally and there's no support for additional directional orderedLights atm.
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (additionalLightsCount >= UniversalRenderPipeline.maxVisibleAdditionalLights)
                    break;

                VisibleLight light = visibleLights[i];
                if (i == lightData.mainLightIndex)
                {
                    perObjectLightIndexMap[i] = -1;
                    ++globalDirectionalLightsCount;
                }
                else
                {
                    perObjectLightIndexMap[i] -= globalDirectionalLightsCount;
                    ++additionalLightsCount;
                }
            }

            // Disable all remaining orderedLights we cannot fit into the global light buffer.
            for (int i = globalDirectionalLightsCount + additionalLightsCount; i < perObjectLightIndexMap.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            cullResults.SetLightIndexMap(perObjectLightIndexMap);

            if (m_UseStructuredBuffer && additionalLightsCount > 0)
            {
                int lightAndReflectionProbeIndices = cullResults.lightAndReflectionProbeIndexCount;
                Assertions.Assert.IsTrue(lightAndReflectionProbeIndices > 0, "Pipelines configures additional orderedLights but per-object light and probe indices count is zero.");
                cullResults.FillLightAndReflectionProbeIndices(ShaderData.instance.GetLightIndicesBuffer(lightAndReflectionProbeIndices));
            }

            perObjectLightIndexMap.Dispose();
            return additionalLightsCount;
        }
    }
}
