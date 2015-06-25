using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public struct neighbouringRoad
{
		public GameObject road;
		public GameObject TLinfront; //controls traffic of this road

		public GameObject TLopposite; //controls traffic of road opposite to this road           //needed for reducing traffic to a road?

}

public class CongestionStatus{ //not struct because structs are Value types so member values cant be modified

	public bool congestion_on_self; 
	public float congestion_forward;

	public CongestionStatus(){

		congestion_forward = 0;
		congestion_on_self = false;
	}
	
}

public class Junction : MonoBehaviour
{

		public GameObject tLightPrefab;
		public GameObject stopPrefab;
		public GameObject chromosome;
		public float lightPosition;
		private GameObject[] trafficLights;
		private GameObject[] stops;
		private bool infoSentToController;
		List<neighbouringRoad> myRoads;//why is this public?
		private Dictionary<GameObject, float> TLtoTimeMapping;
		private int counterForLights;////////////////////////////////////////////////////////////////
		private int indexOfGreenLight;////////////////////////////////////////////////////////////////
		private int timeOfGreen;////////////////////////////////////////////////////////////////

		private Dictionary<GameObject, GameObject> StopToLightMapping;

		private Dictionary<GameObject, CongestionStatus> CongestionStatuses; //maps congestion staus to appropriate TrafficLight
	

		// Use this for initialization
		void Start ()
		{

				chromosome = GameObject.FindGameObjectWithTag ("chromosome");

				counterForLights = -1;////////////////////////////////////////////////////////////////
				indexOfGreenLight = 0;////////////////////////////////////////////////////////////////
				timeOfGreen = 1000;////////////////////////////////////////////////////////////////

				infoSentToController = false;
		
				trafficLights = new GameObject[4];
				stops = new GameObject[4];
				StopToLightMapping = new Dictionary<GameObject, GameObject> ();


				myRoads = new List<neighbouringRoad> ();

				//TLtoTimeMapping = new Hashtable ();


				//assumes junction rotation is always Euler(0,0,0) Does it? the code works for other rotations as well I think
				for (int i=0; i<4; i++) {

						float rotate;

						if (i % 2 == 0)
								rotate = 90;
						else
								rotate = 0;

						trafficLights [i] = Instantiate (tLightPrefab, new Vector3 (0, 0, 0), transform.rotation) as GameObject;
						stops [i] = Instantiate (stopPrefab, new Vector3 (0, 0, 0), transform.rotation) as GameObject;

						trafficLights [i].transform.parent = this.transform;
						stops [i].transform.parent = this.transform;

						stops [i].transform.Rotate (0, 0, rotate);

						trafficLights [i].transform.localScale = new Vector3 (0.5f, 0.5f, 1); //!!!!!!!!!!depends on prefab size (see by dragging prefab into heirarchy)
						stops [i].transform.localScale = new Vector3 (0.07f, 0.22f, 1);

						float constStopPosition = 0.5f;

						if (i == 0) {
								trafficLights [i].transform.localPosition = new Vector3 (0, lightPosition, 1);
								stops [i].transform.localPosition = new Vector3 (0.125f, constStopPosition, 1);
						} else if (i == 1) {
								trafficLights [i].transform.localPosition = new Vector3 (lightPosition, 0, 1);
								stops [i].transform.localPosition = new Vector3 (constStopPosition, -0.125f, 1);
						} else if (i == 2) {
								trafficLights [i].transform.localPosition = new Vector3 (0, -lightPosition, 1);
								stops [i].transform.localPosition = new Vector3 (-0.12f, -constStopPosition, 1);
						} else if (i == 3) {
								trafficLights [i].transform.localPosition = new Vector3 (-lightPosition, 0, 1);
								stops [i].transform.localPosition = new Vector3 (-constStopPosition, 0.12f, 1);
						}



						StopToLightMapping [stops [i]] = trafficLights [i];

						//TLtoTimeMapping[ trafficLights[i] ] = 1;


				}

				CongestionStatuses = new Dictionary<GameObject, CongestionStatus>();
				for (int i=0; i<trafficLights.Length; i++) {
					
					CongestionStatuses [trafficLights[i]] = new CongestionStatus();
				}



		}

