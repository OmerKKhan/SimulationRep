using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XInputDotNetPure;
using System;
using System.IO;


public class trafficController : MonoBehaviour {

	public GUISkin guiSkin;
	private List<GameObject> junctions; //better to use a set so same junction cannot be added multiple times
	private List<GameObject> roads; //better to use a set so same junction cannot be added multiple times
	private float normalizedAverageCarWaitingTime;

	private int totalCarsExited;


	private int underConstructionCount;
	public bool constructionComplete;

	private List<completePath> fullRoutes;


	private float timeDelta;

	private bool createMultipleCars;
	private GameObject currentEntryPoint;
	private float multipleCarDelata;
	private int multipleCarCount;

	// Use this for initialization
	void Start () {

		junctions = new List<GameObject> ();
		roads = new List<GameObject> ();

		normalizedAverageCarWaitingTime = 0f;

		totalCarsExited = 0;

		timeDelta = 0f;

		createMultipleCars = false;

		multipleCarCount = 0;
		multipleCarDelata = 0f;

		underConstructionCount = GameObject.FindGameObjectsWithTag ("Junction").Length + GameObject.FindGameObjectsWithTag ("roadCreator").Length;
		constructionComplete = false;

	}
	
	// Update is called once per frame
	void Update () {

		if (constructionComplete) {

			if (Input.GetMouseButtonDown (0)) { 
			
								RaycastHit2D hit = Physics2D.GetRayIntersection (Camera.main.ScreenPointToRay (Input.mousePosition));


								if (hit.collider != null && hit.collider.gameObject.tag == "carEntryPoint") {

										GameObject newCar = hit.collider.gameObject.GetComponent<carEntryPoint> ().createCar ();

								}

			
						}

			if ( Input.GetMouseButtonDown (1) ){

				RaycastHit2D hit = Physics2D.GetRayIntersection (Camera.main.ScreenPointToRay (Input.mousePosition));

				if (hit.collider != null && hit.collider.gameObject.tag == "carEntryPoint" && !createMultipleCars) {

					createMultipleCars = true;
					currentEntryPoint = hit.collider.gameObject;
				}

			}

			if( createMultipleCars ){

				multipleCarDelata+= Time.deltaTime;

				if(multipleCarDelata > 1){



					GameObject newCar = currentEntryPoint.GetComponent<carEntryPoint> ().createCar ();


					multipleCarCount++;
					multipleCarDelata = 0;
				}
				if( multipleCarCount>4){

					multipleCarCount=0;
					createMultipleCars = false;
				}


			}


						

				timeDelta+= Time.deltaTime;
			if(timeDelta>5){

				print ( "Printing to csv... Total cars exited: "+ totalCarsExited.ToString() );

				timeDelta = 0f;

				writeCSV("output.csv");

			}

				}
		
		else if (underConstructionCount == 0) {

			constructionComplete = true;

			Time.timeScale = 1.0f;

			getAllRoutes();

				}
	
	}

	public void newJunctionAdded(GameObject junc){

		//print (junc.transform.position);

		if (junc.tag == "Junction") {
						junctions.Add (junc);
			underConstructionCount--;


			if ( underConstructionCount<0 ) {
				
				print ("!!!!!!!!!!!!!!!!!!!!!!!!Exception!!!!!!!!!!!!!!!!!!");
			}
				}

	}


	public void newRoadAdded(GameObject road){
		
		//print (junc.transform.position);
		
		if (road.tag == "Road") {
						roads.Add (road);

						underConstructionCount--;

						if (underConstructionCount < 0) {
				
								print ("!!!!!!!!!!!!!!!!!!!!!!!!Exception!!!!!!!!!!!!!!!!!!");
						}
				}
		
	}

	public void destroyCar(GameObject car){

		if (car.tag != "Car") 
						return;

		//update car waiting time
		{
			totalCarsExited++;
		
			float newWT = car.GetComponent<carTest> ().waitingTime / car.GetComponent<carTest> ().junctionsEntered;
			if (car.GetComponent<carTest> ().waitingTime!=0) {//waitingTime* 1/totalCarsExited(including new one) + oldWT * (carsExitedBeforeThisOne/TotalCarsExited) 
				normalizedAverageCarWaitingTime = newWT * ((float)1 / totalCarsExited) + normalizedAverageCarWaitingTime * ((totalCarsExited - 1) / (float)totalCarsExited);
			}
		}

		//find and notify road that contains this car that it is -about to- exit
		foreach( GameObject r in roads ){

			if( r.GetComponent<road>().containsCar( car ) ){

				r.GetComponent<road>().carExited(car);
				break;
			}
		}
		Debug.Log (car.GetComponent<carTest> ().waitingTime);
		Destroy (car);

	}

	

