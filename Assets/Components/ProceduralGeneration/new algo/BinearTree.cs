using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using VTools.RandomService;

public class BinearTree
{
    public List<List<Node>> _tree = new();
    public List<Node> _all_leave = new();
    public List<Node> _all_parent = new();

    public BinearTree(int separation_count, RectInt size, RandomService random)
    {
        _tree.Clear();
        _all_parent.Clear();
        _all_leave.Clear();

        Node root = new Node(null, size, random);
        _tree.Add(new List<Node> { root });

        for (int i = 1; i < separation_count; i++)
        {
            _tree.Add(new List<Node>());
            foreach (var parent in _tree[i - 1])
            {
                RectInt size1 = parent._size;
                RectInt size2 = parent._size;

                bool horizontal;

                float width = parent._size.width;
                float height = parent._size.height;
                float ratio = width / height;

                if (ratio > 1.5f)
                    horizontal = false;
                else if (ratio < 0.66f)
                    horizontal = true;
                else
                    horizontal = random.Chance(0.5f);

                if (horizontal)
                {
                    int pos = parent._size.yMin + (parent._size.height / 2) + random.Range(-2, 6);
                    size1.yMax = pos;
                    size2.yMin = pos;
                }
                else
                {
                    int pos = parent._size.xMin + (parent._size.width / 2) + random.Range(-2, 6);
                    size1.xMax = pos;
                    size2.xMin = pos;
                }

                Node child1 = new Node(parent, size1, random);
                Node child2 = new Node(parent, size2, random);

                parent.child1 = child1;
                parent.child2 = child2;

                _tree[i].Add(child1);
                _tree[i].Add(child2);

            }
        }

        // Remplir les leaves avec le dernier niveau
        foreach (var leaf in _tree[_tree.Count - 1])
            _all_leave.Add(leaf);

        foreach (var parent in _tree[_tree.Count - 2])
                _all_parent.Add(parent);
    }
}
