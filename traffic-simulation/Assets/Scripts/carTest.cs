using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XInputDotNetPure;

public class carTest : MonoBehaviour
{
		//if dontMove is true car doesnt move. if its false car moves forward. car tries to face the next graphNode. onJunction is needed to handle case when car is halfway onto junction and traffic light turns red. it would erronously stop as it is colliding with a Stop

		//public GameObject graph;
		public float speed;
		public completePath journey;
		public float waitingTime;
		public int junctionsEntered;
		public float signalWaitingTime;/////////////////////////////////////////////should this be public?
		private bool onJunction;
		private bool dontMove;
		private List<Vector3> graphNodes;
		private int currentNode;
		private bool onRoad;
		private bool collidingWithCar;

		private Vector2 seperation_velocity;//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	
		void Start ()
		{

				onJunction = false; // assuming car is not created on a junction
				dontMove = false;


				currentNode = 0;

				waitingTime = 0;
				signalWaitingTime = 0;

				junctionsEntered = 0;

				onRoad = true;

				getPath ();

				//print ("Destination :"+destination);

				collider2D.isTrigger = true;

				collidingWithCar = false;

				seperation_velocity = Vector2.zero;
		}

		void FixedUpdate ()
		{
				//destroyIfBeyondScreen ();

				float speedToUse = speed;

				if (onRoad) {

						if (collidingWithCar) {

								speedToUse = (transform.right + (Vector3) seperation_velocity).magnitude < transform.right.magnitude ? speed*0.4f: speed;
						}
		
						speedToUse = slowDownIfCarAhead (speedToUse);

				}

				if (dontMove)
						rigidbody2D.velocity = Vector2.zero;
				else
						rigidbody2D.velocity = transform.right * speedToUse; //transform.right is actually forward in our case


				if (dontMove || speedToUse == 0) {

						signalWaitingTime += Time.fixedDeltaTime;

				}


				if (currentNode > graphNodes.Count - 1) {

						//print ("journey complete"); //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

						askToBeDestroyed (); //may be as problem right at start beacause trafficController does not use constructor to set journey, also may mess with a road's count of vehicles, may not be ideal anyway

						return;
				}
		
				if ((graphNodes [currentNode] - transform.position).magnitude < 0.10f) {
						currentNode++;
				}

				if (!dontMove && currentNode < graphNodes.Count) {

						float angleDiff = Mathf.Atan2 (graphNodes [currentNode].y - transform.position.y, graphNodes [currentNode].x - transform.position.x) * 180 / Mathf.PI;

						Vector3 temp = transform.eulerAngles;
						temp.z = Mathf.LerpAngle (temp.z, angleDiff, 1.0f);
						transform.eulerAngles = temp;
				}
				
		}

		void destroyIfBeyondScreen ()
		{

				Vector2 screenPosition = Camera.main.WorldToScreenPoint (transform.position);

				//print (screenPosition);

				if (screenPosition.y > Screen.height || screenPosition.y < 0 || screenPosition.x > Screen.width || screenPosition.x < 0) {

						//print ("A car destroyed");
						//Destroy (this.gameObject);
						askToBeDestroyed ();
				}

		}

		void askToBeDestroyed ()
		{

				GameObject.FindGameObjectWithTag ("trafficController").GetComponent<trafficController> ().destroyCar (this.gameObject); 
		}

		/*void onCollisionEnter (Collision2D collision)
	{
			print (collision.collider);

	}*/

		void getPath ()
		{
				graphNodes = journey.nodes;

				currentNode = 0;
		}

		float slowDownIfCarAhead (float prevSpeedToUse)
		{

				int maskForVehicleLayer = 1 << gameObject.layer; //create a bit mask
				RaycastHit2D[] hits = Physics2D.RaycastAll (transform.position, transform.right, 1.0f, maskForVehicleLayer); //////////////////////////////////////////////////////////////////PROBLEM: raycast distance is hardcoded 


				if (hits.Length >= 2) { //ray detects own car also so >=2

						//print (hits[1].fraction);
						//print (rigidbody2D.velocity.magnitude);
						float speedFraction = 1;

						if (collidingWithCar) {

								if (hits.Length == 2) {
										//print ("Car colliding with other but nothing infront");
										return prevSpeedToUse;
								}

								speedFraction = hits [2].fraction;
						} else {
								speedFraction = hits [1].fraction;
						}

						if (speedFraction < 0.3) { ///////////////////////////////////////////////////////////////////////////////
								speedFraction = 0; 
						}

						return prevSpeedToUse * speedFraction;

				} else {

						return prevSpeedToUse;
				}

		}

		void OnTriggerEnter2D (Collider2D other)
		{
				
				/*if (other.gameObject.tag == "Road") {
			
			onRoad = true;				
		}*/

				if (other.gameObject.tag == "Junction" || other.gameObject.tag == "Road") {
						
						//print ("Car says: Trigger enter : " + other.gameObject.tag);

						//getPath (other.gameObject);

						if (other.gameObject.tag == "Junction") {

								junctionsEntered++;

						} else {

								signalWaitingTime = 0;
						}

				}


		}

		void OnTriggerStay2D (Collider2D other)
		{
				if (other.gameObject.tag == "Car") {
		
					collidingWithCar = true;

					if( (other.transform.position - transform.position).magnitude < 1 && onRoad && rigidbody2D.velocity.magnitude == 0 && other.rigidbody2D.velocity.magnitude == 0 ){
						//print ("THAT EDGE CASE! CAR DELETED");
						Destroy (this.gameObject);
					}

					seperation_velocity = (Vector2) (transform.position - other.transform.position).normalized;
				}


				if (graphNodes.Count == 0 && (other.gameObject.tag == "Junction" || other.gameObject.tag == "Road")) { //special case when car is present on a J or R at start
				
						print ("special case which is doing nothing atm - change it");
						//getPath (other.gameObject);
				} else {
	

						if (other.gameObject.tag == "Junction") {
	
								onJunction = true; 
								dontMove = false;
			
						} else if (!onJunction && other.gameObject.tag == "Stop") {

								//Debug.Log ("On Stop!");

								string trafficLightColor = other.gameObject.transform.parent.GetComponent<Junction> ().checkTrafficLight (other.gameObject);
								//print (trafficLightColor);

								if (trafficLightColor == "green") {
										dontMove = false;
								} else {

										dontMove = true;
								}
						}
				}

		}

		void OnTriggerExit2D (Collider2D other)
		{

				if (other.gameObject.tag == "Road") { //assumes car will always go onto juntion when it exits road
			
						onRoad = false;				
				}
				if (other.gameObject.tag == "Junction") { //assumes car will always go onto juntion when it exits road
			
						onRoad = true;				
				}
				if (other.gameObject.tag == "Car") {
			
						collidingWithCar = false;				
				}
				if (other.gameObject.tag == "Junction") {

						//print ("junction exited"); //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

						onJunction = false;		

				} else if (other.gameObject.tag == "Road") {
						waitingTime += signalWaitingTime;

				}
	
	
		}
	
	
}