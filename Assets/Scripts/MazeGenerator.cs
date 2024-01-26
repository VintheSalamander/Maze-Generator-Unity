using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.PackageManager;
using System.IO.Compression;
using Unity.VisualScripting;

public enum Elevation{
    MinusMax = -75,
    MinusMin = -25,
    None = 0,
    PlusMin = 25,
    PlusMax = 75,
}

public enum Direction{
    Left,
    Right,
    Bot,
    Top
}

public class MazeGenerator : MonoBehaviour
{
    [SerializeField]
    private MazeCell mazeCellPrefab;
    [SerializeField]
    private GameObject Player;
    [SerializeField]
    private GameObject Exit;
    [SerializeField]
    private GameObject Key;

    [SerializeField]
    private int mazeWidth;

    [SerializeField]
    private int mazeDepth;
    [SerializeField]
    private float ellersMergeProb;
    [SerializeField]
    private float elevationRatioIn;
    private static float elevationRatio;

    [SerializeField]
    private int voronoidCellSize;
    [SerializeField]
    private Color[] possibleColors;
    [SerializeField]
    private Color areaEVA;
    [SerializeField]
    private Color areaPlant;

    private MazeCell[,] mazeGrid;
    private Dictionary<int, List<int>> cellSets = new Dictionary<int, List<int>>();
    private MazeCell[,] voronoidGrid;
    private int voronoidGridWidth;
    private int voronoidGridDepth;
    private MazeCell start;

    void Start(){
        //Set defaults
        elevationRatio = elevationRatioIn;
        mazeGrid = new MazeCell[mazeWidth, mazeDepth];
        voronoidGridWidth = mazeWidth/voronoidCellSize;
        voronoidGridDepth = mazeDepth/voronoidCellSize;
        voronoidGrid = new MazeCell[voronoidGridWidth, voronoidGridDepth];

        Rigidbody playerRigidbody = Player.GetComponent<Rigidbody>();
        
        GenerateMaze();

        //Set start position and mesh
        int startX = Random.Range(0, mazeWidth - 1);
        start = mazeGrid[startX, 0];
        start.SetElevation(Elevation.None);
        start.GenerateMesh(0f, Direction.Right, start.cellVerts);

        //Move player to start
        playerRigidbody.MovePosition(new Vector3(startX + 0.5f, 1f, 0.5f));

        SetExitandKey();
        SetColorVoronoid();
    }

    //Generates Maze using Ellers algorithm for maze generation
    private void GenerateMaze(){
        for (int z = 0; z < mazeDepth; z++){
            for (int x = 0; x < mazeWidth; x++){
                int cellIndex = z * mazeWidth + x;

                //Instantiates the Prefabs and set for each cell in the row
                if(mazeGrid[x, z] == null){
                    mazeGrid[x, z] = Instantiate(mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity);
                    mazeGrid[x, z].transform.SetParent(transform);
                    cellSets.Add(cellIndex, new List<int>{cellIndex});
                }

                //There is a probabily of joining different cells in different sets
                //or if its the last row join the different cells in different sets to create perfect maze
                if (x != 0 && (Random.value < ellersMergeProb || z == mazeDepth - 1 )){
                    int currentSet = FindSet(cellIndex);
                    int leftSet = FindSet(cellIndex - 1);
                    if(currentSet != leftSet){
                        MazeCell currentCell = mazeGrid[x, z];
                        MazeCell leftCell = mazeGrid[x - 1, z];

                        currentCell.BreakLeftWall();
                        leftCell.BreakRightWall();
                        currentCell.AddNeighbour(leftCell);
                        leftCell.AddNeighbour(currentCell);

                        MergeSets(currentSet, leftSet);
                    }                    
                }
            }

            if(z == mazeDepth - 1){
                continue;
            }

            //For each set remaining join one random cell with the above row
            foreach(int set in cellSets.Keys){
                int currentCellIndex = cellSets[set][Random.Range(0, cellSets[set].Count)];
                int aboveCellIndex = currentCellIndex + mazeWidth;
                MazeCell currentCell = FindCellByIndex(currentCellIndex);
                MazeCell aboveCell = FindCellByIndex(aboveCellIndex);

                currentCell.BreakTopWall();
                aboveCell.BreakBottomWall();
                currentCell.AddNeighbour(aboveCell);
                aboveCell.AddNeighbour(currentCell);

                cellSets[set].Clear();
                cellSets[set].Add(aboveCellIndex);
            }
        }
        

    }

