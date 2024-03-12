using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine.Assertions;
using Manus;
using Python.Runtime;
using Unity.VisualScripting;
using UnityEngine.Events;
using ColorUtility = UnityEngine.ColorUtility;

public class GameSceneKeyListener : MonoBehaviour
{
    private static bool isInitialized = false;
    private readonly Color defaultColor = Color.white;
    private readonly Color correctColor = Color.green;
    private readonly Color wrongColor = Color.red;
    string path;
    List<string> words = new();
    List<string> randomWords = new();
    int wordIndex = 0;
    int letterIndex = 0;
    bool wrongFlag = false;
    private const double STARTINGTIME = 90.0d;
    private double timeRemaining = STARTINGTIME;
    private double wrongTimePassed = 0;
    private readonly double delayTime = 1.0d;
    private double startingTimeCurrentWord = STARTINGTIME;
    private double fastestTime = Double.PositiveInfinity;
    private string fastestWord;
    private double slowestTime = Double.NegativeInfinity;
    private string slowestWord;
    private enum Alphabet {
        a,b,c,d,e,f,i,k,l,m,n,o,q,r,s,t,u,v,w,x,y
    }
    List<Color> letterColors = new();
    public GameObject currentWord;
    public GameObject Timer;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI currentWordText;
    private UnityAction<CommunicationHub.ErgonomicsStream> ergonomicsAction;
    private dynamic hmu;
    private Frame frame = new();
    // Start is called before the first frame update
    public void Start()
    {
        currentWordText = currentWord.GetComponent<TextMeshProUGUI>();
        timerText = Timer.GetComponent<TextMeshProUGUI>();
        import_prediction_module();
        ergonomicsAction += ErgonomicsActionFun;
        ManusManager.communicationHub.onErgonomicsData.AddListener(ergonomicsAction);
        if (!isInitialized)
        {
            InitializeExample();
            Debug.Log("event should fire");
            isInitialized = true;
        }
        else
        {
            ReadDataOrInitialize();
        }
        //currentWord = GameObject.Find("CurrentWord"); // Text for displaying the current word; If GameObject can't be found re-check the name!
        //Timer = GameObject.Find("Timer"); // Text for displaying the remaining time.
    }
    
    private void ErgonomicsActionFun(CommunicationHub.ErgonomicsStream stream)
    {
        //Debug.Log("event fired");
        if (stream.data.Count > 1) 
        {
            Debug.Log("there are too many gloves connected");
            return;
        } else if (stream.data.Count == 0) 
        {
            Debug.Log("there are no gloves connected");
            return;
        }
        else
        {
            frame.frame = stream.data[0].data;
        }
    }

    private void import_prediction_module()
    {
        if (!PythonEngine.IsInitialized)
        {
            Runtime.PythonDLL = "python39.dll";
            PythonEngine.Initialize();
        }
        //Runtime.PythonDLL = @"C:\Users\flori\AppData\Local\Programs\Python\Python39\python39.dll"; 
        //Runtime.PythonDLL = "python39.dll";
        //Debug.Log("running");
        //float[] input = getLeftErgoData();
        using (Py.GIL()) {
            //dynamic os = Py.Import("os");
            //os.environ["PYTHONPATH"] =  new PyString(@"C:\Users\flori\UnityProjects\Buchstabieren3D");
            //Debug.Log(os.environ["PYTHONPATH"]);
            //var env = os.getcwd();
            //var env2 = new PyString(env).ToString();
            //var env3 = os;
            //Debug.Log(env2);
            hmu = Py.Import("florian_disect");
            //var environ = hmu.predict(input);
            //var environ2 = new PyInt(environ).ToString();
            //Debug.Log(environ2);
        }
        //Debug.Log("done");
    }

