using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace VTools.RandomService
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/nois generator")]
    public class noisGeneration : ProceduralGenerationMethod
    {
        [Header("general")]
        [SerializeField] FastNoiseLite.NoiseType _nois_type = FastNoiseLite.NoiseType.OpenSimplex2;
        [SerializeField] FastNoiseLite.RotationType3D _rota_3d = FastNoiseLite.RotationType3D.None;
        [SerializeField][Range(0, 100)] int _speed = 1;
        [SerializeField] [Range(0,2)] float _frequency = 0.5f;

        [Header ("fractale")]
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


        [Header("map parametre")]
        [SerializeField] float _water = -0.7f;
        [SerializeField] float _sand = -0.1f;
        [SerializeField] float _grass = 0.8f;
        [SerializeField] float _rock = 1.0f;


        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            // Create and configure FastNoise object
            FastNoiseLite noise = new FastNoiseLite();
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

            noise.SetNoiseType(_nois_type);
            if (_nois_type != FastNoiseLite.NoiseType.OpenSimplex2)
                noise.SetRotationType3D(_rota_3d);
            noise.SetSeed(_speed);
            noise.SetFrequency(_frequency);


            if(_fractal_type != FastNoiseLite.FractalType.None)
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
            float[,] noiseData = new float[Grid.Width, Grid.Lenght];

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    noiseData[x, y] = noise.GetNoise(x, y);
                }
            }
            const float _w = 0.15f;
            const float _s = 0.45f;
            const float _g = 0.80f;
            const float _r = 1.0f;

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, y, out var currentCell))
                        continue;
                        switch (noiseData[x, y]) 
                    {
                            //water
                        case < _w:
                            AddTileToCell(currentCell, WATER_TILE_NAME, true);
                            break;

                            //sand
                        case < _s:
                            AddTileToCell(currentCell, SAND_TILE_NAME, true);
                            break;

                            //grass
                        case < _g:
                            AddTileToCell(currentCell, GRASS_TILE_NAME, true);
                            break;

                            //rock
                        case < _r:
                            AddTileToCell(currentCell, ROCK_TILE_NAME, true);
                            break;


                        default:
                            AddTileToCell(currentCell, WATER_TILE_NAME, true);
                            break;
                    }

                }

                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }

        }
    }
}
