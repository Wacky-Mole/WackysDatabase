using System.Globalization;
using UnityEngine;
public class ColorUtil
{
	public static Color GetColorFromHex(string hex)
	{
		hex = hex.TrimStart('#');
		Color result = new Color(255f, 0f, 0f);
		if (hex.Length >= 6)
		{
			result.r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
			result.g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
			result.b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
			if (hex.Length == 8)
			{
				result.a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
			}
		}
		return result;
	}

	public static string GetHexFromColor(Color color)
	{
		return $"#{(int)(color.r * 255f):X2}" + $"{(int)(color.g * 255f):X2}" + $"{(int)(color.b * 255f):X2}" + $"{(int)(color.a * 255f):X2}";
	}
}
