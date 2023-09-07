using System;
using System.Drawing;
using System.Windows.Forms;

namespace Shamiga
{
    public class GraphicsRenderer
    {
        private byte[] frameBuffer;
        private int frameBufferWidth;
        private int frameBufferHeight;

        public GraphicsRenderer(int width, int height)
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
    }
}
