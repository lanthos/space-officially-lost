﻿using System.Collections;
using UnityEngine;
public class AgentBehaviour :
MonoBehaviour 
{
	public GameObject target;
	public Agent agent;
	public virtual void Awake()
	{
		agent = 
			gameObject.GetComponent<Agent> ();
	}

	public virtual void Update()
	{

		agent.SetSteering (GetSteering ());
	}

	public virtual Steering
		GetSteering ()
		{
			return new Steering();
		}
}

