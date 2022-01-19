using src.gameboy;
using UnityEngine.InputSystem;

namespace src.adapters
{
    public class Joypad
    {
        private const int JOYPAD_INTERRUPT = 4;
        private const byte PAD_MASK = 0x10;
        private const byte BUTTON_MASK = 0x20;
        private byte pad = 0xF;
        private byte buttons = 0xF;

        public enum Keys
        {
            UP,
            DOWN,
            LEFT,
            RIGHT,
            A,
            B,
            START,
            SELECT
        }

        public void Update(MMU mmu)
        {
            var joyp = mmu.JOYP;
            if (!Util.IsBit(4, joyp))
            {
                mmu.JOYP = (byte)((joyp & 0xF0) | pad);
                if (pad != 0xF) mmu.requestInterrupt(JOYPAD_INTERRUPT);
            }

            if (!Util.IsBit(5, joyp))
            {
                mmu.JOYP = (byte)((joyp & 0xF0) | buttons);
                if (buttons != 0xF) mmu.requestInterrupt(JOYPAD_INTERRUPT);
            }

            if ((joyp & 0b00110000) == 0b00110000) mmu.JOYP = 0xFF;
        }

        public void HandleKeyAction(Keys k, bool keyDown)
        {
            var b = GetKeyBit(k);

            if (keyDown)
            {
                if ((b & PAD_MASK) == PAD_MASK)
                {
                    pad = (byte)(pad & ~(b & 0xF));
                }
                else if ((b & BUTTON_MASK) == BUTTON_MASK)
                {
                    buttons = (byte)(buttons & ~(b & 0xF));
                }
            }
            else
            {
                if ((b & PAD_MASK) == PAD_MASK)
                {
                    pad = (byte)(pad | (b & 0xF));
                }
                else if ((b & BUTTON_MASK) == BUTTON_MASK)
                {
                    buttons = (byte)(buttons | (b & 0xF));
                }
            }
        }

        private static byte GetKeyBit(Keys k)
        {
            return k switch
            {
                Keys.RIGHT => 0x11,
                Keys.LEFT => 0x12,
                Keys.UP => 0x14,
                Keys.DOWN => 0x18,
                Keys.A => 0x21,
                Keys.B => 0x22,
                Keys.START => 0x24,
                Keys.SELECT => 0x28,
                _ => 0x0
            };
        }
    }
}