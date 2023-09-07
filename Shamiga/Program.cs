using System;
using System.IO;
using System.Reflection.Emit;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using Shamiga;

namespace Shamiga
{
    class Program
    {
        static void Main(string[] args)
        {
            using (AmigaEmulator emulator = new AmigaEmulator())
            {
                emulator.LoadKickstartROM("kickstart.rom");
                emulator.InsertDisk("game.adf");

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                while (!emulator.ShouldQuit)
                {
                    emulator.HandleInput();
                    emulator.EmulateFrame();
                    emulator.RenderScreen();

                    Form form = new Form();
                    Application.Run(form);

                }
            }
        }
    }
    class AmigaEmulator : IDisposable
    {
        private bool[] keyboardState = new bool[256];
        private CPU68000 cpu;
        private byte[] memory = new byte[655360]; // this is 640KB

        private GraphicsRenderer graphicsRenderer;
        private byte[] frameBuffer;
        private int frameBufferWidth;
        private int frameBufferHeight;

        public AmigaEmulator()
        {
            cpu = new CPU68000(this);
            graphicsRenderer = new GraphicsRenderer(frameBufferWidth, frameBufferHeight);
        }

        public byte[] Memory
        {
            get { return memory; }
        }

        public void LoadKickstartROM(string romPath)
        {
            Console.WriteLine("Loading Kickstart ROM Into Memory...");
            try
            {
                byte[] romData = File.ReadAllBytes(romPath);
                if (romData.Length <= memory.Length)
                {
                    Array.Copy(romData, 0, memory, 0, romData.Length);
                    Console.WriteLine("Kickstart ROM Loaded Successfully.");
                    Console.WriteLine($"ROM Data Size: {romData.Length} Bytes");
                }
                else
                {
                    Console.WriteLine("Kickstart ROM Is Too Large To Fit In Memory.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed To Load Kickstart ROM: " + ex.Message);
            }
        }

        public void InsertDisk(string diskImagePath)
        {
            Console.WriteLine("Inserting Disk Into The Virtual Drive..."); try
            {
                byte[] diskData = File.ReadAllBytes(diskImagePath); const int VirtualDriveSize = 880 * 1024; byte[] virtualDrive = new byte[VirtualDriveSize]; if (diskData.Length <= virtualDrive.Length)
                { Array.Copy(diskData, 0, virtualDrive, 0, diskData.Length); Console.WriteLine("Disk Inserted Successfully."); } else
                { Console.WriteLine("Disk Data Is Too Large To Fit In The Virtual Drive."); }
            } catch (Exception ex)
            { Console.WriteLine("Failed To Insert Disk: ", ex.Message); }
        }

        public void EmulateFrame()
        {
            cpu.ExecuteNextInstruction();
        }

        public void HandleInput()
        {
            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                int keyValue = (int)keyInfo.Key;

                if (keyValue >= 0 && keyValue < 256)
                {
                    if (keyInfo.KeyChar != 0)
                    {
                        keyboardState[keyValue] = true;
                    }
                    else
                    {
                        keyboardState[keyValue] = false;
                    }
                }
            }
        }

        public bool IsKeyPressed(int key)
        {
            if (key >= 0 && key < 256)
            {
                return keyboardState[key];
            }
            return false;
        }

        public void RenderScreen()
        {
            int frameBufferWidth = 640; int frameBufferHeight = 480; byte[] frameBuffer = ConvertAmigaGraphicsToModernFormat(); 
            if (frameBuffer.Length != frameBufferWidth * frameBufferHeight * 3)
            { Console.WriteLine("Error: Frame Buffer Dimensions Do Not Match The Buffer Data."); return; }
            DisplayFrameBuffer(frameBuffer, frameBufferWidth, frameBufferHeight);
        }

        private byte[] ConvertAmigaGraphicsToModernFormat()
        {
            return new byte[0];
        }

        private void DisplayFrameBuffer(byte[] frameBuffer, int width, int height)
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * width + x) * 3;

                    byte red = frameBuffer[pixelIndex];
                    byte green = frameBuffer[pixelIndex + 1];
                    byte blue = frameBuffer[pixelIndex + 2];

