sampler2D implicitInputSampler : register(S0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 original = tex2D(implicitInputSampler, uv );
	float4 yellow = 0;
	yellow.a = 1.0;
	yellow.r = 1.0;
	yellow.g = 1.0;
		
	float4 color = original;

	color.r = min(color.r, yellow.r);
	color.g = min(color.g, yellow.g);
	color.b = min(color.b, yellow.b);

	return color;
}

technique RenderSceneWithTexture1Light
{
	pass P0
	{          
		PixelShader  = compile ps_3_0 main();
	}
}