using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public GameObject Timer;
    private TextMeshProUGUI timerText;
    // Start is called before the first frame update
    void Start()
    {
        timerText = Timer.GetComponent<TextMeshProUGUI>();
        timerText.text = GameSceneStaticData.timeRemaining.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
