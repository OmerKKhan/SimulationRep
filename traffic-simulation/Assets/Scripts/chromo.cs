using UnityEngine;
using System.Collections;
using System.IO;
using LitJson;
//using Newtonsoft.Json;

/*public class values{

	public float localTrafficWeightage;
	public float minReduceFlowSignalStrength;
	public float minLocalThreshold;
	public float maxTrafficLightTime;
	public float maxTrafficLightTimeChangePerCycle;
	public float ReduceFlowSignalStrengthReductionPerNode;
	public float minTrafficLightTime;
	public float maxLocalThreshold;
}*/


public class chromo : MonoBehaviour {


	public float localTrafficWeightage;
	public float minReduceFlowSignalStrength;
	public float minLocalThreshold;
	public float maxTrafficLightTime;
	public float maxTrafficLightTimeChangePerCycle;
	public float ReduceFlowSignalStrengthReductionPerNode;
	public float minTrafficLightTime;
	public float maxLocalThreshold;
	

	// Use this for initialization
	void Start () {
		StreamReader sr = new StreamReader ("chromosome.txt");
		string jsonTxt = sr.ReadToEnd();
		print(jsonTxt);

		//values newTarget = JsonConvert.DeserializeObject<values>(json);
		JsonData jsonObj = JsonMapper.ToObject(jsonTxt);

		maxTrafficLightTime = float.Parse( jsonObj["maxTrafficLightTime"].ToString() );
		minTrafficLightTime = float.Parse( jsonObj["minTrafficLightTime"].ToString() );
		maxLocalThreshold = float.Parse( jsonObj["maxLocalThreshold"].ToString() );
		ReduceFlowSignalStrengthReductionPerNode = float.Parse( jsonObj["ReduceFlowSignalStrengthReductionPerNode"].ToString() );
		maxTrafficLightTimeChangePerCycle = float.Parse( jsonObj["maxTrafficLightTimeChangePerCycle"].ToString() );
		minLocalThreshold = float.Parse( jsonObj["minLocalThreshold"].ToString() );
		minReduceFlowSignalStrength = float.Parse( jsonObj["minReduceFlowSignalStrength"].ToString() );
		localTrafficWeightage = float.Parse( jsonObj["localTrafficWeightage"].ToString() );

		print (localTrafficWeightage.ToString()+","+
		       minReduceFlowSignalStrength.ToString()+","+
		       minLocalThreshold.ToString()+","+
		       maxTrafficLightTime.ToString()+","+
		       maxTrafficLightTimeChangePerCycle.ToString()+","+
		       ReduceFlowSignalStrengthReductionPerNode.ToString()+","+
		       minTrafficLightTime.ToString()+","+
		       maxLocalThreshold.ToString());
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
