using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARCore;
using System.Reflection;


public class DetectImages : MonoBehaviour
{
    ARTrackedImageManager _artrackedimagemanager;
    XRReferenceImageLibrary library;

    void Awake(){
        _artrackedimagemanager = GetComponent<ARTrackedImageManager>();
        library = _artrackedimagemanager.referenceLibrary;
		_artrackedimagemanager.trackedImagesChanged += OnImageChanged;

    }

	private void OnEnable()
	{
		_artrackedimagemanager.trackedImagesChanged += OnImageChanged;
	}

	private void OnDisable()
	{
		_artrackedimagemanager.trackedImagesChanged -= OnImageChanged;
	}

    void Start()
    {

        StartCoroutine (LoadDatabase());
    }

    // Update is called once per frame
    void Update()
    {
		//Debug.Log("Get Name: " + ARTrackedImage.referenceImage.name);
    }

    IEnumerator LoadDatabase(){
        /* Get image database from Streaming Assets
        // string url = "jar:file://"+Application.dataPath+"!/assets/database.imgdb";
        // Debug.Log("Got file : "+url);
        // UnityWebRequest www = UnityWebRequest.Get(url);
        // DownloadHandlerBuffer dhf = new DownloadHandlerBuffer();
        // www.downloadHandler = dhf;
        // yield return www.SendWebRequest();
        */

        string url = "https://arpath-2020.firebaseio.com/ImageDatabase/Images.json";
        WWW www = new WWW (url);
        yield return www;
        while(!www.isDone){ }

        UnityWebRequest imgurl = UnityWebRequest.Get(www.text.Trim('"'));
        DownloadHandlerBuffer dhf = new DownloadHandlerBuffer();
        imgurl.downloadHandler = dhf;
        imgurl.SendWebRequest();
        while(!imgurl.isDone){ }
        
        byte[] libraryinfo = imgurl.downloadHandler.data;
        ChangeARCoreImagesDatabase(library,libraryinfo);
    }

       
    #if UNITY_ANDROID
    private unsafe void ChangeARCoreImagesDatabase( XRReferenceImageLibrary library, byte[] arcoreDB){
        Debug.Log("Initializing DB Creation");
        if (arcoreDB == null || arcoreDB.Length == 0)
        {
            throw new InvalidOperationException(string.Format("Failed to load image library '{0}' - file was empty!", library.name));
        }
        var guids = new NativeArray<Guid>(library.count, Allocator.Temp);
        try
        {
	        for (int i = 0; i < library.count; ++i)
	            guids[i] = library[i].guid;
	 
	        fixed (byte* blob = arcoreDB)
	        {
	          	 // Retrieve the ARCore image tracking provider by reflection
	             var provider = typeof(ARCoreImageTrackingProvider).GetNestedType("Provider", BindingFlags.NonPublic | BindingFlags.Instance);
	             Debug.Log("retrieved image tracking provider");
	 
	         	 // Destroy the current image tracking database
	             var destroy = provider.GetMethod("UnityARCore_imageTracking_destroy", BindingFlags.NonPublic | BindingFlags.Static);
	             destroy.Invoke(null, null);
	             Debug.Log("Destroyed current tracking database");
	 
	            // Set the image tracking database
	             var setDatabase = provider.GetMethod("UnityARCore_imageTracking_setDatabase", BindingFlags.NonPublic | BindingFlags.Static);
	             setDatabase.Invoke(null, new object[]
	             {
	                 new IntPtr(blob),
	                 arcoreDB.Length,
	                 new IntPtr(guids.GetUnsafePtr()),
	                 guids.Length
	             });
	             Debug.Log("Database creation completed");
	         }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Error while loading '{0}': {1}", library.name, e));
        }
        finally
        {
			guids.Dispose();
        }

//		foreach (var referenceImage in library)
//			Debug.LogFormat("Image guid: {0}", referenceImage.guid);
    }

	public void OnImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
	{
		foreach (ARTrackedImage trackedImage in eventArgs.added)
		{
			Debug.Log("Added : " + trackedImage.referenceImage.name);
		}

		foreach (ARTrackedImage trackedImage in eventArgs.updated)
		{
			/* If an image is properly tracked */
			if (trackedImage.trackingState == TrackingState.Tracking || trackedImage.trackingState == TrackingState.Limited)
			{
				/* If trackedImage matches an image in the array */
				//Debug.Log("Updated : Tracking : Limited : " + trackedImage.referenceImage.name);
			}
			else if (trackedImage.trackingState == TrackingState.None)
			{
				//Deactivate all prefabs */
				Debug.Log("Updated : None : " + trackedImage.referenceImage.name);
			}
		}

		foreach (ARTrackedImage trackedImage in eventArgs.removed)
		{
			Debug.Log("Removed : " + trackedImage.referenceImage.name);
		}

	}

    #endif
}

