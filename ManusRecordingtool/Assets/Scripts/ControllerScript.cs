using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System.Text;
using UnityEngine.Events;
using Manus;
using UnityEngine.Assertions;
using System.Linq;
using Palmmedia.ReportGenerator.Core.Common;
using UnityEngine.UI;
using TMPro;

public class ControllerScript : MonoBehaviour
{
    private string path;
    int ergocounter = 0;
    int skeletoncounter = 0;
    private double dataratePerSecond = 30;
    private double timePassed = 0;
    private int currentRepeat = 0;
    private const int REPEATS = 3;

    private readonly String[] SIGNARRAY = {
        "1",
        "x",
        "1_curved",
        "1_flat",
        "3",
        "4",
        "4_curved",
        "4_flat",
        "5",
        "5_curved",
        "5_flat_spread",
        "5_stacked",
        "7",
        "8",
        "8_open",
        "a",
        "a_open",
        "b_closed",
        "b",
        "b_flat",
        "b_open",
        "c",
        "d",
        "e",
        "e_closed",
        "e_open",
        "e_open_spread",
        "e_spread",
        "f",
        "f_open",
        "goody_goody",
        "h_curved",
        "h_flat",
        "h_open",
        "horns",
        "horns_flat",
        "horns_open",
        "i",
        "ily",
        "ily_flat",
        "k",
        "l",
        "l_bent",
        "l_curved",
        "m",
        "m_flat",
        "n",
        "n_flat",
        "o",
        "o_baby",
        "beak",
        "q",
        "r",
        "s",
        "t",
        "u",
        "v",
        "v_bent",
        "v_curved",
        "v_flat",
        "w",
        "y"
    };

    private String[] randomSigns = {};
    private CSVBuffer buffer = new();
    private int currentIndex = 0;
    private int currCSVId = 0;
    private UnityAction<Manus.CommunicationHub.ErgonomicsStream> ergonomicsAction;
    private UnityAction<CoreSDK.RawSkeletonStream> rawSkeletonAction;