    //Functions used for the maze generation
    private int FindSet(int cellIndex) {
        foreach (KeyValuePair<int, List<int>> entry in cellSets) {
            if (entry.Value.Contains(cellIndex)) {
                return entry.Key;
            }
        }
        Debug.LogError("FindSet() Error with cell in: " + FindCellByIndex(cellIndex).transform.position);
        return -1;
    }
    private void MergeSets(int setFrom, int setTo) {

        foreach (int cellIndex in cellSets[setFrom]) {
            cellSets[setTo].Add(cellIndex);
        }

        cellSets.Remove(setFrom);
    }
    private MazeCell FindCellByIndex(int index) {
        int x = index % mazeWidth;
        int z = index / mazeWidth;
        if(mazeGrid[x, z] == null){
            mazeGrid[x, z] = Instantiate(mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity);
            mazeGrid[x, z].transform.SetParent(transform);
        }
        return mazeGrid[x, z];
    }

    //Determines nextCells Elevation and generates its mesh
    //To generate some sense of terrain there is a 1/4 of probability staying the same elevation as the currentCell, 
    //if elevation is None then it changes slightly, if its already slightly
    //is has a probality of 1/3 to change back to None, if not it will change drastically
    //and if its already changed drastically it will go to a slight change
    public static void GenerateCellMesh(MazeCell currentCell, MazeCell nextCell){
        float elevation;
        Elevation currentElev = currentCell.cellElevation;
        if(!(Random.value == 0.25f)){
            switch(currentElev){
                case Elevation.MinusMax:
                    nextCell.SetElevation(Elevation.MinusMin);
                    elevation = (float)Elevation.MinusMin + Random.Range(-30, 10);
                    break;
                case Elevation.MinusMin:
                    if(Random.value < 1 / 3){
                        nextCell.SetElevation(Elevation.None);
                        elevation = 0f;
                    }else{
                        nextCell.SetElevation(Elevation.MinusMax);
                        elevation = (float)Elevation.MinusMax + Random.Range(-10, 15);
                    }
                    break;
                case Elevation.None:
                    if(Random.value < 0.5f){
                        nextCell.SetElevation(Elevation.PlusMin);
                        elevation = (float)Elevation.PlusMin + Random.Range(-10, 30);
                    }else{
                        nextCell.SetElevation(Elevation.MinusMin);
                        elevation = (float)Elevation.MinusMin + Random.Range(-30, 10);
                    }
                    break;
                case Elevation.PlusMin:
                    if(Random.value < 1 / 3){
                        nextCell.SetElevation(Elevation.None);
                        elevation = 0f;
                    }else{
                        nextCell.SetElevation(Elevation.PlusMax);
                        elevation = (float)Elevation.PlusMax + Random.Range(-15, 10);
                    }
                    break;
                case Elevation.PlusMax:
                    nextCell.SetElevation(Elevation.PlusMin);
                    elevation = (float)Elevation.PlusMin + Random.Range(-10, 30);
                    break;
                default:
                    elevation = 0f;
                    nextCell.SetElevation(currentElev);
                    Debug.LogError("Error in GenerateCellMesh()");
                    break;
            }
        }else{
            nextCell.SetElevation(currentElev);
            elevation = (float)currentElev;
        }
        nextCell.transform.position += new Vector3(0, currentCell.transform.position.y + elevation / elevationRatio, 0);
        nextCell.GenerateMesh(-elevation / elevationRatio, GetDirection(currentCell, nextCell), currentCell.cellVerts);
    }

    //Gets the direction between currentCell and nextCell
    private static Direction GetDirection(MazeCell currentCell, MazeCell nextCell){
        float xDifference = nextCell.transform.position.x - currentCell.transform.position.x;
        float zDifference = nextCell.transform.position.z - currentCell.transform.position.z;
        
        if (Mathf.Abs(xDifference) > Mathf.Abs(zDifference)){
            if (xDifference > 0){
                return Direction.Right;
            }else{
                return Direction.Left;
            }
        }else{
            if (zDifference > 0){
                return Direction.Top;
            }else{
                return Direction.Bot;
            }
        }
    }

