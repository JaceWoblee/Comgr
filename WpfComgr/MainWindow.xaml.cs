using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace WpfComgr {

    //Ray
    public struct Ray {
        public Vector3 Origin;
        public Vector3 Direction;
    }

    //Color for the Vectors
    public struct Material {
        public Vector3 Color;
    }

    //Helper class to calculate rays
    public struct HitInfo {
        public bool DidHit;
        public float Distance;
        public Vector3 HitPoint;
        public Vector3 Normal;
        public Material Material;
    }

    //Object in 3d space (Not just spheres also rectangles and stuff)
    public class Sphere {
        public Vector3 Center;
        public float Radius;
        public Material Material;

        //Looks if the given ray hits the current sphere
        public HitInfo Intersect(Ray ray) {
            Vector3 oc = ray.Origin - Center;
            float a = Vector3.Dot(ray.Direction, ray.Direction);
            float b = 2.0f * Vector3.Dot(oc, ray.Direction);
            float c = Vector3.Dot(oc, oc) - Radius * Radius;
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0) {
                return new HitInfo { DidHit = false }; // If no hit
            }
            
            float t = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
            // Only camera hits (t > 0)
            if (t < 0.001f) {
                t = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
                if (t < 0.001f) return new HitInfo { DidHit = false };
            }

            Vector3 hitPoint = ray.Origin + t * ray.Direction;
            return new HitInfo {
                DidHit = true,
                Distance = t,
                HitPoint = hitPoint,
                Normal = Vector3.Normalize(hitPoint - Center),
                Material = this.Material
            };
        }
    }

    //3D Room with all spheres
    public class Scene {
        public List<Sphere> Spheres = new List<Sphere>();
    }


    public partial class MainWindow : Window {
        const int W = 512, H = 512;
        private WriteableBitmap _wb;

        public MainWindow() {
            InitializeComponent();
            Image canvasImage = new Image();
            this.Content = canvasImage;
            _wb = new WriteableBitmap(W, H, 96, 96, PixelFormats.Bgra32, null);
            canvasImage.Source = _wb;

            // Rendreing process
            RenderScene();
        }
        
        //Genereating scene with all the objects in it
        void RenderScene() {
            var scene = new Scene();
            //Adding Spheres
            scene.Spheres.Add(new Sphere { Center = new Vector3(-1001, 0, 0), Radius = 1000, Material = new Material { Color = new Vector3(0.8f, 0.1f, 0.1f) } }); // Red
            scene.Spheres.Add(new Sphere { Center = new Vector3(1001, 0, 0), Radius = 1000, Material = new Material { Color = new Vector3(0.1f, 0.1f, 0.8f) } }); // Blue
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, 1001, 0), Radius = 1000, Material = new Material { Color = new Vector3(0.8f, 0.8f, 0.8f) } }); // White
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, -1001, 0), Radius = 1000, Material = new Material { Color = new Vector3(0.55f, 0.55f, 0.55f) } }); // Gray
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, 0, 1001), Radius = 1000, Material = new Material { Color = new Vector3(0.55f, 0.55f, 0.55f) } }); // Gray

            scene.Spheres.Add(new Sphere { Center = new Vector3(-0.6f, -0.7f, -0.6f), Radius = 0.3f, Material = new Material { Color = new Vector3(0.9f, 0.9f, 0.1f) } }); // Yellow
            scene.Spheres.Add(new Sphere { Center = new Vector3(0.3f, -0.4f, 0.3f), Radius = 0.6f, Material = new Material { Color = new Vector3(0.1f, 0.9f, 0.9f) } }); // Cyan
            
            // Define Camera
            Vector3 eye = new Vector3(-0.9f, -0.5f, 0.9f);
            Vector3 lookAt = new Vector3(0, 0, 0);
            float fov = 110;
            Vector3 backgroundColor = new Vector3(0,0,0);

            int stride = _wb.BackBufferStride;
            byte[] pixels = new byte[stride * H];
            
            // Setup for camera
            float aspectRatio = (float)W / H; 
            float fovRad = MathF.PI / 180.0f * fov;
            float beta = 2.0f * MathF.Tan(fovRad / 2.0f);
            float gamma = beta * aspectRatio;

            Vector3 f = Vector3.Normalize(lookAt - eye);
            Vector3 rVector = Vector3.Normalize(Vector3.Cross(new Vector3(0, 1, 0), f));
            Vector3 upVector = Vector3.Cross(f, rVector);

            
            for (int y = 0; y < H; y++) {
                for (int x = 0; x < W; x++) {
                    // Creates Ray for this pixel
                    float u = (x / (float)(W - 1)) * 2.0f - 1.0f; // -1 bis 1
                    float v = ((H - 1 - y) / (float)(H - 1)) * 2.0f - 1.0f; // -1 bis 1 (y reversed)
                    
                    Ray dRay = new Ray { 
                        Origin = eye,
                        Direction = Vector3.Normalize(f + (u * gamma/2 * rVector) + (v * beta/2 * upVector))
                    };
                    
                    HitInfo closestHit = new HitInfo { DidHit = false, Distance = float.MaxValue };
                    foreach(var sphere in scene.Spheres) {
                        //Searches for all the spheres that get hit by this ray
                        HitInfo H = sphere.Intersect(dRay);
                        //Searches the nearest Sphrere in front of Camrea
                        if(H.DidHit && H.Distance < closestHit.Distance) {
                            closestHit = H;
                        }
                    }

                    // Color computing
                    Vector3 linearColor = closestHit.DidHit ? closestHit.Material.Color : backgroundColor;
                    
                    // Writes pixel in buffer WriteBgra
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
        static byte ToSrgb8(float linear) {
            if (linear <= 0f) return 0;
            if (linear >= 1f) return 255;
            float srgb = linear <= 0.0031308f ? 12.92f * linear : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;
            return (byte)Math.Clamp((int)MathF.Round(srgb * 255f), 0, 255);
        }

        static void WriteBgra(byte[] buf, int idx, Vector3 linearRgb) {
            buf[idx + 0] = ToSrgb8(linearRgb.Z); // B
            buf[idx + 1] = ToSrgb8(linearRgb.Y); // G
            buf[idx + 2] = ToSrgb8(linearRgb.X); // R
            buf[idx + 3] = 255; // A
        }
        #endregion
    }
}