		public string checkTrafficLight (GameObject stop)
		{

				if (stop.tag != "Stop") {
						print ("checkTrafficLight recieved an object that was not a Stop!");
						return "Green";
				}

				if (!StopToLightMapping.ContainsKey (stop)) {
						print ("checkTrafficLight recieved an Stop that was not part of that junction!");
						return "Green";
				}

				return StopToLightMapping [stop].GetComponent<TLight> ().getColor ();


		}

		private void manageTrafficLights ()
		{////////////////////////////////////////////////////////////////

				if (counterForLights == -1) { //initial color of lights

						for (int i=0; i<trafficLights.Length; i++) {
								trafficLights [i].GetComponent<TLight> ().setColor ("red");
						}
						indexOfGreenLight = Random.Range (0, 4);

						trafficLights [indexOfGreenLight].GetComponent<TLight> ().setColor ("green");

				}

				counterForLights++;


				int indexOfnext = indexOfGreenLight + 1;
				if (indexOfnext > trafficLights.Length - 1) {
						indexOfnext = 0;
				}


				float totalTime = timeOfGreen * trafficLights [indexOfGreenLight].GetComponent<TLight> ().time;


				if (counterForLights == (int)(0.70 * totalTime)) {
				
						trafficLights [indexOfnext].GetComponent<TLight> ().setColor ("yellow");

				} else if (counterForLights >= totalTime) {

						trafficLights [indexOfGreenLight].GetComponent<TLight> ().setColor ("red");

						indexOfGreenLight = indexOfnext;
						counterForLights = 0;

						trafficLights [indexOfGreenLight].GetComponent<TLight> ().setColor ("green");



						//set time for signal right at the start of its green time:
						{
							GameObject thisTL = trafficLights [indexOfGreenLight];
							
							if( !CongestionStatuses[thisTL].congestion_on_self ){
								
								if( thisTL.GetComponent<TLight> ().time > chromosome.GetComponent<chromo>().minTrafficLightTime ){
									
									thisTL.GetComponent<TLight> ().time -= chromosome.GetComponent<chromo>().maxTrafficLightTimeChangePerCycle; //no local congestion, reduce TL time by max possible
								
									if( thisTL.GetComponent<TLight> ().time < chromosome.GetComponent<chromo>().minTrafficLightTime ){
										thisTL.GetComponent<TLight> ().time = chromosome.GetComponent<chromo>().minTrafficLightTime;
									}
								}
							}
							else{
								if( thisTL.GetComponent<TLight> ().time < chromosome.GetComponent<chromo>().maxTrafficLightTime ){

									float lTWeight = chromosome.GetComponent<chromo>().localTrafficWeightage;
									float nTWeight = 1 - lTWeight;
									
									float ratio = (lTWeight/(lTWeight + nTWeight*CongestionStatuses[thisTL].congestion_forward));
									
									if(ratio>1)ratio=1; //needed?

									thisTL.GetComponent<TLight> ().time += chromosome.GetComponent<chromo>().maxTrafficLightTimeChangePerCycle * ratio;
								}
							}
							
							
						}


						
						

				}


		}

	
		// Update is called once per frame
		void Update ()
		{

				if (!infoSentToController) {
						GameObject.FindGameObjectWithTag ("trafficController").GetComponent<trafficController> ().newJunctionAdded (this.gameObject);////////////////////////////////////////////////////////
						infoSentToController = true;
				}


				manageTrafficLights ();////////////////////////////////////////////////////////////////
		}

