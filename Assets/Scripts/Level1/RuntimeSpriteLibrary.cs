using UnityEngine;

public static class RuntimeSpriteLibrary
{
    private static Sprite diamondSprite;
    private static Sprite circleSprite;
    private static Sprite fallbackSprite;

    public static Sprite GetDiamondSprite()
    {
        if (diamondSprite != null)
        {
            return diamondSprite;
        }

        try
        {
            const int size = 24;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            int center = size / 2;
            int radius = Mathf.Max(1, center - 2);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Abs(x - center);
                    int dy = Mathf.Abs(y - center);
                    bool inside = (dx + dy) <= radius;
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            texture.name = "Level1DiamondRuntime";
            diamondSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }
        catch
        {
            diamondSprite = GetFallbackSprite();
        }

        return diamondSprite != null ? diamondSprite : GetFallbackSprite();
    }

    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        try
        {
            const int size = 24;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            float center = (size - 1) * 0.5f;
            float radius = size * 0.4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    bool inside = (dx * dx) + (dy * dy) <= radius * radius;
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            texture.name = "Level1CircleRuntime";
            circleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }
        catch
        {
            circleSprite = GetFallbackSprite();
        }

        return circleSprite != null ? circleSprite : GetFallbackSprite();
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        try
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }

            texture.Apply();
            fallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
        }
        catch
        {
            fallbackSprite = null;
        }

        return fallbackSprite;
    }
}
