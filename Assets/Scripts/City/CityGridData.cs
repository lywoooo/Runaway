using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CityGridData
{
    [Flags]
    public enum Conn : byte { None = 0, N = 1, E = 2, S = 4, W = 8 }

    public readonly int width;
    public readonly int height;

    // cell grid data in 1d arrays
    private readonly Conn[] connections; 
    private readonly bool[] isRoad;

    public CityGridData(int width, int height)
    {
        this.width = width;
        this.height = height;

        connections = new Conn[this.width * this.height];
        isRoad = new bool[this.width * this.height];
    }

    public int Idx(int x, int y) => y * width + x;
    public bool InBounds(int x, int y) => (uint)x < (uint)width && (uint)y < (uint)height;

    public bool IsRoad(int x, int y) => InBounds(x, y) && isRoad[Idx(x, y)];
    public Conn GetConn(int x, int y) => connections[Idx(x, y)];

    public void SetRoad(int x, int y, bool road)
    {
        if (!InBounds(x, y)) return;
        isRoad[Idx(x, y)] = road;
        if (!road) connections[Idx(x, y)] = Conn.None;
    }

    public int Degree(int x, int y)
    {
        Conn c = connections[Idx(x, y)];
        int d = 0;
        if ((c & Conn.N) != 0) d++;
        if ((c & Conn.E) != 0) d++;
        if ((c & Conn.S) != 0) d++;
        if ((c & Conn.W) != 0) d++;
        return d;
    }

    public void Connect(int ax, int ay, int bx, int by)
    {
        if (!InBounds(ax, ay) || !InBounds(bx, by)) return;

        int dx = bx - ax;
        int dy = by - ay;

        // 4-neighbor only
        if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) return;

        int a = Idx(ax, ay);
        int b = Idx(bx, by);

        isRoad[a] = true;
        isRoad[b] = true;

        if (dx == 1) { connections[a] |= Conn.E; connections[b] |= Conn.W; }
        else if (dx == -1) { connections[a] |= Conn.W; connections[b] |= Conn.E; }
        else if (dy == 1) { connections[a] |= Conn.N; connections[b] |= Conn.S; }
        else if (dy == -1) { connections[a] |= Conn.S; connections[b] |= Conn.N; }
    }

    public bool HasEdge(int ax, int ay, int bx, int by)
    {
        if (!InBounds(ax, ay) || !InBounds(bx, by)) return false;

        int dx = bx - ax;
        int dy = by - ay;

        Conn c = connections[Idx(ax, ay)];
        if (dx == 1 && dy == 0) return (c & Conn.E) != 0;
        if (dx == -1 && dy == 0) return (c & Conn.W) != 0;
        if (dx == 0 && dy == 1) return (c & Conn.N) != 0;
        if (dx == 0 && dy == -1) return (c & Conn.S) != 0;
        return false;
    }

    public void RemoveEdge(int ax, int ay, int bx, int by)
    {
        if (!InBounds(ax, ay) || !InBounds(bx, by)) return;

        int dx = bx - ax;
        int dy = by - ay;

        int a = Idx(ax, ay);
        int b = Idx(bx, by);

        if (dx == 1 && dy == 0) { connections[a] &= ~Conn.E; connections[b] &= ~Conn.W; }
        else if (dx == -1 && dy == 0) { connections[a] &= ~Conn.W; connections[b] &= ~Conn.E; }
        else if (dx == 0 && dy == 1) { connections[a] &= ~Conn.N; connections[b] &= ~Conn.S; }
        else if (dx == 0 && dy == -1) { connections[a] &= ~Conn.S; connections[b] &= ~Conn.N; }
    }

    // bfs path check
    public bool Reachable(Vector2Int start, Vector2Int goal)
    {
        if (!InBounds(start.x, start.y) || !InBounds(goal.x, goal.y)) return false;

        int si = Idx(start.x, start.y);
        int gi = Idx(goal.x, goal.y);
        if (!isRoad[si] || !isRoad[gi]) return false;

        var q = new Queue<Vector2Int>();
        var visited = new bool[width * height];

        visited[si] = true;
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            if (p == goal) return true;

            Conn c = connections[Idx(p.x, p.y)];

            Try(p.x, p.y + 1, (c & Conn.N) != 0);
            Try(p.x + 1, p.y, (c & Conn.E) != 0);
            Try(p.x, p.y - 1, (c & Conn.S) != 0);
            Try(p.x - 1, p.y, (c & Conn.W) != 0);
        }

        return false;

        void Try(int nx, int ny, bool allowed)
        {
            if (!allowed) return;
            if (!InBounds(nx, ny)) return;

            int id = Idx(nx, ny);
            if (visited[id]) return;
            if (!isRoad[id]) return;

            visited[id] = true;
            q.Enqueue(new Vector2Int(nx, ny));
        }
    }

    public void PruneToComponent(Vector2Int start)
    {
        if (!InBounds(start.x, start.y)) return;

        bool[] keep = new bool[width * height];
        Queue<Vector2Int> q = new Queue<Vector2Int>();

        int si = Idx(start.x, start.y);
        if (!isRoad[si]) isRoad[si] = true;

        keep[si] = true;
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            Conn c = connections[Idx(p.x, p.y)];

            Try(p.x, p.y + 1, (c & Conn.N) != 0);
            Try(p.x + 1, p.y, (c & Conn.E) != 0);
            Try(p.x, p.y - 1, (c & Conn.S) != 0);
            Try(p.x - 1, p.y, (c & Conn.W) != 0);
        }

        for (int i = 0; i < keep.Length; i++)
        {
            if (!keep[i])
            {
                isRoad[i] = false;
                connections[i] = Conn.None;
            }
        }

        void Try(int nx, int ny, bool allowed)
        {
            if (!allowed) return;
            if (!InBounds(nx, ny)) return;

            int id = Idx(nx, ny);
            if (keep[id]) return;
            if (!isRoad[id]) return;

            keep[id] = true;
            q.Enqueue(new Vector2Int(nx, ny));
        }
    }

    public void CleanConnections()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int id = Idx(x, y);
                if (!isRoad[id])
                {
                    connections[id] = Conn.None;
                    continue;
                }

                Conn c = connections[id];

                if (!ValidNeighbor(x, y + 1, Conn.S)) c &= ~Conn.N;
                if (!ValidNeighbor(x + 1, y, Conn.W)) c &= ~Conn.E;
                if (!ValidNeighbor(x, y - 1, Conn.N)) c &= ~Conn.S;
                if (!ValidNeighbor(x - 1, y, Conn.E)) c &= ~Conn.W;

                connections[id] = c;
            }

        bool ValidNeighbor(int nx, int ny, Conn requiredBitOnNeighbor)
        {
            if (!InBounds(nx, ny)) return false;

            int nid = Idx(nx, ny);
            if (!isRoad[nid]) return false;

            return (connections[nid] & requiredBitOnNeighbor) != 0;
        }
    }
}
