using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class read_m : MonoBehaviour
{
	void Start ()
	{
		var cam = GetComponent<Camera>();
		var camp = cam.projectionMatrix;
		var glp = GL.GetGPUProjectionMatrix(camp, false);

		
		// [-1,1] * -1 => [1,-1] *0.5 => [0.5,-0.5]+0.5=>[1,0]
		Matrix4x4 tmp = Matrix4x4.identity;
		tmp.m22 = -0.5f;
		tmp.m23 = 0.5f;
		Debug.Log("cam.projectionMatrix\n"+camp
		          +"\ngl.projectionMatrix\n"+glp
		          +"\ntmp\n"+tmp
		          +"\n=\n"+tmp*camp
		          +"\ncamp.inv\n"+camp.inverse
		          +"\nglp.inv\n"+glp.inverse);
	}
}
