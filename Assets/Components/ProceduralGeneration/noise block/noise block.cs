using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;


namespace VTools.RandomService
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/nois generator 3d block")]
    public class noiseblock : ProceduralGenerationMethod
    {
        float[,] noiseData;
        [Header("map param")]
        [SerializeField][Range(0, 50)] int _accentuation_denivler = 2;
        [SerializeField] GameObject _prefab = null;
        [NonSerialized] GameObject[] _map_block_list = null;


        [Header("general")]
        [SerializeField] FastNoiseLite.NoiseType _nois_type = FastNoiseLite.NoiseType.OpenSimplex2;
        [SerializeField] FastNoiseLite.RotationType3D _rota_3d = FastNoiseLite.RotationType3D.None;
        [SerializeField][Range(0, 100)] int _speed = 1;
        [SerializeField][Range(0, 2)] float _frequency = 0.025f;

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

            GenerateMap();

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

        private void GenerateMap()
        {

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    float val = noiseData[x, y];
                    GameObject newBlock = Instantiate(_prefab);
                    newBlock.transform.localScale = new Vector3(1, val*_accentuation_denivler, 1);
                    newBlock.transform.position = new Vector3(x, (val * _accentuation_denivler) /2, y);
                    newBlock.GetComponent<Renderer>().material.color = _gradient.Evaluate(val);
                }
            }
        }
    }
}
