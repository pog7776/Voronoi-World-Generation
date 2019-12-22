using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PerlinGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField]
    private int width = 256;        // Width resolution of texture
    [SerializeField]    
    private int height = 256;       // Height resolution of texture
    [SerializeField]
    private float scale = 20f;      // Zoom of texture
    [SerializeField]
    private bool limitScale = true;
    [SerializeField]
    private float offsetX = 100f;   // X coord of visible texture
    [SerializeField]
    private float offsetY = 100f;   // Y coord of visible texture
    [SerializeField]
    private bool startRandomized = true;
    [SerializeField]
    private float colourThreshold = 0.5f;  // How bright does the pixel have to be to change colour
    [SerializeField]
    private bool limitThreshold = true;
    [Header("Display Settings")]
    [SerializeField]
    private bool colourTexture = true;     //Should the texture be coloured
    [SerializeField]
    private bool randomizeRange = false;
    [SerializeField]
    private bool update = false;
    [Header("Colour Settings")]
    [SerializeField]
    public List<Color> colours;

    private Texture2D texture;

    private new Renderer renderer;

    // Set instance
	private static PerlinGenerator _instance;
	public static PerlinGenerator Instance { get { return _instance; } }

    private void SetSingleton() {
		if (_instance != null && _instance != this) {
			Destroy(this.gameObject);
		} else {
			_instance = this;
		}
	}

    void Awake()
    {
        SetSingleton();
    }

    void Start(){
        CheckDimensions();
        CheckThreshold();
        CheckScale();
        if(startRandomized){
            offsetX = Random.Range(0f, 99999f);
            offsetY = Random.Range(0f, 99999f);
        }
        renderer = GetComponent<Renderer>();
        texture = GenerateTexture();
        //renderer.material.mainTexture = GenerateTexture();
        renderer.material.mainTexture = texture;
        SaveTextureAsPNG(texture, Application.dataPath + "/Scripts/WorldGen/NoiseGen.png");
    }

    void Update(){
        CheckDimensions();
        CheckThreshold();
        CheckScale();
        if(randomizeRange) RandomizeRange();

        if(update){
        texture = GenerateTexture();
        renderer.material.mainTexture = texture;
        }
    }

    /// <summary>
	/// Generate texture using Perlin Noise and colour based on brightness
	/// </summary>
    private Texture2D GenerateTexture(){
        texture = new Texture2D(width, height);                     // Create texture

        for(int x = 0; x < width; x++){                             // Iterate horizontally through texture
            for(int y = 0; y < height; y++){                        // Iterate vertically through texture
                Color color = CalculateColor(x, y);
                
                float brightness = GetBrightness(color);            // Calculate the brightness of a colour between 0 and 1
                if(colourTexture){
                    int i = 1;
                    foreach (Color _color in colours){
                        if(i == 1){
                            if(brightness < ((float)1/(float)colours.Count)*i+colourThreshold){
                            texture.SetPixel(x, y, _color);
                            }
                        }
                        else{
                            if(brightness > ((float)1/(float)colours.Count)*((i-1)+colourThreshold) && brightness < ((float)1/(float)colours.Count)*(i+colourThreshold)){
                                texture.SetPixel(x, y, _color);
                            }

                            if(brightness > 1){
                                texture.SetPixel(x, y, colours[colours.Count-1]);
                            }
                        }
                        i++;
                    }
                }
                else{
                    texture.SetPixel(x, y, color);      // For Black and White
                }
            }
        }
        
        texture.Apply();        // Save texture's new state
        return texture;
    }

    /// <summary>
	/// Calculate Perlin Noise texture
	/// </summary>
    private Color CalculateColor(int x, int y){
        float xCoord = (float)x/width * scale + offsetX;        
        float yCood = (float)y/height * scale + offsetY;
        float sample = Mathf.PerlinNoise(xCoord, yCood);    // Generate Perlin Noise based off x/y, scale and location offset
        return new Color(sample, sample, sample);           // Return grayscale pixel colour 0 - 1
    }

    private Color RandomColour(){
        Color sik = new Color(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255));
        return sik;
    }

    /// <summary>
	/// Ensure the dimensions of the texture isn't below 0
	/// </summary>
    private void CheckDimensions(){
        if(height <= 0){
            height = 1;
        }

        if(width <= 0){
            width = 1;
        }
    }

    private void CheckThreshold(){
        if(colourThreshold < 0 && limitThreshold){
            colourThreshold = 0;
        }
    }

    private void CheckScale(){
        if(scale < 0 && limitScale){
            scale = 0;
        }
    }

    /// <summary>
	/// Saves a texture as PNG
	/// </summary>
    public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath){
        byte[] _bytes =_texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        //Debug.Log(_bytes.Length/1024  + "Kb was saved as: " + _fullPath);
    }

    /// <summary>
	/// Randomizes location of visible texture
	/// </summary>
    private void RandomizeRange(){
        offsetX = Random.Range(0f, 99999f);
        offsetY = Random.Range(0f, 99999f);
    }

    /// <summary>
	/// Calculate the brightness of a colour between 0 and 1
	/// </summary>
    private float GetBrightness(Color color){
        float brightness;
        if(color.r + color.g + color.r == 0){
            brightness = 0;
        }
        else{
            brightness = (color.r + color.g + color.r)/3;
        }
        return brightness;
    }

    private Color ColourZeroToOne(Color colour){
        Color returnColour = new Color(colour.r/255, colour.g/255, colour.b/255);
        return returnColour;
    }

    /// <summary>
	/// Returns the generated texture
	/// </summary>
    public Texture2D GetTexture(){
        return texture;
    }
}
