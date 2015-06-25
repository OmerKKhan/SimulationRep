using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class roadCreator : MonoBehaviour {

	public GameObject roadShell;
	public GameObject roadSegmentPrefab;

	private float percentComplete;
	private List<Transform> roadNodes;
	private GameObject newRoad;

	private List<Vector2> polyColliderVertices;

	private bool segmentPlacementComplete;

	// Use this for initialization
	void Start () {

		segmentPlacementComplete = false;



		newRoad = new GameObject ("Road");

		newRoad.tag = "Road";

		PolygonCollider2D pc = newRoad.AddComponent<PolygonCollider2D> ();
		pc.isTrigger = true;

		polyColliderVertices = new List<Vector2>();
		//newRoad.rigidbody2D.isKinematic = true;

		newRoad.AddComponent<road> (); //add script



		percentComplete = 0;

		roadNodes = new List<Transform> (roadShell.GetComponentsInChildren<Transform> ());
		roadNodes.RemoveAt (0);

	}
	
	// Update is called once per frame
	void Update () {

		if (segmentPlacementComplete) {

						GameObject.FindGameObjectWithTag ("trafficController").GetComponent<trafficController> ().newRoadAdded (newRoad);////////////////////////////////////////////////////////

						Object.Destroy(roadShell);
						Object.Destroy (this.gameObject);
						
						return;
				}

		if (percentComplete >= 1){

			definePolygonCollider();  //must happen before definepaths
			definePaths();
			segmentPlacementComplete = true;
			return;
		}


		for (int i=0; i<roadNodes.Count-1; i++) {

			Vector3 pos = Vector3.Lerp(roadNodes[i].transform.position,roadNodes[i+1].transform.position,percentComplete);



			float zAngle = Mathf.Atan2 (roadNodes[i+1].transform.position.y - pos.y, roadNodes[i+1].transform.position.x - pos.x) * 180 / Mathf.PI;

			float dist = (roadNodes[i+1].transform.position - pos).magnitude / (roadNodes[i+1].transform.position - roadNodes[i].transform.position).magnitude;

			//float lerper = percentComplete*percentComplete * (3f - 2f*percentComplete);//percentComplete*percentComplete*percentComplete * (percentComplete * (6f*percentComplete - 15f) + 10f);


			//zAngle = Mathf.LerpAngle(0, zAngle, dist);

			Vector3 rotat = new Vector3(0,0,zAngle);


			GameObject segment = Instantiate(roadSegmentPrefab, pos, Quaternion.Euler(rotat)) as GameObject;

			segment.transform.parent = newRoad.transform;

		}
		percentComplete += 0.1f;


	
	}

	void definePaths(){


		List<GameObject> paths = new List<GameObject> ();
		paths.Add( new GameObject ("Path1") );
		paths.Add( new GameObject ("Path2") );
		paths.Add( new GameObject ("Path3") );
		paths.Add( new GameObject ("Path4") );

		for (int x=0; x<4; x++) {

			paths[x].tag = "Path";

						for (int i=0; i<roadNodes.Count; i++) {

								GameObject edge = new GameObject ("edge");

								
				edge.transform.parent = paths[x].transform;
				edge.transform.position = roadNodes [i].transform.position;
						}

			paths[x].transform.parent = newRoad.transform;
						
						

				}


		float offset = -0.17f-0.34f; List<Transform> pathNodes;

		for (int x=0; x<4; x++) {



				pathNodes = new List<Transform> (paths[x].GetComponentsInChildren<Transform> ());
				pathNodes.RemoveAt (0);



						for (int i=0; i<pathNodes.Count; i++) {


				pathNodes [i].transform.position += getPerpendicularInRoadNodes(i, (int) Mathf.Sign(offset), i==0? Vector3.zero : pathNodes[i-1].transform.position , -1)* offset;
			
				if(i==pathNodes.Count-1){

					pathNodes [i].transform.Translate ( (pathNodes[i-1].transform.position - pathNodes[i].transform.position).normalized*0.34f ); //to fix ending nodes that are placed outside road for some reason
				}

						}
			offset+=0.34f;


			//for first two paths reverse direction
			if(x<2){

				int start =0; int end = pathNodes.Count-1;

				while(start<end){

					Vector3 temp = pathNodes[start].position;

					pathNodes[start].position = pathNodes[end].position;

					pathNodes[end].position = temp;

					start++; end--;
				}

			}


				}


		}


	void definePolygonCollider(){

		float shiftNodesMag = 0.9f;
		float shiftEndsMag = 0.44f;


		Vector3 recentlyAdded = Vector3.zero;
		int sign = 1;

				for (int i=0; i<roadNodes.Count; i++) {
					
			Vector3 shiftEnds = Vector3.zero; //to shift vertices at start and end of road
					
			Vector3 perpendicular = getPerpendicularInRoadNodes(i, sign, recentlyAdded, -1);


					if( i==0){

				Vector3 dir = (roadNodes[i+1].transform.position - roadNodes[i].transform.position).normalized;

				shiftEnds = -dir*shiftEndsMag;

			}

			
			recentlyAdded = roadNodes [i].transform.position + perpendicular * shiftNodesMag*sign + shiftEnds;

			polyColliderVertices.Add (recentlyAdded);


				}

		recentlyAdded = Vector3.zero;
		sign = -1;

		for (int i=roadNodes.Count-1; i>=0; i--) {
			
			Vector3 shiftEnds = Vector3.zero; //to shift back vertices at start and end of road
			
			Vector3 perpendicular = getPerpendicularInRoadNodes(i, sign, recentlyAdded, +1);
			
			if( i==0){
				
				Vector3 dir = (roadNodes[i+1].transform.position - roadNodes[i].transform.position).normalized;
				
				shiftEnds = -dir*shiftEndsMag;

			}

			
			recentlyAdded = roadNodes [i].transform.position + perpendicular * shiftNodesMag*sign + shiftEnds;
			
			polyColliderVertices.Add (recentlyAdded);
			
			
		}
				
		newRoad.GetComponent<PolygonCollider2D>().SetPath (0, polyColliderVertices.ToArray());
		}

	Vector3 getPerpendicularInRoadNodes(int i, int sign, Vector3 recentlyAdded, int previousIndex){

				if (i == 0) {
			
						Vector3 dir = (roadNodes [1].transform.position - roadNodes [0].transform.position).normalized;
			
						return getPerpendicularHelper (dir);

				} else if (i == roadNodes.Count - 1) {
			
						Vector3 dir = -(roadNodes [i - 1].transform.position - roadNodes [i].transform.position).normalized;
			
						return getPerpendicularHelper (dir);

				} else {
			
						Vector3 dir1 = (roadNodes [i - 1].transform.position - roadNodes [i].transform.position).normalized;
						Vector3 dir2 = (roadNodes [i + 1].transform.position - roadNodes [i].transform.position).normalized;
			
						float angle = Vector3.Angle (dir1, dir2);
						angle /= 2;
			
						Vector3 perpendicular = Vector3.RotateTowards (dir1, dir2, Mathf.Deg2Rad * angle, Mathf.Infinity);
			
						if (lineSegmentIntersectHelper (roadNodes [i + previousIndex].transform.position, roadNodes [i].transform.position, recentlyAdded, roadNodes [i].transform.position + perpendicular * sign)) {
				
								perpendicular *= -1;
						}
			return perpendicular;

				}
		}

	static Vector3 getPerpendicularHelper(Vector3 inp){

		Vector2 perp = new Vector2(-inp.y, inp.x) / Mathf.Sqrt( Mathf.Pow(inp.x,2) + Mathf.Pow(inp.y,2) );
		perp = perp.normalized;

		return perp;
	}


	static bool lineSegmentIntersectHelper(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {
		
		Vector2 a = p2 - p1;
		Vector2 b = p3 - p4;
		Vector2 c = p1 - p3;
		
		float alphaNumerator = b.y*c.x - b.x*c.y;
		float alphaDenominator = a.y*b.x - a.x*b.y;
		float betaNumerator  = a.x*c.y - a.y*c.x;
		float betaDenominator  = alphaDenominator; /*2013/07/05, fix by Deniz*/
		
		bool doIntersect = true;
		
		if (alphaDenominator == 0 || betaDenominator == 0) {
			doIntersect = false;
		} else {
			
			if (alphaDenominator > 0) {
				if (alphaNumerator < 0 || alphaNumerator > alphaDenominator) {
					doIntersect = false;
				}
			} else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator) {
				doIntersect = false;
			}
			
			if (doIntersect && betaDenominator > 0) {
				if (betaNumerator < 0 || betaNumerator > betaDenominator) {
					doIntersect = false;
				}
			} else if (betaNumerator > 0 || betaNumerator < betaDenominator) {
				doIntersect = false;
			}
		}
		
		return doIntersect;
	}
}