    // Start is called before the first frame update
    void Start()
    {
        string userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        path = userProfile + "\\Downloads";
        GetNewRandomSigns();
        Debug.Log(path);
        SelectFilePath();
        currCSVId = Directory.GetFiles(path + "/ManusRecordings/Left/A").Length;
        ergonomicsAction += ErgonomicsActionFun;
        rawSkeletonAction += RawSkeletonActionFun;
        ManusManager.communicationHub.onErgonomicsData.AddListener(ergonomicsAction);
        ManusManager.communicationHub.onRawSkeletonData.AddListener(rawSkeletonAction);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space)) {
            RecordData();
        }
        if (Input.GetKeyUp(KeyCode.Space)) {
            WriteDataToCSV();
        }
        if (Input.GetKeyDown(KeyCode.A)) {
            PrintStreamInfo();
        }
        if (Input.GetKeyDown(KeyCode.Backspace)) {
            GoBackOneSign();
        }
    }

    private void ErgonomicsActionFun(CommunicationHub.ErgonomicsStream stream) {
        ergocounter++;
        buffer.ergonomicsStream = stream;
    }
    private void RawSkeletonActionFun(CoreSDK.RawSkeletonStream stream) {
        skeletoncounter++;
        buffer.rawSkeletonStream = stream;
    }

    public void SelectFilePath() {
        string newPath = EditorUtility.OpenFolderPanel("Wähle Speicherpfad für Aufnahmen", path, "");
        if (newPath == "") {
            return;
        }
        path = newPath;
        Debug.Log("Gewählter Pfad: " + path);
        
        Directory.CreateDirectory(path + "/ManusRecordings");
        Directory.CreateDirectory(path + "/ManusRecordings" + "/Left");
        Directory.CreateDirectory(path + "/ManusRecordings" + "/Right");

        foreach (String signValue in SIGNARRAY) {
            Directory.CreateDirectory(path + "/ManusRecordings/Left/" + signValue);
            Directory.CreateDirectory(path + "/ManusRecordings/Right/" + signValue);
        }
        if (!File.Exists(path + "/ManusRecordings/counter.txt")) {
            File.WriteAllText(path + "/ManusRecordings/counter.txt", "0");
            currCSVId = 0;
        } else {
            currCSVId = File.ReadAllText(path + "/ManusRecordings/counter.txt").ParseLargeInteger();
        }
    }
    private void RecordData() {
        timePassed += Time.deltaTime;
        if (timePassed > 1.0 / dataratePerSecond) {
            timePassed = 0;
            buffer.AddData();
            //Debug.Log("data recorded");
        }
    }
    private void WriteDataToCSV() {
        File.WriteAllText(path + "/ManusRecordings/Left/" + randomSigns[currentIndex] + "/" + currCSVId + "_" + currentRepeat + ".csv", buffer.GetLeftData());
        File.WriteAllText(path + "/ManusRecordings/Right/" + randomSigns[currentIndex] + "/" + currCSVId + "_" + currentRepeat + ".csv", buffer.GetRightData());
        buffer.FlushBuffer();
        currentRepeat += 1;
        if (currentRepeat < REPEATS) {
            Debug.Log("Current Sign " + randomSigns[currentIndex] + " Repeat:" + currentRepeat + " Index: " + currentIndex);
            return;
        }

        currentIndex += 1;
        currentRepeat = 0;
        if (currentIndex >= randomSigns.Length) {
            currentIndex = 0;
            GetNewRandomSigns();
            currCSVId += 1;
            File.WriteAllText(path + "/ManusRecordings/counter.txt", currCSVId.ToString());
        }
        Debug.Log("Current Sign " + randomSigns[currentIndex] + " Repeat:" + currentRepeat + " Index: " + currentIndex);
        Debug.Log("Current File Identifier: " + currCSVId);
        GUIUtility.systemCopyBuffer = "https://sign-parametrization.netlify.app/recording/" + randomSigns[currentIndex];
        Debug.Log("data written");
    }

    private void PrintStreamInfo() {
        CommunicationHub.ErgonomicsStream ergonomicsStream = buffer.ergonomicsStream;
        Debug.Log(ergonomicsStream.data.Count);
        if (ergonomicsStream.data.Count > 0) {
            for (int index = 0; index < ergonomicsStream.data.Count; index++) {
                CoreSDK.ErgonomicsData data = ergonomicsStream.data[index];
                Debug.Log("ergonomics id: " + data.id);
                for (int ergonomicsDataTypeIndex = 0; ergonomicsDataTypeIndex < (int) CoreSDK.ErgonomicsDataType.MAX_SIZE; ergonomicsDataTypeIndex++) {
                    CoreSDK.ErgonomicsDataType dataType = (CoreSDK.ErgonomicsDataType) ergonomicsDataTypeIndex;
                    Debug.Log(dataType + ": " + data.data[ergonomicsDataTypeIndex]);
                }
                Debug.Log("moving to next stream entry..");
            }
        }
        //Debug.Log("end of stream reached.");
        //CoreSDK.SkeletonStream skeletonStream = buffer.skeletonStream;
        CoreSDK.RawSkeletonStream rawSkeletonStream = buffer.rawSkeletonStream;
        //Debug.Log("comparing raw vs 'processed' skeletonstream");
        //Debug.Log(skeletonStream.skeletons.Count);
        Debug.Log(rawSkeletonStream.skeletons.Count);
        //Debug.Assert(skeletonStream.skeletons.Count > 0 && rawSkeletonStream.skeletons.Count > 0);
        for (int index = 0; index < rawSkeletonStream.skeletons.Count; index++) {
            CoreSDK.RawSkeleton rawSkeleton = rawSkeletonStream.skeletons[index];
            //Debug.Log(rawSkeleton.nodes.Length);
            ManusManager.communicationHub.GetRawSkeletonNodeInfo(rawSkeleton.gloveId, out CoreSDK.NodeInfo[] nodeInfos);
            Debug.Log("rawskeleton id: " + rawSkeleton.gloveId);
            //Debug.Log(nodeInfos.Length);
            Debug.Log(nodeInfos[0].side);
            //for (int nodeIndex = 0; nodeIndex < nodeInfos.Length; nodeIndex++) {
            //    CoreSDK.NodeInfo currentInfo = nodeInfos[nodeIndex];
            //    Debug.Log("===");
            //    Debug.Log(currentInfo.fingerJointType);
            //    Debug.Log(currentInfo.side);
            //    Debug.Log(currentInfo.chainType);
            //}
            Debug.Log("moving to next stream entry..");
        }
    }
    private void GetNewRandomSigns() {
        Array.Resize(ref randomSigns, SIGNARRAY.Length);
        System.Random random = new();
        Array.Copy(SIGNARRAY, randomSigns, SIGNARRAY.Length);
        randomSigns = randomSigns.OrderBy(x => random.Next()).ToArray();
        currentIndex = 0;
        Debug.Log("Current Sign " + randomSigns[currentIndex] + " Repeat:" + currentRepeat + " Index: " + currentIndex);
        GUIUtility.systemCopyBuffer = "https://sign-parametrization.netlify.app/recording/" + randomSigns[0];
    }
    public void GoBackOneSign() {
        currentRepeat = 0;
        if (currentIndex > 0) {
            currentIndex -= 1;
        }
        Debug.Log("Current Sign " + randomSigns[currentIndex] + " Repeat:" + currentRepeat + " Index: " + currentIndex);
        GUIUtility.systemCopyBuffer = "https://sign-parametrization.netlify.app/recording/" + randomSigns[currentIndex];
    }
}

