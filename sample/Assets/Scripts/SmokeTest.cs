using System;
using GrovsSDK;
using UnityEngine;

/// <summary>Exercises the basic Grovs calls on device and logs each result.</summary>
public class SmokeTest : MonoBehaviour
{
    const string Tag = "[GrovsSmoke] ";

    async void Start()
    {
        Grovs.DeeplinkReceived += OnDeeplink;
        Grovs.ErrorOccurred += OnError;

        Debug.Log(Tag + "start, supported=" + Grovs.IsSupportedPlatform + ", enabled=" + Grovs.IsEnabled);
        Grovs.Track("smoke_start");
        Debug.Log(Tag + "tracked smoke_start");

        try
        {
            var link = await Grovs.GenerateLinkAsync(new GenerateLinkOptions { Title = "smoke" });
            Debug.Log(Tag + "link " + link);
        }
        catch (Exception e)
        {
            Debug.Log(Tag + "link failed: " + e.Message);
        }
    }

    void OnDestroy()
    {
        Grovs.DeeplinkReceived -= OnDeeplink;
        Grovs.ErrorOccurred -= OnError;
    }

    static void OnDeeplink(GrovsDeeplink deeplink)
    {
        Debug.Log(Tag + "deeplink " + deeplink.Link);
    }

    static void OnError(GrovsError error)
    {
        Debug.Log(Tag + "error " + error.Code + ": " + error.Message);
    }
}
