using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Networking;
using UnityEngine.Experimental.XR;

public class ARTapToPlaceObject : MonoBehaviour
{
    public GameObject objectToPlace;
    public GameObject placementIndicator;

    ARSessionOrigin arOrigin;
    public Pose placementPose;
    bool placementPoseIsValid = false;

    // Start is called before the first frame update
    void Start()
    {
        arOrigin = FindObjectOfType<ARSessionOrigin>();
    }

    // Update is called once per frame
    void Update()
    {
        //联网时给gameSession持续发送位置更新信息
        if (ARNetGameSession.instance) {
            ARNetGameSession.instance.UpdateTargetPose(objectToPlace.transform.position, objectToPlace.transform.rotation);
        }

#if !UNITY_EDITOR
        UpdatePlacementPose();
        UpdatePlacementIndicator();

        //if(placementPoseIsValid && Input.GetMouseButtonDown(0)) {
        //    objectToPlace.SetActive(true);
        //    objectToPlace.transform.position = placementPose.position;
        //    objectToPlace.transform.rotation = placementPose.rotation;
        //}
#endif
    }

    public void PlaceTarget() {
        //if (ARNetGameSession.instance == null) {
            //print("~~~~~~~local");
            objectToPlace.SetActive(true);
            objectToPlace.transform.position = placementPose.position;
            objectToPlace.transform.rotation = placementPose.rotation;
        //} else {
        //    print("~~~~~~~~~net");
        //    ARNetGameSession.instance.RpcPlaceTarget(placementPose);
        //}
    }

    //public void PlaceTarget(Pose pose) {
    //    print("~~~~~~~~~place target");
    //    objectToPlace.SetActive(true);
    //    objectToPlace.transform.position = pose.position;
    //    objectToPlace.transform.rotation = pose.rotation;
    //}

    void UpdatePlacementPose() {
        Vector3 screenCenter = Camera.current.ViewportToScreenPoint(new Vector2(.5f,.5f));
        var hits = new List<ARRaycastHit>();
        arOrigin.Raycast(screenCenter, hits, TrackableType.Planes);

        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid) {
            placementPose = hits[0].pose;
        }
    }

    void UpdatePlacementIndicator() {
        if (placementPoseIsValid) {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        } else {
            placementIndicator.SetActive(false);
        }
    }
}
