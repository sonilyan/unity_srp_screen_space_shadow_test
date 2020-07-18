using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class sonil_renderpipeline_asset : RenderPipelineAsset {
#if UNITY_EDITOR
	[UnityEditor.MenuItem("sonil/create sonil render pipeline asset")]
	static void CreateRPA() {
		var tmp = ScriptableObject.CreateInstance<sonil_renderpipeline_asset>();
		UnityEditor.AssetDatabase.CreateAsset(tmp, "Assets/sonil_renderpipeline.asset");
	}
#endif
	protected override IRenderPipeline InternalCreatePipeline() {
		return new sonil_renderpipeline();
	}
}