public class CSVBuffer {
    public CommunicationHub.ErgonomicsStream ergonomicsStream;
    //public CoreSDK.SkeletonStream skeletonStream;
    public CoreSDK.RawSkeletonStream rawSkeletonStream;

    private class CSVLine {
        public float[] frame = {};
    }
    private List<String> csvColumnNames = new(){
        "ThumbMCPSpread",
        "ThumbMCPStretch",
        "ThumbPIPStretch",
        "ThumbDIPStretch",
        "IndexMCPSpread",
        "IndexMCPStretch",
        "IndexPIPStretch",
        "IndexDIPStretch",
        "MiddleMCPSpread",
        "MiddleMCPStretch",
        "MiddlePIPStretch",
        "MiddleDIPStretch",
        "RingMCPSpread",
        "RingMCPStretch",
        "RingPIPStretch",
        "RingDIPStretch",
        "PinkyMCPSpread",
        "PinkyMCPStretch",
        "PinkyPIPStretch",
        "PinkyDIPStretch",
        "Hand_X",
        "Hand_Y",
        "Hand_Z",
        "Hand_R1",
        "Hand_R2",
        "Hand_R3",
        "Hand_R4",
        "Hand_S1",
        "Hand_S2",
        "Hand_S3",
        "Thumb_Metacarpal_X",
        "Thumb_Metacarpal_Y",
        "Thumb_Metacarpal_Z",
        "Thumb_Metacarpal_R1",
        "Thumb_Metacarpal_R2",
        "Thumb_Metacarpal_R3",
        "Thumb_Metacarpal_R4",
        "Thumb_Metacarpal_S1",
        "Thumb_Metacarpal_S2",
        "Thumb_Metacarpal_S3",
        "Thumb_Proximal_X",
        "Thumb_Proximal_Y",
        "Thumb_Proximal_Z",
        "Thumb_Proximal_R1",
        "Thumb_Proximal_R2",
        "Thumb_Proximal_R3",
        "Thumb_Proximal_R4",
        "Thumb_Proximal_S1",
        "Thumb_Proximal_S2",
        "Thumb_Proximal_S3",
        "Thumb_Distal_X",
        "Thumb_Distal_Y",
        "Thumb_Distal_Z",
        "Thumb_Distal_R1",
        "Thumb_Distal_R2",
        "Thumb_Distal_R3",
        "Thumb_Distal_R4",
        "Thumb_Distal_S1",
        "Thumb_Distal_S2",
        "Thumb_Distal_S3",
        "Thumb_Tip_X",
        "Thumb_Tip_Y",
        "Thumb_Tip_Z",
        "Thumb_Tip_R1",
        "Thumb_Tip_R2",
        "Thumb_Tip_R3",
        "Thumb_Tip_R4",
        "Thumb_Tip_S1",
        "Thumb_Tip_S2",
        "Thumb_Tip_S3",
    };
    private static String[] fingers = {"Index", "Middle", "Ring", "Pinky"};
    private static String[] joints = {"Metacarpal", "Proximal", "Intermediate", "Distal", "Tip"};
    private static String[] endings = {"X", "Y", "Z", "R1", "R2", "R3", "R4", "S1", "S2", "S3"};
    private List<CSVLine> currentRecordingLeft = new();
    private List<CSVLine> currentRecordingRight = new();

    public CSVBuffer() {
        //thumb is hardcoded.
        foreach(String finger in fingers) {
            foreach (String joint in joints) {
                foreach (String ending in endings) {
                    csvColumnNames.Add(finger + "_" + joint + "_" + ending);
                }
            }
        }
        Debug.Assert(csvColumnNames.Count == 270);
    }

    public void AddData() {
        const int NUMHANDS = 2;
        Debug.Assert(ergonomicsStream.data.Count == NUMHANDS);
        Debug.Assert(rawSkeletonStream.skeletons.Count == NUMHANDS);
        for (int index = 0; index < NUMHANDS; index++) {
            CoreSDK.ErgonomicsData ergoData = ergonomicsStream.data[index];
            Debug.Log(ergoData.data[19]);
            Debug.Log(ergoData.data[20]);
            CoreSDK.RawSkeleton rawSkeleton = rawSkeletonStream.skeletons[index];
            ManusManager.communicationHub.GetRawSkeletonNodeInfo(rawSkeleton.gloveId, out CoreSDK.NodeInfo[] nodeInfos);
            Debug.Log(IsLeftInfo(nodeInfos));
            if (IsLeftInfo(nodeInfos)) {
                AddLeftData(nodeInfos, ergoData, rawSkeleton);
            } else {
                AddRightData(nodeInfos, ergoData, rawSkeleton);
            }
        }

    }

