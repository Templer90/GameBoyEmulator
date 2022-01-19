namespace src.gameboy.GamePack
{
    public class MBC0 : IGamePack
    {
        private byte[] _rom;

        public void Init(byte[] rom)
        {
            _rom = rom;
        }

        public byte ReadERAM(ushort addr)
        {
            return 0xFF;
        }

        public byte ReadLoROM(ushort addr)
        {
            return _rom[addr];
        }

        public byte ReadHiROM(ushort addr)
        {
            return _rom[addr];
        }

        public void WriteERAM(ushort addr, byte value) {}

        public void WriteROM(ushort addr, byte value) {}
    }
}