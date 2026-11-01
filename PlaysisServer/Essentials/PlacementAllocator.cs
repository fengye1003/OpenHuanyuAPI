using System;
using System.Collections.Generic;

namespace PlaysisServer.Essentials
{
    public readonly struct Footprint
    {
        public readonly float CenterX;
        public readonly float CenterZ;
        public readonly float Width;
        public readonly float Depth;

        public Footprint(float centerX, float centerZ, float width, float depth)
        {
            CenterX = centerX;
            CenterZ = centerZ;
            Width = width;
            Depth = depth;
        }

        public float Left => CenterX - Width * 0.5f;
        public float Right => CenterX + Width * 0.5f;
        public float Bottom => CenterZ - Depth * 0.5f;
        public float Top => CenterZ + Depth * 0.5f;
    }

    public readonly struct KeepOutRect
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Bottom;
        public readonly float Top;

        public KeepOutRect(float left, float bottom, float right, float top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }
    }

    public readonly struct PlacementResult
    {
        public readonly float X;
        public readonly float Z;
        public readonly float Scale;
        public readonly bool Found;

        public PlacementResult(float x, float z, float scale, bool found)
        {
            X = x;
            Z = z;
            Scale = scale;
            Found = found;
        }
    }

    public static class PlacementAllocator
    {
        public const float CellSize = 4f;
        public const float Margin = 1f;
        public const float MinFootprint = 2f;
        public const float MaxFootprint = 120f;

        public static (float width, float depth, float scale) Normalize(float width, float depth)
        {
            width = Math.Max(width, MinFootprint);
            depth = Math.Max(depth, MinFootprint);

            var longest = Math.Max(width, depth);
            if (longest <= MaxFootprint) return (width, depth, 1f);

            var scale = MaxFootprint / longest;
            return (width * scale, depth * scale, scale);
        }

        public static bool Overlaps(Footprint a, Footprint b, float margin)
        {
            return a.Left - margin < b.Right + margin &&
                   a.Right + margin > b.Left - margin &&
                   a.Bottom - margin < b.Top + margin &&
                   a.Top + margin > b.Bottom - margin;
        }

        public static bool IsFree(Footprint candidate, IReadOnlyList<Footprint> occupied, float margin)
        {
            foreach (var other in occupied)
                if (Overlaps(candidate, other, margin)) return false;
            return true;
        }

        public static bool IntersectsKeepOut(Footprint candidate, KeepOutRect area, float margin)
        {
            return candidate.Left - margin < area.Right &&
                   candidate.Right + margin > area.Left &&
                   candidate.Bottom - margin < area.Top &&
                   candidate.Top + margin > area.Bottom;
        }

        static int CellsOf(float size, float cellSize, float margin)
        {
            var span = Math.Max(size, 0f) + margin * 2f;
            return Math.Max(1, (int)Math.Ceiling(span / cellSize));
        }

        static void MarkFootprint(HashSet<(int, int)> occupied, Footprint f, float cellSize, float margin)
        {
            var cw = CellsOf(f.Width, cellSize, margin);
            var cd = CellsOf(f.Depth, cellSize, margin);

            var i0 = (int)Math.Round((f.CenterX - (cw - 1) * cellSize * 0.5f) / cellSize, MidpointRounding.AwayFromZero);
            var j0 = (int)Math.Round((f.CenterZ - (cd - 1) * cellSize * 0.5f) / cellSize, MidpointRounding.AwayFromZero);

            for (var i = 0; i < cw; i++)
            for (var j = 0; j < cd; j++)
                occupied.Add((i0 + i, j0 + j));
        }

        static bool BlockFree(HashSet<(int, int)> occupied, int i0, int j0, int cw, int cd)
        {
            for (var i = 0; i < cw; i++)
            for (var j = 0; j < cd; j++)
                if (occupied.Contains((i0 + i, j0 + j))) return false;
            return true;
        }

        public static PlacementResult Allocate(float width, float depth, IReadOnlyList<Footprint> occupiedFootprints,
                                               float cellSize = CellSize, float margin = Margin,
                                               int maxRing = 64, KeepOutRect? keepOut = null)
        {
            var normalized = Normalize(width, depth);
            var scale = normalized.scale;
            width = normalized.width;
            depth = normalized.depth;

            var cw = CellsOf(width, cellSize, margin);
            var cd = CellsOf(depth, cellSize, margin);

            var occupied = new HashSet<(int, int)>();
            foreach (var f in occupiedFootprints) MarkFootprint(occupied, f, cellSize, margin);

            var found = false;
            var bestSide = float.MaxValue;
            var bestWaste = float.MaxValue;
            var bestDistance = float.MaxValue;
            var bestI = 0;
            var bestJ = 0;

            for (var ring = 0; ring <= maxRing; ring++)
            {
                for (var i = -ring; i <= ring; i++)
                for (var j = -ring; j <= ring; j++)
                {
                    if (ring > 0 && Math.Abs(i) != ring && Math.Abs(j) != ring) continue;

                    var i0 = i - (cw - 1) / 2;
                    var j0 = j - (cd - 1) / 2;

                    if (!BlockFree(occupied, i0, j0, cw, cd)) continue;

                    var cx = (i0 + (cw - 1) * 0.5f) * cellSize;
                    var cz = (j0 + (cd - 1) * 0.5f) * cellSize;
                    var left = cx - cw * cellSize * 0.5f;
                    var right = cx + cw * cellSize * 0.5f;
                    var bottom = cz - cd * cellSize * 0.5f;
                    var top = cz + cd * cellSize * 0.5f;

                    if (keepOut.HasValue)
                    {
                        var k = keepOut.Value;
                        if (left < k.Right && right > k.Left && bottom < k.Top && top > k.Bottom) continue;
                    }

                    float minX = left, maxX = right, minZ = bottom, maxZ = top;
                    foreach (var f in occupiedFootprints)
                    {
                        if (f.Left < minX) minX = f.Left;
                        if (f.Right > maxX) maxX = f.Right;
                        if (f.Bottom < minZ) minZ = f.Bottom;
                        if (f.Top > maxZ) maxZ = f.Top;
                    }

                    var side = Math.Max(maxX - minX, maxZ - minZ);
                    var waste = (maxX - minX) * (maxZ - minZ) - cw * cd * cellSize * cellSize;
                    var distance = (float)Math.Sqrt(cx * cx + cz * cz);

                    var better = !found
                                 || side < bestSide - 0.001f
                                 || (Math.Abs(side - bestSide) <= 0.001f && waste < bestWaste - 0.001f)
                                 || (Math.Abs(side - bestSide) <= 0.001f && Math.Abs(waste - bestWaste) <= 0.001f && distance < bestDistance);

                    if (!better) continue;

                    found = true;
                    bestSide = side;
                    bestWaste = waste;
                    bestDistance = distance;
                    bestI = i0;
                    bestJ = j0;
                }
            }

            if (!found) return new PlacementResult(0f, 0f, scale, false);

            var fx = (bestI + (cw - 1) * 0.5f) * cellSize;
            var fz = (bestJ + (cd - 1) * 0.5f) * cellSize;
            return new PlacementResult(fx, fz, scale, true);
        }

        public static List<Footprint> FromModels<T>(IEnumerable<T> models,
                                                   Func<T, float> getX,
                                                   Func<T, float> getZ,
                                                   Func<T, float> getWidth,
                                                   Func<T, float> getDepth)
        {
            var list = new List<Footprint>();
            foreach (var m in models)
                list.Add(new Footprint(getX(m), getZ(m), Math.Max(getWidth(m), 0.5f), Math.Max(getDepth(m), 0.5f)));
            return list;
        }
    }
}