                    Console.Write($"({red}, {green}, {blue}) ");
                }
                Console.WriteLine();
            }
        }

        private static void InitializeForm(int width, int height)
        {
            Form1 form = new Form1();
            form.Text = "Frame Buffer Renderer";
            form.ClientSize = new System.Drawing.Size(width, height);
            PictureBox pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            form.Controls.Add(pictureBox);
        }

        public bool ShouldQuit
        {
            get { return IsKeyPressed((int)ConsoleKey.Escape); }
        }

        public void GraphicsRenderer(int width, int height)
        {
            frameBufferWidth = width;
            frameBufferHeight = height;
        }
        public void UpdateFrameBuffer(byte[] newFrameBuffer)
        {
            frameBuffer = newFrameBuffer;
        }

        public void RenderFrame(PaintEventArgs e)
        {
            int frameBufferWidth = 320;
            int frameBufferHeight = 240;

            if (frameBuffer != null)
            {
                using (Bitmap bitmap = new Bitmap(frameBufferWidth, frameBufferHeight))
                {
                    for (int y = 0; y < frameBufferHeight; y++)
                    {
                        for (int x = 0; x < frameBufferWidth; x++)
                        {
                            int pixelIndex = (y * frameBufferWidth + x) * 3;
                            Color pixelColor = Color.FromArgb(frameBuffer[pixelIndex], frameBuffer[pixelIndex + 1], frameBuffer[pixelIndex + 2]);
                            bitmap.SetPixel(x, y, pixelColor);
                        }
                    }

                    e.Graphics.DrawImage(bitmap, 0, 0);
                }
            }
        }

        public void Dispose()
        {
            cpu.Dispose();
        }
    }

    class CPU68000 : IDisposable
    {
        private uint[] dataRegisters = new uint[8];
        private uint[] addressRegisters = new uint[8];
        private ushort conditionCodes;
        private AmigaEmulator emulator; 

        public CPU68000(AmigaEmulator emulator)
        {
            this.emulator = emulator;
        }

        private class ConditionCode
        {
            public const int N = 7;
            public const int Z = 4;
            public const int V = 1;
            public const int C = 0;
            public const int S = 2;
        }

        private class OtherCC
        {
            public const ushort N = 0x8000;
            public const ushort Z = 0x4000;
            public const ushort V = 0x1000;
            public const ushort C = 0x0001;
        }

        public void ExecuteNextInstruction()
        {
            ushort opcode = FetchWord(addressRegisters[CPU68000.AddressRegisterIndex.PC]);

            switch (opcode)
            {
                case 0x4E75: 
                    ReturnFromSubroutine();
                    break;

                case 0x1081: 
                    MoveByteD0ToD1(opcode);
                    break;

                case 0xD442: 
                    AddLongD2ToD3(opcode);
                    break;

                case 0x91E7: 
                    SubtractWordA4ToD7(opcode);
                    break;

                case 0xB045: 
                    CompareByteD5toA2();
                    break;

                case 0x4EBA: 
                    JumpToA6();
                    break;

                case 0x6000: 
                    BranchToLabel();
                    break;

                case 0x1111:
                    BranchIfLE(opcode);
                    break;

                default:
                    Console.WriteLine($"Unknown Instruction: 0x{opcode:X}");
                    break;
            }
        }

        private void BranchIfLE(ushort opcode)
        {
            if ((conditionCodes & ConditionCode.Z) != 0 ||
                (((conditionCodes & ConditionCode.N) != 0) ^ ((conditionCodes & ConditionCode.V) != 0)))
            {
                int displacement = (sbyte)(opcode & 0xFF);

                addressRegisters[AddressRegisterIndex.PC] += (uint)displacement;
            }
        }

        private void MoveByteD0ToD1(ushort opcode)
        {
            int sourceRegisterIndex = (int)((opcode >> 9) & 0x07);
            int destinationRegisterIndex = (int)(opcode & 0x07);

            unchecked
            {
                byte sourceValue = (byte)dataRegisters[sourceRegisterIndex];

                dataRegisters[destinationRegisterIndex] &= 0xFFFFFF00; 
                dataRegisters[destinationRegisterIndex] |= sourceValue; 

                if (sourceValue == 0)
                {
                    conditionCodes |= (ushort)(1 << ConditionCode.Z);
                }
                else
                {
                    conditionCodes &= (ushort)~(1 << ConditionCode.Z);
                }

                if ((sourceValue & 0x80) != 0)
                {
                    conditionCodes |= (ushort)(1 << ConditionCode.N);
                }
                else
                {
                    conditionCodes &= (ushort)~(1 << ConditionCode.N);
                }

                conditionCodes &= (ushort)~(1 << ConditionCode.V);
                conditionCodes &= (ushort)~(1 << ConditionCode.C);
            }
        }

        private void AddLongD2ToD3(ushort opcode)
        {
            int sourceRegisterIndex = (int)((opcode >> 9) & 0x07);
            int destinationRegisterIndex = (int)(opcode & 0x07);

            uint sourceValue = dataRegisters[sourceRegisterIndex];
            uint destinationValue = dataRegisters[destinationRegisterIndex];

            ulong result = (ulong)destinationValue + sourceValue;

            unchecked
            {
                if ((result & 0xFFFFFFFF00000000) != 0) 
                {
                    conditionCodes |= (ushort)(1 << ConditionCode.C);
                }
                else
                {
                    conditionCodes &= (ushort)~(1 << ConditionCode.C);
                }

                if ((result & 0x80000000) != 0) 
                {
                    conditionCodes |= (ushort)(1 << ConditionCode.N);
                }
                else
                {
                    conditionCodes &= (ushort)~(1 << ConditionCode.N);
                }

                if (result == 0)
                {
                    conditionCodes |= (ushort)(1 << ConditionCode.Z);
                }
                else
                {
                    conditionCodes &= (ushort)~(1 << ConditionCode.Z);
                }

                conditionCodes &= (ushort)~(1 << ConditionCode.V);
            }

            dataRegisters[destinationRegisterIndex] = (uint)result;
        }


        private void SubtractWordA4ToD7(ushort opcode)
        {
            int sourceRegisterIndex = (int)((opcode >> 9) & 0x07);
            int destinationRegisterIndex = (int)(opcode & 0x07);

            ushort sourceValue = (ushort)dataRegisters[sourceRegisterIndex];
            ushort destinationValue = (ushort)addressRegisters[destinationRegisterIndex];

            short signedSource = (short)sourceValue;
            short signedDestination = (short)destinationValue;

            int result = signedDestination - signedSource;

            unchecked
            {
                if (result == 0)
                {
                    conditionCodes |= (ushort)(1 << ConditionCode.Z);
                    conditionCodes &= (ushort)~(1 << ConditionCode.N);
                    conditionCodes &= (ushort)~(1 << ConditionCode.V);
                    conditionCodes &= (ushort)~(1 << ConditionCode.C);
                }
                else if (result < 0)
                {
                    conditionCodes |= (ushort)(1 << ConditionCode.N);
                    conditionCodes &= (ushort)~(1 << ConditionCode.Z);
                    conditionCodes |= (ushort)(1 << ConditionCode.V);
                    conditionCodes |= (ushort)(1 << ConditionCode.C);
                }
                else
                {
                    conditionCodes &= (ushort)~(1 << ConditionCode.N);
                    conditionCodes &= (ushort)~(1 << ConditionCode.Z);
                    conditionCodes &= (ushort)~(1 << ConditionCode.V);
                    conditionCodes &= (ushort)~(1 << ConditionCode.C);
                }
            }

            addressRegisters[destinationRegisterIndex] = (uint)(ushort)result;
        }

        private void CompareByteD5toA2()
        {
            byte valueD5 = (byte)dataRegisters[DataRegisterIndex.D5];
            byte valueA2 = (byte)addressRegisters[AddressRegisterIndex.A2];

            unchecked
            {
                if (valueD5 < valueA2)
                {
                    conditionCodes |= (ushort)(1 << ConditionCode.N);
                    conditionCodes |= (ushort)(1 << ConditionCode.C);
                }
                else if (valueD5 > valueA2)
                {
                    conditionCodes &= (ushort)~(1 << ConditionCode.N);
                    conditionCodes |= (ushort)(1 << ConditionCode.C);
                }
                else
                {
                    conditionCodes &= (ushort)~(1 << ConditionCode.N);
                    conditionCodes &= (ushort)~(1 << ConditionCode.C);
                    conditionCodes |= (ushort)(1 << ConditionCode.Z);
                }
            }

        }

        private void JumpToA6()
        {
            addressRegisters[CPU68000.AddressRegisterIndex.PC] = addressRegisters[CPU68000.AddressRegisterIndex.A6];
        }

        private void BranchToLabel()
        {
            short offset = (short)(FetchWord(addressRegisters[CPU68000.AddressRegisterIndex.PC]) & 0xFFFF);

            uint newPC = addressRegisters[CPU68000.AddressRegisterIndex.PC] + (uint)offset;

            addressRegisters[CPU68000.AddressRegisterIndex.PC] = newPC;
        }

        private ushort FetchWord(uint address)
        {
            byte highByte = ReadMemory(address);
            byte lowByte = ReadMemory(address + 1);

            ushort value = (ushort)((highByte << 8) | lowByte);
            return value;
        }

        private byte ReadMemory(uint address)
        {
            return emulator.Memory[address];
        }

        public void Dispose()
        {

        }

        private void ReturnFromSubroutine()
        {
            addressRegisters[CPU68000.AddressRegisterIndex.PC] = Pop();
            conditionCodes = (ushort)(conditionCodes & ~(1 << ConditionCode.V));
        }

        private uint Pop()
        {
            uint value = ReadMemory(addressRegisters[CPU68000.AddressRegisterIndex.A7]);
            addressRegisters[CPU68000.AddressRegisterIndex.A7] += 4;
            return value;
        }

        private class AddressRegisterIndex
        {
            public const int PC = 7;

            public const int A0 = 0;
            public const int A1 = 1;
            public const int A2 = 2;
            public const int A3 = 3;
            public const int A4 = 4;
            public const int A5 = 5;
            public const int A6 = 6;
            public const int A7 = 7;
        }

        private class DataRegisterIndex
        {
            public const int D0 = 0;
            public const int D1 = 1;
            public const int D2 = 2;
            public const int D3 = 3;
            public const int D4 = 4;
            public const int D5 = 5;
            public const int D6 = 6;
            public const int D7 = 7;

        }
    }
}