using System;
using System.Collections.Generic;
using UnityEngine;

class A_StarPathFinder
{


    public A_StarPathFinder(Cell[,] cells, int n, int m)
    {
        // Build graph dependencies
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                var cell = cells[i, j];
                if (i < (n - 1))
                {
                    cell.down = cells[i + 1, j];
                    cell.neighbours.Add(cells[i + 1, j]);
                }

                if (i != 0)
                {
                    cell.up = cells[i - 1, j];
                    cell.neighbours.Add(cells[i - 1, j]);
                }

                if (j < (m - 1))
                {
                    cell.right = cells[i, j + 1];
                    cell.neighbours.Add(cells[i, j + 1]);
                }

                if (j != 0)
                {
                    cell.left = cells[i, j - 1];
                    cell.neighbours.Add(cells[i, j - 1]);
                }
            }
        }
    }

    public List<Cell> FindPath(Cell start, Cell target)
    {
        var path = new List<Cell>();

        List<Cell> open = new List<Cell>();
        HashSet<Cell> closed = new HashSet<Cell>();
        open.Add(start);

        while(open.Count > 0)
        {
            var curr = open[0];
            for (int i = 1; i < open.Count; i++)
            {
                if(curr.fCost > open[i].fCost || curr.fCost == open[i].fCost && curr.hCost > open[i].hCost)
                {
                    curr = open[i];
                }
            }
            open.Remove(curr);
            closed.Add(curr);

            if(curr == target)
            {
                while(curr != start)
                {
                    path.Add(curr);
                    curr = curr.parent;
                }

                return path;
            }

            foreach (var neighbour in curr.neighbours)
            {
                if(neighbour.unit != null || closed.Contains(neighbour))
                {
                    continue;
                }

                int newMovementCostToNeighbour = curr.gCost + ChebyshefDistance(curr.coords, neighbour.coords);
                if(newMovementCostToNeighbour < neighbour.gCost || !open.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = ChebyshefDistance(neighbour.coords, target.coords);
                    neighbour.parent = curr;

                    if(!open.Contains(neighbour))
                    {
                        open.Add(neighbour);
                    }
                }
            }

        }

        return path;
    }

    private int ChebyshefDistance(Vector2Int a, Vector2Int b)
    {
        return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
    }
}
