using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CityGen
{
    public struct Result
    {
        public CityGridData grid;
        public Vector2Int spawn;
        public Vector2Int dest;
        public int seed;
    }

    public Result Generate(CitySettings s)
    {
        int w = Mathf.Max(4, s.width);
        int h = Mathf.Max(4, s.height);
        int arterial = Mathf.Max(2, s.arterialSpacing);

        int m = Mathf.Clamp(s.roadEdgeMargin, 0, Mathf.Min((w - 1) / 2, (h - 1) / 2));

        int padMax = Mathf.Min(w - 1, h - 1);
        int padding = Mathf.Clamp(s.edgePadding, 0, padMax);

        int seed = s.randomSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : s.seed;
        var rng = new System.Random(seed);

        var g = new CityGridData(w, h);

        BuildArterials(g, arterial, m);
        BuildLocalStreets(g, s.localStreetChance, rng, arterial, buffer: 1, m: m);
        AddExtraLoops(g, Mathf.Max(0, s.extraLoops), rng, m);
        BreakSomeEdges(g, s.streetBreakChance, rng);

        TrimDeadEnds(g, iterations: 6);

        Vector2Int spawn = PickSpawn(g, padding, s.preferSpawnNearEdge, rng);
        Vector2Int dest = PickDestinationFar(g, spawn, padding, Mathf.Max(2, s.minManhattan), rng);

        EnsureConnected(g, spawn, dest, rng);

        g.PruneToComponent(spawn);
        g.CleanConnections();

        if (!g.IsRoad(dest.x, dest.y) || !g.Reachable(spawn, dest))
        {
            EnsureConnected(g, spawn, dest, rng);
            g.PruneToComponent(spawn);
            g.CleanConnections();
        }

        return new Result { grid = g, spawn = spawn, dest = dest, seed = seed };
    }

    private void BuildArterials(CityGridData g, int spacing, int m)
    {
        for (int x = m; x < g.width - m; x += spacing)
            for (int y = m; y < g.height - 1 - m; y++)
                g.Connect(x, y, x, y + 1);

        for (int y = m; y < g.height - m; y += spacing)
            for (int x = m; x < g.width - 1 - m; x++)
                g.Connect(x, y, x + 1, y);
    }

    private void BuildLocalStreets(CityGridData g, float chance, System.Random rng, int arterialSpacing, int buffer, int m)
    {
        double p = Mathf.Clamp01(chance);
        int sp = Mathf.Max(2, arterialSpacing);
        buffer = Mathf.Max(0, buffer);

        for (int y = m; y < g.height - m; y++)
            for (int x = m; x < g.width - m; x++)
            {
                int xm = x % sp;
                int ym = y % sp;

                int dxToArt = Math.Min(xm, sp - xm);
                int dyToArt = Math.Min(ym, sp - ym);

                if (dxToArt <= buffer || dyToArt <= buffer) continue;

                if (x + 1 < g.width - m && rng.NextDouble() < p) g.Connect(x, y, x + 1, y);
                if (y + 1 < g.height - m && rng.NextDouble() < p) g.Connect(x, y, x, y + 1);
            }
    }

    private void AddExtraLoops(CityGridData g, int loops, System.Random rng, int m)
    {
        int minX = m;
        int maxXEx = Mathf.Max(minX + 1, g.width - m);
        int minY = m;
        int maxYEx = Mathf.Max(minY + 1, g.height - m);

        for (int i = 0; i < loops; i++)
        {
            int x = rng.Next(minX, maxXEx);
            int y = rng.Next(minY, maxYEx);

            int dir = rng.Next(0, 4);
            int nx = x + (dir == 1 ? 1 : dir == 3 ? -1 : 0);
            int ny = y + (dir == 0 ? 1 : dir == 2 ? -1 : 0);

            if (!g.InBounds(nx, ny)) continue;
            g.Connect(x, y, nx, ny);
        }
    }

    private void BreakSomeEdges(CityGridData g, float chance, System.Random rng)
    {
        float p = Mathf.Clamp01(chance);
        if (p <= 0f) return;

        for (int y = 0; y < g.height; y++)
            for (int x = 0; x < g.width; x++)
            {
                if (!g.IsRoad(x, y)) continue;

                if (x + 1 < g.width && g.HasEdge(x, y, x + 1, y) && rng.NextDouble() < p)
                    g.RemoveEdge(x, y, x + 1, y);

                if (y + 1 < g.height && g.HasEdge(x, y, x, y + 1) && rng.NextDouble() < p)
                    g.RemoveEdge(x, y, x, y + 1);
            }
    }

    private void TrimDeadEnds(CityGridData g, int iterations)
    {
        iterations = Mathf.Max(0, iterations);
        if (iterations == 0) return;

        var toRemove = new List<Vector2Int>(g.width * g.height);

        for (int it = 0; it < iterations; it++)
        {
            toRemove.Clear();

            for (int y = 0; y < g.height; y++)
                for (int x = 0; x < g.width; x++)
                {
                    if (!g.IsRoad(x, y)) continue;
                    if (g.Degree(x, y) <= 1) toRemove.Add(new Vector2Int(x, y));
                }

            if (toRemove.Count == 0) break;

            for (int i = 0; i < toRemove.Count; i++)
            {
                var p = toRemove[i];
                g.SetRoad(p.x, p.y, false);
            }

            g.CleanConnections();
        }
    }

    private Vector2Int PickSpawn(CityGridData g, int padding, bool nearEdge, System.Random rng)
    {
        int minX = padding;
        int maxXEx = Mathf.Max(minX + 1, g.width - padding);
        int minY = padding;
        int maxYEx = Mathf.Max(minY + 1, g.height - padding);

        Vector2Int best = Vector2Int.zero;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < 3000; i++)
        {
            int x = rng.Next(minX, maxXEx);
            int y = rng.Next(minY, maxYEx);

            if (!g.IsRoad(x, y)) continue;
            if (g.Degree(x, y) < 2) continue;

            int left = x;
            int bottom = y;
            int right = (g.width - 1) - x;
            int top = (g.height - 1) - y;

            int edgeDist = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));

            float score = nearEdge ? -edgeDist : edgeDist;
            score += (float)rng.NextDouble() * 0.2f;

            if (score > bestScore)
            {
                bestScore = score;
                best = new Vector2Int(x, y);
            }
        }

        if (bestScore > float.NegativeInfinity) return best;

        for (int y = 0; y < g.height; y++)
            for (int x = 0; x < g.width; x++)
                if (g.IsRoad(x, y)) return new Vector2Int(x, y);

        return Vector2Int.zero;
    }

    private Vector2Int PickDestinationFar(CityGridData g, Vector2Int spawn, int padding, int minManhattan, System.Random rng)
    {
        int minX = padding;
        int maxXEx = Mathf.Max(minX + 1, g.width - padding);
        int minY = padding;
        int maxYEx = Mathf.Max(minY + 1, g.height - padding);

        Vector2Int best = spawn;
        int bestD = -1;

        for (int i = 0; i < 5000; i++)
        {
            int x = rng.Next(minX, maxXEx);
            int y = rng.Next(minY, maxYEx);

            if (!g.IsRoad(x, y)) continue;
            if (g.Degree(x, y) < 2) continue;

            int d = Mathf.Abs(x - spawn.x) + Mathf.Abs(y - spawn.y);
            if (d > bestD)
            {
                bestD = d;
                best = new Vector2Int(x, y);
            }
        }

        if (bestD < minManhattan)
        {
            int x = (g.width - 1) - spawn.x;
            int y = (g.height - 1) - spawn.y;
            best = new Vector2Int(Mathf.Clamp(x, 0, g.width - 1), Mathf.Clamp(y, 0, g.height - 1));
        }

        return best;
    }

    private void EnsureConnected(CityGridData g, Vector2Int spawn, Vector2Int dest, System.Random rng)
    {
        g.SetRoad(spawn.x, spawn.y, true);
        g.SetRoad(dest.x, dest.y, true);

        if (g.Reachable(spawn, dest)) return;

        CarveConnector(g, spawn, dest, rng);

        if (g.Reachable(spawn, dest)) return;

        int safety = g.width * g.height * 10;
        while (!g.Reachable(spawn, dest) && safety-- > 0)
        {
            int x = rng.Next(0, g.width);
            int y = rng.Next(0, g.height);
            int dir = rng.Next(0, 4);

            int nx = x + (dir == 1 ? 1 : dir == 3 ? -1 : 0);
            int ny = y + (dir == 0 ? 1 : dir == 2 ? -1 : 0);

            if (!g.InBounds(nx, ny)) continue;
            g.Connect(x, y, nx, ny);
        }
    }

    private void CarveConnector(CityGridData g, Vector2Int a, Vector2Int b, System.Random rng)
    {
        Vector2Int p = a;
        int safety = g.width * g.height * 20;

        while (p != b && safety-- > 0)
        {
            int dx = b.x - p.x;
            int dy = b.y - p.y;

            bool preferX = Math.Abs(dx) >= Math.Abs(dy);
            if (rng.NextDouble() < 0.25) preferX = !preferX;

            Vector2Int n = p;

            if (preferX && dx != 0) n.x += Math.Sign(dx);
            else if (!preferX && dy != 0) n.y += Math.Sign(dy);
            else if (dx != 0) n.x += Math.Sign(dx);
            else if (dy != 0) n.y += Math.Sign(dy);

            if (!g.InBounds(n.x, n.y) || n == p)
            {
                n = p;

                if (dy != 0)
                {
                    var cand = new Vector2Int(p.x, p.y + Math.Sign(dy));
                    if (g.InBounds(cand.x, cand.y)) n = cand;
                }

                if (n == p && dx != 0)
                {
                    var cand = new Vector2Int(p.x + Math.Sign(dx), p.y);
                    if (g.InBounds(cand.x, cand.y)) n = cand;
                }

                if (n == p)
                {
                    var candidates = new List<Vector2Int>(4);
                    if (p.y + 1 < g.height) candidates.Add(new Vector2Int(p.x, p.y + 1));
                    if (p.x + 1 < g.width) candidates.Add(new Vector2Int(p.x + 1, p.y));
                    if (p.y - 1 >= 0) candidates.Add(new Vector2Int(p.x, p.y - 1));
                    if (p.x - 1 >= 0) candidates.Add(new Vector2Int(p.x - 1, p.y));

                    if (candidates.Count == 0) break;
                    n = candidates[rng.Next(0, candidates.Count)];
                }
            }

            g.Connect(p.x, p.y, n.x, n.y);
            p = n;
        }
    }
}
