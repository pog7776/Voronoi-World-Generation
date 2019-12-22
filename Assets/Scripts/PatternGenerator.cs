using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PatternGenerator : MonoBehaviour{
    [SerializeField]
    private bool debugPattern = false;  // Show debug logs
    [SerializeField]
    private bool showMarkerPlacementDebug = false;  // Show coordinates of Cells in console
    
    public int _textureWidth = 255;     // Texture width
    public int _textureHeight = 255;    // Texture height
    public int _density = 10;           // Amount of Cells to create
    
    [SerializeField]
    private Vector2 startPos;
    [SerializeField]
    private bool update = false;
    [SerializeField]
    private bool circle = false;
    [SerializeField]
    private bool clear = false;
    [SerializeField]
    private int r = 1;
    [SerializeField]
    private int iterations = 1;

    private List<Cell> Cells;       // List of Cells (centre points of each cell)
    private Texture2D texture;          // Texture created
    private new Renderer renderer;      // Renderer of the current gameObject

    void Awake(){
        Cells = new List<Cell>();
    }

    void Start(){
        texture = new Texture2D(_textureWidth, _textureHeight);
        texture.name = "Generated Voronoi";
        renderer = GetComponent<Renderer>();

        RandomPoints();
        texture = PaintMarkers();
        renderer.material.SetTexture("_MainTex", texture);

        //SaveTextureAsPNG(texture, Application.dataPath + "/VoronoiGen.png");
    }

    void Update(){
        // Regenerate pattern every frame
        if(update){
            RandomPoints();
            texture = PaintMarkers();
            renderer.material.SetTexture("_MainTex", texture);
        }

        if(circle){
            Cells[0] = NewCell();
            CalculateCircle(Cells[0]);
            renderer.material.SetTexture("_MainTex", texture);
        }

        if(clear){
            // Clear the texture            
            // Colour everything white
            for (int x = 0; x < _textureWidth; x++){
                for (int y = 0; y < _textureHeight; y++){
                    texture.SetPixel(x, y, Color.white);
                }
            }
            texture.Apply();
            renderer.material.SetTexture("_MainTex", texture);
            r = 0;
            clear = false;
            if(debugPattern) Debug.Log("Texture cleared.");
        }
    }

    private void RandomPoints(){
        // Clear the markers list
        Cells.Clear();
        if(debugPattern) Debug.Log("Markers list cleared.");

        // Add a new Cell based off density.
        for(int i = 0; i < _density; i++){
            Cells.Add(NewCell());
        }
        if(debugPattern) Debug.Log(Cells.Count + " random points created.");

        // TODO padding to ensure Cells aren't too close together.
    }

    private Texture2D PaintMarkers(){
        // Clear the texture
        Texture2D tex = new Texture2D(_textureWidth, _textureHeight);
        if(debugPattern) Debug.Log("Texture cleared.");
        
        // Colour everything white
        for (int x = 0; x < _textureWidth; x++){
            for (int y = 0; y < _textureHeight; y++){
                tex.SetPixel(x, y, Color.white);
            }
        }

        // For each Cell, paint a pixel at it's coordinates
        foreach(Cell Cell in Cells){
            tex.SetPixel((int)Cell.centre.x, (int)Cell.centre.y, Color.black);

            if(showMarkerPlacementDebug) Debug.Log(Cell.name + "'s centre marked at " + (int)Cell.centre.x + ", " + (int)Cell.centre.y + " - set to black.");
        }

        // Save texture's new state
        tex.Apply();
        if(debugPattern) Debug.Log("Apply new texture.");
        return tex;
    }

    private void CalculateCircle(Cell Cell){
        //int r = 1;
        float x = Cell.centre.x +r;// = Cell.centre.x;
        float y = Cell.centre.y +r;// = Cell.centre.y;


        for (r = r; r < iterations; r++){
            // for (int i = 0; i < r; i++){
            //     x += Mathf.Lerp(Cell.centre.x + r, Cell.centre.x - r, 1f);
            //     y += Mathf.Lerp(Cell.centre.y + r, Cell.centre.y - r, 1f);
            //     texture.SetPixel((int)x, (int)y, Color.Lerp(Color.red, Color.blue, 1));
            // }

            for (int i = 0; i < iterations; i++){
            x += Mathf.Lerp(x + r, x - r, 1f);
            y += Mathf.Lerp(y + r, y - r, 1f);
            texture.SetPixel((int)x, (int)y, Color.red);
        }
        }
        texture.Apply();
        circle = false;
        
    }

    private Texture2D GenerateTexture(){
        throw new NotImplementedException();
    }

    // Generate new random Cell
    private Cell RandomCell(){
        Cell Cell = new Cell();
        Cell.id = Cells.Count;
        Cell.name = "Cell " + Cell.id;
        Cell.centre = new Vector2(Random.Range(0, _textureWidth), Random.Range(0, _textureHeight));
        Cell.color = new Color(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255));
        return Cell;
    }

    private Cell NewCell(){
        Cell Cell = new Cell();
        Cell.id = Cells.Count;
        Cell.name = "Cell " + Cell.id;
        Cell.centre = new Vector2(startPos.x, startPos.y);
        Cell.color = new Color(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255));
        return Cell;
    }

    /// <summary>
	/// Saves a texture as PNG
	/// </summary>
    public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath){
        byte[] _bytes =_texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length/1024  + "Kb was saved as: " + _fullPath);
    }
}

public class Cell{
    public int id;
    public string name;
    public Vector2 centre;
    public Color color;
}