	public void getAllRoutes(){

		List<completePath> immediatePaths = getImmediatePaths ();

		fullRoutes = combineImmediatePaths (immediatePaths); 

	}


	private List<completePath> getImmediatePaths(){

		
		List<completePath> immediatePaths = new List<completePath> (); //immediate road-junction and junction-road paths 
		
		List<GameObject> roadsAndJunctions = roads.Concat (junctions).ToList();

		
		
		for (int x=0; x<roadsAndJunctions.Count; x++) {
			
			List<Transform> paths = new List<Transform> ( roadsAndJunctions[x].transform.Cast<Transform> ().Where (c => c.gameObject.tag == "Path").ToArray () );
			
			//print (roads [0].collider2D.bounds.center);
			
			for (int j=0; j<paths.Count; j++) {
				
				List<Transform> currentPathNodes = new List<Transform> ( paths [j].gameObject.GetComponentsInChildren<Transform> () ); //list of nodes within path
				currentPathNodes.RemoveAt (0); 
				
				//print (currentPathNodes [currentPathNodes.Count - 1].transform.position);
				
				//int maskForVehicleLayer = 1<<gameObject.layer; //create a bit mask
				
				Vector3 pathEnd = currentPathNodes [currentPathNodes.Count - 1].transform.position;
				
				
				
				GameObject infront;
				{
					Vector3 direction = pathEnd - currentPathNodes [currentPathNodes.Count - 2].transform.position;
					Vector3 position = pathEnd;
					
					RaycastHit2D[] hits = Physics2D.RaycastAll (position, direction, 2.0f);
					
					if( hits.Length < 2 ){
						continue;
					}
					
					infront = hits [1].collider.gameObject;
					
					while (infront.transform.parent != null) {
						infront = infront.transform.parent.gameObject; //to handle things like junction stops
					} 
					
					if ( roadsAndJunctions[x].tag == infront.tag || (infront.tag!="Junction" && infront.tag!="Road") ) {
						
						//print ("!!!!!!!!!!!!!!!!!!!!!!!!Exception!!!!!!!!!!!!!!!!!!" + infront.tag);
						continue;
					}
				}
				
				
				List<Transform> pathsInfront; //list of paths
				{
					pathsInfront = new List<Transform> (infront.transform.Cast<Transform> ().Where (c => c.gameObject.tag == "Path").ToArray ());

					int nearPathsCount = (infront.tag=="Junction") ? (pathsInfront.Count/4) : (pathsInfront.Count/2);
					
					pathsInfront.Sort ((a,b) => (a.gameObject.GetComponentsInChildren<Transform> () [1].transform.position - pathEnd).magnitude.CompareTo ((b.gameObject.GetComponentsInChildren<Transform> () [1].transform.position - pathEnd).magnitude)); //index 0 transform is parent itself
					
					pathsInfront.RemoveRange (nearPathsCount, pathsInfront.Count - nearPathsCount);
					
				}
				
				for( int k=0; k<pathsInfront.Count; k++ ){

					if( roadsAndJunctions[x].tag == "Road" && (paths[j].name == "Path1" || paths[j].name == "Path4") && pathsInfront[k].name!="turn-left"){
						continue; //so left lane cars turn left at junctions always
					}
						
					List<Transform> pInfront = new List<Transform> ( pathsInfront[k].gameObject.GetComponentsInChildren<Transform> () );
					pInfront.RemoveAt(0);

					int transitionIndex = currentPathNodes.Count;
					
					List<Transform> connectedPathTransforms	= currentPathNodes.Concat( pInfront ).ToList();

					//is there a simpler/shorter way to get this vector3 list? 
					List<Vector3> connectedPath = new List<Vector3>();
					for( int v=0; v<connectedPathTransforms.Count; v++ ){
						connectedPath.Add( connectedPathTransforms[v].position );
					}


					int goStraight = 1;
					if( roadsAndJunctions[x].tag == "Road" && pathsInfront[k].name=="straight-ahead"){
						goStraight = 12;
					}


					immediatePaths.Add( new completePath( roadsAndJunctions[x], connectedPath, transitionIndex, infront, goStraight ) );
					//print (paths[j].name+" -> "+ pathsInfront[k].name);

				}
				
			}
			
		}
		
		
		
		
		/*print (immediatePaths.Count);
		for (int i=0; i<immediatePaths.Count; i++) {
			
			print (immediatePaths[i].startObject.name+"  "+immediatePaths[i].nodes[0]+"->"+immediatePaths[i].nodes[immediatePaths[i].nodes.Count-1]);
			
		}*/

		return immediatePaths;
	}


