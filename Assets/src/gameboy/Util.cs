using System.Runtime.CompilerServices;

namespace src.gameboy
{
    public static class Util
    {
        private const int DMG_4Mhz = 4194304;
        private const float REFRESH_RATE = 59.7275f;
        public const int CYCLES_PER_UPDATE = (int)(DMG_4Mhz / REFRESH_RATE);
        public const float MILLIS_PER_FRAME = 16.74f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte BITSet(byte n, byte v)
        {
            return (byte)(v | (byte)(1 << n));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte BITClear(int n, byte v)
        {
            return (byte)(v & (byte)~(1 << n));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBit(int n, int v)
        {
            return ((v >> n) & 1) == 1;
        }
    }
}