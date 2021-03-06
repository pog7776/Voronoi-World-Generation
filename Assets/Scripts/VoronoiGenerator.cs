﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class VoronoiGenerator : MonoBehaviour{
    [Header("Debug")]
    [SerializeField]
    private bool debugVoronoi = false;  // Show debug logs
    [SerializeField]
    private bool showMarkerPlacementDebug = false;  // Show coordinates of Regions in console
    [SerializeField]
    private Text progressText;

    [Header("Centre Markers")]
    [SerializeField]
    private bool showMarkers = false;   // Show region centre points
    [SerializeField]
    private int markerSize = 0;
    
    [Header("Texture")]
    public int _textureWidth = 255;     // Texture width
    public int _textureHeight = 255;    // Texture height

    [Header("Mega Regions")]
    public bool _useMegaRegions = false;
    public Vector2Int _megaRegionThreshold = new Vector2Int(10,20);   // Threshold for mega regions
    [SerializeField]
    private bool showMegaRegion = false;
    private enum MegaRegionColourType{
        Solid,
        Pattern,
        Gradient
    }
    [SerializeField]
    MegaRegionColourType megaRegionColour = MegaRegionColourType.Solid;
    
    [SerializeField]
    private bool showMegaRegionLines = false;
    [SerializeField]
    private bool solidLine = false;
    [SerializeField]
    private float linePointDensity = 20f;

    [Header("Generation Settings")]
    public int _density = 10;           // Amount of Regions to create

    [SerializeField]
    private Slider densitySlider;

    private List<Region> regions;       // List of Regions (centre points of each cell)
    private List<MegaRegion> megaRegions; // List of MegaRegions (Collection of close regions)
    private Texture2D texture;          // Texture created
    private new Renderer renderer;      // Renderer of the current gameObject

    void Awake(){
        regions = new List<Region>();
        megaRegions = new List<MegaRegion>();
    }

    void Start(){
        // Initialize texture
        texture = new Texture2D(_textureWidth, _textureHeight);
        texture.name = "Generated Voronoi";
        renderer = GetComponent<Renderer>();

        GenerateTexture();
    }



    /// <summary>
	/// Generate voronoi texture.
	/// </summary>
    public void GenerateTexture(){
        texture = new Texture2D(_textureWidth, _textureHeight);
        RandomPoints();
        AllocatePixels();

        if(_useMegaRegions) CreateMegaRegions();

        // Colour each region
        if(!showMegaRegion){
            foreach(Region region in regions){
                ColourRegion(region);
            }
        }

        // MegaRegion Visualisations
        if(_useMegaRegions){
            MegaRegionLines();
            if(showMegaRegion) ColourMegaRegion();
            if(showMegaRegionLines )DrawLines();
        }

        PaintMarkers();
        texture.Apply();
        renderer.material.SetTexture("_MainTex", texture);
        // return texture;
    }



    /// <summary>
	/// Set the region density (amount of regions).
	/// </summary>
    public void SetDensity(int number = 0){
        _density = (int)densitySlider.value;
        if(number != 0) _density = number;
    }

    /// <summary>
	/// Save the Voronoi texture to Assets/VoronoiGen.png.
	/// </summary>
    public void SaveVoronoi(){
        SaveTextureAsPNG(texture, Application.dataPath + "/VoronoiSave/VoronoiGen-" + CleanInput(System.DateTime.Now.ToString()) + ".png");
    }

    private void RandomPoints(){
        UpdateProgressInformation("Creating region points.");
        // Clear the markers list
        regions.Clear();
        if(debugVoronoi) Debug.Log("Markers list cleared.");

        // Add a new Region based off density.
        for(int i = 0; i < _density; i++){
            regions.Add(RandomRegion());
        }
        if(debugVoronoi) Debug.Log(regions.Count + " random points created.");

        // TODO padding to ensure regions aren't too close together.
    }

    private void PaintMarkers(){
        UpdateProgressInformation("Drawing region markers.");
        // For each Region, paint a pixel at it's coordinates
        if(showMarkers){
            foreach(Region region in regions){
                for (int i = 0; i < markerSize; i++){
                    texture.SetPixel((int)region.centre.x+i, (int)region.centre.y, Color.black);
                    texture.SetPixel((int)region.centre.x, (int)region.centre.y+i, Color.black);
                    texture.SetPixel((int)region.centre.x-i, (int)region.centre.y+i, Color.black);
                    texture.SetPixel((int)region.centre.x+i, (int)region.centre.y+i, Color.black);
                    texture.SetPixel((int)region.centre.x-i, (int)region.centre.y, Color.black);
                    texture.SetPixel((int)region.centre.x, (int)region.centre.y-i, Color.black);
                    texture.SetPixel((int)region.centre.x-i, (int)region.centre.y-i, Color.black);
                    texture.SetPixel((int)region.centre.x+i, (int)region.centre.y-i, Color.black);
                }

                if(showMarkerPlacementDebug) Debug.Log(region.name + "'s centre marked at " + (int)region.centre.x + ", " + (int)region.centre.y + " - set to black.");
            }
        }

        // Save texture's new state
        //texture.Apply();
    }

    // Generate new random region
    private Region RandomRegion(){
        Region region = new Region();
        region.id = regions.Count;
        region.name = "Region " + region.id;
        region.centre = new Vector2Int(Random.Range(0, _textureWidth), Random.Range(0, _textureHeight));
        region.ownedNodes = new List<Node>();
        region.colour = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        return region;
    }

    private void AllocatePixels(){
        UpdateProgressInformation("Allocating region nodes.");
        // ! Change to each pixel find their closest region (multithread) then add them to region ownedPixels
        // Ensure there are regions in the regions list
        if(regions.Count > 0){
            // Iterate over every pixel of the image
            for (int x = 0; x < _textureWidth; x++){
                for (int y = 0; y < _textureHeight; y++){
                    StartCoroutine(FindOwnerRegion(new Vector2Int(x,y)));
                }
            }

            if(debugVoronoi){
                foreach(Region region in regions){
                    Debug.Log(region.name + " owns " + region.ownedNodes.Count + " pixels.");
                }
            }
        }
    }

    // IEnumerator for getting closest regions
    private IEnumerator FindOwnerRegion(Vector2Int point){
        // Add pixel to region ownedPixels
        Region closeOne = GetClosestRegion(point);
        closeOne.ownedNodes.Add(new Node(new Vector2Int(point.x, point.y), closeOne));
        yield return null;
    }
    

    // Function to get closest region
    private Region GetClosestRegion(Vector2Int point){
        Region closestRegion = regions[0];
        int closestDistance = PointDistance(point, closestRegion.centre);
        // For every region, check which is close
        foreach(Region region in regions){
            // Calculate distance from region
            int regionDistance = PointDistance(point, region.centre);

            // If region distance < closest distance replace closest region
            if(regionDistance < closestDistance){
                closestRegion = region;
                closestDistance = regionDistance;
            }
        }
        
        return closestRegion;
    }

    /// <summary>
	/// Return the distance between 2 points as an int
	/// </summary>
    private int PointDistance(Vector2Int point1, Vector2Int point2){
        return (int)(Mathf.Sqrt(Mathf.Pow((point1.x - point2.x), 2) + Mathf.Pow((point1.y - point2.y), 2)));
    }

    private void CreateMegaRegions(){
        UpdateProgressInformation("Creating MegaRegions.");
        megaRegions.Clear();
        // List of regions within distance threshold
        List<Region> closeRegions = new List<Region>();

        // Iterate through all regions
        foreach(Region region in regions){
            closeRegions.Add(region);

            // Compare other regions
            foreach(Region region1 in regions){
                // Don't check yourself dummy
                // And check if already part of a mega region
                if(region1 != region && !region.partOfMega && !region1.partOfMega){
                    int distance = PointDistance(region.centre, region1.centre);

                    // If within threshold add to closeRegions and mark as part of MegaRegion
                    if(distance > _megaRegionThreshold.x && distance < _megaRegionThreshold.y){
                        closeRegions.Add(region1);
                        region1.partOfMega = true;
                    }
                }
            }

            // Mark region as part of MegaRegion
            // Create MegaRegion and fill megaregion.ownedRegions with closeRegions
            if(closeRegions.Count > 1){
                region.partOfMega = true;
                MegaRegion megaRegion = CreateMegaRegion();
                
                // Copy regions to the ownedRegions (doing ownedRegions = closeRegions would reset it?)
                foreach(Region copyRegion in closeRegions){
                    megaRegion.ownedRegions.Add(copyRegion);
                }

                if(debugVoronoi) Debug.Log(megaRegion.name + " created containing " + megaRegion.ownedRegions.Count + " regions.");
            }
            // Clear closeRegions for next region check
            closeRegions.Clear();
        }
    }

    private MegaRegion CreateMegaRegion(){
        MegaRegion megaRegion = new MegaRegion();
        megaRegion.id = megaRegions.Count;
        megaRegions.Add(megaRegion);
        megaRegion.ownedRegions = new List<Region>();
        megaRegion.spinePoints = new List<Vector2Int>();
        megaRegion.name = "MegaRegion " + megaRegion.id;    // ! Add biome type when biome is added
        megaRegion.colour = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        return megaRegion;
    }

    // Colours a each pixel that belongs to a region of the region's colour
    private void ColourRegion(Region region){
        UpdateProgressInformation("Colouring Regions.");
        // Iterate through each owned pixel
        foreach(Node coord in region.ownedNodes){
            // Set the pixel to the colour of the region
            texture.SetPixel(coord.pos.x, coord.pos.y, region.colour);
        }
        //texture.Apply();
    }

    // For visualising MegaRegions
    private void ColourMegaRegion(){
        UpdateProgressInformation("Colouring MegaRegions.");

        int largestDistance = -1;

        foreach(MegaRegion megaRegion in megaRegions){
            foreach(Region region in megaRegion.ownedRegions){
                foreach(Node point in region.ownedNodes){
                    //Distance
                    GetDistanceFromSpine(point, megaRegion);
                    if(point.distanceFromSpine > largestDistance){
                        largestDistance = point.distanceFromSpine;
                    }
                }
            }

        // TODO make this dynamic based on the biome object
        int segment = largestDistance/5;    // Divide by number of sub biomes.

            foreach(Region region in megaRegion.ownedRegions){
                foreach(Node point in region.ownedNodes){

                    // Pattern
                    if(megaRegionColour == MegaRegionColourType.Pattern){
                        // If megaregion ID is even
                        if(megaRegion.id % 2 == 0){
                            // If x is even and y is odd
                            if(point.pos.x % 2 == 0 && !(point.pos.y % 2 == 0)){
                                texture.SetPixel(point.pos.x, point.pos.y, megaRegion.colour);
                            }
                        }
                        else{
                            // If y is even
                            if(point.pos.y % 2 == 0){// && !(point.pos.y % 2 == 0)){
                                texture.SetPixel(point.pos.x, point.pos.y, megaRegion.colour);
                            }
                        }
                    }

                    // Solid
                    if(megaRegionColour == MegaRegionColourType.Solid){
                        texture.SetPixel(point.pos.x, point.pos.y, megaRegion.colour);
                    }

                    //Distance
                    //! paint based on distance
                    if(megaRegionColour == MegaRegionColourType.Gradient){
                        if(point.distanceFromSpine <= segment){
                            texture.SetPixel(point.pos.x, point.pos.y, megaRegion.colour * new Color(0.2f,0.2f,0.2f));
                        }
                        
                        if(point.distanceFromSpine > segment && point.distanceFromSpine <= segment*2){
                            texture.SetPixel(point.pos.x, point.pos.y, megaRegion.colour * new Color(0.3f,0.3f,0.3f));
                        }
                        
                        if(point.distanceFromSpine > segment*2 && point.distanceFromSpine <= segment*3){
                            texture.SetPixel(point.pos.x, point.pos.y, megaRegion.colour * new Color(0.4f,0.4f,0.4f));
                        }
                        
                        if(point.distanceFromSpine > segment*3 && point.distanceFromSpine <= segment*4){
                            texture.SetPixel(point.pos.x, point.pos.y, megaRegion.colour * new Color(0.5f,0.5f,0.5f));
                        }
                        
                        if(point.distanceFromSpine > segment*4){
                            texture.SetPixel(point.pos.x, point.pos.y, megaRegion.colour * new Color(0.6f,0.6f,0.6f));
                        }
                    }
                }
            }
        }
        //texture.Apply();
    }

    private void MegaRegionLines(){
        UpdateProgressInformation("Calculating and Drawing MegaRegion spines.");
        // Find closest MegaRegion neighbour
        // Every megaregion
        foreach(MegaRegion megaRegion in megaRegions){
            // Every owned region
            foreach(Region region in megaRegion.ownedRegions){
                // Setup closest distance
                int closestDistance = -1;
                Region closestRegion = null;
                // Compare all other regions to region
                foreach(Region otherRegion in megaRegion.ownedRegions){
                    if(region != otherRegion && !otherRegion.drawnLine){
                        // Check distance
                        int distance = PointDistance(region.centre, otherRegion.centre);
                        // Set closest distance if next region is closer
                        if(distance < closestDistance || closestDistance == -1){
                            closestRegion = otherRegion;
                            closestDistance = distance;
                            region.drawnLine = true;
                        }
                    }
                }

                // Calculate Line
                if(closestRegion != null && closestRegion.centre != region.centre){
                    //List<Vector2Int> linePoints = new List<Vector2Int>();

                    // Line direction
                    Vector2 dir = closestRegion.centre - region.centre;
                    dir.Normalize();
                    float dist = Vector2.Distance(region.centre, closestRegion.centre);
                    
                    // Make lines solid for visualisation.
                    if(solidLine){
                        linePointDensity = closestDistance;
                    }

                    Vector2 increment = (dist/linePointDensity) * dir;
                    Vector2 nextValue =   region.centre;

                    for(int x=0; x<linePointDensity-1; x++) {
                        nextValue += increment;
                        int pointX = Mathf.RoundToInt(nextValue.x);
                        int pointY = Mathf.RoundToInt(nextValue.y);

                        megaRegion.spinePoints.Add(new Vector2Int(pointX, pointY));
                        //points.Add(startPoint+increment);
                    }

                    // Draw Lines
                    // if(showMegaRegionLines){
                    //     foreach(Vector2Int point in megaRegion.spinePoints){
                    //         texture.SetPixel(point.x, point.y, Color.white);
                    //     }
                    // }
                }
            }
        }
        //texture.Apply();
    }

    private void DrawLines(){
        foreach(MegaRegion megaRegion in megaRegions){
            // Draw Lines
            foreach(Vector2Int point in megaRegion.spinePoints){
                texture.SetPixel(point.x, point.y, Color.white);
            }
        }
    }

    // 
    private void GetDistanceFromSpine(Node point, MegaRegion megaRegion){
        // Set closest spinePoint to point 0
        int closestDistance = PointDistance(point.pos, megaRegion.spinePoints[0]);

        foreach(Vector2Int spinePoint in megaRegion.spinePoints){
            int distance = PointDistance(point.pos, spinePoint);
            if(distance < closestDistance){
                closestDistance = distance;
            }
        }
        point.distanceFromSpine = closestDistance;
    }

    /// <summary>
	/// Saves a texture as PNG
	/// </summary>
    private static void SaveTextureAsPNG(Texture2D _texture, string _fullPath){
        byte[] _bytes =_texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length/1024  + "Kb was saved as: " + _fullPath);
    }

    static string CleanInput(string strIn){
        // Replace invalid characters with empty strings.
        try {
            return Regex.Replace(strIn, @"[^\w\.@-]", "", 
            RegexOptions.None, TimeSpan.FromSeconds(1.5)); 
        }
        // If we timeout when replacing invalid characters, 
        // we should return Empty.
        catch (RegexMatchTimeoutException) {
           return String.Empty;   
        }
    }
    
    private void UpdateProgressInformation(String progress){
        //Debug.Log(progress);
        progressText.text = progress;
    }
}

public class Region{
    public int id;
    public string name;
    public Vector2Int centre;
    public List<Node> ownedNodes;
    public Color colour;    //! Change to biome ... or not? biome colour? idk, maybe a list of colours?
    public bool partOfMega = false;
    public bool drawnLine = false;
}

public class MegaRegion{
    public int id;
    public string name;
    public List<Region> ownedRegions;
    public List<Vector2Int> spinePoints;
    public Color colour;    //! Change to biome
}

public class Node{
    public Vector2Int pos;
    public Region closestRegion;
    public int distanceFromSpine;

    public Node(Vector2Int pos, Region closestRegion = null){
        this.pos = pos;
        this.closestRegion = closestRegion;
    }
}

// TODO Convert generator to using Node instead of Vector2Ints
// TODO Multithread
// TODO Progress bar/text
// TODO Save different textures and swap them for different colourings (region, megaregion)