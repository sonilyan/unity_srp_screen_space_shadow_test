using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class sonil_renderpipeline : RenderPipeline {
	private int _ScreenSpaceShadowMapId;
	private int _CameraDepthId;
	private int _ShadowResolution;
	private int _ShadowColorId;
	private int _OpaqueId;
	private int _mId;

	private RenderTargetIdentifier _ScreenSpaceShadowMapRT;
	private RenderTargetIdentifier _CameraDepthTextureRT;
	private RenderTargetIdentifier _ShadowColorRT;
	private RenderTargetIdentifier _OpaqueRT;

	CommandBuffer cmd;
	int shadowResolution = 1024;
	public sonil_renderpipeline() {
		_ScreenSpaceShadowMapId = Shader.PropertyToID("_ScreenSpaceShadowMap");
		_CameraDepthId = Shader.PropertyToID("_CameraDepth");
		_ShadowResolution = Shader.PropertyToID("_ShadowResolution");
		_ShadowColorId = Shader.PropertyToID("_ShadowColor");
		_OpaqueId = Shader.PropertyToID("_Opaque");
		_mId = Shader.PropertyToID("_m");

		cmd = new CommandBuffer();

		_ScreenSpaceShadowMapRT = new RenderTargetIdentifier(_ScreenSpaceShadowMapId);
		_CameraDepthTextureRT = new RenderTargetIdentifier(_CameraDepthId);
		_ShadowColorRT = new RenderTargetIdentifier(_ShadowColorId);
		_OpaqueRT = new RenderTargetIdentifier(_OpaqueId);
	}

	public override void Render(ScriptableRenderContext context, Camera[] cameras) {
		foreach (var camera in cameras) {
			cmd.Clear();
			cmd.GetTemporaryRT(_ScreenSpaceShadowMapId, shadowResolution, shadowResolution, 32, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
			cmd.GetTemporaryRT(_CameraDepthId, camera.pixelWidth, camera.pixelHeight, 32,FilterMode.Bilinear,RenderTextureFormat.ARGB32);
			cmd.GetTemporaryRT(_ShadowColorId,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Bilinear,RenderTextureFormat.ARGB32);
			cmd.GetTemporaryRT(_OpaqueId, camera.pixelWidth,camera.pixelHeight,32,FilterMode.Bilinear,RenderTextureFormat.ARGB32);
			context.ExecuteCommandBuffer(cmd);

			CullResults cull;
			ScriptableCullingParameters cullingParams;
			if (!CullResults.GetCullingParameters(camera, out cullingParams))
				continue;
			cullingParams.shadowDistance = Mathf.Min(1000,camera.farClipPlane);

			CullResults.Cull(camera,context,out cull);

			shadow_pass(ref cull, ref context);

			context.SetupCameraProperties(camera);

			depth_pass(camera,ref cull, ref context);

			collect_shadow(camera,ref cull, ref context);

			opaque_pass(camera,ref cull,ref context);

			transparent_pass(camera,ref cull,ref context);

			blit_pass(camera,ref cull,ref context);

			cmd.Clear();
			cmd.ReleaseTemporaryRT(_ScreenSpaceShadowMapId);
			cmd.ReleaseTemporaryRT(_CameraDepthId);
			cmd.ReleaseTemporaryRT(_ShadowColorId);
			cmd.ReleaseTemporaryRT(_OpaqueId);
			context.ExecuteCommandBuffer(cmd);

			context.Submit();
		}
	}

	private void blit_pass(Camera camera,ref CullResults cull,ref ScriptableRenderContext context) {
		cmd.Clear();
		cmd.SetGlobalTexture(_ShadowColorId, _ShadowColorRT);
		cmd.Blit(_OpaqueRT,BuiltinRenderTextureType.CameraTarget,
			new Material(Shader.Find("sonil/shadow_combine")));
		context.ExecuteCommandBuffer(cmd);
	}

	private void collect_shadow(Camera camera,ref CullResults cull,ref ScriptableRenderContext context) {
		cmd.Clear();
		cmd.name = "collect_shadow";
		cmd.SetGlobalTexture(_ScreenSpaceShadowMapId,_ScreenSpaceShadowMapRT);
		cmd.SetGlobalTexture(_CameraDepthId,_CameraDepthTextureRT);
		cmd.SetGlobalFloat(_ShadowResolution,shadowResolution);
		cmd.SetRenderTarget(_ShadowColorRT);
		cmd.ClearRenderTarget(true, true, Color.black);
		cmd.Blit(_ShadowColorRT,_ShadowColorRT,new Material(Shader.Find("sonil/shadow_color")));
		context.ExecuteCommandBuffer(cmd);
	}

	private void transparent_pass(Camera camera,ref CullResults cull,ref ScriptableRenderContext context) {
		var settings = new DrawRendererSettings(camera, new ShaderPassName("sonil_light_base")) {
			flags = DrawRendererFlags.EnableDynamicBatching | DrawRendererFlags.EnableInstancing,
			rendererConfiguration = RendererConfiguration.PerObjectLightIndices8,
		};

		settings.sorting.flags = SortFlags.CommonTransparent;

		var filter = new FilterRenderersSettings(true) {
			renderQueueRange = RenderQueueRange.transparent,
			layerMask = ~0,
		};

		context.DrawRenderers(cull.visibleRenderers, ref settings, filter);
	}

	private void opaque_pass(Camera camera,ref CullResults cull,ref ScriptableRenderContext context) {
		// setup render target and clear it
		cmd.Clear();
		cmd.name = "Clear CommandBuffer";
		cmd.SetRenderTarget(_OpaqueRT);
		cmd.ClearRenderTarget(true, true, Color.black);
		context.ExecuteCommandBuffer(cmd);
		// draw all the opaque objects using ForwardBase shader pass
		var settings = new DrawRendererSettings(camera, new ShaderPassName("sonil_light_base")) 
		{
			//flags = DrawRendererFlags.EnableDynamicBatching | DrawRendererFlags.EnableInstancing,
			//disable this for debug
			rendererConfiguration = RendererConfiguration.PerObjectLightIndices8,
		};

		settings.sorting.flags = SortFlags.CommonOpaque;

		var filter = new FilterRenderersSettings(true) {
			renderQueueRange = RenderQueueRange.opaque,
			layerMask = ~0,
		};

		context.DrawRenderers(cull.visibleRenderers, ref settings, filter);
		context.DrawSkybox(camera);
	}

	private void depth_pass(Camera camera,ref CullResults cull,ref ScriptableRenderContext context) {
		cmd.Clear();
		cmd.name = "clear depth buffer";
		cmd.SetRenderTarget(_CameraDepthTextureRT);
		cmd.ClearRenderTarget(true, true, Color.black);
		context.ExecuteCommandBuffer(cmd);

		var settings = new DrawRendererSettings(camera, new ShaderPassName("ShadowCaster")) {
			//flags = DrawRendererFlags.EnableDynamicBatching | DrawRendererFlags.EnableInstancing,
			//disable this for debug
			rendererConfiguration = RendererConfiguration.PerObjectLightIndices8,
		};

		settings.sorting.flags = SortFlags.CommonOpaque;

		var filter = new FilterRenderersSettings(true) {
			renderQueueRange = RenderQueueRange.opaque,
			layerMask = ~0,
		};


		context.DrawRenderers(cull.visibleRenderers, ref settings, filter);
	}

	private void shadow_pass(ref CullResults cull, ref ScriptableRenderContext context) {
		int shadowLightIndex = 0;
		int cascadeIdx = 0;
		int m_ShadowCasterCascadesCount = 1;
		Vector3 directionalLightCascades = new Vector3(1.0f, 0.0f, 0.0f);
		int shadowNearPlaneOffset = 0;

		Matrix4x4 view;
		Matrix4x4 proj;

		foreach (var light in cull.visibleLights) {
			if (light.lightType == LightType.Directional) {
				DrawShadowsSettings sss = new DrawShadowsSettings(cull, shadowLightIndex);

				var success = cull.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowLightIndex,
					cascadeIdx, m_ShadowCasterCascadesCount, directionalLightCascades,
					shadowResolution, shadowNearPlaneOffset, out view, out proj,
					out sss.splitData);

				//view 光源的视空间
				//proj 光源的裁剪空间
				if (success) {
					cmd.Clear();
					cmd.name = "clear shadow map";
					cmd.SetRenderTarget(_ScreenSpaceShadowMapId);
					cmd.ClearRenderTarget(true, true, Color.black);
					context.ExecuteCommandBuffer(cmd);


					cmd.Clear();
					cmd.name = "create shadow map";
					cmd.SetViewport(new Rect(0, 0, shadowResolution, shadowResolution));
					cmd.SetViewProjectionMatrices(view, proj);
					context.ExecuteCommandBuffer(cmd);

					context.DrawShadows(ref sss);
					context.ExecuteCommandBuffer(cmd);
				}

				cmd.Clear();
				if (SystemInfo.usesReversedZBuffer)
				{
					//Debug.Log("usesReversedZBuffer ");
				}
				cmd.SetGlobalMatrix(_mId, proj * view);
				context.ExecuteCommandBuffer(cmd);
			}

			shadowLightIndex++;
		}
	}
}
