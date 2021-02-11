using Geometry;
using System;
using System.Collections.Generic;
using UnityEngine;

class A_StarPathFinder
{


    public A_StarPathFinder()
    { }


    public List<Triangle> FindPath(Triangle start, Triangle target)
    {
        List<Triangle> open = new List<Triangle>();
        HashSet<Triangle> openHashSet = new HashSet<Triangle>();
        HashSet<Triangle> closed = new HashSet<Triangle>();
        open.Add(start);
        openHashSet.Add(start);

        while (open.Count > 0)
        {
            var curr = open[0];
            
            // Instead of sorting the list we look for the best candidate
            for (int i = 1; i < open.Count; i++)
            {
                if(curr.fCost > open[i].fCost || curr.fCost == open[i].fCost && curr.hCost > open[i].hCost)
                    curr = open[i];
            }

            open.Remove(curr);
            openHashSet.Remove(curr);
            closed.Add(curr);

            if(curr == target)
                return RestorePath(start, curr);
            
            foreach (var neighbour in curr.neighbours)
            {
                if(closed.Contains(neighbour))
                    continue;
                
                var newMovementCostToNeighbour = curr.gCost + ChebyshefDistance(curr, neighbour);
                bool isOpenContainsNeighbor = openHashSet.Contains(neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !isOpenContainsNeighbor)
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = ChebyshefDistance(neighbour, target);
                    neighbour.fCost = neighbour.gCost + neighbour.hCost;
                    neighbour.parent = curr;

                    if(!isOpenContainsNeighbor)
                    {
                        open.Add(neighbour);
                        openHashSet.Add(neighbour);
                    }
                }
            }

        }

        return null;
    }


    private List<Triangle> RestorePath(Triangle start, Triangle end)
    {
        var path = new List<Triangle>();

        while (end != start)
        {
            path.Add(end);
            end = end.parent;
        }
        path.Add(start);

        return path;
    }


    private float ChebyshefDistance(Triangle a, Triangle b)
    {
        // TODO: precalculate triangles centers
        var c0 = (a.a + a.b + a.c) / 3f;
        var c1 = (b.a + b.b + b.c) / 3f;

        // Y * 10 to encrease the cost traveling through hills
        return Math.Abs(c0.x - c1.x) + Math.Abs(c0.y - c1.y) * 10 + Math.Abs(c0.z - c1.z); 
    }


}
