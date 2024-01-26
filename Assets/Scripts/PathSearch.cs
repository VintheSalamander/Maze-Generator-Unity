using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PathSearch{

    public static (MazeCell, MazeCell) GetExitandKey(MazeCell[,] mazeGrid, MazeCell start){
        (List<MazeCell> exitPath, MazeCell exitCell) = BreadthSearchExit(start);
        MazeCell keyCell = GetKeyCell(mazeGrid, exitPath);
        return (exitCell, keyCell);
    }

    //Gets the Exit by doing a breadth search, the longest cell from the start is reached when the
    //whole maze is already searched
    private static (List<MazeCell>, MazeCell) BreadthSearchExit(MazeCell start){
        List<MazeCell> result;
        HashSet<MazeCell> visited = new HashSet<MazeCell>();
        Queue<MazeCell> work = new Queue<MazeCell>();
        visited.Add(start);
        work.Enqueue(start);
 
        while(work.Count > 0){
            MazeCell current = work.Dequeue();
            List<MazeCell> currentNeighbours = current.GetNeighbours();
            foreach(MazeCell neighbour in currentNeighbours){
                if(!visited.Contains(neighbour)){
                    MazeGenerator.GenerateCellMesh(current, neighbour);
                    neighbour.SetHistory(new List<MazeCell>(current.GetHistory()));
                    neighbour.AddHistory(current);
                    visited.Add(neighbour);
                    work.Enqueue(neighbour);
                }
            }
            if(work.Count == 0){
                result = current.GetHistory();
                result.Add(current);
                return (result, current);
            }
        }
        return (null, null);
    }

    //For the key cell we search the history of each cell which is the path from the start to that cell, then
    //we compare that path to the exit path and the history who has the less matches its the second furthest cell
    //which will be the key cell
    private static MazeCell GetKeyCell(MazeCell[,] mazeGrid, List<MazeCell> exitPath){
        MazeCell keyCell = null;
        int countKeyCell = 0;
        for (int x = 0; x < mazeGrid.GetLength(0); x++) {
            for (int z = 0; z < mazeGrid.GetLength(1); z++) {
                List<MazeCell> currentCellHistory = mazeGrid[x, z].GetHistory();
                List<MazeCell> cellsNotInFarP = currentCellHistory.Except(exitPath).ToList();
                int countNotInFarP = cellsNotInFarP.Count;
                if(countNotInFarP > countKeyCell){
                    countKeyCell = countNotInFarP;
                    keyCell = mazeGrid[x, z];
                }
            }
        }
        return keyCell;
    }

    //We do a breadth search from each cell to determine the closest voronoid also taking into account cases when
    //there are two voronoids at the same distant from the cell
    public static MazeCell GetClosestVoronoid(MazeCell start, HashSet<MazeCell> voronoidPoints){
        if(voronoidPoints.Contains(start)){
            return start;
        }
        List<MazeCell> closeVors = new List<MazeCell>();
        HashSet<MazeCell> visited = new HashSet<MazeCell>();
        Queue<MazeCell> work = new Queue<MazeCell>();
        visited.Add(start);
        work.Enqueue(start);
        while(true){
            MazeCell current = work.Dequeue();
            if(voronoidPoints.Contains(current) || closeVors.Count > 0){
                break;
            }else{
                List<MazeCell> currentNeighbours = current.GetNeighbours();
                foreach(MazeCell neighbour in currentNeighbours){
                    if(!visited.Contains(neighbour)){
                        neighbour.SetHistory(new List<MazeCell>(current.GetHistory()));
                        neighbour.AddHistory(current);
                        if(voronoidPoints.Contains(neighbour)){
                            closeVors.Add(neighbour);
                        }
                        visited.Add(neighbour);
                        work.Enqueue(neighbour);

                    }
                }
            }
        }
        if(closeVors.Count == 1){
            return closeVors[0];
        }else{
            MazeCell closestVor = null;
            List<MazeCell> neighbours = start.GetNeighbours();
            Color maxneighboursColor = Color.black;
            if(neighbours.Count > 1){
                List<int> colorsCount = new List<int>();
                List<Color> colors = new List<Color>();
                foreach(MazeCell neighbour in neighbours){
                    if(neighbour.color == Color.black){
                        continue;
                    }
                    if(!colors.Contains(neighbour.color)){
                        colors.Add(neighbour.color);
                        colorsCount.Add(1);
                    }else{
                        colorsCount[colors.IndexOf(neighbour.color)]++;
                    }
                }
                if(colors.Count != 0){
                    maxneighboursColor = colors[colorsCount.IndexOf(colorsCount.Max())];
                }
            }else{
                maxneighboursColor = neighbours[0].color;
            }
            foreach(MazeCell closeVor in closeVors){
                //Cases were neighbours were the vors are same distance and choose another one diff from neighbours
                if(maxneighboursColor != Color.black){
                    if(maxneighboursColor == closeVor.color){
                        closestVor = closeVor;
                    }else{
                        if(closestVor == null){
                            closestVor = closeVor;
                        }
                    }
                }else{
                    if(closestVor == null){
                        closestVor = closeVor;
                    }else if(Random.Range(0, 2) == 0){
                        closestVor = closeVor;
                    }
                }
            }
            
            return closestVor;
        }
    }
    
}
