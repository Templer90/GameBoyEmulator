using System.Runtime.CompilerServices;
using UnityEngine;

namespace src.adapters
{
    public class DirectBitMap
    {
        private const int Height = 144;
        private const int Width = 160;

        private static readonly Color32[] Pallet =
        {
            new Color32(255, 255, 255, 255),
            new Color32(128, 128, 128, 255),
            new Color32(64, 64, 64, 255),
            new Color32(0, 0, 0, 255)
        };

        private readonly Color32[] _colors;
        private readonly int[] _bits;
        private readonly SpriteRenderer _spriteRenderer;

        public DirectBitMap(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
            
            var texture = Texture2D.redTexture;
            texture.Resize(Width, Height);
            _spriteRenderer.sprite = Sprite.Create(texture, new Rect(0,0,Width, Height), new Vector2(0.5f, 0.5f));

            _bits = new int[Width * Height];
            _colors = new Color32[Width * Height];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixel(int x, int y, int colourID)
        {
            _bits[x + (y * Width)] = colourID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPixel(int x, int y)
        {
            return _bits[x + (y * Width)];
        }

        public void CommitData()
        {
            for (var i = 0; i < _bits.Length; i++)
            {
                _colors[i] = Pallet[_bits[i]];
            }

            _spriteRenderer.sprite.texture.SetPixels32(_colors);
            _spriteRenderer.sprite.texture.Apply();
        }
    }
}