#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

int OutlineWidth;
float4 OutlineColor;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 OutlinePS(VertexShaderOutput input) : COLOR
{
	float4 pixelColor = tex2D(SpriteTextureSampler, input.TextureCoordinates);
	
	if(pixelColor.a > 0.3f)
		return float4(pixelColor.rgb * input.Color.rgb, pixelColor.a * input.Color.a);
		
	float alpha = pixelColor.a;
	float size = 1.01f/256 * OutlineWidth;
				
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(size, 0)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(size, -size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(0, -size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(-size, -size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(-size, 0)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(-size, size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(0, size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(size, size)).a);
	
	if(alpha > 0.1f)
		return OutlineColor;
		
	return float4(0,0,0,0);
}

float4 Outline2PS(VertexShaderOutput input) : COLOR
{
	float4 pixelColor = tex2D(SpriteTextureSampler, input.TextureCoordinates);
	
	if(pixelColor.a > 0.3f)
		return float4(pixelColor.rgb * input.Color.rgb, pixelColor.a * input.Color.a);
		
	float alpha = pixelColor.a;
	float size = 1.0f/256 * OutlineWidth;
				
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(size, 0)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(size, -size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(0, -size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(-size, -size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(-size, 0)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(-size, size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(0, size)).a);
	alpha = max(alpha, tex2D(SpriteTextureSampler, input.TextureCoordinates + float2(size, size)).a);
	
	if(alpha > 0.1f)
		return float4(0,0,1,1);
		
	return float4(0,0,0,0);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL OutlinePS();
	}
};

technique OtherTechnique
{
	pass P0	
	{
		PixelShader = compile PS_SHADERMODEL OutlinePS();
	}
	pass P1
	{
		PixelShader = compile PS_SHADERMODEL Outline2PS();
	}
}