		public	void registerRoad (GameObject r, Vector2 contactPoint)
		{

				GameObject nearestTL = null;
				GameObject farthestTL = null;
				//get nearest and farthest traffic lights to contactPoint
				{

						float minDistance = -1;


						float maxDistance = -1;

	
						foreach (GameObject l in trafficLights) {

								float dis = (contactPoint - (Vector2)l.transform.position).magnitude;

								if (minDistance < 0) {

										minDistance = dis;
										nearestTL = l;

								} else if (dis < minDistance) {

										minDistance = dis;
										nearestTL = l;
								}

								if (dis > maxDistance) {
				
										maxDistance = dis;
										farthestTL = l;
								}

						}

				}

				if (nearestTL == null || farthestTL == null) {
						print ("NO TRAFFIC LIGHT CLOSE TO CONTACTPOINT OF ROAD FOUND");
						return;
				}

				neighbouringRoad nr = new neighbouringRoad ();

				nr.TLinfront = nearestTL;
				nr.TLopposite = farthestTL;
				nr.road = r;

				myRoads.Add (nr);

		}

		public void resolveCongestion (GameObject r, float congestion)
		{

				/*if (myRoads.Count < 4) { //maybe some junctions donot have roads on all sides
						return;
				}*/
				if (myRoads.Count > 4) {

						print ("MORE THAN 4 ROADS REGISTERED IN JUNCTION");
						return;
				}


				if (!myRoads.Exists (c => c.road == r)) {

						print ("NON NEIGHBOURING ROAD ASKING JUNCTION TO RESOLVE CONGESTION");
						return;
				}

				int index = myRoads.FindIndex (c => c.road == r);

				if (congestion > chromosome.GetComponent<chromo>().minLocalThreshold){//checking against min local threshold

					//print ("Resolve congestion!!!");///////////////////////////////////////////////////////////////
			
					CongestionStatuses[myRoads [index].TLinfront].congestion_on_self = true;
				
				} else {

					CongestionStatuses[myRoads [index].TLinfront].congestion_on_self = false;

				}
				
				//pass message to neighbouring junction to reduce traffic if congestion greater than max local threshold
				if (congestion > chromosome.GetComponent<chromo>().maxLocalThreshold) { // ( >1 means this will never be true so multicasting will not occur )
				
					GameObject junction_behind_this_road = (this.gameObject == r.GetComponent<road> ().junctionForward ? r.GetComponent<road> ().junctionOpposite : r.GetComponent<road> ().junctionForward);
				
					if(junction_behind_this_road){junction_behind_this_road.GetComponent<Junction> ().reduceForwardFlow (r, 1);}
		
		
				}

		}

		public void reduceForwardFlow (GameObject r, float weightage)
		{
	
			if (weightage < chromosome.GetComponent<chromo>().minReduceFlowSignalStrength) { //checking against min signal strength

				return;
			}
			
			int index = myRoads.FindIndex (c => c.road == r);


			//print ("road pos"+r.collider2D.bounds.center.ToString()+" asking junction pos"+transform.position.ToString()+" to reduce forward flow with weightage "+weightage.ToString());
				


			if (CongestionStatuses [myRoads [index].TLopposite].congestion_forward < weightage) { //to handle multiple reduceFlow requests

				CongestionStatuses [myRoads [index].TLopposite].congestion_forward = weightage;
			}

			weightage -= chromosome.GetComponent<chromo>().ReduceFlowSignalStrengthReductionPerNode; //reducing signal strength


			//pass reduce forward flow message onward
			for( int i=0; i< myRoads.Count; i++){

				if( myRoads[i].TLinfront == myRoads [index].TLopposite ){

					GameObject junction_behind_this_road = (this.gameObject == myRoads[i].road.GetComponent<road> ().junctionForward ? myRoads[i].road.GetComponent<road> ().junctionOpposite : myRoads[i].road.GetComponent<road> ().junctionForward);

					if(junction_behind_this_road){junction_behind_this_road.GetComponent<Junction> ().reduceForwardFlow (myRoads[i].road, weightage);}
					
					return;
				}

			}

			//print ("APPROPRIATE ROAD NOT FOUND!!!!!"); //An exceptional case but also happens if appropriate road is not attatched to junction

			

		}
	


}
