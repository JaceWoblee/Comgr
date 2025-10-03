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
        public Vector3 Diffusion; // Diffuse color (albedo)
        public Vector3 Emission; // Light emitted from the surface
        public Vector3 Specular; // Color of specular reflections
        public float SpecularChance; // 0 = purely diffuse, 1 = purely specular (mirror)
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
        private static readonly Random _random = new Random();

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

        // Liht path tracer (Recursive)
        private Vector3 ComputeColor(Ray ray, Scene scene) {
            var closestHit = new HitInfo { DidHit = false, Distance = float.MaxValue };
            foreach (var sphere in scene.Spheres) {
                var hit = sphere.Intersect(ray);
                if (hit.DidHit && hit.Distance < closestHit.Distance) closestHit = hit;
            }

            if (!closestHit.DidHit) return Vector3.Zero;

            var material = closestHit.Material;

            // Prevention from infin Loop
            float q = Math.Max(material.Diffusion.X, Math.Max(material.Diffusion.Y, material.Diffusion.Z));
            if (material.SpecularChance > 0) {
                q = Math.Max(q, Math.Max(material.Specular.X, Math.Max(material.Specular.Y, material.Specular.Z)));
            }

            if (_random.NextSingle() > q) {
                return material.Emission; // Path terminates
            }

            Vector3 reflectedColor;
            if (_random.NextSingle() < material.SpecularChance) {
                var reflectionDir = Vector3.Reflect(ray.Direction, closestHit.Normal);
                var newRay = new Ray
                    { Origin = closestHit.HitPoint + closestHit.Normal * 0.0001f, Direction = reflectionDir };

                reflectedColor = material.Specular * ComputeColor(newRay, scene);
            } else {
                var randomDir = GetRandomDirectionInHemisphere(closestHit.Normal);
                var newRay = new Ray
                    { Origin = closestHit.HitPoint + closestHit.Normal * 0.0001f, Direction = randomDir };

                reflectedColor = material.Diffusion * ComputeColor(newRay, scene);
            }

            if (q > 0) {
                reflectedColor /= q;
            }

            return material.Emission + reflectedColor;
        }

        // Helper function: generate a random direction in a hemisphere oriented by the normalVector
        Vector3 GetRandomDirectionInHemisphere(Vector3 normal) {
            // Random point in UnitSphere
            Vector3 randomPoint;
            do {
                randomPoint = new Vector3(
                    (float)_random.NextDouble() * 2.0f - 1.0f,
                    (float)_random.NextDouble() * 2.0f - 1.0f,
                    (float)_random.NextDouble() * 2.0f - 1.0f
                );
            } while (randomPoint.LengthSquared() >= 1.0f);

            Vector3 randomDirection = Vector3.Normalize(randomPoint);

            // Random vector needs to be in the same 180° as normalVector
            if (Vector3.Dot(randomDirection, normal) < 0) {
                return -randomDirection;
            }

            return randomDirection;
        }

        //Genereating scene with all the objects in it
        void RenderScene() {
            var scene = new Scene();
            //Wall Spheres
            scene.Spheres.Add(new Sphere { Center = new Vector3(-1001, 0, 0), Radius = 1000, Material = new Material { Diffusion = new Vector3(0.8f, 0.1f, 0.1f), Emission = new Vector3(0, 0, 0) } }); // Red
            scene.Spheres.Add(new Sphere { Center = new Vector3(1001, 0, 0), Radius = 1000, Material = new Material { Diffusion = new Vector3(0.1f, 0.1f, 0.8f), Emission = new Vector3(0, 0, 0) } }); // Blue
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, -1001, 0), Radius = 1000, Material = new Material { Diffusion = new Vector3(0.55f, 0.55f, 0.55f), Emission = new Vector3(0, 0, 0) } }); // Gray
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, 0, 1001), Radius = 1000, Material = new Material { Diffusion = new Vector3(0.55f, 0.55f, 0.55f), Emission = new Vector3(0, 0, 0) } }); // Gray
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, 1001.1f, 0), Radius = 1000, Material = new Material { Diffusion = new Vector3(0.1f, 0.1f, 0.1f), Emission = new Vector3(0, 0, 0) } }); // Black

            //Ball spheres
            scene.Spheres.Add(new Sphere { Center = new Vector3(-0.6f, -0.7f, -0.6f), Radius = 0.3f, Material = new Material { Diffusion = new Vector3(0.8f, 0.8f, 0.1f), Emission = new Vector3(0, 0, 0) } }); // Yellow
            scene.Spheres.Add(new Sphere { Center = new Vector3(0.3f, -0.4f, 0.3f), Radius = 0.6f, Material = new Material { Diffusion = new Vector3(0.1f, 0.8f, 0.8f), Emission = new Vector3(0, 0, 0) } }); // Cyan

            //Light: for round light (Center = new Vector3(0, 1.3f, 0), Radius = 0.4f) 
            //Light: for lit ceiling (Center = new Vector3(0, 1001, 0), Radius = 1000)
            scene.Spheres.Add(new Sphere { Center = new Vector3(0, 1001, 0), Radius = 1000, Material = new Material { Diffusion = new Vector3(0.8f, 0.8f, 0.8f), Emission = 4 * new Vector3(0.8f, 0.8f, 0.8f) } }); // White

            // -Define Camera normal angle-
            //Vector3 eye = new Vector3(-0.0f, -0.0f, -4f);
            //Vector3 lookAt = new Vector3(0, 0, 6);
            //float fov = 36;
            //Vector3 backgroundColor = new Vector3(0,0,0);

            // -Define Camera angle 2-
            Vector3 eye = new Vector3(-0.9f, -0.5f, 0.9f);
            Vector3 lookAt = new Vector3(0, 0, 0);
            float fov = 110;
            Vector3 backgroundColor = new Vector3(0, 0, 0);

            //Samplesize
            int samplesPerPixel = 4000;

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


            //Main Loop (paralell for efficiency)
            Parallel.For(0, H, y => {
                for (int x = 0; x < W; x++) {
                    Vector3 totalPixelColor = Vector3.Zero;

                    // Average multiple random paths per pixel(Monte Cralo)
                    for (int s = 0; s < samplesPerPixel; s++) {
                        float u = (x + (float)_random.NextDouble()) / (W - 1);
                        float v = (H - 1 - (y + (float)_random.NextDouble())) / (H - 1);

                        Ray ray = new Ray {
                            Origin = eye,
                            Direction = Vector3.Normalize(f + (u * 2.0f - 1.0f) * gamma / 2 * rVector + (v * 2.0f - 1.0f) * beta / 2 * upVector)
                        };

                        totalPixelColor += ComputeColor(ray, scene);
                    }

                    // Average the color + gamma correction
                    Vector3 linearColor = totalPixelColor / samplesPerPixel;

                    int idx = y * stride + x * 4;
                    WriteBgra(pixels, idx, linearColor);
                }
            });


            _wb.Lock();
            _wb.WritePixels(new Int32Rect(0, 0, W, H), pixels, stride, 0);
            _wb.AddDirtyRect(new Int32Rect(0, 0, W, H));
            _wb.Unlock();
        }

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
    }
}