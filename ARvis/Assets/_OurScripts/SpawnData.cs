using UnityEngine;
using System.Collections.Generic;
using Mapbox.Utils;
using Mapbox.Unity.Map;

public class SpawnData : MonoBehaviour
{
    [Header("Abstract map. Drag 'Map Root' here")]
    public AbstractMap _map; //MapBox's abstract map. This is the object, the rest of our objects are created in relation too.

    [Header("Initial zoom level of the map")]
    public int _mapZoomLevel;

    [Header("Minimum and maximum y values for the cylinders. Controls their height.")]
    public float _scaledHigh; //The max height of the cylinders after being rescaled.
    public float _scaledLow; //The min height of the cylinders after being rescaled.

    [Header("Diamter of cylinders")]
    public float _cylinderScale; //Diameter of the cylinder.

    [Header("Input CSV- Format like this: 'id,lat,lon,value'")]
    public TextAsset _inputData;

    //All three following lists will be populated with data in the Start function, when the readCSV file is called. This only happens once during the runtime of the programme.
    private List<GameObject> _spawnedData;//list of spawned gameObjects, one for each row of the CSV file.
    private List<double[]> _dataValues; //For each data row in the CSV file, the lat, lon and data value is stored.
    private List<float> _dataPointValues;//List that specifically holds all the point data values, cast to float data.

    private void Awake() //We use the Awake function to make sure the data from the CSV is read and parsed before the map is spawned.
    {
        _spawnedData = new List<GameObject>();//Initialize list to hold all the gameobjects we spawn on the map
        _dataValues = new List<double[]>(); //Initialize list to containt the raw data from the CSV file - lat, lon, value.
        _dataPointValues = new List<float>();//Initializing the list of floats.

        //Calling the function that reads our CSV file and populate lists with data.
        readCSV();
    }


    void Start()
    {
        //Mapbox map is initialized and centered on the average coordinate of input data.
        _map.Initialize(calculateAverageCenter(), _mapZoomLevel);

        //Initial spawning of all the data points.
        foreach (var item in _dataValues)
        {
            _dataPointValues.Add((float)item[2]);
        }

        //The following two values 
        var high = highestValue(_dataPointValues);
        var low = lowestValue(_dataPointValues);

        foreach (var item in _dataValues)
        {
            //Rescale the data values to a given range.
            var rescaledValue = linearRescale(high, low, (float)item[2], _scaledHigh, _scaledLow);
            spawnData(convertToGamePos(item[0], item[1]), rescaledValue);
        }
    }

    void Update()
    {
        //Loop over all the datapoints every frame to update their position. This is necessary to keep all data located correcly on the map.
        int count = _spawnedData.Count;
        for (int i = 0; i < count; i++)
        {
           
            var spawnedData = _spawnedData[i];
            var pointData = _dataValues[i];
            var scale =_spawnedData[i].transform.position.y;
            var updatedPosition = convertToGamePos(pointData[0], pointData[1]);

            //Conditional check to see if the datapoints are within a given distance of the actual map.
            if ((updatedPosition.x < 4 && updatedPosition.x > -1) && (updatedPosition.z < 4 && updatedPosition.z > -2))
            {
                spawnedData.SetActive(true);
                updatedPosition.y = scale;
                spawnedData.transform.position = updatedPosition;
            }
            else //If the datapoints are outside the range, they will be made invisible.
            {
                spawnedData.SetActive(false);
                updatedPosition.y = scale;
                spawnedData.transform.position = updatedPosition;
            }
        }
    }

    //**************************************
    //**************************************
    //THIS IS THE FUNCTION FOR THE CHALLENGE - spawnData
    //**************************************
    //**************************************

    void spawnData(Vector3 positionGame, float dataValue)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.parent = gameObject.transform;
        cylinder.transform.localScale = new Vector3(_cylinderScale, dataValue, _cylinderScale);
        cylinder.transform.position = new Vector3(positionGame.x, cylinder.transform.localScale.y, positionGame.z);
        _spawnedData.Add(cylinder);

        //CHALLENGE:
        //See if you can change the colors of the cylinders based on their values.
        //Try coding a classification scheme like equal interval e.g. in a seperate helper function.
        //Hint: use the values stored in the _dataPointValues. The order of the values in this list corresponds to the order of the gameObjects in the _spawnedData list.
    }

    //**************************************
    //**************************************
    //**************************************
    //**************************************
    //**************************************

    void readCSV()
    {
        var textFile = Resources.Load<TextAsset>(_inputData.name);//Returns the whole CSV file as one long string.
        var dataLineSplit = textFile.text.Split('\n'); //Split the string at "new Line".

        var lineCount = 0;
        foreach (var dataRow in dataLineSplit) //For each line of the CSV, split at comma to seperate values, convert from string and add to list from above.
        {
            var data_values = dataRow.Split(',');
   
            if (lineCount > 1 && lineCount < dataLineSplit.Length - 2)
            {
                int id;
                var idInt = int.TryParse(data_values[0], out id);//Parse string value into integer.

                double lat;
                double lon;
                double value;
                double.TryParse(data_values[1], out lat); //Parse string values into double values.
                double.TryParse(data_values[2], out lon);
                double.TryParse(data_values[3], out value);
         
                _dataValues.Add(new double[3] { lat, lon, value }); //Add data to container
            }
            lineCount += 1;
        }
    }

    //HELPER FUNCTIONS
    //************************************************************************************************
    //1 - linearRescale
    //Linearly rescale the data values to put them in a range that translates better to world scale.
    //Use the output from the functions highestValue and lowestValue to compute observed highest and lowest. 
    //FORMULA: (newHighest - newLowest)/(observedHighest - observedLowest)*(currentValue - highestValue) + newHighest
    float linearRescale(float highest, float lowest, float current, float newHigh, float newLow)
    {
        float newvalue = (newHigh - newLow) / (highest - lowest) * (current - highest) + newHigh;
        return newvalue;
    }
    //************************************************************************************************

    //2 - highestValue. Returns the highest number in a list of float values.
    float highestValue(List<float> values)
    {
        float highest = Mathf.NegativeInfinity;
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] > highest)
            {
                highest = values[i];
            }
        }
        return highest;
    }
    //************************************************************************************************

    //3 - lowestValue. Returns the lowest number in a list of float values.
    float lowestValue(List<float> values)
    {
        float lowest = Mathf.Infinity;
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] < lowest)
            {
                lowest = values[i];
            }
        }
        return lowest;
    }
    //************************************************************************************************

    //4 - convertToGamepos. Returns a vector3 position vector for the game world space, given a real world coordinate input.
    //Vector3 position is relative to the abstractMap center position at spawn time.
    Vector3 convertToGamePos(double lat, double lon)
    {
        var newDataPoint = new Vector2d(lat, lon);
        return _map.GeoToWorldPosition(newDataPoint);
    }
    //************************************************************************************************

    //5 - calculateAverageCenter. 
    //This function will be used to initialize the map at the average center point of the input data.
    Vector2d calculateAverageCenter()
    {
        double avgLat = 0;
        double avgLon = 0;

        foreach (var item in _dataValues)
        {
            //Add every Latitude and longitude value of the data to the variables above.
            avgLat += item[0];
            avgLon += item[1];
        }

        //Divide the sum by the number of points to get the average.
        avgLat = avgLat / _dataValues.Count;
        avgLon = avgLon / _dataValues.Count;

        var averagePosition = new Vector2d(avgLat, avgLon);
        return averagePosition;
    }
    //************************************************************************************************
}