using System.IO;
using src.adapters;
using src.gameboy.GamePack;
using UnityEngine;

namespace src.gameboy
{
    public class GameBoy
    {
        private readonly CPU _cpu;
        private readonly MMU _mmu;
        private readonly PPU _ppu;
        private readonly Timer _timer;
        private readonly Joypad _joypad;

        private int _cpuCycles;
        private long _cyclesThisUpdate;

        public GameBoy(DirectBitMap bitmap, Joypad joypad, string rom)
        {
            _timer = new Timer();
            _joypad = joypad;

            _mmu = new MMU(load_gamePack(rom));
            _cpu = new CPU(_mmu);
            _ppu = new PPU(bitmap);
        }

        public void Tick()
        {
            while (_cyclesThisUpdate < Util.CYCLES_PER_UPDATE)
            {
                _cpuCycles = _cpu.FetchDecodeExecute();
                _cyclesThisUpdate += _cpuCycles;

                _timer.Update(_cpuCycles, _mmu);
                _ppu.Update(_cpuCycles, _mmu);
                _joypad.Update(_mmu);
                HandleInterrupts();
            }

            _cyclesThisUpdate -= Util.CYCLES_PER_UPDATE;
        }

        private void HandleInterrupts()
        {
            for (var i = 0; i < 5; i++)
            {
                if ((((_mmu.IE & _mmu.IF) >> i) & 0x1) == 1)
                {
                    _cpu.ExecuteInterrupt(i);
                }
            }

            _cpu.UpdateIME();
        }

        private static IGamePack load_gamePack(string cartName)
        {
            var rom = File.ReadAllBytes(cartName);
            IGamePack gamePack;
            switch (rom[0x147])
            {
                case 0x00:
                    gamePack = new MBC0();
                    break;
                case 0x01:
                case 0x02:
                case 0x03:
                    gamePack = new MBC1();
                    break;
                case 0x05:
                case 0x06:
                    gamePack = new MBC2();
                    break;
                case 0x0F:
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                    gamePack = new MBC3();
                    break;
                case 0x19:
                case 0x1A:
                case 0x1B:
                    gamePack = new MBC5();
                    break;
                default:
                    Debug.Log("Unsupported: " + rom[0x147].ToString("x2"));
                    return null;
            }

            gamePack.Init(rom);
            return gamePack;
        }
    }
}