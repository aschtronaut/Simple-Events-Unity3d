using UnityEngine;
using System.Collections.Generic;

//Use this if you want to visualize all implemented IEvents
//Just put it on something in the scene
public class EventViewer : BaseObject, IEventInvoker{
	[SerializeField]
	private List<EventVisualizer> eventImplementations;

	protected override void LateAwake()
	{
		eventImplementations = Events.EventVisualizers;
	}
}
