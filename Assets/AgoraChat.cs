using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using agora_gaming_rtc;
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
using UnityEngine.Android;
#endif

public class AgoraChat : MonoBehaviour
{
    public string AppID;
    public string ChannelName;
    #if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
        private ArrayList permissionList = new ArrayList();
    #endif

    VideoSurface myView;
    VideoSurface remoteView;
    IRtcEngine mRtcEngine;

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
    }

    void Update()
    {
        CheckPermissions();
    }

    void Join()
    {
        mRtcEngine.EnableVideo();
        mRtcEngine.EnableVideoObserver();
        myView.SetEnable(true);
        mRtcEngine.JoinChannel(ChannelName, "", 0);
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
        myView.SetEnable(false);
        if (remoteView != null)
        {
            remoteView.SetEnable(false);
        }
    }

    void OnUserJoined(uint uid, int elapsed)
    {
        GameObject go = GameObject.Find("RemoteView");

        if (remoteView == null)
        {
            remoteView = go.AddComponent<VideoSurface>();
            remoteView.GetComponent<VideoSurface>().EnableFlipTextureApply(true, true);
        }

        remoteView.SetForUser(uid);
        remoteView.SetEnable(true);
        remoteView.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
        remoteView.SetGameFps(30);
        Debug.Log("User joined");
    }

    void OnUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        remoteView.SetEnable(false);
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
        myView = go.AddComponent<VideoSurface>();
        myView.GetComponent<VideoSurface>().EnableFlipTextureApply(true, true);
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
    
}
