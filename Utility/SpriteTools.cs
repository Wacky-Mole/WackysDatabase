using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;


namespace RainbowTrollArmor
{
    public class SpriteTools
    {
        Sprite outcome;
        Texture2D texture;
        Texture2D rawTexture;
        Texture2D secondaryLayer;
        Sprite source;
        Color tint;
        public float ratio = 0.5f;


        public void setTint(Color tint)
        {
            this.tint = tint;
        }
        public void setSecondaryLayer(Texture2D secondaryLayer)
        {
            this.secondaryLayer = secondaryLayer;
        }
        public void setRatio(int ratio)
        {
            this.ratio = ((float)ratio / 100);
        }
        public void setRatio(float ratio)
        {
            this.ratio = ratio;
        }

        public Texture2D CreateTexture2D(Texture2D rawTexture, bool useTint)
        {
            this.rawTexture = rawTexture;
            makeTexture(rawTexture.width, rawTexture.height);
            getTexturePixels();
            if (useTint && tint != null)
            {
                BlendTexture();
            }
            return texture;
        }

        public Texture2D CreateTexture2D(Sprite source, bool useTint)
        {
            this.source = source;
            makeTexture((int)source.rect.width, (int)source.rect.height);
            getSpritePixels();
            if (useTint && tint != null)
            {
                BlendTexture();
            }
            return texture;
        }

        void MakeGrayscale()
        {
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color pixel = texture.GetPixel(x, y);
                    if (pixel.a > 0)
                    {
                        var gray = pixel.grayscale;
                        Color grayish = new Color(gray, gray, gray, pixel.a);
                        texture.SetPixel(x, y, grayish);
                    }
                }
            }
            texture.Apply();
        }

        public Sprite CreateSprite(Sprite source, bool useTint, bool grayscale = false)
        {
            this.source = source;
            makeTexture((int)source.rect.width, (int)source.rect.height);
            getSpritePixels();

            if (grayscale)
            {
                MakeGrayscale();
            }

            if (useTint && tint != null)
            {
                BlendTexture();
            }
            MakeSprite();
            return outcome;
        }

        public Sprite CreateSprite(Texture2D rawTexture, bool useTint, bool grayscale = false)
        {
            this.rawTexture = rawTexture;
            makeTexture(rawTexture.width, rawTexture.height);
            getTexturePixels();

            if (grayscale)
            {
                MakeGrayscale();
            }
            if (useTint && tint != null)
            {
                BlendTexture();
            }
            MakeSprite();
            return outcome;
        }
        public void makeTexture(int width, int height)
        {
            texture = new Texture2D(width, height);
        }
        void getTexturePixels()
        {
            var pixels = rawTexture.GetPixels();
            texture.SetPixels(pixels);
            texture.Apply();
        }

        void getSpritePixels()
        {
            var pixels = source.texture.GetPixels((int)source.textureRect.x,
                                                    (int)source.textureRect.y,
                                                    (int)source.textureRect.width,
                                                    (int)source.textureRect.height);
            texture.SetPixels(pixels);
            texture.Apply();
        }

        void BlendTexture()
        {
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color pixel = texture.GetPixel(x, y);
                    if (pixel.a > 0)
                    {
                        texture.SetPixel(x, y, Color.Lerp(pixel, tint, ratio));
                        if (secondaryLayer != null)
                        {
                            Color secPixel = secondaryLayer.GetPixel(x, y);
                            if (secPixel.a > 0)
                            {
                                texture.SetPixel(x, y, secPixel);
                            }
                        }
                    }
                }
            }
            texture.Apply();
        }

        public Sprite convertTextureToSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        void MakeSprite()
        {
            outcome = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        public Texture2D GetTexture2D()
        {
            return texture;
        }

        public Sprite getSprite()
        {
            return outcome;
        }


        public static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
        {

            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference

            Texture2D SpriteTexture = LoadTexture(FilePath);
            Sprite NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);

            return NewSprite;
        }

        public static Sprite ConvertTextureToSprite(Texture2D texture, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
        {
            // Converts a Texture2D to a sprite, assign this texture to a new sprite and return its reference

            Sprite NewSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);

            return NewSprite;
        }

        public static Texture2D LoadTexture(string FilePath)
        {

            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails
            Texture2D Tex2D;
            byte[] FileData;

            if (File.Exists(FilePath))
            {
                FileData = File.ReadAllBytes(FilePath);
                Tex2D = new Texture2D(2, 2);           // Create new "empty" texture
                if (Tex2D.LoadImage(FileData))           // Load the imagedata into the texture (size is set automatically)
                    return Tex2D;                 // If data = readable -> return texture
            }
            return null;                     // Return null if load failed
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }


    }

}
/*
Shader "UI/GrayscaleTransparent"
    {
    Properties
        {
        _MainTex("Texture", 2D) = "white" { }
        _Color("Color", Color) = (1, 1, 1, 1)
        }

    SubShader
        {
        GrabPass { "_BackgroundTexture" }

        Pass
            {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

            ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha
                ZTest Off

                CGPROGRAM
                #pragma vertex vert
#pragma fragment frag
# include "UnityCG.cginc"
            sampler2D _BackgroundTexture;
            sampler2D _MainTex;
            fixed4 _MainTex_ST;
            fixed4 _Color;


                struct v2f
{
    fixed4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    fixed4 grabUV : TEXCOORD1;
                };

struct appdata
{
    fixed4 vertex : POSITION;
                    fixed4 color : COLOR;
                };

v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.grabUV = ComputeGrabScreenPos(o.vertex);
    o.color = v.color * _Color;
    return o;
};

fixed4 frag(v2f i) : SV_Target
{
    fixed4 texcol = tex2Dproj(_BackgroundTexture, i.grabUV);
    texcol.rgb = lerp(texcol.rgb, dot(texcol.rgb, float3(0.3, 0.59, 0.11)), texcol.a);
    texcol = texcol * i.color;
    return texcol;
};

ENDCG
            }
        }
        FallBack Off
    }
*/