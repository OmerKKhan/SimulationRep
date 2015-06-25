using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class carEntryPoint : MonoBehaviour {

	public GameObject carPrefab;
	private Vector3 directionOfCar;

	private int randomCarCreationInterval;
	private int intervalCounter;

	private bool carOnTop;

	private String[] args;

	// Use this for initialization
	void Start () {

		args = System.Environment.GetCommandLineArgs ();

		if (args.Count() == 2) { //we are running the executable with a parameter. assumes we will have only one command line parameter ALWAYS
			randomCarCreationInterval = int.Parse(args [1]);
		} 
		else { //we are using the unity editor. it has 2 command line parameters for a total of 3 args. OR the executable was not passed any parameters.
			randomCarCreationInterval = 200;
		}

		intervalCounter = 0;

		directionOfCar = -transform.up;

		carOnTop = false;
	}
	
	// Update is called once per frame
	void Update () {
		intervalCounter++;
		if ( intervalCounter%randomCarCreationInterval ==0  && !carOnTop) {

			createCar();
		}
	}

	public GameObject createCar(){

		GameObject newCar = Instantiate (carPrefab, transform.Find("createPos").transform.position, Quaternion.identity ) as GameObject; //uses name to find child instead of tag///////////////////////////////////////////////////

		newCar.transform.localScale = new Vector3 (0.13f, 0.13f, 1.0f);// should this be done here? and its hardcoded///////////////////////////////////////////////////////////////////////////////////////////////////////////

		var angle = Mathf.Atan2(directionOfCar.y, directionOfCar.x) * Mathf.Rad2Deg;
		newCar.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);



		//assign random path
		if (newCar != null) {
			
			List<completePath> pathsThatStartNearCar = new List<completePath>();

			List<completePath> fullRoutes;
			try{
				if( GameObject.FindGameObjectWithTag ("trafficController").GetComponent<trafficController> ().constructionComplete ){

					fullRoutes = GameObject.FindGameObjectWithTag ("trafficController").GetComponent<trafficController> ().getRoutes();

				}else{
					Destroy(newCar);return null;
				}
			}catch{

				Destroy(newCar);return null;
			}
			foreach ( completePath c in fullRoutes ){
				
				if( (c.nodes[0]-newCar.transform.position).magnitude < 3 ){ //!!!!!!!!!!!!!!!!!!!!hardcoded!!!!!!!!!!!!!!!!!!!!!!!!!!!
					//maybe startObject should be used here somehow
					pathsThatStartNearCar.Add (c);
				}
				
			}


			int totalGoStraightWeightage = 0;
			foreach ( completePath c in pathsThatStartNearCar ){

				totalGoStraightWeightage += c.goStraightWeightage;	
			}

			int randomNum = UnityEngine.Random.Range (0, totalGoStraightWeightage);

			int pos = 0;
			foreach ( completePath c in pathsThatStartNearCar ){
				
				pos += c.goStraightWeightage;

				if ( pos > randomNum ){

					newCar.GetComponent<carTest> ().journey = c;

					//print ( randomNum.ToString()+" "+c.goStraightWeightage.ToString() );
					break;
				}
			}


		}




		return newCar;
	}

	void OnTriggerStay2D (Collider2D other)
	{
		if (other.gameObject.tag == "Car") {
		
			carOnTop = true;				
		}
	}

	void OnTriggerExit2D (Collider2D other)
	{
		if (other.gameObject.tag == "Car") {
			
			carOnTop = false;				
		}
	}


	/*void OnGUI(){

		String argsStr = "";

		foreach (String a in args) {

			argsStr += a+"  ";
		}


		GUI.skin.label.normal.textColor = Color.yellow;
		
		int labelSize = 500;
		
		Vector3 position = transform.position;
		
		Vector3 screenPosition = Camera.main.WorldToScreenPoint (position);
		screenPosition = new Vector3 (screenPosition.x-(labelSize/2), Screen.height - screenPosition.y-(labelSize/2), 0);



		GUI.Label(new Rect(screenPosition.x, screenPosition.y, labelSize, labelSize), argsStr);

	}*/
}
