namespace src.gameboy.GamePack
{
    public class MBC2 : IGamePack
    {
        private byte[] _rom;
        private readonly byte[] _eram = new byte [0x200]; //MBC2 512x4b internal ram
        private bool ERAM_ENABLED;
        private int ROM_BANK;
        private const int ROM_OFFSET = 0x4000;

        public void Init(byte[] rom)
        {
            _rom = rom;
        }

        public byte ReadERAM(ushort addr)
        {
            return ERAM_ENABLED ? _eram[addr & 0x1FFF] : (byte)0xFF;
        }

        public byte ReadLoROM(ushort addr)
        {
            return _rom[addr];
        }

        public byte ReadHiROM(ushort addr)
        {
            return _rom[(ROM_OFFSET * ROM_BANK) + (addr & 0x3FFF)];
        }

        public void WriteERAM(ushort addr, byte value)
        {
            if (ERAM_ENABLED)
            {
                _eram[addr & 0x1FFF] = value;
            }
        }

        public void WriteROM(ushort addr, byte value)
        {
            switch (addr)
            {
                case ushort r when addr >= 0x0000 && addr <= 0x1FFF:
                    ERAM_ENABLED = ((value & 0x1) == 0x0) ? true : false;
                    break;
                case ushort r when addr >= 0x2000 && addr <= 0x3FFF:
                    ROM_BANK = value & 0xF; //only last 4bits are used
                    break;
            }
        }
    }
}