	private List<completePath> combineImmediatePaths(List<completePath> immediatePaths){

		List<completePath> result = new List<completePath>();
		{ //find paths with no incoming edges - could also be done by racasting from carEntryPoints. Should be roads only ideally.

			for( int i=0; i<immediatePaths.Count; i++ ){

				bool thisIsStarter = true;

				for( int j=0;  j<immediatePaths.Count; j++ ){
					
					if( immediatePaths[j].nodes[ immediatePaths[j].transitionIndex ] == immediatePaths[i].nodes[0] ){

						thisIsStarter = false;

						break;
					}
					
				}

				if( thisIsStarter && immediatePaths[i].startObject.tag!="Junction" ){//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

					result.Add( immediatePaths[i] );
				}

			}


		}

		/*for (int i=0; i<result.Count; i++) {
			
			print (result[i].startObject.name+"  "+result[i].nodes[0]+"->"+result[i].nodes[result[i].nodes.Count-1]);
			
		}*/


		foreach (var t in Enumerable.Range(0, 6)) //t is threshold. t not used anywhere else
		{
			
			List<int> toRemoveIndexes = new List<int>();

			int resultStaticCount = result.Count; //so elements can be added at end of result without affecting loop iteration count
			for( int i=0; i<resultStaticCount; i++ ){

				for( int j=0; j<immediatePaths.Count; j++ ){
					
					if( result[i].nodes[ result[i].transitionIndex ] == immediatePaths[j].nodes[0] ){

						//print ("next segemt found for "+i+" found");

						if( !result[i].nodes.Any(x=> x==immediatePaths[j].nodes[immediatePaths[j].nodes.Count-1]) ){
							
							//print ( immediatePaths[j].startObject.tag );//########################################################

							int nextTransitionIndex = result[i].nodes.Count;

							completePath temp = result[i].getClone(); //why not make a new completePath instead of doing this?
							
							//print ( temp.transitionIndex+" " + result[i].transitionIndex );

							temp.nodes = temp.nodes.Take( temp.transitionIndex ).ToList();

							temp.nodes.AddRange( immediatePaths[j].nodes );

							temp.transitionIndex = nextTransitionIndex; 

							temp.endObject = immediatePaths[j].endObject; //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!check this

							temp.goStraightWeightage = result[i].goStraightWeightage + immediatePaths[j].goStraightWeightage;

							result.Add( temp );

							
							if( !toRemoveIndexes.Any(x=> x==i) ){ toRemoveIndexes.Add( i ); //print ("added index "+i);
									}
							else{ //print("didnot add "+ i);
									}

						}
					}
				}

			}

			if( toRemoveIndexes.Count == 0 ){ break; } //nothing in the result list was changed which means we are done - unlikely to be true because we run loop for fixed number of iterations within which this usually wont happen

			toRemoveIndexes.Sort();
			toRemoveIndexes.Reverse();
			foreach (int index in toRemoveIndexes){ result.RemoveAt(index); }

		}

		print ("Done!");
		return result;
	}

	
	
	
	private void OnGUI() {
		GUI.skin = guiSkin;
		GUI.skin.label.normal.textColor = Color.red;
		
		GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
		          "Waiting time: "+normalizedAverageCarWaitingTime);
	}


	private void writeCSV( string name ){

		string towrite = Environment.NewLine;

		foreach (GameObject r in roads) {

		towrite = towrite + r.GetComponent<road>().movingOppositeCarsCount()+","
					+ r.GetComponent<road>().movingOppositeSignalWaitTime+","
					+ r.GetComponent<road>().movingForwardCarsCount()+","
					+ r.GetComponent<road>().movingForwardSignalWaitTime+",";
				}

		towrite = towrite + normalizedAverageCarWaitingTime;// + Environment.NewLine;

		//towrite = string.Format("{0},{1}{2}", normalizedAverageCarWaitingTime, timeDelta, Environment.NewLine);
		//csv.Append(newLine);    

		File.AppendAllText(name, towrite);

	}

	public List<completePath> getRoutes(){

		return fullRoutes;
	}
}
