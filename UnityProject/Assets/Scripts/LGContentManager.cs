using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using UnityEngine;
using Debug = UnityEngine.Debug;
using SFB;
using UnityEngine.UI;
using B83.Win32;
using UnityEngine.Video;
using uGIF;
using Image = uGIF.Image;
using System.Threading;
using Leap;
using Moments;
using LightBuzz.Archiver;
using Newtonsoft.Json;
using Shibuya24.Utility;

public class LGContentManager : MonoBehaviour {
    public Material quiltMaterial;
    [TextArea] public string quiltPath;
    Gyroscope m_Gyro;
    public UnityEngine.UI.AspectRatioFitter aspectRatioFitter;
    private Vector3 startEulerAngles;
    private Vector3 startGyroAttitudeToEuler;

    private bool gyroEnabled = false;

    public UnityEngine.UI.Slider slider;
    public GameObject popup;
    public TMPro.TextMeshProUGUI fileNameText;
    public TMPro.TextMeshProUGUI fieInfoText;

    class DropInfo {
        public string file;
        public bool video;
        public Vector2 pos;
    }

    void OnEnable() {
#if UNITY_STANDALONE_WIN
        UnityDragAndDropHook.InstallHook();

        UnityDragAndDropHook.OnDroppedFiles += OnFiles;


#elif UNITY_STANDALONE_OSX
        UniDragAndDrop.Initialize();
                UniDragAndDrop.OnDroppedFiles += OnFile;

#endif

        recorder.OnFileSaveProgress += OnFileSaveProgress;
        recorder.OnFileSaved += OnFileSaved;
    }

    void OnFileSaveProgress(int x, float progress) {
        currentPositionGIF = (int) (progress * currentQuiltInfo.quilt_settings.viewTotal);
    }

    void OnFileSaved(int x, string result) {
        capture = true;
    }


    void OnDisable() {
#if UNITY_STANDALONE_WIN

        UnityDragAndDropHook.UninstallHook();

#elif UNITY_STANDALONE_OSX
                UniDragAndDrop.OnDroppedFiles -= OnFile;


#endif

        recorder.OnFileSaveProgress -= OnFileSaveProgress;
    }

    DropInfo dropInfo = null;

    enum fileType {
        photo,
        video,
        hop
    }

    fileType getFileType(string file) {
        var fi = new System.IO.FileInfo(file);
        var ext = fi.Extension.ToLower();
        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
            return fileType.photo;
        }

        if (ext == ".mp4" || ext == ".webm" || ext == ".mov") {
            return fileType.video;
        }

        if (ext == ".hop") {
            return fileType.hop;
        }