    private long MakePrediction()
    {
        using (Py.GIL())
        {
            dynamic numpy = Py.Import("numpy");
            var floats = new List<float>(GetLeftErgoData());
            
            var array = numpy.array(floats, dtype: numpy.float32);
            var environ = hmu.predict(array);
            //var environ2 = new PyInt(environ).ToString();
            //Debug.Log(environ);
            long prediction = new PyInt(environ).ToInt64();
            //Debug.Log(environ);
            //Debug.Log("prediction: " + prediction);
            
            return prediction;
        }
    }
    
    

    private void GetNewWordOrdering() {
        randomWords = new(words);
        System.Random rng = new();
        int n = randomWords.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (randomWords[n], randomWords[k]) = (randomWords[k], randomWords[n]);
        }
    }

    private void InitializeExample() {
        path = "Assets/Words/text.txt";
        StreamReader wordReader = new(path, true);
        string line;
        while ((line = wordReader.ReadLine()) != null) {
            words.Add(line);
        }
        GetNewWordOrdering();
        wordIndex = 0;
        letterIndex = 0;
        InitializeLetterColors();
        SetText();
    }
    private void InitializeLetterColors() {
        int wordlength = randomWords[wordIndex].Length;
        letterColors.Clear();
        for (int i = 0; i < wordlength; i++) {
            letterColors.Add(defaultColor);
        }
    }

    void PrintInfo() {
        Debug.Log("wordIndex: " + wordIndex);
        Debug.Log("letterIndex: " + letterIndex);
    }

    // Update is called once per frame
    public void Update()
    {
        //Debug.Log(PredictionToLetter(MakePrediction()));
        TimerCountdown();
        TryReleaseWrongColor();
        TryMoveToNextWord();
        DetectInput();
    }

    private void TimerCountdown() {
        timeRemaining -= Time.deltaTime;
        timerText.text = string.Format("{0:0.00}", timeRemaining);
        if (timeRemaining <= 0)
        {
            SaveStatsToStatic();
            UnityUtility.MoveToScene(3);
        }
    }
    private void TryReleaseWrongColor() {
        if (wrongFlag) {
            wrongTimePassed += Time.deltaTime;
            if (wrongTimePassed >= delayTime) {
                wrongTimePassed = 0;
                wrongFlag = false;
                SetColorAtIndex(defaultColor, letterIndex);
            }
        }
    }
    private void TryMoveToNextWord() {
        if (letterIndex == randomWords[wordIndex].Length) {
            UpdateStats();
            wordIndex++;
            if (wordIndex == randomWords.Count) {
                SaveStatsToStatic();
                UnityUtility.MoveToScene(3);
                return;
            }
            letterIndex = 0;
            InitializeLetterColors();
            SetText();
        }
    }

    private void UpdateStats()
    {
        double timeForLastWord = startingTimeCurrentWord - timeRemaining;

        if (timeForLastWord < fastestTime)
        {
            fastestTime = timeForLastWord;
            fastestWord = randomWords[wordIndex];
        }

        if (timeForLastWord > slowestTime)
        {
            slowestTime = timeForLastWord;
            slowestWord = randomWords[wordIndex];
        }
            
            
        startingTimeCurrentWord = timeRemaining;
    }
    private void DetectInput() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            SaveDataToStatic();
            UnityUtility.MoveToScene(2); // 2: PauseMenu
        } else if (Input.GetKeyDown(KeyCode.A)) {
            DetectLetter(Alphabet.a);
        } else if (Input.GetKeyDown(KeyCode.B)) {
            DetectLetter(Alphabet.b);
        } else if (Input.GetKeyDown(KeyCode.C)) {
            DetectLetter(Alphabet.c);
        } else if (Input.GetKeyDown(KeyCode.Backspace)) {
            SetText();
        } else if (Input.GetKeyUp(KeyCode.Backspace)) {
            SetColorAtIndex(defaultColor, 0);
        } else if (Input.GetKeyUp(KeyCode.Space))
        {
            DetectLetter(PredictionToLetter(MakePrediction()));
        }
    }

    private Alphabet PredictionToLetter(long prediction) {
        if (prediction < 0 || prediction > 20)
        {
            throw new UnexpectedEnumValueException<string>("model prediction was outside of the expected range.");
        }
        return (Alphabet) prediction;
    }

    private void DetectLetter(Alphabet letter) {
        if (letter.ToString()[0] == randomWords[wordIndex][letterIndex]) {
            SetColorAtIndex(correctColor, letterIndex);
            letterIndex++;
        }
        else {
            SetColorAtIndex(wrongColor, letterIndex);
            wrongTimePassed = 0.0d;
            wrongFlag = true;
        }
    }

    private void SetText() {
        string temp = "";
        int wordlength = randomWords[wordIndex].Length;
        Debug.Assert(letterColors.Count == wordlength);
        for (int i = 0; i < wordlength; i++) {
            temp += "<color=#";
            temp += ColorUtility.ToHtmlStringRGB(letterColors[i]);
            temp += ">";
            temp += randomWords[wordIndex].ToCharArray()[i];
            temp += "</color>";
        }
        currentWordText.text = temp;
        //Debug.Log(currentWordText.text);
    }

    private void SetColorAtIndex(Color color, int index) {
        letterColors[index] = color;
        SetText();
    }
    private class Frame {
        private const int FRAME_SIZE = 40;
        public float[] frame = new float[FRAME_SIZE];
    }

    private void SaveDataToStatic()
    {
        GameSceneStaticData.path            = path;
        GameSceneStaticData.letterColors    = letterColors;
        GameSceneStaticData.timeRemaining   = timeRemaining;
        GameSceneStaticData.randomWords     = randomWords;
        GameSceneStaticData.wordIndex       = wordIndex;
        GameSceneStaticData.letterIndex     = letterIndex;
        GameSceneStaticData.wrongFlag = wrongFlag;
        GameSceneStaticData.wrongTimePassed = wrongTimePassed;
        GameSceneStaticData.AverageTime = STARTINGTIME / wordIndex;
        GameSceneStaticData.WordsCompleted = wordIndex;
        GameSceneStaticData.FastestTime = fastestTime;
        GameSceneStaticData.FastestWord = fastestWord;
        GameSceneStaticData.SlowestTime = slowestTime;
        GameSceneStaticData.SlowestWord = slowestWord;
        
        GameSceneStaticData.readdata = true;
    }

    private void ReadDataOrInitialize()
    {
        if (GameSceneStaticData.readdata)
        {
            path = GameSceneStaticData.path;
            letterColors = GameSceneStaticData.letterColors;
            timeRemaining = GameSceneStaticData.timeRemaining;
            randomWords = GameSceneStaticData.randomWords;
            wordIndex = GameSceneStaticData.wordIndex;
            letterIndex = GameSceneStaticData.letterIndex;
            wrongFlag = GameSceneStaticData.wrongFlag;
            wrongTimePassed = GameSceneStaticData.wrongTimePassed;
            fastestTime = GameSceneStaticData.FastestTime;
            fastestWord = GameSceneStaticData.FastestWord;
            slowestTime = GameSceneStaticData.SlowestTime;
            slowestWord = GameSceneStaticData.SlowestWord;
        }
        else
        {
            InitializeExample();
        }
        SetText();
    }

    private void SaveStatsToStatic()
    {
        if (wordIndex == 0)
        {
            GameSceneStaticData.AverageTime = STARTINGTIME;
        }
        else
        {
            GameSceneStaticData.AverageTime = (STARTINGTIME - timeRemaining) / wordIndex;
        }
        GameSceneStaticData.WordsCompleted = wordIndex;
        GameSceneStaticData.FastestTime = fastestTime;
        GameSceneStaticData.FastestWord = fastestWord;
        GameSceneStaticData.SlowestTime = slowestTime;
        GameSceneStaticData.SlowestWord = slowestWord;
    }
    
    /*
     * Take the first 20 Elements from the currentData frame (manus 
     */
    private float[] GetLeftErgoData()
    {
        return frame.frame.Take(20).ToArray();
    }
}
