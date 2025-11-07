using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEditor.SpeedTree.Importer;
using UnityEngine;

[CreateAssetMenu(menuName = "Procedural Generation Method/New algo")]
public class newmethode : ProceduralGenerationMethod
{
    [Header("Room Parameters")]
    [SerializeField] private int _max_separ = 10;
    [NonSerialized] private BinearTree _tree;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        //cancellationToken.ThrowIfCancellationRequested();

        GenerateTree();

        foreach(var node  in _tree._all_leave)
        {
            PlaceRoom(node.CreateRoom()); 
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
        }


        foreach (var parent in _tree._all_parent)
        {
            if (parent.child1 != null && parent.child2 != null)
            {
                Vector2Int mid1 = parent.child1.GetMid();
                Vector2Int mid2 = parent.child2.GetMid();
                CreateDogLegCorridor(mid1, mid2); // tu feras cette fonction pour tracer un couloir entre 2 points
                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }
        }

        Debug.Log(_tree._all_parent[0].child1.GetMid().x+","+ _tree._all_parent[0].child1.GetMid().y+" : "+ _tree._all_parent[0].child2.GetMid().x+"," + _tree._all_parent[0].child2.GetMid().y);


    }
    private void PlaceRoom(RectInt room)
    {
        for (int ix = room.xMin; ix < room.xMax; ix++)
        {
            for (int iy = room.yMin; iy < room.yMax; iy++)
            {
                if (!Grid.TryGetCellByCoordinates(ix, iy, out var cell))
                    continue;

                AddTileToCell(cell, ROOM_TILE_NAME, true);
            }
        }
    }
    private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
    {
        bool horizontalFirst = RandomService.Chance(0.5f);

        if (horizontalFirst)
        {
            // Draw horizontal line first, then vertical
            CreateHorizontalCorridor(start.x, end.x, start.y);
            CreateVerticalCorridor(start.y, end.y, end.x);
        }
        else
        {
            // Draw vertical line first, then horizontal
            CreateVerticalCorridor(start.y, end.y, start.x);
            CreateHorizontalCorridor(start.x, end.x, end.y);
        }
    }
    private void CreateHorizontalCorridor(int x1, int x2, int y)
    {
        int xMin = Mathf.Min(x1, x2);
        int xMax = Mathf.Max(x1, x2);

        for (int x = xMin; x <= xMax; x++)
        {
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                continue;

            AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
        }
    }
    private void CreateVerticalCorridor(int y1, int y2, int x)
    {
        int yMin = Mathf.Min(y1, y2);
        int yMax = Mathf.Max(y1, y2);

        for (int y = yMin; y <= yMax; y++)
        {
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                continue;

            AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
        }
    }

    private void GenerateTree()
    {
        RectInt size = new RectInt(0,0, Grid.Width,Grid.Lenght);
        _tree = new BinearTree(_max_separ,size,RandomService);
    }
}
