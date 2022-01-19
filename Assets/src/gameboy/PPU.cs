using System.Runtime.CompilerServices;
using src.adapters;

namespace src.gameboy
{
    public class PPU
    {
        private const int SCREEN_WIDTH = 160;
        private const int SCREEN_HEIGHT = 144;

        private const int SCREEN_VBLANK_HEIGHT = 153;
        private const int OAM_CYCLES = 80;
        private const int VRAM_CYCLES = 172;
        private const int HBLANK_CYCLES = 204;
        private const int SCANLINE_CYCLES = 456;

        private const int VBLANK_INTERRUPT = 0;
        private const int LCD_INTERRUPT = 1;

        private readonly DirectBitMap _bmp;
        private int _scanlineCounter;

        public PPU(DirectBitMap bitmap)
        {
            _bmp = bitmap;
        }

        public void Update(int cycles, MMU mmu)
        {
            _scanlineCounter += cycles;
            var currentMode = (byte)(mmu.STAT & 0x3); //Current Mode Mask

            if (IsLcdEnabled(mmu.LCDC))
            {
                switch (currentMode)
                {
                    case 0: //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
                        if (_scanlineCounter >= HBLANK_CYCLES)
                        {
                            mmu.LY++;
                            _scanlineCounter -= HBLANK_CYCLES;

                            if (mmu.LY == SCREEN_HEIGHT)
                            {
                                //check if we arrived Vblank
                                ChangeStatMode(1, mmu);
                                mmu.requestInterrupt(VBLANK_INTERRUPT);
                                RenderFrame();
                            }
                            else
                            {
                                //not arrived yet so return to 2
                                ChangeStatMode(2, mmu);
                            }
                        }

                        break;
                    case 1: //VBLANK - Mode 1 (4560 cycles - 10 lines)
                        if (_scanlineCounter >= SCANLINE_CYCLES)
                        {
                            mmu.LY++;
                            _scanlineCounter -= SCANLINE_CYCLES;

                            if (mmu.LY > SCREEN_VBLANK_HEIGHT)
                            {
                                //check end of VBLANK
                                ChangeStatMode(2, mmu);
                                mmu.LY = 0;
                            }
                        }

                        break;
                    case 2: //Accessing OAM - Mode 2 (80 cycles)
                        if (_scanlineCounter >= OAM_CYCLES)
                        {
                            ChangeStatMode(3, mmu);
                            _scanlineCounter -= OAM_CYCLES;
                        }

                        break;
                    case 3: //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles
                        if (_scanlineCounter >= VRAM_CYCLES)
                        {
                            ChangeStatMode(0, mmu);
                            DrawScanLine(mmu);
                            _scanlineCounter -= VRAM_CYCLES;
                        }

                        break;
                }

                if (mmu.LY == mmu.LYC)
                {
                    //handle coincidence Flag
                    mmu.STAT = Util.BITSet(2, mmu.STAT);
                    if (Util.IsBit(6, mmu.STAT))
                    {
                        mmu.requestInterrupt(LCD_INTERRUPT);
                    }
                }
                else
                {
                    mmu.STAT = Util.BITClear(2, mmu.STAT);
                }
            }
            else
            {
                //LCD Disabled
                _scanlineCounter = 0;
                mmu.LY = 0;
                mmu.STAT = (byte)(mmu.STAT & ~0x3);
            }
        }

        private static void ChangeStatMode(int mode, MMU mmu)
        {
            var stat = (byte)(mmu.STAT & ~0x3);
            mmu.STAT = (byte)(stat | mode);
            //Accessing OAM - Mode 2 (80 cycles)
            if (mode == 2 && Util.IsBit(5, stat))
            {
                // Bit 5 - Mode 2 OAM Interrupt         (1=Enable) (Read/Write)
                mmu.requestInterrupt(LCD_INTERRUPT);
            }

            //case 3: //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles

            //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
            else if (mode == 0 && Util.IsBit(3, stat))
            {
                // Bit 3 - Mode 0 H-Blank Interrupt     (1=Enable) (Read/Write)
                mmu.requestInterrupt(LCD_INTERRUPT);
            }

            //VBLANK - Mode 1 (4560 cycles - 10 lines)
            else if (mode == 1 && Util.IsBit(4, stat))
            {
                // Bit 4 - Mode 1 V-Blank Interrupt     (1=Enable) (Read/Write)
                mmu.requestInterrupt(LCD_INTERRUPT);
            }
        }

