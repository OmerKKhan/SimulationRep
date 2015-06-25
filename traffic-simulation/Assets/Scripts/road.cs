using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class road : MonoBehaviour
{

		//important: maintian 23.1 to 10 ratio of scaleX and scaleY in inspector (?)

		public HashSet<GameObject> movingForwardCars;/////////////////////////////everything's public !!
		public HashSet<GameObject> movingOppositeCars;
		public float movingForwardSignalWaitTime;
		public float movingOppositeSignalWaitTime;
		public int totalForwardHistorical; //count used for averaging
		public int totalOppositeHistorical; //count used for averaging

		public GameObject junctionForward;
		public GameObject junctionOpposite;
		bool neighbouringJunctionsFound;
		int carCountThreshold;
		int maxCarCountThreshold;
		public GUISkin guiSkin;

		void Start ()
		{

				movingForwardSignalWaitTime = 0;
				movingOppositeSignalWaitTime = 0;

				totalForwardHistorical = 0;
				totalOppositeHistorical = 0;

				movingForwardCars = new HashSet<GameObject> ();
				movingOppositeCars = new HashSet<GameObject> ();

				neighbouringJunctionsFound = false;

				carCountThreshold = 5;///////////////////////////////////////////////////////////////////////////////////////////////
				maxCarCountThreshold = 10;///////////////////////////////////////////////////////////////////////////////////////////////

		}
	
		// Update is called once per frame
		void Update ()
		{

		
			if (!neighbouringJunctionsFound)
					registerNeighbouringJunctions ();
			else
					reportCongestionToNeighbours ();


			movingForwardCars.RemoveWhere  ( s => s == null || !this.gameObject.collider2D.bounds.Intersects(new Bounds(s.collider2D.bounds.center, s.collider2D.bounds.size*2)) );
			movingOppositeCars.RemoveWhere ( s => s == null || !this.gameObject.collider2D.bounds.Intersects(new Bounds(s.collider2D.bounds.center, s.collider2D.bounds.size*2)) );
		}

		void OnTriggerEnter2D (Collider2D other)
		{
		
				if (other.gameObject.tag == "Car") {

						if (movingForwardCars.Contains (other.gameObject) || movingOppositeCars.Contains (other.gameObject)) {

								print ("EXCEPTION: SAME CAR ENTERED TWICE!");
						}


						float dist_with_path2_end = 0; //if mapping between pathx and movingForward/movingOpposite cars changes, this will become wrong
						float dist_with_path3_end = 0; //if mapping between pathx and movingForward/movingOpposite cars changes, this will become wrong
						{
								List<Transform> paths = new List<Transform> (this.gameObject.transform.Cast<Transform> ().Where (c => c.gameObject.tag == "Path").ToArray ());
				
				
								if (paths.Count < 1)
										return;
				
								for (int j=0; j<paths.Count; j++) {
					
										if (paths [j].gameObject.name == "Path2") {
						
												List<Transform> currentPathNodes = new List<Transform> (paths [j].gameObject.GetComponentsInChildren<Transform> ()); //list of nodes within path
												currentPathNodes.RemoveAt (0); 
						
												Vector3 pathEnd = currentPathNodes [currentPathNodes.Count - 1].transform.position;

												dist_with_path2_end = (other.transform.position - pathEnd).magnitude;
						
										} else if (paths [j].gameObject.name == "Path3") {
						
						
												List<Transform> currentPathNodes = new List<Transform> (paths [j].gameObject.GetComponentsInChildren<Transform> ()); //list of nodes within path
												currentPathNodes.RemoveAt (0); 
						
												Vector3 pathEnd = currentPathNodes [currentPathNodes.Count - 1].transform.position;

												dist_with_path3_end = (other.transform.position - pathEnd).magnitude;
										}
					
								}

						}


						if (dist_with_path2_end > dist_with_path3_end) {

								//print ( "Path2" );
								movingForwardCars.Add (other.gameObject);

						} else {
								//print ( "Path3" );
								movingOppositeCars.Add (other.gameObject);
						}

				}
	
		}
	
		void OnTriggerStay2D (Collider2D other)
		{

		}
	
		void OnTriggerExit2D (Collider2D other)
		{
				if (other.gameObject.tag != "Car")
						return;

				carExited (other.gameObject);
		}

		public void carExited (GameObject exiter, bool? forward = null) //!!! default parameter values are not supported in unity's version of .net !!!
		{ 

				if (!containsCar (exiter)) {

						print ("EXCEPTION - CAR NOT IN ROAD LIST BUT CAREXITED CALLED");
						return;
				}
			
				if (forward == null) {

						if (movingForwardCars.Contains (exiter)) {
					
								forward = (bool?)true; //changing input variable that is by-reference(?)
					
						} else if (movingOppositeCars.Contains (exiter)) {
					
								forward = (bool?)false; //changing input variable that is by-reference(?)
					
						} else {
								print ("EXCEPTION - BRANCHING HERE SHOULD BE IMPOSSIBLE");
								return;
						}

				}
			
				if ((bool)forward) {

						totalForwardHistorical++;

						movingForwardSignalWaitTime = exiter.GetComponent<carTest> ().signalWaitingTime * ((float)1 / totalForwardHistorical) + movingForwardSignalWaitTime * ((totalForwardHistorical - 1) / (float)totalForwardHistorical);
				
				} else {

						totalOppositeHistorical++;

						movingOppositeSignalWaitTime = exiter.GetComponent<carTest> ().signalWaitingTime * ((float)1 / totalOppositeHistorical) + movingOppositeSignalWaitTime * ((totalOppositeHistorical - 1) / (float)totalOppositeHistorical);
				}

		}

		private void OnGUI ()
		{
				GUI.skin = guiSkin;
				GUI.skin.label.normal.textColor = Color.red;

				int labelSize = 100;

				Vector3 position = collider2D.bounds.center;

				Vector3 roadScreenPosition = Camera.main.WorldToScreenPoint (position);
				roadScreenPosition = new Vector3 (roadScreenPosition.x, Screen.height - roadScreenPosition.y, 0);

				GUI.Label (new Rect (roadScreenPosition.x, roadScreenPosition.y, labelSize, labelSize),
		           movingOppositeCars.Count + ", " + movingOppositeSignalWaitTime.ToString ("f1") + " | " + movingForwardCars.Count + ", " + movingForwardSignalWaitTime.ToString ("f1"));

		}

		public int movingForwardCarsCount ()
		{

				return movingForwardCars.Count;
		}

		public	int movingOppositeCarsCount ()
		{
		
				return movingOppositeCars.Count;
		}

		public bool containsCar (GameObject c)
		{ //used by traffic controller to see if this road contains the car its about to destroy
				
				if (movingForwardCars.Contains (c) || movingOppositeCars.Contains (c))
						return true;

				return false;
		}

		private void registerNeighbouringJunctions ()
		{

			
				List<Transform> paths = new List<Transform> (this.gameObject.transform.Cast<Transform> ().Where (c => c.gameObject.tag == "Path").ToArray ());


				if (paths.Count < 1)
						return;
			
				for (int j=0; j<paths.Count; j++) {

						if (paths [j].gameObject.name == "Path2") {

								List<Transform> currentPathNodes = new List<Transform> (paths [j].gameObject.GetComponentsInChildren<Transform> ()); //list of nodes within path
								currentPathNodes.RemoveAt (0); 
				
								Vector3 pathEnd = currentPathNodes [currentPathNodes.Count - 1].transform.position;
				
				
				
								GameObject infront;
								Vector2 contactPoint;
								{
										Vector3 direction = pathEnd - currentPathNodes [currentPathNodes.Count - 2].transform.position;
										Vector3 position = pathEnd;
					
										RaycastHit2D[] hits = Physics2D.RaycastAll (position, direction, Mathf.Infinity);//, maskForVehicleLayer );
					
										if (hits.Length < 2) {
												continue;
										}

										infront = hits [1].collider.gameObject;
										contactPoint = hits [1].point;
					
										while (infront.transform.parent != null) {
												infront = infront.transform.parent.gameObject; //to handle things like junction stops
										} 
					
										if (infront.tag != "Junction") {
						
												//print ("!!!!!!!!!!!!!!!!!!!!!!!!Exception!!!!!!!!!!!!!!!!!!" + infront.tag);
												continue;
										}


								}

								junctionForward = infront;
								junctionForward.GetComponent<Junction> ().registerRoad (this.gameObject, contactPoint);

				
								/*GameObject marker = Instantiate (GameObject.Find("traffic-light(Clone)"), contactPoint, transform.rotation) as GameObject;
				marker.transform.localScale = new Vector3 (0.005f, 0.005f, 1);
				marker.name = "Path2";*/


						} else if (paths [j].gameObject.name == "Path3") {

				
								List<Transform> currentPathNodes = new List<Transform> (paths [j].gameObject.GetComponentsInChildren<Transform> ()); //list of nodes within path
								currentPathNodes.RemoveAt (0); 
				
								Vector3 pathEnd = currentPathNodes [currentPathNodes.Count - 1].transform.position;
				
				
				
								GameObject infront;
								Vector2 contactPoint;
								{
										Vector3 direction = pathEnd - currentPathNodes [currentPathNodes.Count - 2].transform.position;
										Vector3 position = pathEnd;
					
										RaycastHit2D[] hits = Physics2D.RaycastAll (position, direction, Mathf.Infinity);//, maskForVehicleLayer );
					
										if (hits.Length < 2) {
												continue;
										}

										infront = hits [1].collider.gameObject;
										contactPoint = hits [1].point;
					
										while (infront.transform.parent != null) {
												infront = infront.transform.parent.gameObject; //to handle things like junction stops
										} 
					
										if (infront.tag != "Junction") {
						
												//print ("!!!!!!!!!!!!!!!!!!!!!!!!Exception!!!!!!!!!!!!!!!!!!" + infront.tag);
												continue;
										}
					
					
								}
				
								junctionOpposite = infront;
								junctionOpposite.GetComponent<Junction> ().registerRoad (this.gameObject, contactPoint);


								/*GameObject marker = Instantiate (GameObject.Find("traffic-light(Clone)"), contactPoint, transform.rotation) as GameObject;
				marker.transform.localScale = new Vector3 (0.005f, 0.005f, 1);
				marker.name = "Path3";*/

						}
				
				


				}

				neighbouringJunctionsFound = true;


		}

		void reportCongestionToNeighbours ()
		{

				if (movingForwardCars.Count >= carCountThreshold) {

						int carCount = movingForwardCars.Count;
						if( carCount>maxCarCountThreshold ){ carCount = maxCarCountThreshold;}

						float congestionRatio = ( carCount - carCountThreshold ) * ( 1.0f/(maxCarCountThreshold - carCountThreshold) );


						if (junctionForward != null) {

								junctionForward.GetComponent<Junction> ().resolveCongestion (this.gameObject, congestionRatio);
						}


				} else {
						if (junctionForward != null) {
								junctionForward.GetComponent<Junction> ().resolveCongestion (this.gameObject, 0);
						}
				}

				if (movingOppositeCars.Count >= carCountThreshold) {

						int carCount = movingOppositeCars.Count;
						if( carCount>maxCarCountThreshold ){ carCount = maxCarCountThreshold;}
						
						float congestionRatio = ( carCount - carCountThreshold ) * ( 1.0f/(maxCarCountThreshold - carCountThreshold) );


						if (junctionOpposite != null) {

								junctionOpposite.GetComponent<Junction> ().resolveCongestion (this.gameObject, congestionRatio);
						}

				} else {
						if (junctionOpposite != null) {
								junctionOpposite.GetComponent<Junction> ().resolveCongestion (this.gameObject, 0);
						}
			
				}
		}




}
