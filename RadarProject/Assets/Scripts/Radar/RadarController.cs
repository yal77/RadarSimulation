using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crest;
using Crest.Examples;
using Unity.Mathematics;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.UIElements;

public class RadarController : MonoBehaviour
{
    [Header("Radars Information")]
    [SerializeField] GameObject radarPrefab;
    public int rows = 1;
    public int cols = 1;
    public int distanceBetweenRadars = 50;
    public int numOfRadars;

    [Header("Generate Radars")]
    [SerializeField] bool generateRadars = false;
    [SerializeField] bool generateOneRadar = false;

    [Header("Debug")]
    [SerializeField] int newRadarID = 0;

    GameObject parentEmptyObject;                   // Parent Object of the radars to easily rotate and move them
    List<List<int>> radarIDAtRow;
    public Dictionary<int, GameObject> radars = new();

    MainMenuController mainMenuController;
    SampleHeightHelper sampleHeightHelper = new();

    // Start is called before the first frame update
    void Start()
    {
        parentEmptyObject = new("Radars");

        radarIDAtRow = new();

        for (int i = 0; i < rows; i++)
        {
            radarIDAtRow.Add(new());
        }

        mainMenuController = FindObjectOfType<MainMenuController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (generateOneRadar)
        {
            GenerateRadar();
            generateOneRadar = false;
        }
        else if (generateRadars)
        {
            if (numOfRadars == 0)
                numOfRadars = rows * cols;
            GenerateRadars(numOfRadars);
            generateRadars = false;
        }
    }

    public void GenerateRadar()
    {
        if (radarPrefab == null) return;
        
        // Create Radar
        GameObject instance = Instantiate(radarPrefab);

        // Update Radar ID for the radar
        RadarScript radarScript = instance.GetComponentInChildren<RadarScript>();
        radarScript.radarID = newRadarID;

        // Get the row with the least radars and its index
        float min = math.INFINITY;
        int index = 0;
        for (int k = 0; k < radarIDAtRow.Count; k++)
        {
            if (radarIDAtRow[k].Count < min)
            {
                min = radarIDAtRow[k].Count;
                index = k;
            }
        }

        // Create radar at the row with least radars
        int latestRadarID = radarIDAtRow[index].LastOrDefault();
        if (radars.Keys.Contains(latestRadarID))
        {
            Vector3 latestRadarPosition = radars[latestRadarID].transform.position;

            // If min == 0 then the radar is on the same row
            if (min == 0)
                instance.transform.position = new Vector3(latestRadarPosition.x, 0, latestRadarPosition.z + (distanceBetweenRadars * index));
            else
                instance.transform.position = new Vector3(latestRadarPosition.x + distanceBetweenRadars, 0, latestRadarPosition.z);
        }

        radarIDAtRow[index].Add(newRadarID);

        // Make the new radar a child of parentEmptyObject
        instance.transform.parent = parentEmptyObject.transform;

        // Keep track of created radars
        radars[newRadarID] = instance;

        newRadarID++; // Update for the next radar generated to use
    }

    public void GenerateRadars(int numOfRadars = 1)
    {
        UnloadRadars();

        // Generate the new radars
        for (int i = 0; i < numOfRadars; i++)
        {
            GenerateRadar();
        }

        mainMenuController.SetRadarsLabel(numOfRadars);
        //Debug.Log($"{numOfRadars} radars have been generated");
    }

    public async void UpdateRadarsPositions()
    {
        foreach (KeyValuePair<int, GameObject> entry in radars)
        {
            Rigidbody rb = entry.Value.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            Vector3 position = entry.Value.transform.position;
            sampleHeightHelper.Init(position, 0, true);

            float o_height = await WaitForSampleAsync();

            entry.Value.transform.position = new Vector3(position.x, o_height, position.z);

            rb.isKinematic = false;
        }
    }

    async Task<float> WaitForSampleAsync()
    {
        float o_height;
        
        // Wait until we get a valid sample height
        while (!sampleHeightHelper.Sample(out o_height))
        {
            await Task.Delay(2);
        }

        return o_height;
    }

    public void UnloadRadars()
    {
        foreach (KeyValuePair<int, GameObject> entry in radars)
        {
            Destroy(entry.Value);
        }
        radars.Clear();
        
        radarIDAtRow.Clear();

        for (int i = 0; i < rows; i++)
        {
            radarIDAtRow.Add(new());
        }

        newRadarID = 0;
    }
}
