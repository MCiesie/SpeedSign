using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    public GameObject wordsCompleted;
    public GameObject averageTime;
    public GameObject slowestTime;
    public GameObject slowestWord;
    public GameObject fastestTime;
    public GameObject fastestWord;
    // Start is called before the first frame update
    void Start()
    {
        wordsCompleted.GetComponent<TextMeshProUGUI>().text = GameSceneStaticData.WordsCompleted.ToString(); 
        averageTime.GetComponent<TextMeshProUGUI>().text = string.Format("{0:0.00}", GameSceneStaticData.AverageTime);
        slowestTime.GetComponent<TextMeshProUGUI>().text = string.Format("{0:0.00}", GameSceneStaticData.SlowestTime);
        slowestWord.GetComponent<TextMeshProUGUI>().text = GameSceneStaticData.SlowestWord;
        fastestTime.GetComponent<TextMeshProUGUI>().text = string.Format("{0:0.00}", GameSceneStaticData.FastestTime);
        fastestWord.GetComponent<TextMeshProUGUI>().text = GameSceneStaticData.FastestWord;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
