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
        [SerializeField] int _viewDistance = 10;

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

        private Dictionary<Vector3Int, GameObject> _allBlocks = new Dictionary<Vector3Int, GameObject>();
        private Dictionary<Vector2Int, List<GameObject>> _vegetation = new Dictionary<Vector2Int, List<GameObject>>();
        private GameObject _playerInstance;
        private Vector2Int _lastPlayerPos;

        // Données précalculées
        private int[,] _finalHeights;
        private GameObject[,] _surfaceMaterials;
        private bool[,] _isLand;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            GenerateNoise();
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);

            PrecalculateMapData();
            GenerateInitialMap();

            // Démarrer la mise à jour
            StartMapUpdate();
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

        private void PrecalculateMapData()
        {
            _finalHeights = new int[Grid.Width, Grid.Lenght];
            _surfaceMaterials = new GameObject[Grid.Width, Grid.Lenght];
            _isLand = new bool[Grid.Width, Grid.Lenght];

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Lenght; y++)
                {
                    float val = noiseData[x, y];
                    if (val > -1)
                    {
                        _finalHeights[x, y] = (int)(val * 10);
                        _surfaceMaterials[x, y] = val > -0.2 ? _grassPrefab : _sandPrefab;
                        _isLand[x, y] = true;
                    }
                    else
                    {
                        _finalHeights[x, y] = -999;
                        _isLand[x, y] = false;
                    }
                }
            }
        }

        private void GenerateInitialMap()
        {
            Vector2Int center = new Vector2Int(Grid.Width / 2, Grid.Lenght / 2);
            GenerateMapArea(center.x - _viewDistance, center.x + _viewDistance,
                           center.y - _viewDistance, center.y + _viewDistance, true);
        }

        private async void StartMapUpdate()
        {
            while (true)
            {
                await UniTask.Delay(100);
                if (_playerInstance != null)
                {
                    Vector2Int currentPos = new Vector2Int(
                        Mathf.RoundToInt(_playerInstance.transform.position.x),
                        Mathf.RoundToInt(_playerInstance.transform.position.z)
                    );

                    if (Vector2Int.Distance(currentPos, _lastPlayerPos) > 1f)
                    {
                        UpdateMapAroundPlayer(currentPos);
                        _lastPlayerPos = currentPos;
                    }
                }
            }
        }

        private void UpdateMapAroundPlayer(Vector2Int playerPos)
        {
            // Montrer/cacher les blocs autour du joueur
            int startX = Mathf.Clamp(playerPos.x - _viewDistance, 0, Grid.Width - 1);
            int endX = Mathf.Clamp(playerPos.x + _viewDistance, 0, Grid.Width - 1);
            int startY = Mathf.Clamp(playerPos.y - _viewDistance, 0, Grid.Lenght - 1);
            int endY = Mathf.Clamp(playerPos.y + _viewDistance, 0, Grid.Lenght - 1);

            // Générer les nouveaux blocs
            GenerateMapArea(startX, endX, startY, endY, false);

            // Cacher les blocs loin du joueur
            HideDistantBlocks(playerPos);
        }

        private void GenerateMapArea(int startX, int endX, int startY, int endY, bool isInitial)
        {
            // Première passe : blocs de surface et remplissage
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    if (_finalHeights[x, y] == -999) continue;

                    int surfaceHeight = _finalHeights[x, y];
                    GameObject surfacePrefab = _surfaceMaterials[x, y];

                    // Bloc de surface
                    Vector3Int surfacePos = new Vector3Int(x, surfaceHeight, y);
                    if (!_allBlocks.ContainsKey(surfacePos))
                    {
                        GameObject block = Instantiate(surfacePrefab);
                        block.transform.position = new Vector3(x, surfaceHeight, y);
                        _allBlocks[surfacePos] = block;

                        // Végétation
                        if (surfacePrefab == _grassPrefab)
                        {
                            GenerateVegetation(x, y, surfaceHeight);
                        }
                    }
                    else
                    {
                        _allBlocks[surfacePos].SetActive(true);
                    }

                    // Blocs de remplissage pour les pentes
                    int maxDifference = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + (i == 0 ? 1 : (i == 1 ? -1 : 0));
                        int ny = y + (i == 2 ? 1 : (i == 3 ? -1 : 0));

                        if (nx >= 0 && nx < Grid.Width && ny >= 0 && ny < Grid.Lenght && _finalHeights[nx, ny] != -999)
                        {
                            int difference = surfaceHeight - _finalHeights[nx, ny];
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
                            Vector3Int fillPos = new Vector3Int(x, height, y);
                            if (!_allBlocks.ContainsKey(fillPos))
                            {
                                GameObject fillBlock = Instantiate(fillMaterial);
                                fillBlock.transform.position = new Vector3(x, height, y);
                                _allBlocks[fillPos] = fillBlock;
                            }
                            else
                            {
                                _allBlocks[fillPos].SetActive(true);
                            }
                        }
                    }
                }
            }

            // Deuxième passe : eau et joueur
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    // Joueur (seulement au début)
                    if (isInitial && _surfaceMaterials[x, y] == _grassPrefab && x > Grid.Width / 2 && y > Grid.Lenght / 2 && _playerInstance == null)
                    {
                        _playerInstance = Instantiate(_Player);
                        _playerInstance.transform.position = new Vector3(x, 20, y);
                        _lastPlayerPos = new Vector2Int(x, y);
                    }

                    // Eau
                    bool hasLandAtWaterLevel = _isLand[x, y] && _finalHeights[x, y] == -4;
                    bool hasLandBelow = _isLand[x, y] && _finalHeights[x, y] < -4;

                    if (!hasLandAtWaterLevel && hasLandBelow)
                    {
                        Vector3Int waterPos = new Vector3Int(x, -4, y); // Position fixe pour l'eau
                        if (!_allBlocks.ContainsKey(waterPos))
                        {
                            GameObject waterBlock = Instantiate(_waterPrefab);
                            waterBlock.transform.position = new Vector3(x, -3.6f, y);
                            _allBlocks[waterPos] = waterBlock;
                        }
                        else
                        {
                            _allBlocks[waterPos].SetActive(true);
                        }
                    }
                }
            }
        }

        private void HideDistantBlocks(Vector2Int playerPos)
        {
            foreach (var block in _allBlocks)
            {
                Vector3Int blockPos = block.Key;
                float distance = Vector2Int.Distance(playerPos, new Vector2Int(blockPos.x, blockPos.z));

                if (distance > _viewDistance)
                {
                    block.Value.SetActive(false);
                }
            }

            // Cacher aussi la végétation
            foreach (var vegList in _vegetation.Values)
            {
                foreach (var plant in vegList)
                {
                    Vector2Int plantPos = new Vector2Int(
                        Mathf.RoundToInt(plant.transform.position.x),
                        Mathf.RoundToInt(plant.transform.position.z)
                    );
                    float distance = Vector2Int.Distance(playerPos, plantPos);
                    plant.SetActive(distance <= _viewDistance);
                }
            }
        }

        private void GenerateVegetation(int x, int y, int surfaceHeight)
        {
            Vector2Int pos = new Vector2Int(x, y);
            if (!_vegetation.ContainsKey(pos))
            {
                _vegetation[pos] = new List<GameObject>();
            }

            int chanceArbre = UnityEngine.Random.Range(1, 32);
            if (chanceArbre == 1)
            {
                GameObject arbre = Instantiate(_arbre);
                arbre.transform.position = new Vector3(x, surfaceHeight + 0.8f, y);
                _vegetation[pos].Add(arbre);
                return;
            }

            int chanceherbe = UnityEngine.Random.Range(1, 10);
            if (chanceherbe == 1)
            {
                GameObject herbe = Instantiate(_herbe);
                herbe.transform.position = new Vector3(x, surfaceHeight + 0.5f, y);
                herbe.transform.localScale = new Vector3(2.5f, 2.2f, 2.5f);
                herbe.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 359), 0);
                _vegetation[pos].Add(herbe);
            }
        }
    }
}