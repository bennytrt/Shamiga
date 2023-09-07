using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shamiga
{
    public partial class Form1 : Form
    {
        private GraphicsRenderer graphicsRenderer;
        private byte[] frameBuffer;
        private int frameBufferWidth;
        private int frameBufferHeight;

        public Form1()
        {
            InitializeComponent();
            graphicsRenderer = new GraphicsRenderer(frameBufferWidth, frameBufferHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            graphicsRenderer.RenderFrame(e);
        }
        public void UpdateGraphics(byte[] newFrameBuffer)
        {
            graphicsRenderer.UpdateFrameBuffer(newFrameBuffer);
            Invalidate(); // Request a repaint
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

        private void RenderOnPictureBox()
        {
            Bitmap bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics graphics = Graphics.FromImage(bitmap);

            graphics.FillRectangle(Brushes.Blue, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));

            pictureBox1.Image = bitmap;
        }

        private void Form1_Load(object sender, EventArgs e)
        { 
        }


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
}
