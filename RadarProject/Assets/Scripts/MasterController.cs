using System.Collections;
using UnityEngine;

public class MasterController : MonoBehaviour
{
    RadarController radarController;
    CSVController csvController;
    ScenarioController scenarioController;

    string[] scenes = {"OceanMain", "KhorfakkanCoastline"};

    void Start()
    {
        radarController = GetComponent<RadarController>();
        csvController = GetComponent<CSVController>();
        scenarioController = GetComponent<ScenarioController>();

        /*
        string[] args = System.Environment.GetCommandLineArgs();

        Debug.Log("CLI Arg Length :" + args.Length);
        foreach (var x in args)
        {
            Debug.Log(x);
        }
        */

        StartCoroutine(RunArguments());
    }

    IEnumerator RunArguments()
    {
        yield return null;

        // TODO: The below causes null pointer exceptions or the scenarios not loading
        //string sceneName = "";
        //if (SetStringArg("-sceneName", ref sceneName))
        //{
        //    SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        //}

        //CSV Controller Params
        if (SetIntListArg("-nships", out int minShips, out int maxShips))
        {
            minShips = Mathf.Clamp(minShips, 0, 120);
            maxShips = Mathf.Clamp(maxShips, 0, 120);
            csvController.numOfShips = new MinMax<int>(minShips, maxShips);
        }
           

        if (SetIntListArg("-nLocations", out int minLocation, out int maxLocation))
        {
            minLocation = Mathf.Clamp(minLocation, 5, 30);
            maxLocation = Mathf.Clamp(maxLocation, 5, 30);
            csvController.locationsToVisit = new MinMax<int>(minLocation, maxLocation);
        }

        SetFloatArg("-coordinateSquareWidth", ref csvController.coordinateSquareWidth);

        if (SetIntListArg("-speed", out int minSpeed, out int maxSpeed))
        {
            minSpeed = Mathf.Clamp(minSpeed, 1, 30);
            maxSpeed = Mathf.Clamp(maxSpeed, 1, 30);
            csvController.speedAtLocations = new MinMax<int>(minSpeed, maxSpeed);
        }

        //Radar Params
        SetIntArg("-radarRows", ref radarController.rows);
        SetFloatArg("-radarPower", ref radarController.transmittedPowerW);
        SetFloatArg("-radarGain", ref radarController.antennaGainDBi);
        SetFloatArg("-wavelength", ref radarController.wavelengthM);
        SetFloatArg("-radarLoss", ref radarController.systemLossesDB);
        SetIntArg("-radarImageRadius", ref radarController.ImageRadius);
        SetFloatArg("-verticalAngle", ref radarController.VerticalAngle);
        SetFloatArg("-beamWidth", ref radarController.BeamWidth);
        SetFloatArg("-rainRCS", ref radarController.rainRCS);

        int nRadars = 0;
        if (SetIntArg("-nRadars", ref nRadars))
        {
            radarController.GenerateRadars(nRadars);
        }

        //Scenario Params
        int nScenarios = 0;
        if (SetIntArg("-nScenarios", ref nScenarios))
        {
            //TODO: refactor scenario controller UI code to seperate method, callable from here
            //TODO: after generating scenarios automatically load them
            csvController.GenerateScenarios(nScenarios);
            scenarioController.LoadAllScenarios();
        }
        SetFloatArg("-scenarioTimeLimit", ref scenarioController.timeLimit);

        //TODO: Start the simulation
    }

    private bool SetIntArg(string argName, ref int parameter)
    {
        string arg = GetArg(argName);
        if (arg != null && int.TryParse(arg, out int value))
        {
            parameter = value;
            return true;
        }
        return false;
    }

    private bool SetFloatArg(string argName, ref float parameter)
    {
        string arg = GetArg(argName);
        if (arg != null && float.TryParse(arg, out float value))
        {
            parameter = value;
            return true;
        }
        return false;
    }

    private bool SetBoolArg(string argName, ref bool parameter)
    {
        string arg = GetArg(argName);
        if (arg != null && bool.TryParse(arg, out bool value))
        {
            parameter = value;
            return true;
        }
        return false;
    }

    private bool SetStringArg(string argName, ref string parameter)
    {
        string arg = GetArg(argName);
        
        if (arg.Equals("OceanMain") || arg.Equals("KhorfakkanCoastline"))
        {
            parameter = arg;
            return true;
        }

        return false;
    }

    private bool SetIntListArg(string argName, out int minShips, out int maxShips)
    {
        string arg = GetArg(argName);
        if (!string.IsNullOrEmpty(arg))
        {
            arg = arg.Trim('[', ']');
            string[] values = arg.Split(',');

            if (values.Length == 2)
            {
                if (int.TryParse(values[0].Trim(), out minShips) && int.TryParse(values[1].Trim(), out maxShips))
                {
                    return true; 
                }
            }
        }
        
        minShips = 0;
        maxShips = 0;
        return false;
    }

    private static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                if (args[i + 1].StartsWith("-") || args[i + 1].StartsWith("--")) return null;
                return args[i + 1];
            }
        }
        return null;
    }
}
