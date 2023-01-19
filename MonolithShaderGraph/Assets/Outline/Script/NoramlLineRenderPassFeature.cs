using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NoramlLineRenderPassFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting {
        public LayerMask layer;
        public Material normalTexMat;
        public Material normalLineMat;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPrePasses;
        [Range(0,1)]
        public float Edge = 0;
    }

    public Setting setting = new Setting();

    public class DrawNormalTexPass : ScriptableRenderPass
    {

        private Setting setting;
        ShaderTagId shaderTag = new ShaderTagId("DepthOnly");
        FilteringSettings filter;
        NoramlLineRenderPassFeature feature;

        public DrawNormalTexPass(Setting setting, NoramlLineRenderPassFeature feature) { 
            this.setting = setting;
            this.feature = feature;

            RenderQueueRange queue = new RenderQueueRange();
            queue.lowerBound = 1000;
            queue.upperBound = 3500;
            filter = new FilteringSettings(queue,setting.layer);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
            int temp = Shader.PropertyToID("_NormalTex");
            RenderTextureDescriptor desc = cameraTextureDescriptor;
            cmd.GetTemporaryRT(temp, desc);
            ConfigureTarget(temp);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("����NormalTex");
            var draw = CreateDrawingSettings(shaderTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            draw.overrideMaterial = setting.normalTexMat;
            draw.overrideMaterialPassIndex = 0;
            context.DrawRenderers(renderingData.cullResults, ref draw, ref filter);
            CommandBufferPool.Release(cmd);
        }
    }


    public class DrawNoramlLinePass : ScriptableRenderPass
    {
        private Setting setting;
        NoramlLineRenderPassFeature feature;

        public DrawNoramlLinePass(Setting setting, NoramlLineRenderPassFeature feature) {
            this.setting = setting;
            this.feature = feature;
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(Shader.PropertyToID("_NormalLineTex"));
            cmd.ReleaseTemporaryRT(Shader.PropertyToID("_NormalTex"));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("���Ʒ������");
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            setting.normalLineMat.SetFloat("_Edge",setting.Edge);
            int normalLineID = Shader.PropertyToID("_NormalLineTex");
            cmd.GetTemporaryRT(normalLineID, desc);
            cmd.Blit(normalLineID, normalLineID, setting.normalLineMat, 0);
            cmd.Blit(normalLineID, normalLineID);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private DrawNormalTexPass _DrawNormalTexPass;
    private DrawNoramlLinePass _DrawNoramlLinePass;


    public override void Create()
    {
        _DrawNormalTexPass = new DrawNormalTexPass(setting, this);
        _DrawNormalTexPass.renderPassEvent = setting.passEvent;
        _DrawNoramlLinePass = new DrawNoramlLinePass(setting, this);
        _DrawNoramlLinePass.renderPassEvent = setting.passEvent;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_DrawNormalTexPass);
        renderer.EnqueuePass(_DrawNoramlLinePass);
    }
}


