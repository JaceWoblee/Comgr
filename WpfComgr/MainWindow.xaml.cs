using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace WpfComgr
{
    // --- DATENSTRUKTUREN FÜR RAY TRACING ---

    /// <summary>
    /// Repräsentiert einen Strahl im 3D-Raum.
    /// </summary>
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;
    }

    /// <summary>
    /// Definiert die Oberflächeneigenschaften eines Objekts.
    /// Vorerst nur die Farbe (im linearen RGB-Raum).
    /// </summary>
    public struct Material
    {
        public Vector3 Color;
    }

    /// <summary>
    /// Speichert Informationen über einen Ray-Objekt-Schnittpunkt.
    /// </summary>
    public struct HitInfo
    {
        public bool DidHit;
        public float Distance;
        public Vector3 HitPoint;
        public Vector3 Normal;
        public Material Material;
    }

    /// <summary>
    /// Repräsentiert eine Kugel in der 3D-Szene.
    /// </summary>
    public class Sphere
    {
        public Vector3 Center;
        public float Radius;
        public Material Material;

        /// <summary>
        /// Berechnet den Schnittpunkt eines Strahls mit dieser Kugel.
        /// </summary>
        /// <param name="ray">Der Strahl, der getestet wird.</param>
        /// <returns>HitInfo mit den Details des Treffers.</returns>
        public HitInfo Intersect(Ray ray)
        {
            Vector3 oc = ray.Origin - Center;
            float a = Vector3.Dot(ray.Direction, ray.Direction);
            float b = 2.0f * Vector3.Dot(oc, ray.Direction);
            float c = Vector3.Dot(oc, oc) - Radius * Radius;
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                return new HitInfo { DidHit = false }; // Kein Treffer
            }
            
            float t = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
            // Only camera hits (t > 0)
            if (t < 0.001f) {
                t = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
                if (t < 0.001f) return new HitInfo { DidHit = false };
            }

            Vector3 hitPoint = ray.Origin + t * ray.Direction;
            return new HitInfo
            {
                DidHit = true,
                Distance = t,
                HitPoint = hitPoint,
                Normal = Vector3.Normalize(hitPoint - Center),
                Material = this.Material
            };
        }
    }

    /// <summary>
    /// Enthält alle Objekte der Szene.
    /// </summary>
    public class Scene
    {
        public List<Sphere> Spheres = new List<Sphere>();
    }


    public partial class MainWindow : Window
    {
        const int W = 512, H = 512;
        private WriteableBitmap _wb;

        public MainWindow()
        {
            InitializeComponent();
            Image canvasImage = new Image();
            this.Content = canvasImage;
            _wb = new WriteableBitmap(W, H, 96, 96, PixelFormats.Bgra32, null);
            canvasImage.Source = _wb;

            // Rendreing process
            RenderScene();
        }
        
        /// <summary>
        /// Die Hauptfunktion, die die Szene aufbaut und rendert.
        /// </summary>
        void RenderScene()
        {
            var scene = new Scene();
            //Adding Spheres
            scene.Spheres.Add(new Sphere { Center = new Vector3(-1001, 0, 0), Radius = 1000, Material = new Material { Color = new Vector3(0.8f, 0.1f, 0.1f) } }); // Left Red
            scene.Spheres.Add(new Sphere { Center = new Vector3(1001, 0, 0), Radius = 1000, Material = new Material { Color = new Vector3(0.1f, 0.1f, 0.8f) } }); // Right Blue
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, 1001, 0), Radius = 1000, Material = new Material { Color = new Vector3(0.8f, 0.8f, 0.8f) } }); // Top White
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, -1001, 0), Radius = 1000, Material = new Material { Color = new Vector3(0.6f, 0.6f, 0.6f) } }); // Bottom Gray
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, 0, 1001), Radius = 1000, Material = new Material { Color = new Vector3(0.6f, 0.6f, 0.6f) } }); // Back Gray

            scene.Spheres.Add(new Sphere { Center = new Vector3(-0.6f, -0.7f, -0.6f), Radius = 0.3f, Material = new Material { Color = new Vector3(0.9f, 0.9f, 0.1f) } }); // Gelbe Kugel
            scene.Spheres.Add(new Sphere { Center = new Vector3(0.3f, -0.4f, 0.3f), Radius = 0.6f, Material = new Material { Color = new Vector3(0.1f, 0.9f, 0.9f) } }); // Cyan Kugel
            
            // 2. Define Camera
            Vector3 eye = new Vector3(0, 0, -4);
            Vector3 lookAt = new Vector3(0, 0, 6);
            float fov = 36.0f;
            Vector3 backgroundColor = new Vector3(0,0,0); // Schwarzer Hintergrund

            int stride = _wb.BackBufferStride;
            byte[] pixels = new byte[stride * H];
            
            // Kamera-Setup für die Strahlenberechnung
            float aspectRatio = (float)W / H;
            float fovRad = MathF.PI / 180.0f * fov;
            float viewportHeight = 2.0f * MathF.Tan(fovRad / 2.0f);
            float viewportWidth = viewportHeight * aspectRatio;

            Vector3 forward = Vector3.Normalize(lookAt - eye);
            Vector3 right = Vector3.Normalize(Vector3.Cross(new Vector3(0, 1, 0), forward));
            Vector3 up = Vector3.Cross(forward, right);

            // 3. Durch jeden Pixel iterieren
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    // a. Strahl für diesen Pixel erstellen (CreateEyeRay)
                    float u = (x / (float)(W - 1)) * 2.0f - 1.0f; // von -1 bis 1
                    float v = ((H - 1 - y) / (float)(H - 1)) * 2.0f - 1.0f; // von -1 bis 1 (y umkehren)
                    
                    Ray ray = new Ray
                    {
                        Origin = eye,
                        Direction = Vector3.Normalize(forward + u * viewportWidth/2 * right + v * viewportHeight/2 * up)
                    };
                    
                    // b. Szene nach Treffern durchsuchen (FindClosestHitPoint)
                    HitInfo closestHit = new HitInfo { DidHit = false, Distance = float.MaxValue };
                    foreach(var sphere in scene.Spheres)
                    {
                        HitInfo currentHit = sphere.Intersect(ray);
                        if(currentHit.DidHit && currentHit.Distance < closestHit.Distance)
                        {
                            closestHit = currentHit;
                        }
                    }

                    // c. Farbe berechnen (ComputeColor)
                    Vector3 linearColor = closestHit.DidHit ? closestHit.Material.Color : backgroundColor;

                    // d. Pixel in den Puffer schreiben
                    int idx = y * stride + x * 4;
                    WriteBgra(pixels, idx, linearColor);
                }
            }

            _wb.Lock();
            _wb.WritePixels(new Int32Rect(0, 0, W, H), pixels, stride, 0);
            _wb.AddDirtyRect(new Int32Rect(0, 0, W, H));
            _wb.Unlock();
        }

        #region Hilfsfunktionen für Farbe und Pixel
        static byte ToSrgb8(float linear)
        {
            if (linear <= 0f) return 0;
            if (linear >= 1f) return 255;
            float srgb = linear <= 0.0031308f ? 12.92f * linear : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;
            return (byte)Math.Clamp((int)MathF.Round(srgb * 255f), 0, 255);
        }

        static void WriteBgra(byte[] buf, int idx, Vector3 linearRgb)
        {
            buf[idx + 0] = ToSrgb8(linearRgb.Z); // B
            buf[idx + 1] = ToSrgb8(linearRgb.Y); // G
            buf[idx + 2] = ToSrgb8(linearRgb.X); // R
            buf[idx + 3] = 255; // A
        }
        #endregion
    }
}