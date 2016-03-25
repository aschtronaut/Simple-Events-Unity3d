// #define DEBUGCALLER

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

//We don't specify the base interface type since we might want to manage several different base interfaces instead of one big monster if we have many events
public class EventCache <TBaseEventInterface> {
	//Dictionary were we link up every TBaseEventInterface interface with a delegate
	//Which is actually an Action<>, but we upcast to delegate so we can have different amounts of arguments (Action<X>, Action<X,Y,Z>, etc).
	private Dictionary<System.RuntimeTypeHandle, Delegate> delegateDict;
	private Dictionary<System.RuntimeTypeHandle, EventVisualizer> eventImplementations;
	//Eventvisualizer lets us see from the inspector which GameObjects subscribe to what.
    public List<EventVisualizer> EventVisualizers{
    	get;
    	private set;
    }

    //For debugging when you don't know where the fuck some shitty event is being called
    //This won't be called or cost performance at all if DEBUGCALLER is not defined in the top
    [System.Diagnostics.Conditional("DEBUGCALLER")]
    private static void GetEventOrigin<TEvent>(Delegate del){
    	//Look back two frames on the call stack and print out that method and the class that owns it
    	var callingMethod = new System.Diagnostics.StackFrame(2).GetMethod();
    	string trace = string.Format("({0}) {1} event was invoked by:\n{2} : {3}", typeof(TEvent), del.Method.Name, callingMethod.DeclaringType, callingMethod.Name);
    	UnityEngine.Debug.Log(trace);
    }

