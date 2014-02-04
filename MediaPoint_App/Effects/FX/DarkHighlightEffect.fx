float strenght: register(C0);
float BlurAmount : register(C1);
sampler2D implicitInputSampler : register(S0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 c = 0;    
    
	for(int i=0; i<15; i++)
    {
        float scale = 1.0 + BlurAmount * (i / 14.0);
        c += tex2D(implicitInputSampler, uv * scale );
    }
   
    c /= 15;

   float4 color = c;
   if (strenght == 0.0) {
	color.rgb = 0;
   }
   else
   {
	color.rgb /= strenght;
   }
   return color;
}

technique RenderSceneWithTexture1Light
{
    pass P0
    {          
        PixelShader  = compile ps_3_0 main();
    }
}