        return fileType.photo;
    }

    bool isVideo(string file) {
        var fi = new System.IO.FileInfo(file);
        var ext = fi.Extension.ToLower();
        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
            return false;
        }

        if (ext == ".mp4" || ext == ".webm" || ext == ".mov") {
            return true;
        }

        return false;
    }

    List<Image> frames = new List<Image>();

    public void SaveAGif() {
        SaveGif(thisTexture);
        GifButton.SetActive(false);
    }

    public ImageRecorder recorder;

    public void SetINIValues(int fpsINI, int gifQINI, float resINI) {
        fpsValue = fpsINI;
        gifQuality = gifQINI;
        resolutionScale = resINI;
    }

    private int fpsValue = 22;
    private int gifQuality = 15;
    private float resolutionScale = 1;

    void SaveGif(Texture2D quiltTexture) {
        capture = false;
        int height = (int) (quiltTexture.height / currentQuiltInfo.quilt_settings.viewY);
        int width = (int) (quiltTexture.width / currentQuiltInfo.quilt_settings.viewX);
        recorder.Setup(false, (int) (width), (int) (height), fpsValue, 3, 0, gifQuality, resolutionScale);
        recorder.ConvertQuiltToGif(quiltTexture, currentQuiltInfo);
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        string newfilename = Path.GetFileNameWithoutExtension(currentPath);
        string newPath = Path.Combine(Path.GetDirectoryName(currentPath),
            newfilename + "_" + timestamp + ".gif");

        recorder.Save(newPath);

        // Encode();
    }

    public GameObject GifButton;
    public UnityEngine.UI.Button Loadbutton;

    private bool capture = true;

    void OnFile(string File) {
        POINT pos = new POINT();
        List<string> fileList = new List<string>();
        fileList.Add(File);
        OnFiles(fileList, pos);
    }

    void OnFiles(List<string> aFiles, POINT aPos) {
        string file = "";

        // videoPlayer.MakeCopy();
        if (thisTexture != null) {
            thisTexture = null;
            Resources.UnloadUnusedAssets();
        }

        // scan through dropped files and filter out supported image types
        foreach (var f in aFiles) {
            var fi = new System.IO.FileInfo(f);
            var ext = fi.Extension.ToLower();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
                file = f;
                currentPath = f;
                LoadQuiltInfoFromFile(file);
                break;
            }

            if (ext == ".hop") {
                file = f;
                currentPath = f;
                file = ProcessHopFile(fi, file);
                break;
            }

            if (ext == ".mp4" || ext == ".webm" || ext == ".mov") {
                file = f;
                currentPath = f;
                LoadQuiltInfoFromFile(file);
                break;
            }
        }

        // If the user dropped a supported file, create a DropInfo
        if (file != "") {
            var info = new DropInfo {
                file = file,
                pos = new Vector2(aPos.x, aPos.y),
            };
            dropInfo = info;


            ProcessPath(dropInfo.file, isVideo(dropInfo.file));
        }
    }

    // Start is called before the first frame update
    void Start() {
        //Input.isGyroAvailable;
        m_Gyro = Input.gyro;
#if UNITY_STANDALONE
        gyroEnabled = false;
#else
        gyroEnabled = (SystemInfo.supportsGyroscope);
#endif
        if (gyroEnabled) {
            m_Gyro.enabled = true;
            startEulerAngles = transform.eulerAngles;
            startGyroAttitudeToEuler = m_Gyro.attitude.eulerAngles;
        }

        //  currentQuiltInfo.quilt_settings.viewTotal = (int)currentQuiltInfo.quilt_settings.viewX * (int)currentQuiltInfo.quilt_settings.viewY - 1;
    }

    [ContextMenu("Test Path")]
    void TestPath() {
        ProcessPath(quiltPath);
    }

    public void LoadImage() {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
            new ExtensionFilter("Video Files", "mp4", "webm"),
            new ExtensionFilter("Hop File", "hop")
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
        if (paths.Length <= 0) return;
        var fi = new System.IO.FileInfo(paths[0]);
        // var ext = fi.Extension.ToLower();
        //  videoPlayer.MakeCopy();
        if (thisTexture != null) {
            thisTexture = null;
            Resources.UnloadUnusedAssets();
        }

        bool load = true;
        string LoadPath = paths[0];
        currentPath = LoadPath;
        fileType theFileType = getFileType(paths[0]);

        switch (theFileType) {
            case fileType.hop:
                LoadPath = ProcessHopFile(fi, LoadPath);
                break;
            case fileType.photo:
                LoadQuiltInfoFromFile(LoadPath);
                break;
            case fileType.video:
                LoadQuiltInfoFromFile(LoadPath);
                break;
        }

        if (load) ProcessPath(LoadPath, isVideo(LoadPath));
    }

    private string ProcessHopFile(FileInfo fi, string LoadPath) {
        string tempPath = Path.Combine(Application.temporaryCachePath, "TempHopExtract");
        var di = new System.IO.DirectoryInfo(tempPath);
        if (!Directory.Exists(di.FullName)) {
            Directory.CreateDirectory(di.FullName);
        }
        else {
            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }
        }

        Archiver.Decompress(fi, di, true);
        var files = Directory.GetFiles(di.FullName);
        for (int i = 0; i < files.Length; i++) {
            var currentFileInfo = new System.IO.FileInfo(files[i]);

            if (currentFileInfo.Extension.ToLower() == ".json") {
                JsonToQuiltInfo(currentFileInfo);
            }
            else {
                LoadPath = currentFileInfo.FullName;
            }
        }

        return LoadPath;
    }

    public VideoPlayer videoPlayer;

    IEnumerator LoadVideo(string videoUrl) {
        if (videoPlayer == null || rawImage == null || string.IsNullOrEmpty(videoUrl))
            yield break;

        videoPlayer.url = videoUrl;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return new WaitForSeconds(1);

        rawImage.texture = videoPlayer.texture;
        if (currentQuiltInfo.mediaType != "photoset")
            videoPlayer.Play();
    }

    public QuiltInfo currentQuiltInfo;

    public void JsonToQuiltInfo(FileInfo jsonFile) {
        string json = File.ReadAllText(jsonFile.FullName);
        currentQuiltInfo = JsonConvert.DeserializeObject<QuiltInfo>(json);

        if (currentQuiltInfo.quilt_settings.aspect == -1) {
            currentQuiltInfo.quilt_settings.aspect = 0.75f;
        }

        fieInfoText.text = json;

        // Debug.Log("JSON should have happened here");
    }

    public void LoadQuiltInfoFromFile(string filePath) {
        var filePathNoExt = Path.GetFileNameWithoutExtension(filePath);
        if (!filePathNoExt.Contains("_qs")) {
            PopupQuiltSettings(true);
            return;
        }
        else {
            popup.SetActive(false);
        }

        string[] split = filePathNoExt.Split(separatingStringsFirstPass, System.StringSplitOptions.RemoveEmptyEntries);

        string afterQS = split[split.Length - 1];
        afterQS = afterQS.Split(separatingStringsAfteQS, System.StringSplitOptions.RemoveEmptyEntries)[0];

        string[] furtherSplit = afterQS.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);
        // for (int i = 0; i < furtherSplit.Length; i++)
        // {
        //     Debug.Log(furtherSplit[i]);
        // }
        if (furtherSplit.Length > 0)
            currentQuiltInfo.quilt_settings.viewX = int.Parse(furtherSplit[0]);
        if (furtherSplit.Length > 1)
            currentQuiltInfo.quilt_settings.viewY = int.Parse(furtherSplit[1]);
        float currentAspect = 0.75f;
        if (furtherSplit.Length > 2) {
            string aspectProper = furtherSplit[2];
            if (float.TryParse(aspectProper, out currentAspect)) {
            }
            else {
                //Aspect
                string[] aspect = furtherSplit[2]
                    .Split(separatingStringsFinal, System.StringSplitOptions.RemoveEmptyEntries);
                if (aspect.Length > 2) {
                    aspectProper = aspect[0] + "." + aspect[1];
                    if (!float.TryParse(aspectProper, out currentAspect)) {
                        aspectProper = aspect[0];
                        currentAspect = float.Parse(aspectProper);
                    }

                    ;
                }
                else {
                    aspectProper = aspect[0];
                    currentAspect = float.Parse(aspectProper);
                }
            }
        }

        currentQuiltInfo.quilt_settings.aspect = currentAspect;

        currentQuiltInfo.quilt_settings.viewTotal = (int) currentQuiltInfo.quilt_settings.viewX *
                                                    (int) currentQuiltInfo.quilt_settings.viewY;

        fieInfoText.text = JsonConvert.SerializeObject(currentQuiltInfo, Formatting.Indented);
    }

    void PopupQuiltSettings(bool clear = false) {
        if (clear) {
            currentQuiltInfo.quilt_settings.viewX = 1;
            currentQuiltInfo.quilt_settings.viewY = 1;
            currentQuiltInfo.quilt_settings.aspect = 1;
        }

        popup.SetActive(true);
    }

    readonly string[] separatingStringsAfteQS = {" ", "_"};
    readonly string[] separatingStringsFinal = {"."};
    readonly string[] separatingStringsFirstPass = {"_qs"};
    readonly string[] separatingStrings = {"x", "a"};
    private int currentPositionGIF;

    void ProcessPath(string pathToProcess, bool video = false) {
        pathToProcess = pathToProcess.Trim('"');
        var filePath = Path.GetFileName(pathToProcess);
        var filePathNoExt = Path.GetFileNameWithoutExtension(pathToProcess);
        fileNameText.text = filePath;

        //    slider.maxValue = quiltTiles.x * quiltTiles.y - 1;\
        int screenSize = Screen.height;
        Screen.SetResolution((int) (screenSize * currentQuiltInfo.quilt_settings.aspect), (int) screenSize, false);
        aspectRatioFitter.aspectRatio = currentQuiltInfo.quilt_settings.aspect;
        if (!video) LoadImage(pathToProcess, filePathNoExt);
        else {
            StartCoroutine(LoadVideo(pathToProcess));
        }

        GifButton.SetActive(!isVideo(pathToProcess));
    }

    private string currentPath;

    public void SetQuiltX(string x) {
        int.TryParse(x, out currentQuiltInfo.quilt_settings.viewX);
    }

    public void SetQuiltY(string y) {
        int.TryParse(y, out currentQuiltInfo.quilt_settings.viewY);
    }

    public void SetAspect(string y) {
        float.TryParse(y, out currentQuiltInfo.quilt_settings.aspect);
        aspectRatioFitter.aspectRatio = currentQuiltInfo.quilt_settings.aspect;
    }

    public void SaveImageInfo() {
        string newfilename = Path.GetFileNameWithoutExtension(currentPath);
        string fileExt = Path.GetExtension(currentPath);

        string newPath = Path.Combine(Path.GetDirectoryName(currentPath),
            newfilename + "_qs" + (int) currentQuiltInfo.quilt_settings.viewX + "x" +
            (int) currentQuiltInfo.quilt_settings.viewY + "a" + currentQuiltInfo.quilt_settings.aspect + fileExt);
        videoPlayer = videoPlayer.MakeCopy();

        StartCoroutine(waitAndSaveFile(currentPath, newPath));
    }

    IEnumerator waitAndSaveFile(string oldPath, string newPath) {
        yield return new WaitForEndOfFrame();
        try {
            System.IO.File.Move(oldPath, newPath);
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }

        yield return new WaitForEndOfFrame();
        ProcessPath(newPath, isVideo(newPath));
    }

    Texture2D thisTexture;
    public UnityEngine.UI.RawImage rawImage;

    void LoadImage(string path, string fileName) {
        byte[] bytes = File.ReadAllBytes(path);
        thisTexture = new Texture2D(100, 100); //NOW INSIDE THE FOR LOOP

        thisTexture.LoadImage(bytes);
        thisTexture.name = fileName;
        rawImage.texture = thisTexture;
    }

    Vector2 intRotationToVector2(int inValue) {
        //0-100
        //Quiltx QuiltY
        //0-45
        //quiltx = 0-quiltx;
        int quiltX = (int) (inValue % currentQuiltInfo.quilt_settings.viewX);
        int quiltY = (int) (inValue / currentQuiltInfo.quilt_settings.viewX);
        // quilty = quiltx / quiltx * quilty;
        Vector2 convertedRotation = new Vector2(quiltX, quiltY);
        //    Debug.Log("Converted Rotation = " + convertedRotation);
        return convertedRotation;
    }

    Vector2 intRotationToVector2Plus(int inValue) {
        inValue++;
        if (inValue >= currentQuiltInfo.quilt_settings.viewTotal) inValue = currentQuiltInfo.quilt_settings.viewTotal;
        //0-100
        //Quiltx QuiltY
        //0-45
        //quiltx = 0-quiltx;
        int quiltX = (int) (inValue % currentQuiltInfo.quilt_settings.viewX);
        int quiltY = (int) (inValue / currentQuiltInfo.quilt_settings.viewX);
        // quilty = quiltx / quiltx * quilty;
        Vector2 convertedRotation = new Vector2(quiltX, quiltY);
        //    Debug.Log("Converted Rotation = " + convertedRotation);
        return convertedRotation;
    }
    // Update is called once per frame


    public void SetQuiltPosition(float x) {
        if (currentQuiltInfo.mediaType == "photoset") {
            double videoTime = (double) ((float) x / (float) currentQuiltInfo.quilt_settings.viewTotal) *
                               videoPlayer.length;
            double currentVideoTime = currentQuiltInfo.viewOrderReversed ? videoPlayer.length - videoTime : videoTime;
            videoPlayer.time = currentVideoTime;
            quiltMaterial?.SetVector("_QuiltVec",
                new Vector4(currentQuiltInfo.quilt_settings.viewX, currentQuiltInfo.quilt_settings.viewY, 0, 0));
            quiltMaterial?.SetVector("_QuiltVec2",
                new Vector4(currentQuiltInfo.quilt_settings.viewX, currentQuiltInfo.quilt_settings.viewY, 0, 0));
        }
        else {
            Vector2 quiltPos = intRotationToVector2((int) x);
            Vector2 quiltPos2 = intRotationToVector2Plus((int) x);
            quiltMaterial?.SetVector("_QuiltVec",
                new Vector4(currentQuiltInfo.quilt_settings.viewX, currentQuiltInfo.quilt_settings.viewY, quiltPos.x,
                    quiltPos.y));
            quiltMaterial?.SetVector("_QuiltVec2",
                new Vector4(currentQuiltInfo.quilt_settings.viewX, currentQuiltInfo.quilt_settings.viewY, quiltPos2.x,
                    quiltPos2.y));
        }
    }


    private void Update() {
        Loadbutton.interactable = capture;
        if (!capture) {
            SetQuiltPosition(currentPositionGIF);
            return;
        }

        if (currentQuiltInfo.quilt_settings.viewTotal <= 0) return;
        int viewTotalMinusOne = currentQuiltInfo.quilt_settings.viewTotal - 1;

        if (gyroEnabled) {
            Vector3 deltaEulerAngles = Input.gyro.attitude.eulerAngles; // - startGyroAttitudeToEuler;
            deltaEulerAngles.x = 0.0f;
            deltaEulerAngles.z = 0.0f;
            if (deltaEulerAngles.y > 40) {
                deltaEulerAngles.y = -(360.0f - deltaEulerAngles.y);
            }

            //   if(deltaEulerAngles)
            float mappedFloat = currentQuiltInfo.viewOrderReversed
                ? Remap(deltaEulerAngles.y, -20.0f, 20.0f, viewTotalMinusOne, 0)
                : Remap(deltaEulerAngles.y, -20.0f, 20.0f, 0, viewTotalMinusOne);
            SetQuiltPosition(mappedFloat);
            //   Debug.Log("Mapped Float :" + deltaEulerAngles.y + " Mapped Float : " + mappedFloat);
        }
        else {
            Vector3 mousePos = Input.mousePosition;
            float mappedFloat = currentQuiltInfo.viewOrderReversed
                ? Remap(mousePos.x, 0, Screen.width, 0, viewTotalMinusOne)
                : Remap(mousePos.x, 0, Screen.width, viewTotalMinusOne, 0);
            SetQuiltPosition(mappedFloat);
            //  Debug.Log("Mapped Float :" + mousePos.x + " Mapped Float : " + mappedFloat);
        }
    }


    public void ResetGyro() {
        startGyroAttitudeToEuler = Input.gyro.attitude.eulerAngles;
    }

    // c#
    float Remap(float input, float oldLow, float oldHigh, float newLow, float newHigh) {
        float t = Mathf.InverseLerp(oldLow, oldHigh, input);
        return Mathf.Lerp(newLow, newHigh, t);
    }

    [System.Serializable]
    public struct QuiltInfo {
        public bool movie;
        public string mediaType;
        public quiltSettings quilt_settings;
        public float depthiness;
        public bool depthInversion;
        public bool chromaDepth;
        public string depthPosition;
        public float focus;
        public bool viewOrderReversed;
        public float zoom;
        public float position_x;
        public float position_y;
        public float duration;
    }

    [System.Serializable]
    public struct quiltSettings {
        public int viewX;
        public int viewY;
        public int viewTotal;
        public bool invertViews;
        public float aspect;
    }
}