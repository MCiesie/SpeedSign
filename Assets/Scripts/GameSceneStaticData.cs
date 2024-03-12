using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameSceneStaticData
{
    public static string path { get; set; }
    public static List<Color> letterColors { get; set; }
    public static double timeRemaining { get; set; }
    public static List<string> randomWords { get; set; }
    public static int wordIndex { get; set; }
    public static int letterIndex { get; set; }
    public static bool wrongFlag { get; set; }
    public static double wrongTimePassed { get; set; }
    public static bool readdata = false;
    public static string FastestWord;
    public static string SlowestWord;
    public static double FastestTime;
    public static double SlowestTime;
    public static double AverageTime;
    public static int WordsCompleted;
}
