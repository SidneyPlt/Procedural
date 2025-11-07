using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using VTools.RandomService;

public class Node 
{
    public Node _parent;
    public RectInt _size;
    RandomService _rs;
    public RectInt _room;

    public Node child1;
    public Node child2;

    public Node(Node parent, RectInt size, RandomService rs)
    {
        _parent = parent;
        _size = size;
        _rs = rs;
    }

    public RectInt CreateRoom()
    {
        int marginXMin = _rs.Range(1, 4);
        int marginYMin = _rs.Range(1, 4);
        int marginXMax = _rs.Range(1, 4);
        int marginYMax = _rs.Range(1, 4);

        int xMin = _size.xMin + marginXMin;
        int yMin = _size.yMin + marginYMin;
        int xMax = _size.xMax - marginXMax;
        int yMax = _size.yMax - marginYMax;

        // Clamp pour éviter d'inverser les bornes
        if (xMax <= xMin + 1) xMax = xMin + 2;
        if (yMax <= yMin + 1) yMax = yMin + 2;

        RectInt room = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        _room = room;
        return room;
    }


    public Vector2Int GetMid()
    {
        Vector2Int pos = new Vector2Int((int)_size.center.x, (int)_size.center.y);
        return pos;
    }

}
