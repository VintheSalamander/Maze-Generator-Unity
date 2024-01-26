using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum Vert
{
    LeftBot,
    RightBot,
    LeftTop,
    RightTop
}

public class MazeCell : MonoBehaviour
{
    [SerializeField]
    private GameObject leftWall;

    [SerializeField]
    private GameObject rightWall;

    [SerializeField]
    private GameObject bottomWall;

    [SerializeField]
    private GameObject topWall;

    Mesh mesh;
    public bool isVisited{get; private set;}
    public Vector3[] cellVerts{get; private set;} = new Vector3[4];
    private int[] cellTris = new int[6];
    private List<MazeCell> history = new List<MazeCell>();
    private List<MazeCell> neighbours = new List<MazeCell>();
    public Color color{get; private set;}
    public Elevation cellElevation{get; private set;}

    void Awake(){
        //Initialize mesh for the floor
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Start(){
        //Set default values
        isVisited = false;
    }

    //Functions for hiding walls when connecting the maze
    public void BreakLeftWall(){
        leftWall.SetActive(false);
    }
    public void BreakRightWall(){
        rightWall.SetActive(false);
    }
    public void BreakBottomWall(){
        bottomWall.SetActive(false);
    }
    public void BreakTopWall(){
        topWall.SetActive(false);
    }

    //Call when MazeCell is visited
    public void Visit(){
        isVisited = true;
    }

    //Calculates the vertices of the floor for the mesh, to align with the previous MazeCell and 
    //according to the elevation of the cell
    public void GenerateMesh(float elevation, Direction direction, Vector3[] prevcellVerts){
        switch(direction){
            case Direction.Right:
                SetVert(Vert.LeftBot, new Vector3(0, elevation + prevcellVerts[(int)Vert.RightBot].y, 0));
                SetVert(Vert.RightBot, new Vector3(1, 0, 0));
                SetVert(Vert.LeftTop, new Vector3(0, elevation + prevcellVerts[(int)Vert.RightTop].y, 1));
                SetVert(Vert.RightTop, new Vector3(1, 0, 1));
                break;
            case Direction.Left:
                SetVert(Vert.LeftBot, new Vector3(0, 0, 0));
                SetVert(Vert.RightBot, new Vector3(1, elevation + prevcellVerts[(int)Vert.LeftBot].y, 0));
                SetVert(Vert.LeftTop, new Vector3(0, 0, 1));
                SetVert(Vert.RightTop, new Vector3(1, elevation + prevcellVerts[(int)Vert.LeftTop].y, 1));
                break;
            case Direction.Bot:
                SetVert(Vert.LeftBot, new Vector3(0, 0, 0));
                SetVert(Vert.RightBot, new Vector3(1, 0, 0));
                SetVert(Vert.LeftTop, new Vector3(0, elevation + prevcellVerts[(int)Vert.LeftBot].y, 1));
                SetVert(Vert.RightTop, new Vector3(1, elevation + prevcellVerts[(int)Vert.RightBot].y, 1));
                break;
            case Direction.Top:
                SetVert(Vert.LeftBot, new Vector3(0, elevation + prevcellVerts[(int)Vert.LeftTop].y, 0));
                SetVert(Vert.RightBot, new Vector3(1, elevation + prevcellVerts[(int)Vert.RightTop].y, 0));
                SetVert(Vert.LeftTop, new Vector3(0, 0, 1));
                SetVert(Vert.RightTop, new Vector3(1, 0, 1));
                break;
            default:
                Debug.LogError("Error in GenerateMesh()");
                break;
        }

		cellTris[0] = (int)Vert.LeftBot;
		cellTris[1] = cellTris[4] = (int)Vert.LeftTop;
		cellTris[2] = cellTris[3] = (int)Vert.RightBot;
		cellTris[5] = (int)Vert.RightTop;
        UpdateMesh();
    }

    public void SetElevation(Elevation newElevation){
        cellElevation = newElevation;
    }

    void UpdateMesh(){
        mesh.Clear();
        mesh.vertices = cellVerts;
        mesh.triangles = cellTris;
        mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
    
    public void SetVert(Vert vert, Vector3 pos){
        cellVerts[(int)vert] = pos;
    }

    //Functions for the history of the MazeCell which is a path to this MazeCell from the first cell on the list
    public void SetHistory(List<MazeCell> newhistory){
        history = newhistory;
    }

    public List<MazeCell> GetHistory(){
        return history;
    }

    public int GetHistoryCount(){
        return history.Count;
    }

    public void AddHistory(MazeCell addition){
        history.Add(addition);
    }

    //Functions for the neighbours of the MazeCell, adjacent reachable cells
    public List<MazeCell> GetNeighbours(){
        return neighbours;
    }

    public void AddNeighbour(MazeCell neighbour){
        neighbours.Add(neighbour);
    }


    public void SetWallsColor(Color newColor){
        leftWall.GetComponentInChildren<Renderer>().material.color = newColor;
        rightWall.GetComponentInChildren<Renderer>().material.color = newColor;
        bottomWall.GetComponentInChildren<Renderer>().material.color = newColor;
        topWall.GetComponentInChildren<Renderer>().material.color = newColor;
        color = newColor;
    }

    //Returns one open direction, only needed for the endCell and keyCell, which only have one open direction.
    public Direction GetOpenDirection(){
        if(!topWall.activeSelf){
            return Direction.Top;
        }else if(!bottomWall.activeSelf){
            return Direction.Bot;
        }else if(!leftWall.activeSelf){
            return Direction.Left;
        }else{
            return Direction.Right;
        }
    }
}
