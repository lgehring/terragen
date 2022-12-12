using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///     Quadtree implementation, adapted from https://gist.github.com/MohHeader/270acd1224e35b89e9f411785ba43562
/// </summary>
/*
Quadtree by Just a Pixel (Danny Goodayle) - http://www.justapixel.co.uk
Copyright (c) 2015
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

//Any object that you insert into the tree must implement this interface
public interface IQuadTreeObject
{
    Vector2 GetPosition();
}

public class QuadTree<T> where T : IQuadTreeObject
{
    private readonly int _mMaxObjectCount;
    private readonly List<T> _mStoredObjects;
    private List<T> _cellObjects;
    private QuadTree<T>[] _cells;
    private Rect _mBounds;

    // Cache for GC Alloc.
    private List<T> _returnedObjects;

    public QuadTree(int maxSize, Rect bounds)
    {
        _mBounds = bounds;
        _mMaxObjectCount = maxSize;
        _cells = new QuadTree<T>[4];
        _mStoredObjects = new List<T>(maxSize);
    }

    public List<T> GetAllElements()
    {
        var elements = new List<T>();

        // Add the elements stored in this node
        elements.AddRange(_mStoredObjects);

        // Recursively add the elements stored in each child node
        if (_cells[0] == null) return elements;
        for (var i = 0; i < 4; i++) elements.AddRange(_cells[i].GetAllElements());

        return elements;
    }


    public void Insert(T objectToInsert)
    {
        if (_cells[0] != null)
        {
            var iCell = GetCellToInsertObject(objectToInsert.GetPosition());
            if (iCell > -1) _cells[iCell].Insert(objectToInsert);

            return;
        }

        _mStoredObjects.Add(objectToInsert);
        //Objects exceed the maximum count
        if (_mStoredObjects.Count <= _mMaxObjectCount) return;
        {
            //Split the quad into 4 sections
            if (_cells[0] == null)
            {
                var subWidth = _mBounds.width / 2f;
                var subHeight = _mBounds.height / 2f;
                var x = _mBounds.x;
                var y = _mBounds.y;
                _cells[0] = new QuadTree<T>(_mMaxObjectCount, new Rect(x + subWidth, y, subWidth, subHeight));
                _cells[1] = new QuadTree<T>(_mMaxObjectCount, new Rect(x, y, subWidth, subHeight));
                _cells[2] = new QuadTree<T>(_mMaxObjectCount, new Rect(x, y + subHeight, subWidth, subHeight));
                _cells[3] = new QuadTree<T>(_mMaxObjectCount,
                    new Rect(x + subWidth, y + subHeight, subWidth, subHeight));
            }

            //Reallocate this quads objects into its children
            var i = _mStoredObjects.Count - 1;
            while (i >= 0)
            {
                var storedObj = _mStoredObjects[i];
                var iCell = GetCellToInsertObject(storedObj.GetPosition());
                if (iCell > -1) _cells[iCell].Insert(storedObj);

                _mStoredObjects.RemoveAt(i);
                i--;
            }
        }
    }

    public void Remove(T objectToRemove)
    {
        if (!ContainsLocation(objectToRemove.GetPosition())) return;
        _mStoredObjects.Remove(objectToRemove);
        if (_cells[0] == null) return;
        for (var i = 0; i < 4; i++)
            _cells[i].Remove(objectToRemove);
    }

    public void Prune()
    {
        // Recursively prune child nodes
        foreach (var t in _cells) t?.Prune();

        // If this node has no objects and no child nodes, remove it
        if (_mStoredObjects.Count == 0 && _cells[0] == null) _cells = null;
    }

    public List<T> RetrieveObjectsInArea(Rect area)
    {
        _returnedObjects ??= new List<T>();

        _returnedObjects.Clear();

        if (!RectOverlap(_mBounds, area)) return _returnedObjects;
        foreach (var t in _mStoredObjects.Where(t => t != null && area.Contains(t.GetPosition())))
            _returnedObjects.Add(t);

        if (_cells[0] == null) return _returnedObjects;
        for (var i = 0; i < 4; i++)
            _cells[i].RetrieveObjectsInAreaNoAlloc(area, ref _returnedObjects);

        return _returnedObjects;
    }

    private void RetrieveObjectsInAreaNoAlloc(Rect area, ref List<T> results)
    {
        if (!RectOverlap(_mBounds, area)) return;
        results.AddRange(_mStoredObjects.Where(t => t != null && area.Contains(t.GetPosition())));

        if (_cells[0] == null) return;
        for (var i = 0; i < 4; i++)
            _cells[i].RetrieveObjectsInAreaNoAlloc(area, ref results);
    }

    // Clear quadtree
    public void Clear()
    {
        _mStoredObjects.Clear();

        for (var i = 0; i < _cells.Length; i++)
            if (_cells[i] != null)
            {
                _cells[i].Clear();
                _cells[i] = null;
            }
    }

    private bool ContainsLocation(Vector2 location)
    {
        return _mBounds.Contains(location);
    }

    private int GetCellToInsertObject(Vector2 location)
    {
        for (var i = 0; i < 4; i++)
            if (_cells[i].ContainsLocation(location))
                return i;

        return -1;
    }

    private static bool ValueInRange(float value, float min, float max)
    {
        return value >= min && value <= max;
    }

    private static bool RectOverlap(Rect a, Rect b)
    {
        var xOverlap = ValueInRange(a.x, b.x, b.x + b.width) ||
                       ValueInRange(b.x, a.x, a.x + a.width);

        var yOverlap = ValueInRange(a.y, b.y, b.y + b.height) ||
                       ValueInRange(b.y, a.y, a.y + a.height);

        return xOverlap && yOverlap;
    }

    public void DrawDebug()
    {
        Gizmos.DrawLine(new Vector3(_mBounds.x, 0, _mBounds.y),
            new Vector3(_mBounds.x, 0, _mBounds.y + _mBounds.height));
        Gizmos.DrawLine(new Vector3(_mBounds.x, 0, _mBounds.y),
            new Vector3(_mBounds.x + _mBounds.width, 0, _mBounds.y));
        Gizmos.DrawLine(new Vector3(_mBounds.x + _mBounds.width, 0, _mBounds.y),
            new Vector3(_mBounds.x + _mBounds.width, 0, _mBounds.y + _mBounds.height));
        Gizmos.DrawLine(new Vector3(_mBounds.x, 0, _mBounds.y + _mBounds.height),
            new Vector3(_mBounds.x + _mBounds.width, 0, _mBounds.y + _mBounds.height));
        if (_cells[0] != null)
            foreach (var t in _cells)
                if (t != null)
                    t.DrawDebug();
    }
}