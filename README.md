# Simple Interface Events for Unity3d

Here is an example event: the inherited 'IEvent' is just an empty interface so we have a common interface for all events.
You could have multiple of those base interfaces for different types of events if needed
```C#
public interface IExampleEvent: IEvent{

    void ExampleEvent(string test, float number); //define the signature of the method that will be called when IExampleEvent is invoked somewhere
}
```


The class below inherits from BaseObject and will be linked with any subscribed events. In this case, IExampleEvent from above
```C#
public class ExampleRecieve : BaseObject, IExampleEvent {

	//Since we have implemented IExampleEvent, this is called if somebody invokes the event
	public void ExampleEvent(string firstParam, float secondParam){
		UnityEngine.Debug.Log(firstParam + " " + secondParam);
	}
}
```


This class below implements IEventInvoker, which allows us to access our BaseObject's static readonly 'Events' property and invoke events if we have their interface name and signature
```C#
public class ExampleInvoke : BaseObject, IEventInvoker {

	void Start(){
		//We invoke the IExampleEvent from here, and any class that implements that interface will receive the message
		Events.InvokeEvent<IExampleEvent>("Example text!", 10f);
		
		//There is also an InvokeEventFast method, which requires you to specify the types involved
		//But if you call an event multiple times in a frame, this could elicit a small performance improvement
		Events.InvokeEventFast<IExampleEvent, string, float>("Fast example text!" 20f);
	}
}
```