	//INVOKE EVENTS
	//First overload takes no arguments and is fast and nice
	public void InvokeEvent<TEvent>() where TEvent : TBaseEventInterface{
		Delegate del;
		//Throw our runtimeTypeHandle of TEvent against the dictionary and hope for a matching delegate
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			//Downcast the delegate back to Action, allowing us to invoke it much faster than Delelegate.DynamicInvoke()
			var act = del as Action;
			if(act!=null){
				GetEventOrigin<TEvent>(del); //If #define DEBUGCALLER is active, this is called
				//Invoke all subscribed methods
				act();
			}
		}
	}

	//This is slower because Dynamicinvoke has to use reflection behind the scenes to figure out what the arguments are, and will just fail if they're wrong
	//Totally generic delegates can only be invoked by DynamicInvoke, whereas Action<> delegates can be called almost as fast as a normal method.
	//It's no problem as long as you don't call it every frame and stuff like that, but it's a shame when our delegate was originally a nice Action<>
	//USE THIS FOR EVENTS THAT ARE NOT CALLED CONSTANTLY
	public void InvokeEvent<TEvent>(params System.Object[] args) where TEvent : TBaseEventInterface{
		Delegate del;
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			GetEventOrigin<TEvent>(del); //If #define DEBUGCALLER is active, this is called
			del.DynamicInvoke(args);
		}
	}

	//we can make it faster by supplying the type as a generic argument, since we can then cast our delegate its original Action type
	//It's more annoying because you use it like: InvokeEvent<IEvent, BlaBlaType1, BlaBlaType2, BlaBlaType3> (x, y, z); etc
	//USE THIS FOR EVENTS THAT ARE CALLED ALL THE TIME (e.g. update)
	public void InvokeEventFast<TEvent, T1>(T1 arg) where TEvent : TBaseEventInterface{
		Delegate del;
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			var act = del as Action<T1>;
			if(act!=null){
				GetEventOrigin<TEvent>(del); //If #define DEBUGCALLER is active, this is called
				act(arg);
			}
		}
	}

	public void InvokeEventFast<TEvent, T1, T2>(T1 arg, T2 arg2) where TEvent : TBaseEventInterface{
		Delegate del;
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			var act = del as Action<T1,T2>;
			if(act!=null){
				GetEventOrigin<TEvent>(del); //If #define DEBUGCALLER is active, this is called
				act(arg, arg2);
			}
		}
	}

	public void InvokeEventFast<TEvent, T1, T2,T3>(T1 arg, T2 arg2, T3 arg3) where TEvent : TBaseEventInterface{
		Delegate del;
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			var act = del as Action<T1,T2,T3>;
			if(act!=null){
				GetEventOrigin<TEvent>(del); //If #define DEBUGCALLER is active, this is called
				act(arg, arg2, arg3);
			}
		}
	}


	//SUBSCRIBE TO EVENT
	public void SubscribeEvent<TEvent>(Action method) where TEvent : TBaseEventInterface{
		var handle = typeof(TEvent).TypeHandle;
		Delegate del;
		if(delegateDict.TryGetValue(handle, out del)){
			//Delegates are copies and not refs it seems,
			//so when we modify our invocationlist with += or -= we have to overrride the old dictionary value with our new delegate to actually update it
			var act = del as Action;

			//We try an unsubscribe before subscribing. This way we can't get into a situation where we call the same method twice
			act -= method;
			act += method;

			//Add the subscriber to our visualizer
			//This way we can track who is subbed to what
			//We're kinda assuming it's a MonoBehaviour for the benefit of being able to click on them in the inspector. If it isn't that's kinda gonna break it
			delegateDict[handle] = act;
			VisualizeSubscribers<TEvent>(method.Target as UnityEngine.MonoBehaviour);
		}
	}

	public void SubscribeEvent<TEvent, T1>(Action<T1> method) where TEvent : TBaseEventInterface{
		var handle = typeof(TEvent).TypeHandle;
		Delegate del;
		if(delegateDict.TryGetValue(handle, out del)){
			var act = del as Action<T1>;
			act -= method;
			act += method;

			delegateDict[handle] = act;
			VisualizeSubscribers<TEvent>(method.Target as UnityEngine.MonoBehaviour);
		}
	}

	public void SubscribeEvent<TEvent, T1, T2>(Action<T1,T2> method) where TEvent : TBaseEventInterface{
		var handle = typeof(TEvent).TypeHandle;
		Delegate del;
		if(delegateDict.TryGetValue(handle, out del)){
			var act = del as Action<T1,T2>;
			act -= method;
			act += method;

			delegateDict[handle] = act;
			VisualizeSubscribers<TEvent>(method.Target as UnityEngine.MonoBehaviour);
		}
	}

	public void SubscribeEvent<TEvent, T1, T2, T3>(Action<T1,T2,T3> method) where TEvent : TBaseEventInterface{
		var handle = typeof(TEvent).TypeHandle;
		Delegate del;
		if(delegateDict.TryGetValue(handle, out del)){
			var act = del as Action<T1,T2,T3>;
			act -= method;
			act += method;

			delegateDict[handle] = act;
			VisualizeSubscribers<TEvent>(method.Target as UnityEngine.MonoBehaviour);
		}
	}

	//Adds our subs to a list so we can see what things implement the different classes
	void VisualizeSubscribers<TEvent>(UnityEngine.MonoBehaviour owner){
		if(owner!=null){
			eventImplementations[typeof(TEvent).TypeHandle].implementations.Add(owner);
		}
	}

	//UNSUB to an event;
	public void UnSubscribeEvent<TEvent>(Action method) where TEvent : TBaseEventInterface{
		Delegate del;
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			//Delegates are copies and not refs it seems,
			//so when we modify our invocationlist with += or -= we have to overrride the old dictionary value with our new delegate to actually update it
			var act = del as Action;
			act -= method;
			delegateDict[typeof(TEvent).TypeHandle] = act;
		}
	}
	public void UnSubscribeEvent<TEvent, T>(Action<T> method) where TEvent : TBaseEventInterface{
		Delegate del;
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			var act = del as Action<T>;
			act -= method;
			delegateDict[typeof(TEvent).TypeHandle] = act;
		}
	}
	public void UnSubscribeEvent<TEvent, T1, T2>(Action<T1,T2> method) where TEvent : TBaseEventInterface{
		Delegate del;
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			var act = del as Action<T1, T2>;
			act -= method;
			delegateDict[typeof(TEvent).TypeHandle] = act;
		}
	}
	public void UnSubscribeEvent<TEvent, T1, T2, T3>(Action<T1,T2,T3> method) where TEvent : TBaseEventInterface{
		Delegate del;
		if(delegateDict.TryGetValue(typeof(TEvent).TypeHandle, out del)){
			var act = del as Action<T1, T2, T3>;
			act -= method;
			delegateDict[typeof(TEvent).TypeHandle] = act;
		}
	}

	//All these are just there to supply a correct empty Action<> delegate for our dictionary
	//This will resolve to something that the compiler will accept as an Action with the correct argument types while actually being null
	//This isn't a problem because we want the Action to be null until we do += on it
	private Action DynDel(){
		return (Action)delegate{} ;
	}
	private Action<T> DynDel<T>(){
		return (Action<T>)delegate{} ;
	}
	private Action<T,T2> DynDel<T,T2>(){
		return (Action<T,T2>)delegate{} ;
	}
	private Action<T,T2,T3> DynDel<T,T2,T3>(){
		return (Action<T,T2,T3>)delegate{} ;
	}
	private Action<T,T2,T3,T4> DynDel<T,T2,T3,T4>(){
		return (Action<T,T2,T3,T4>)delegate{} ;
	}

	//Jesus christ killmee
	//This automatically creates an Action delegate for every event interface we have
	private void SetupActions(){
		delegateDict = new Dictionary<System.RuntimeTypeHandle, Delegate>();
		eventImplementations = new Dictionary<System.RuntimeTypeHandle,EventVisualizer>();
		EventVisualizers = new List<EventVisualizer>();

		var baseType = typeof(TBaseEventInterface);
	    var assembly = baseType.Assembly;
	    //Make a list of every interface that implements IEvent (which is just an empty interface so we can identify our events)
		List<Type> IEvents = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(baseType) && t.IsInterface).ToList();
		foreach (var x in IEvents) {

			//we add every interface to a public dictionary to get an overview of who is implementing what
			if(x.ToString()!=""){
				var evViz = new EventVisualizer (x.ToString());
				eventImplementations.Add(x.TypeHandle, evViz);
				EventVisualizers.Add(evViz);
			}

			var methods = x.GetMethods();
			//every event interface should have exactly one method, but for now we'll just go through all of them if there are more
			//This will result in only the last defined method being used. We could probably fix this by having a Dictionary<TypeHandle, List<Delegate> > instead
			foreach(MethodInfo method in methods)
			{
				//Get the type for each argument/parameter in that method
				List<Type> paramTypes = method.GetParameters().Select(p => p.ParameterType).ToList();
				//Dictionary.Add() wants compile-time verifiable types
				//But we can call it through reflection to get around that
				MethodInfo dictMethod = typeof(IDictionary).GetMethod("Add");
				MethodInfo dictMethodGeneric = dictMethod;
				MethodInfo generic;

				//The method we use here will create a fitting Action<X,Y,Z> to be the value in our dictionary
				List<MethodInfo> subscribeMethods = typeof(EventCache<TBaseEventInterface>).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(t => t.Name =="DynDel").ToList();

				//Find the correct overloaded method depending on how many generic arguments it has
				//We use the one that has as many arguments as our own method
				MethodInfo subscriptionMethod = null;
				foreach (var z in subscribeMethods) {
					var par = z.GetGenericArguments();
					if(par.Count() == paramTypes.Count){
						subscriptionMethod = z;
					}
				}

				if (paramTypes.Count > 3 || subscriptionMethod == null) {
					throw new ArgumentException(method.Name + " has too many parameters, please cut down to a maximum of 3!");
				}
				else{
					//Generate an Action delegate with the amount of generic parameters it needs
					generic = subscriptionMethod.MakeGenericMethod(paramTypes.ToArray());
					//The generic method returns the action that we are going to supply to our dictionary
					var output = generic.Invoke(this, null);
					object[] parametersArray = new object[] {x.TypeHandle,output};
					//Invoke the Dictionary.Add() method through reflection and supply our parameters
					dictMethodGeneric.Invoke(this.delegateDict,parametersArray);

				}
			}
		}
	}

	public EventCache () {
		SetupActions();
	}
}

//One EventVisualizer for each interface
//Inside we list all that implement that interface
[Serializable]
public class EventVisualizer{
	[UnityEngine.HideInInspector]
	public string name;
	public List<UnityEngine.MonoBehaviour> implementations;

	public EventVisualizer(string name){
		this.name = name;
		implementations = new List<UnityEngine.MonoBehaviour>();
	}
}