        private void DrawScanLine(MMU mmu)
        {
            var LCDC = mmu.LCDC;
            if (Util.IsBit(0, LCDC))
            {
                //Bit 0 - BG Display (0=Off, 1=On)
                RenderBg(mmu);
            }

            if (Util.IsBit(1, LCDC))
            {
                //Bit 1 - OBJ (Sprite) Display Enable
                RenderSprites(mmu);
            }
        }

        private void RenderBg(MMU mmu)
        {
            byte WX = (byte)(mmu.WX - 7); //WX needs -7 Offset
            byte WY = mmu.WY;
            byte LY = mmu.LY;
            byte LCDC = mmu.LCDC;
            byte SCY = mmu.SCY;
            byte SCX = mmu.SCX;
            byte BGP = mmu.BGP;
            bool isWin = IsWindow(LCDC, WY, LY);

            byte y = isWin ? (byte)(LY - WY) : (byte)(LY + SCY);
            byte tileLine = (byte)((y & 7) * 2);

            ushort tileRow = (ushort)(y / 8 * 32);
            ushort tileMap = isWin ? GetWindowTileMapAddress(LCDC) : GetBgTileMapAddress(LCDC);

            byte hi = 0;
            byte lo = 0;

            for (int p = 0; p < SCREEN_WIDTH; p++)
            {
                byte x = isWin && p >= WX ? (byte)(p - WX) : (byte)(p + SCX);
                if ((p & 0x7) == 0 || ((p + SCX) & 0x7) == 0)
                {
                    ushort tileCol = (ushort)(x / 8);
                    ushort tileAdress = (ushort)(tileMap + tileRow + tileCol);

                    ushort tileLoc;
                    if (IsSignedAddress(LCDC))
                    {
                        tileLoc = (ushort)(GetTileDataAddress(LCDC) + mmu.readVRAM(tileAdress) * 16);
                    }
                    else
                    {
                        tileLoc = (ushort)(GetTileDataAddress(LCDC) + ((sbyte)mmu.readVRAM(tileAdress) + 128) * 16);
                    }

                    lo = mmu.readVRAM((ushort)(tileLoc + tileLine));
                    hi = mmu.readVRAM((ushort)(tileLoc + tileLine + 1));
                }

                int colorBit = 7 - (x & 7); //inversed
                int colorId = GetColorIdBits(colorBit, lo, hi);
                int colorIdThroughtPalette = GetColorIdThroughtPalette(BGP, colorId);

                _bmp.SetPixel(p, LY, colorIdThroughtPalette);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetColorIdBits(int colorBit, byte l, byte h)
        {
            return (((h >> colorBit) & 0x1) << 1) | ((l >> colorBit) & 0x1);
        }

        private int GetColorIdBitsOLD(int colorBit, byte l, byte h)
        {
            int hi = (h >> colorBit) & 0x1;
            int lo = (l >> colorBit) & 0x1;
            return (hi << 1 | lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetColorIdThroughtPalette(int palette, int colorId)
        {
            return (palette >> colorId * 2) & 0x3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSignedAddress(byte LCDC)
        {
            //Bit 4 - BG & Window Tile Data Select   (0=8800-97FF, 1=8000-8FFF)
            return Util.IsBit(4, LCDC);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort GetBgTileMapAddress(byte LCDC)
        {
            //Bit 3 - BG Tile Map Display Select     (0=9800-9BFF, 1=9C00-9FFF)
            return Util.IsBit(3, LCDC) ? (ushort)0x9C00 : (ushort)0x9800;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort GetWindowTileMapAddress(byte LCDC)
        {
            //Bit 6 - Window Tile Map Display Select(0 = 9800 - 9BFF, 1 = 9C00 - 9FFF)
            return Util.IsBit(6, LCDC) ? (ushort)0x9C00 : (ushort)0x9800;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort GetTileDataAddress(byte LCDC)
        {
            //Bit 4 - BG & Window Tile Data Select   (0=8800-97FF, 1=8000-8FFF)
            return Util.IsBit(4, LCDC) ? (ushort)0x8000 : (ushort)0x8800; //0x8800 signed area
        }

        private void RenderSprites(MMU mmu)
        {
            byte LY = mmu.LY;
            byte LCDC = mmu.LCDC;
            for (int i = 0x9C; i >= 0; i -= 4)
            {
                //0x9F OAM Size, 40 Sprites x 4 bytes:
                int y = mmu.readOAM(i) - 16; //Byte0 - Y Position //needs 16 offset
                int x = mmu.readOAM(i + 1) - 8; //Byte1 - X Position //needs 8 offset
                byte tile = mmu.readOAM(i + 2); //Byte2 - Tile/Pattern Number
                byte attr = mmu.readOAM(i + 3); //Byte3 - Attributes/Flags

                if ((LY >= y) && (LY < (y + SpriteSize(LCDC))))
                {
                    byte palette =
                        Util.IsBit(4, attr)
                            ? mmu.OBP1
                            : mmu.OBP0; //Bit4   Palette number  **Non CGB Mode Only** (0=OBP0, 1=OBP1)

                    int tileRow = IsYFlipped(attr) ? SpriteSize(LCDC) - 1 - (LY - y) : (LY - y);

                    ushort tileddress = (ushort)(0x8000 + (tile * 16) + (tileRow * 2));
                    byte lo = mmu.readVRAM(tileddress);
                    byte hi = mmu.readVRAM((ushort)(tileddress + 1));

                    for (int p = 0; p < 8; p++)
                    {
                        int IdPos = IsXFlipped(attr) ? p : 7 - p;
                        int colorId = GetColorIdBits(IdPos, lo, hi);
                        int colorIdThroughtPalette = GetColorIdThroughtPalette(palette, colorId);

                        if ((x + p) >= 0 && (x + p) < SCREEN_WIDTH)
                        {
                            if (!IsTransparent(colorId) && (IsAboveBg(attr) || IsBgWhite(mmu.BGP, x + p, LY)))
                            {
                                _bmp.SetPixel(x + p, LY, colorIdThroughtPalette);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsBgWhite(byte BGP, int x, int y)
        {
            int id = BGP & 0x3;
            return _bmp.GetPixel(x, y) == id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAboveBg(byte attr)
        {
            //Bit7 OBJ-to - BG Priority(0 = OBJ Above BG, 1 = OBJ Behind BG color 1 - 3)
            return attr >> 7 == 0;
        }

        public void RenderFrame()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLcdEnabled(byte LCDC)
        {
            //Bit 7 - LCD Display Enable
            return Util.IsBit(7, LCDC);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SpriteSize(byte LCDC)
        {
            //Bit 2 - OBJ (Sprite) Size (0=8x8, 1=8x16)
            return Util.IsBit(2, LCDC) ? 16 : 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsXFlipped(int attr)
        {
            //Bit5   X flip(0 = Normal, 1 = Horizontally mirrored)
            return Util.IsBit(5, attr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsYFlipped(byte attr)
        {
            //Bit6 Y flip(0 = Normal, 1 = Vertically mirrored)
            return Util.IsBit(6, attr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTransparent(int b)
        {
            return b == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWindow(byte LCDC, byte WY, byte LY)
        {
            //Bit 5 - Window Display Enable (0=Off, 1=On)
            return Util.IsBit(5, LCDC) && WY <= LY;
        }
    }
}