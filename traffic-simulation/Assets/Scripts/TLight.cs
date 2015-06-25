using UnityEngine;
using System.Collections;

public class TLight : MonoBehaviour {


	string currentColor;

	public float time;

	public GUISkin guiSkin;

	
	// Use this for initialization
	void Start () {

		renderer.material.color = Color.white;
		currentColor = "white";

		time = 1;

	}
	
	// Update is called once per frame
	void Update () {
		//print (time);
	}


	public void setColor(string clr){

		if( clr!="red" && clr!="yellow" && clr!="green" ){ return;}

		if(clr == "red") renderer.material.color = Color.red;
		if(clr == "green") renderer.material.color = Color.green;
		if(clr == "yellow") renderer.material.color = Color.yellow;

		currentColor = clr;

	}

	public string getColor(){////////////////////////////////////////////////////////////////

		return currentColor;

	}


	private void OnGUI() {
		GUI.skin = guiSkin;
		GUI.skin.label.normal.textColor = Color.yellow;
		
		int labelSize = 30;
		
		Vector3 position = transform.position;
		
		Vector3 screenPosition = Camera.main.WorldToScreenPoint (position);
		screenPosition = new Vector3 (screenPosition.x-(labelSize/2), Screen.height - screenPosition.y-(labelSize/2), 0);
		
		GUI.Label(new Rect(screenPosition.x, screenPosition.y, labelSize, labelSize), time.ToString("f2"));
		
	}

}