    private void AddLeftData(CoreSDK.NodeInfo[] nodeInfos, CoreSDK.ErgonomicsData ergoData, CoreSDK.RawSkeleton rawSkeleton) {
        float[] frameData = new float[csvColumnNames.Count];
        CSVLine newLine = new();
        for (int i = 0; i < 20; i++) {
            frameData[i] = ergoData.data[i];
        }
        for (int i = 0; i < nodeInfos.Length; i++) {
            frameData[20 + i * 10 + 0] = rawSkeleton.nodes[i].transform.position.x;
            frameData[20 + i * 10 + 1] = rawSkeleton.nodes[i].transform.position.y;
            frameData[20 + i * 10 + 2] = rawSkeleton.nodes[i].transform.position.z;
            frameData[20 + i * 10 + 3] = rawSkeleton.nodes[i].transform.rotation.w;
            frameData[20 + i * 10 + 4] = rawSkeleton.nodes[i].transform.rotation.x;
            frameData[20 + i * 10 + 5] = rawSkeleton.nodes[i].transform.rotation.y;
            frameData[20 + i * 10 + 6] = rawSkeleton.nodes[i].transform.rotation.z;
            frameData[20 + i * 10 + 7] = rawSkeleton.nodes[i].transform.scale.x;
            frameData[20 + i * 10 + 8] = rawSkeleton.nodes[i].transform.scale.y;
            frameData[20 + i * 10 + 9] = rawSkeleton.nodes[i].transform.scale.z;
        }
        newLine.frame = frameData;
        currentRecordingLeft.Add(newLine);
    }

    private void AddRightData(CoreSDK.NodeInfo[] nodeInfos, CoreSDK.ErgonomicsData ergoData, CoreSDK.RawSkeleton rawSkeleton) {
        float[] frameData = new float[csvColumnNames.Count];
        CSVLine newLine = new();
        for (int i = 0; i < 20; i++) {
            frameData[i] = ergoData.data[i + 20];
        }
        for (int i = 0; i < nodeInfos.Length; i++) {
            frameData[20 + i * 10 + 0] = rawSkeleton.nodes[i].transform.position.x;
            frameData[20 + i * 10 + 1] = rawSkeleton.nodes[i].transform.position.y;
            frameData[20 + i * 10 + 2] = rawSkeleton.nodes[i].transform.position.z;
            frameData[20 + i * 10 + 3] = rawSkeleton.nodes[i].transform.rotation.w;
            frameData[20 + i * 10 + 4] = rawSkeleton.nodes[i].transform.rotation.x;
            frameData[20 + i * 10 + 5] = rawSkeleton.nodes[i].transform.rotation.y;
            frameData[20 + i * 10 + 6] = rawSkeleton.nodes[i].transform.rotation.z;
            frameData[20 + i * 10 + 7] = rawSkeleton.nodes[i].transform.scale.x;
            frameData[20 + i * 10 + 8] = rawSkeleton.nodes[i].transform.scale.y;
            frameData[20 + i * 10 + 9] = rawSkeleton.nodes[i].transform.scale.z;
        }
        newLine.frame = frameData;
        currentRecordingRight.Add(newLine);
    }

    private Boolean IsLeftInfo(CoreSDK.NodeInfo[] nodeInfos) {
        Boolean allEqual = true;
        Boolean isLeft = true;
        //Debug.Assert(nodeInfos[0].side == CoreSDK.Side.Invalid); // hand root does not have a valid side.
        for (int i = 1; i < nodeInfos.Length; i++) {
            if (i == 1) {
                isLeft = nodeInfos[i].side == CoreSDK.Side.Left;
            } else {
                if (isLeft != (nodeInfos[i].side == CoreSDK.Side.Left)) {
                    allEqual = false;
                }
            }

        }
        Debug.Assert(allEqual);
        return isLeft;
    }
    public String GetLeftData() {
        var csv = new StringBuilder();
        csv.AppendLine(String.Join(';', csvColumnNames));
        foreach (CSVLine line in currentRecordingLeft) {
            csv.AppendLine(String.Join(';', line.frame));
        }
        return csv.ToString();
    }
    public String GetRightData() {
        var csv = new StringBuilder();
        csv.AppendLine(String.Join(';', csvColumnNames));
        foreach (CSVLine line in currentRecordingRight) {
            csv.AppendLine(String.Join(';', line.frame));
        }
        return csv.ToString();
    }
    public void FlushBuffer() {
        currentRecordingLeft = new();
        currentRecordingRight = new();
    }
}
