using System;
using UnityEngine;

namespace wackydatabase
{
    public class Colour
    {
        private static Color GRAYSCALE = new Color(0.2126729f, 0.7151522f, 0.0721750f);

        public static Texture2D CloneTexture(Texture2D texture)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            Graphics.Blit(texture, tmp);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            Texture2D clone = new Texture2D(texture.width, texture.height);
                      clone.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                      clone.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return clone;
        }

        public static Color Screen(Color pixel, Color screen)
        {
            Color ret = pixel + screen - pixel * screen;

            ret.a = pixel.a;

            return ret;
        }

        public static Color Multiply(Color pixel, Color multiply)
        {
            Color ret = pixel * multiply;

            ret.a = pixel.a;

            return ret;
        }

        public static Color Overlay(Color pixel, Color overlay)
        {
            return pixel.grayscale < 0.5 ? 2.0f * pixel * overlay : Color.white - (Color.white * 2) * (Color.white - pixel) * (Color.white - overlay);
        }

        private static float Luminance(Color a)
        {
            return GRAYSCALE.r * a.r + GRAYSCALE.g * a.g + GRAYSCALE.b * a.b;
        }

        public static Color32 Cell(Color32 initial, Color32 high, Color32 low, Color32 mid, float midHigh = 0.66f, float midLow = 0.33f)
        {
            float l = Luminance(initial);

            if (l >= midHigh)
            {
                Color ret = Color.Lerp(initial, high, l / 2);
                ret.a = initial.a;
                return ret;
                //return Screen(initial, high);
            }
            else if (l <= midLow)
            {
                Color ret = Color.Lerp(initial, low, l / 2);
                ret.a = initial.a;
                return ret;
                //return Screen(initial, low);
            }
            else
            {
                return Color.Lerp(initial, mid, l / 2);
            }
        }

        public static Texture2D AsGrayscale(Texture2D texture)
        {
            Texture2D clone = CloneTexture(texture);

            try
            {
                Color[] data = clone.GetPixels();
                Color[] grayscale = new Color[data.Length];

                for (uint x = 0; x < texture.width; x++)
                {
                    for (uint y = 0; y < texture.height; y++)
                    {
                        Color pixel = data[x + y * texture.width];

                        grayscale[x + y * texture.width] = pixel * GRAYSCALE;
                    }
                }

                clone.SetPixels(grayscale);
                clone.Apply();
            } catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }

            return clone;
        }

        public static Texture2D AsGrayscaleColoured(Texture2D texture, Color colour)
        {
            Texture2D clone = CloneTexture(texture);

            try
            {
                Color[] data = clone.GetPixels();
                Color[] grayscale = new Color[data.Length];

                for (uint x = 0; x < texture.width; x++)
                {
                    for (uint y = 0; y < texture.height; y++)
                    {
                        Color pixel = data[x + y * texture.width];

                        grayscale[x + y * texture.width] = Screen(pixel * GRAYSCALE, colour);
                    }
                }

                clone.SetPixels(grayscale);
                clone.Apply();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }

            return clone;
        }

        public static Texture2D AsScreen(Texture2D texture, Color colour)
        {
            Texture2D clone = CloneTexture(texture);            

            try
            {
                Color32[] data = clone.GetPixels32();
                Color32[] pixels = new Color32[data.Length];

                for (uint x = 0; x < texture.width; x++)
                {
                    for (uint y = 0; y < texture.height; y++)
                    {
                        Color32 pixel = data[x + y * texture.width];

                        pixels[x + y * texture.width] = Screen(pixel, colour);
                    }
                }

                clone.SetPixels32(pixels);
                clone.Apply();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }

            return clone;
        }

        public static Texture2D AsMultiply(Texture2D texture, Color colour)
        {
            Texture2D clone = CloneTexture(texture);

            try
            {
                Color32[] data = clone.GetPixels32();
                Color32[] pixels = new Color32[data.Length];

                for (uint x = 0; x < texture.width; x++)
                {
                    for (uint y = 0; y < texture.height; y++)
                    {
                        Color32 pixel = data[x + y * texture.width];

                        pixels[x + y * texture.width] = Multiply(pixel, colour);
                    }
                }

                clone.SetPixels32(pixels);
                clone.Apply();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }

            return clone;
        }

        public static Texture2D AsCell(Texture2D texture, Color32 high, Color32 low, Color32? mid)
        {
            Texture2D clone = CloneTexture(texture);

            try
            {
                Color32[] data = clone.GetPixels32();
                Color32[] grayscale = new Color32[data.Length];

                for (uint x = 0; x < texture.width; x++)
                {
                    for (uint y = 0; y < texture.height; y++)
                    {
                        Color32 pixel = data[x + y * texture.width];

                        if (mid.HasValue)
                        {
                            grayscale[x + y * texture.width] = Cell(pixel, high, low, mid.Value);
                        } else
                        {
                            grayscale[x + y * texture.width] = Cell(pixel, high, low, pixel);
                        }
                    }
                }

                clone.SetPixels32(grayscale);
                clone.Apply();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }

            return clone;
        }
    }
}
