using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XInputDotNetPure;
using System;
//just copied the above from trafficController.cs


public struct completePath
{
	public GameObject startObject;
	
	public List<Vector3> nodes;
	
	public int transitionIndex;
	
	public GameObject endObject;

	public int goStraightWeightage;
	
	public completePath(GameObject s, List<Vector3> n, int i, GameObject e, int goS) 
	{
		this.startObject = s;
		this.nodes = n;
		this.transitionIndex = i;
		this.endObject = e;
		this.goStraightWeightage = goS;
	}
	
	public completePath getClone() { return (completePath) this.MemberwiseClone(); }
}
