using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using agora_gaming_rtc;
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
using UnityEngine.Android;
#endif
using UnityEngine.XR.ARFoundation;
using System.Runtime.InteropServices;
using Firebase;
using Firebase.Database;

public class AgoraChat : MonoBehaviour
{
    public string AppID;
    public string ChannelName;
    public ARCameraBackground CameraImage;
    #if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
        private ArrayList permissionList = new ArrayList();
    #endif

    VideoSurface myView;
    VideoSurface remoteView;
    IRtcEngine mRtcEngine;
    Texture2D mTexture;
    Rect mRect;
    int i = 0;

    Firebase.FirebaseApp app;
    DatabaseReference db;

    void Awake()
    {
        #if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
            permissionList.Add(Permission.Microphone);         
            permissionList.Add(Permission.Camera);               
        #endif
        SetupUI();
    }

    void Start()
    {
        Debug.Log("HELLO");
        SetupAgora();
        SetupFirebase();
    }

    void Update()
    {
        CheckPermissions();
        StartCoroutine(shareScreen());
    }

    void Join()
    {
        mRtcEngine.EnableVideo();
        mRtcEngine.EnableVideoObserver();
        mRtcEngine.SetExternalVideoSource(true, false);
        // myView.SetEnable(true);
        mRtcEngine.JoinChannel(ChannelName, "", 0);
        mRect = new Rect(0, 0, Screen.width, Screen.height);
        // Creates a texture of the rectangle you create.
        mTexture = new Texture2D((int)mRect.width, (int)mRect.height, TextureFormat.BGRA32, false);
    }

    void Leave()
    {
        mRtcEngine.LeaveChannel();
        mRtcEngine.DisableVideo();
        mRtcEngine.DisableVideoObserver();
    }

    void OnJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        // can add other logics here, for now just print to the log
        Debug.Log("Join channel successful");
    }

    void OnLeaveChannelHandler(RtcStats stats)
    {
        // myView.SetEnable(false);
        // if (remoteView != null)
        // {
        //     remoteView.SetEnable(false);
        // }
    }

    void OnUserJoined(uint uid, int elapsed)
    {
        // GameObject go = GameObject.Find("RemoteView");

        // if (remoteView == null)
        // {
        //     remoteView = go.AddComponent<VideoSurface>();
        //     remoteView.GetComponent<VideoSurface>().EnableFlipTextureApply(true, true);
        // }

        // remoteView.SetForUser(uid);
        // remoteView.SetEnable(true);
        // remoteView.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
        // remoteView.SetGameFps(30);
        // Debug.Log("User joined");
    }

    void OnUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        // remoteView.SetEnable(false);
    }

    void OnErrorHandler(int err, string str) {
        Debug.Log("ERROR: " + err);
    }

    void OnApplicationQuit()
    {
        if (mRtcEngine != null)
        {
            IRtcEngine.Destroy(); 
            mRtcEngine = null;
        }
    }

    void SetupUI()
    {
        GameObject go = GameObject.Find("MyView");
        // myView = go.AddComponent<VideoSurface>();
        // myView.GetComponent<VideoSurface>().EnableFlipTextureApply(true, true);
        go = GameObject.Find("LeaveButton");
        go?.GetComponent<Button>()?.onClick.AddListener(Leave);
        go = GameObject.Find("JoinButton");
        go?.GetComponent<Button>()?.onClick.AddListener(Join);
    }

    void SetupAgora()
    {
        mRtcEngine = IRtcEngine.GetEngine(AppID);

        mRtcEngine.OnUserJoined = OnUserJoined;
        mRtcEngine.OnUserOffline = OnUserOffline;
        mRtcEngine.OnJoinChannelSuccess = OnJoinChannelSuccessHandler;
        mRtcEngine.OnLeaveChannel = OnLeaveChannelHandler;
        mRtcEngine.OnError = OnErrorHandler;
    }

    void SetupFirebase() {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
        var dependencyStatus = task.Result;
        if (dependencyStatus == Firebase.DependencyStatus.Available) {
            // Create and hold a reference to your FirebaseApp,
            // where app is a Firebase.FirebaseApp property of your application class.
            app = Firebase.FirebaseApp.DefaultInstance;
            db = FirebaseDatabase.DefaultInstance.GetReference("clicks");
            db.ChildAdded += HandleNewClick;

            // Set a flag here to indicate whether Firebase is ready to use by your app.
        } else {
            UnityEngine.Debug.LogError(System.String.Format(
            "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            // Firebase Unity SDK is not safe to use here.
        }
        });
    }

    private void CheckPermissions()
    {
        #if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
            foreach(string permission in permissionList)
            {
                if (!Permission.HasUserAuthorizedPermission(permission))
                {                 
                    Permission.RequestUserPermission(permission);
                }
            }
        #endif
    }

    void HandleNewClick(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        Dictionary<string, double> val = (Dictionary<string, double>)snapshot.GetValue(false);
        Debug.Log(val);
        
        var screenPoint = new Vector3((float) ((val["offsetX"] * Screen.width) + Screen.width/2), (float) (Screen.height/2 - (val["offsetY"] * Screen.height)), 1);
        var worldPos = Camera.main.ScreenToWorldPoint(screenPoint);
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.transform.position = worldPos;
    }

    IEnumerator shareScreen()
    {
        yield return new WaitForEndOfFrame();
        // Reads the Pixels of the rectangle you create.
        mTexture.ReadPixels(mRect, 0, 0);
        // Applies the Pixels read from the rectangle to the texture.
        mTexture.Apply();
        // Gets the Raw Texture data from the texture and apply it to an array of bytes.
        byte[] bytes = mTexture.GetRawTextureData();
        // Gives enough space for the bytes array.
        int size = Marshal.SizeOf(bytes[0]) * bytes.Length;
        // Checks whether the IRtcEngine instance is existed.
        IRtcEngine rtc = IRtcEngine.QueryEngine();
        if (rtc != null)
        {
            // Creates a new external video frame.
            ExternalVideoFrame externalVideoFrame = new ExternalVideoFrame();
            // Sets the buffer type of the video frame.
            externalVideoFrame.type = ExternalVideoFrame.VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA;
            // Sets the format of the video pixel.
            externalVideoFrame.format = ExternalVideoFrame.VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_BGRA;
            // Applies raw data.
            externalVideoFrame.buffer = bytes;
            // Sets the width (pixel) of the video frame.
            externalVideoFrame.stride = (int)mRect.width;
            // Sets the height (pixel) of the video frame.
            externalVideoFrame.height = (int)mRect.height;
            // Removes pixels from the sides of the frame
        //    externalVideoFrame.cropLeft = 10;
        //    externalVideoFrame.cropTop = 10;
        //    externalVideoFrame.cropRight = 10;
        //    externalVideoFrame.cropBottom = 10;
            // Rotates the video frame (0, 90, 180, or 270)
            externalVideoFrame.rotation = 180;
            // Increments i with the video timestamp.
            externalVideoFrame.timestamp = i++;
            // Pushes the external video frame with the frame you create.
            int a = rtc.PushVideoFrame(externalVideoFrame);
        }
   }    
}
