using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace VTools.RandomService
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/nois generator 3d")]
    public class noise3d : ProceduralGenerationMethod
    {
        float[,] noiseData;
        [Header("map param")]
        [SerializeField][Range(0, 50)] int _accentuation_denivler = 20;


        [Header("general")]
        [SerializeField] FastNoiseLite.NoiseType _nois_type = FastNoiseLite.NoiseType.OpenSimplex2;
        [SerializeField] FastNoiseLite.RotationType3D _rota_3d = FastNoiseLite.RotationType3D.None;
        [SerializeField][Range(0, 100)] int _speed = 1;
        [SerializeField][Range(0, 2)] float _frequency = 0.5f;

        [Header("fractale")]
        [SerializeField] FastNoiseLite.FractalType _fractal_type = FastNoiseLite.FractalType.None;
        [SerializeField] int _octave = 3;
        [SerializeField] float _lacunarity = 2.0f;
        [SerializeField] float _gain = 0.5f;
        [SerializeField] float _weigther = 0.0f;
        [SerializeField] float _ping_pong = 2.0f;

        [Header("cellular")]
        [SerializeField] FastNoiseLite.CellularDistanceFunction _distance_function = FastNoiseLite.CellularDistanceFunction.Euclidean;
        [SerializeField] FastNoiseLite.CellularReturnType _return_type = FastNoiseLite.CellularReturnType.Distance;
        [SerializeField] float _jiter = 1.0f;

        [SerializeField] Vertex[] _vertices_list;
        [SerializeField] public Gradient _gradient = new();


        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            GenerateNoise();
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);

            GenerateMesh();

        }
        private void GenerateNoise()
        {
            FastNoiseLite noise = new FastNoiseLite();
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

            noise.SetNoiseType(_nois_type);
            if (_nois_type != FastNoiseLite.NoiseType.OpenSimplex2)
                noise.SetRotationType3D(_rota_3d);
            noise.SetSeed(_speed);
            noise.SetFrequency(_frequency);


            if (_fractal_type != FastNoiseLite.FractalType.None)
            {
                noise.SetFractalOctaves(_octave);
                noise.SetFractalLacunarity(_lacunarity);
                noise.SetFractalGain(_gain);
                noise.SetFractalWeightedStrength(_weigther);
                if (_fractal_type == FastNoiseLite.FractalType.PingPong)
                    noise.SetFractalPingPongStrength(_ping_pong);
            }

            if (_nois_type == FastNoiseLite.NoiseType.Cellular)
            {
                noise.SetCellularDistanceFunction(_distance_function);
                noise.SetCellularReturnType(_return_type);
                noise.SetCellularJitter(_jiter);
            }

            // Gather noise data
            noiseData = new float[Grid.Width, Grid.Lenght];

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    noiseData[x, y] = noise.GetNoise(x, y);
                }
            }
        }

        private void GenerateMesh()
        {
            _vertices_list = new Vertex[Grid.Width * Grid.Lenght];
            Color[] colors = new Color[_vertices_list.Length];

            // ensuite on crée les sommets + couleur
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    int index = y * Grid.Width + x;

                    Vertex v = new Vertex();
                    float height = noiseData[x, y];
                    v.position = new UnityEngine.Vector3(x, height*_accentuation_denivler, y);

                    _vertices_list[index] = v;
                    // normalise la valeur entre 0 et 1
                    colors[index] = _gradient.Evaluate(height);
                }
            }

            List<int> triangles = new List<int>();
            for (int y = 0; y < Grid.Lenght - 1; y++)
            {
                for (int x = 0; x < Grid.Width - 1; x++)
                {
                    int i = y * Grid.Width + x;

                    triangles.Add(i);
                    triangles.Add(i + Grid.Width);
                    triangles.Add(i + 1);

                    triangles.Add(i + 1);
                    triangles.Add(i + Grid.Width);
                    triangles.Add(i + Grid.Width + 1);
                }
            }

            // création du mesh
            Mesh mesh = new Mesh();
            UnityEngine.Vector3[] vertices = new UnityEngine.Vector3[_vertices_list.Length];
            for (int i = 0; i < _vertices_list.Length; i++)
                vertices[i] = _vertices_list[i].position;

            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors; // on ajoute la couleur par vertex
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // objet dans la scène
            var go = new GameObject("GeneratedMesh");
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mf.mesh = mesh; 
            mr.material.EnableKeyword("_VERTEX_COLOR");


            // nouveau matériau qui utilise la couleur des vertices
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetFloat("_SurfaceType", 0); // opaque
            mat.SetFloat("_BlendMode", 0);
            mat.SetColor("_BaseColor", Color.white);
            mat.enableInstancing = true;
            mr.material = mat;

        }
    }
}