    private void SetExitandKey(){
        (MazeCell endCell, MazeCell keyCell) = PathSearch.GetExitandKey(mazeGrid, start);
        Direction endprevDirection = endCell.GetOpenDirection();
        Direction keyprevDirection = keyCell.GetOpenDirection();
        MazeCell endprevCell = GetCellInDirection(endCell, endprevDirection);
        MazeCell keyprevCell = GetCellInDirection(keyCell, keyprevDirection);
        endCell.transform.position = new Vector3(endCell.transform.position.x, endprevCell.transform.position.y, endCell.transform.position.z);
        keyCell.transform.position = new Vector3(keyCell.transform.position.x, keyprevCell.transform.position.y, keyCell.transform.position.z);

        endCell.GenerateMesh((float)Elevation.None, GetDirection(endprevCell, endCell), endprevCell.cellVerts);
        keyCell.GenerateMesh((float)Elevation.None, GetDirection(keyprevCell, keyCell), keyprevCell.cellVerts);

        Exit.transform.position = new Vector3(endCell.transform.position.x + 0.5f, endCell.transform.position.y + 0.5f, endCell.transform.position.z + 0.5f);
        Key.transform.position = new Vector3(keyCell.transform.position.x + 0.5f, keyCell.transform.position.y + 0.85f, keyCell.transform.position.z + 0.5f);
    }


    private MazeCell GetCellInDirection(MazeCell currentCell, Direction direction){
        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        switch(direction){
            case Direction.Top:
                return mazeGrid[x, z + 1];
            case Direction.Bot:
                return mazeGrid[x, z - 1];
            case Direction.Left:
                return mazeGrid[x - 1, z];
            case Direction.Right:
                return mazeGrid[x + 1, z];
            default:
                Debug.LogError("GetCellInDirection() error");
                return null;
        }
    }

    //A voronoid grid is used to create areas with different colors in the maze
    private void SetColorVoronoid(){
        if(mazeWidth % voronoidCellSize != 0 || mazeDepth % voronoidCellSize != 0){
            Debug.LogError("voronoidCellSize must be divisible by maze");
        }
        HashSet<MazeCell> voronoidPoints = new HashSet<MazeCell>();
        //int w = 0;
        for(int i = 0; i < voronoidGridDepth; i++){
            for(int j = 0; j < voronoidGridWidth; j++){
                voronoidGrid[i, j] = mazeGrid[Random.Range(i*voronoidCellSize, (i+1)*voronoidCellSize), Random.Range(j*voronoidCellSize, (j+1)*voronoidCellSize)];
                voronoidGrid[i, j].SetWallsColor(possibleColors[Random.Range(0, possibleColors.Length)]);
                // Debug.Log("Voronoid " + w + ": "+ voronoidGrid[i, j].transform.position);
                //w++;
                voronoidPoints.Add(voronoidGrid[i, j]);
            }
        }


        int xExit = (int)(Exit.transform.position.x-0.5f);
        int zExit = (int)(Exit.transform.position.z-0.5f);
        int xKey = (int)(Key.transform.position.x-0.5f);
        int zKey = (int)(Key.transform.position.z-0.5f);
        MazeCell exitCell = mazeGrid[xExit, zExit];
        MazeCell keyCell = mazeGrid[xKey, zKey];
        exitCell.SetWallsColor(areaEVA);
        keyCell.SetWallsColor(areaPlant);
        PathSearch.GetClosestVoronoid(exitCell, voronoidPoints).SetWallsColor(areaEVA);
        PathSearch.GetClosestVoronoid(keyCell, voronoidPoints).SetWallsColor(areaPlant);
        


        for(int x = 0; x < mazeWidth; x++){
            for(int z = 0; z < mazeDepth; z++){
                if((x == xExit && z == zExit ) || (x == xKey && z == zKey)){
                    continue;
                }
                if(voronoidPoints.Contains(mazeGrid[x, z])){
                    continue;
                }
                MazeCell nearestVor = PathSearch.GetClosestVoronoid(mazeGrid[x, z], voronoidPoints);
                mazeGrid[x, z].SetWallsColor(nearestVor.color);
            }
        }
    }
}
