using UnityEngine;
using System.Collections;
using System.Reflection;

namespace IllusionLoop.CoasterPluginZF{
	[HelpURL("http://www.illusionloop.com/docs/animatedsteelcoaster.html")]
	public class CoasterStationControllerLXRZF : MonoBehaviour {
		[HideInInspector] public bool info = true;
		[HideInInspector] public bool editMode = false;
		public GameObject[] carts;
		public GameObject stationTrack;
		Vector3[] stpos;
		Quaternion[] strot;

		System.Type TrackCartZF;
		System.Type TrackZF;
		System.Type StationZF;
		// Use this for initialization
		void Start () {
			if (gameObject.activeInHierarchy == false)//do not execute in asset browser
				return;
			stpos = new Vector3[carts.Length];
			strot = new Quaternion[carts.Length];
			for (int ct = 0; ct < carts.Length; ct++) {
				stpos[ct] = carts[ct].transform.position;
				strot[ct] = carts[ct].transform.rotation;
			}
		}
		
		// Update is called once per frame
		public void ResetTrain () {
			if (gameObject.activeInHierarchy == false)
				return;
			if(CheckForZFTrack()){
				CoasterStationLXRZF station = GetComponent<CoasterStationLXRZF>();
				if(station != null){

					PropertyInfo ctr = TrackCartZF.GetProperty ("CurrentTrack");

					for (int ct = 0; ct < carts.Length; ct++) {
						carts[ct].GetComponent<Rigidbody>().velocity = Vector3.zero;
						carts[ct].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
						carts[ct].transform.position = stpos[ct];
						carts[ct].transform.rotation = strot[ct];

						ctr.SetValue(carts[ct].GetComponent(TrackCartZF),station.rail.GetComponent(TrackZF),null);
					}
				}else{
					Debug.LogWarning("failed to reset coaster train, station script could not be found or is not initialized correctly");
				}
			}else{
				Debug.LogWarning("failed to reset coaster train, ZFTrack is missing");
			}
		}

		public void SendTrain(){
			if (gameObject.activeInHierarchy == false)
				return;
			if (CheckForZFTrack ()) {
				stationTrack.GetComponent(StationZF).SendMessage("Send");
			}
		}

		bool CheckForZFTrack(){//check if tracks and rails plugin is installed and assign types
			if (gameObject.activeInHierarchy == false)
				return false;
			//are all required tracks + rails components there?
            TrackCartZF = System.Type.GetType("ZenFulcrum.Track.TrackCart");
            if (TrackCartZF == null)
                TrackCartZF = System.Type.GetType("ZenFulcrum.Track.TrackCart, ZFTrack");
            if (TrackCartZF == null)
                goto ResetVars;
            TrackZF = System.Type.GetType("ZenFulcrum.Track.Track");
            if (TrackZF == null)
                TrackZF = System.Type.GetType("ZenFulcrum.Track.Track, ZFTrack");
            if (TrackZF == null)
                goto ResetVars;
            StationZF = System.Type.GetType("ZenFulcrum.Track.Station");
            if (StationZF == null)
                StationZF = System.Type.GetType("ZenFulcrum.Track.Station, ZFTrack");
            if (StationZF == null)
                goto ResetVars;
            //all tracks and rails components were found and assigned -> return true
            return true;

            ResetVars:
            //if components are missing -> reset and return false
            TrackCartZF = null;
            TrackZF = null;
            StationZF = null;

            return false;
		}
	}
}
