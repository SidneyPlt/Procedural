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
            GameObject Water = Instantiate(_waterPrefab);
            Water.transform.localScale = new Vector3(Grid.Width, 1, Grid.Lenght);
            Water.transform.position = new Vector3(Grid.Width / 2, -4, Grid.Lenght / 2);

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    float val = noiseData[x, y];

                    if (val > -0.4)
                    {
                        int valInt = (int)(val * 10);
                        GameObject blockPrefab = val > -0.2 ? _grassPrefab : _sandPrefab;

                        int chanceArbre = RandomService.Range(1, 32);
                        if (blockPrefab == _grassPrefab && chanceArbre == 1)
                        {
                            float posArbre = ((float)valInt+1) - 0.2f;
                            Instantiate(_arbre);
                            _arbre.transform.localPosition = new Vector3(x, posArbre, y);
                            _herbe.transform.localScale = new Vector3(1, 2.5f, 1);
                        }
                        else if(blockPrefab == _grassPrefab && chanceArbre != 1)
                        {
                            int chanceherbe = RandomService.Range(1, 10);
                            if (chanceherbe == 1)
                            {
                                float posherbe = ((float)valInt) + 0.5f;
                                Instantiate(_herbe);
                                _herbe.transform.localPosition = new Vector3(x, posherbe, y);
                                _herbe.transform.localScale = new Vector3(2.5f, 2.2f, 2.5f);
                                _herbe.transform.localRotation = new Quaternion(0, RandomService.Range(0, 359), 0, 0);
                            }
                            
                        }

                        for (int height = valInt; height >= valInt - 1; height--)
                        {
                            GameObject newBlock = Instantiate(blockPrefab);
                            newBlock.transform.position = new Vector3(x, height, y);
                        }
                    }
                }
            }
        }
    }
}
