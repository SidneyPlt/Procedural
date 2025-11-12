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
        [SerializeField] GameObject _waterPrefab = null;
        [SerializeField] GameObject _sandPrefab = null;
        [SerializeField] GameObject _grassPrefab = null;
        [SerializeField] GameObject _dirtPrefab = null;
        [SerializeField] GameObject _arbre = null;
        [SerializeField] GameObject _herbe = null;
        [SerializeField] GameObject _Player = null;

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

        void fixedUpdate()
        {
            UpdateMap();
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
            bool isValidEmplacement = false;
            int[,] finalHeights = new int[Grid.Width, Grid.Lenght];
            GameObject[,] surfaceMaterials = new GameObject[Grid.Width, Grid.Lenght];
            bool[,] isLand = new bool[Grid.Width, Grid.Lenght];

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    float val = noiseData[x, y];
                    if (val > -1)
                    {
                        finalHeights[x, y] = (int)(val * 10);
                        surfaceMaterials[x, y] = val > -0.2 ? _grassPrefab : _sandPrefab;
                        isLand[x, y] = true;
                    }
                    else
                    {
                        finalHeights[x, y] = -999;
                        isLand[x, y] = false;
                    }
                }
            }

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    if (finalHeights[x, y] == -999) continue;

                    int surfaceHeight = finalHeights[x, y];
                    GameObject surfacePrefab = surfaceMaterials[x, y];

                    if (surfacePrefab == _grassPrefab)
                    {
                        GenerateVegetation(x, y, surfaceHeight);
                    }

                    Instantiate(surfacePrefab).transform.position = new Vector3(x, surfaceHeight, y);

                    int maxDifference = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + (i == 0 ? 1 : (i == 1 ? -1 : 0));
                        int ny = y + (i == 2 ? 1 : (i == 3 ? -1 : 0));

                        if (nx >= 0 && nx < Grid.Width && ny >= 0 && ny < Grid.Lenght && finalHeights[nx, ny] != -999)
                        {
                            int difference = surfaceHeight - finalHeights[nx, ny];
                            if (difference > maxDifference)
                            {
                                maxDifference = difference;
                            }
                        }
                    }

                    if (maxDifference > 1)
                    {
                        GameObject fillMaterial = surfacePrefab == _sandPrefab ? _sandPrefab : _dirtPrefab;

                        for (int height = surfaceHeight - 1; height > surfaceHeight - maxDifference; height--)
                        {
                            Instantiate(fillMaterial).transform.position = new Vector3(x, height, y);
                        }
                    }

                   
                }
            }

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {

                    if (surfaceMaterials[x, y] == _grassPrefab && isValidEmplacement == false && x > Grid.Width / 2 && y > Grid.Lenght / 2)
                    {
                        GameObject player = Instantiate(_Player);
                        player.transform.position = new Vector3(x, 20, y);
                        isValidEmplacement = true;
                    }

                    bool hasLandAtWaterLevel = isLand[x, y] && finalHeights[x, y] == -4;
                    bool hasLandBelow = isLand[x, y] && finalHeights[x, y] < -4;

                    if (!hasLandAtWaterLevel && hasLandBelow)
                    {
                        GameObject waterBlock = Instantiate(_waterPrefab);
                        waterBlock.transform.position = new Vector3(x, -3.6f, y);
                    }
                }  
            }
        }

        private void GenerateVegetation(int x, int y, int surfaceHeight)
        {
            int chanceArbre = RandomService.Range(1, 32);
            if (chanceArbre == 1)
            {
                GameObject arbre = Instantiate(_arbre);
                arbre.transform.position = new Vector3(x, surfaceHeight + 0.8f, y);
                return; 
            }

            int chanceherbe = RandomService.Range(1, 10);
            if (chanceherbe == 1)
            {
                GameObject herbe = Instantiate(_herbe);
                herbe.transform.position = new Vector3(x, surfaceHeight + 0.5f, y);
                herbe.transform.localScale = new Vector3(2.5f, 2.2f, 2.5f);
                herbe.transform.rotation = Quaternion.Euler(0, RandomService.Range(0, 359), 0);
            }
            
        }

        void UpdateMap() 
        {

        }

    }
}
