using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace WpfComgr
{
    public partial class MainWindow : Window
    {

        const int W = 512, H = 256;
        private WriteableBitmap _wb;

        public MainWindow()
        {
            InitializeComponent();
            _wb = new WriteableBitmap(W, H, 96, 96, PixelFormats.Bgra32, null);
            Canvas.Source = _wb;
            DrawGradientLinearRgbThenToSrgbLineByLine();
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return a + (b - a) * t;
        }

        static byte ToSrgb8(float linear)
        {
            // 1) clip to [0..1]
            if (linear <= 0f) return 0;
            if (linear >= 1f) return 255;

            // 2) sRGB transfer function (gamma ≈ 2.4 piecewise is the standard)
            // Linear→sRGB
            float srgb = linear <= 0.0031308f
                ? 12.92f * linear
                : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;

            // 3) ×255
            int v = (int)MathF.Round(srgb * 255f);
            if (v < 0) v = 0;
            if (v > 255) v = 255;
            return (byte)v;
        }

        static void WriteBgra(byte[] buf, int idx, Vector3 linearRgb)
        {
            byte r = ToSrgb8(linearRgb.X);
            byte g = ToSrgb8(linearRgb.Y);
            byte b = ToSrgb8(linearRgb.Z);
            buf[idx + 0] = b; // B
            buf[idx + 1] = g; // G
            buf[idx + 2] = r; // R
            buf[idx + 3] = 255; // A
        }

        void DrawGradientLinearRgbThenToSrgbLineByLine()
        {
            // Endpoints in *Linear RGB*
            Vector3 left = new(1f, 0f, 0f); // pure red
            Vector3 right = new(0f, 1f, 0f); // pure green

            int stride = _wb.BackBufferStride; // BGRA32 stride
            byte[] pixels = new byte[stride * H];

            // Loop through each horizontal line
            for (int y = 0; y < H; y++)
            {
                // For each line, create a temporary array to hold the pixel data for that line
                byte[] linePixels = new byte[W * 4];

                // Loop through each pixel in the line to calculate its color
                for (int x = 0; x < W; x++)
                {
                    float t = (float)x / (W - 1);
                    Vector3 linear = Lerp(left, right, t);
                    int idx = x * 4; // Use a local index for the linePixels array
            
                    // Write the BGRA values to the line's buffer
                    WriteBgra(linePixels, idx, linear);
                }

                // Now, copy the entire line's data into the main pixel array
                Buffer.BlockCopy(linePixels, 0, pixels, y * stride, W * 4);
            }

            _wb.Lock();
            _wb.WritePixels(new Int32Rect(0, 0, W, H), pixels, stride, 0);
            _wb.AddDirtyRect(new Int32Rect(0, 0, W, H));
            _wb.Unlock();
        }
        
        
        //Pixel by Pixel
        void DrawGradientLinearRgbThenToSrgb() {
            Vector3 left  = new(1f, 0f, 0f); 
            Vector3 right = new(0f, 1f, 0f);

            int stride = _wb.BackBufferStride;
            Console.WriteLine($"Stride={stride}");
            byte[] pixels = new byte[stride * H];

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float t = (float)x / (W - 1);
                    Vector3 linear = Lerp(left, right, t);  // interpolation in Linear RGB (lab requirement)
                    int idx = y * stride + x * 4;
                    Console.WriteLine(idx);
                    WriteBgra(pixels, idx, linear);         // convert to sRGB 8-bit when writing
                }
            }

            _wb.Lock();
            _wb.WritePixels(new Int32Rect(0, 0, W, H), pixels, stride, 0);
            _wb.AddDirtyRect(new Int32Rect(0, 0, W, H));
            _wb.Unlock();
        }
    }
}