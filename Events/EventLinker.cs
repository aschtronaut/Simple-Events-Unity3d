using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

// Links/unlinks the appropriate methods with their respective delegates in the EventCache
public class EventLinker {
	private enum LinkType{ Link, Unlink };

	public static void LinkEvents <TBaseEventInterface> (Object obj, EventCache<TBaseEventInterface> Events) {
		LinkEvents <TBaseEventInterface> (obj, Events, LinkType.Link);
	}

	public static void UnLinkEvents <TBaseEventInterface> (Object obj, EventCache<TBaseEventInterface> Events) {
		LinkEvents <TBaseEventInterface> (obj, Events, LinkType.Unlink);
	}

	private static void LinkEvents <TBaseEventInterface> (Object obj, EventCache<TBaseEventInterface> Events, LinkType linkType){
		var baseType = typeof(TBaseEventInterface);

		//Load the .NET assembly related to our IEvent (all code we make in a project is usually compiled to the same assembly I think)
	    var assembly = baseType.Assembly;

	    //Get all interfaces of type IEvent
		List<Type> IEvents = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(baseType) && t.IsInterface).ToList();
		foreach (Type eventInterface in IEvents)
		{
			//check if we implement the current interface in our list
			if(IsImplementationOf(obj.GetType(),eventInterface))
			{
				//Get all methods in the interface
				MethodInfo[] methods = eventInterface.GetMethods();

				//Iterate over the methods on the interface that is going to be subscribed to the delegate
				//Idealy we'd probably only have one method for each interface though
				foreach(MethodInfo method in methods)
				{
					//Get the type for each argument/parameter in that method
					List<Type> paramTypes = method.GetParameters().Select(p => p.ParameterType).ToList();

					//The method in our EventCache through which we subscribe our method through
					//Getting methods by string feels dirty though. Must be a better way.
					string subMethodName = "SubscribeEvent";
					if(linkType == LinkType.Unlink){
						subMethodName = "UnSubscribeEvent";
					}
					List<MethodInfo> subscribeMethods = typeof(EventCache<TBaseEventInterface>).GetMethods().Where(t => t.Name == subMethodName).ToList();

					//Find the correct overloaded method depending on how many generic arguments it has
					//We use the one that has as many arguments as our own method
					MethodInfo subscriptionMethod = null;

					foreach (var z in subscribeMethods) {
						var par = z.GetParameters();
						foreach(var a in par){
							if(a.ParameterType.GetGenericArguments().Count() == paramTypes.Count){
								subscriptionMethod = z;
							}
						}
					}

					//Collect all the arguments that we'll use with the MakeGenericMethod method.
					Type[] totalArguments = new List<Type> {eventInterface}.Concat(paramTypes).ToArray();
					Type action = null;
					Type finalGenericType = null;

					//Depending on the amount of arguments we have to construct a fitting delegate out of our method so we can pass it as an argument to the subscription method
					//Also, we have to call the subscription method through reflection because we cant specify type at compile time

					//If the method we want to subscribe to doesn't have any arguments, we just specify the type as a simple non-generic Action
					if(paramTypes.Count == 0){
						finalGenericType = typeof(Action);
					}
					//If it does have arguments we have to first create an open generic type and create the final closed type with MakeGenericType
					else {
						if (paramTypes.Count > 3) {
							throw new ArgumentException(method.Name + " has too many parameters, please cut down to a maximum of 3!");
						}
						else if (paramTypes.Count > 2) {
							action = typeof(Action<,,>);
						}
						else if (paramTypes.Count > 1) {
							action = typeof(Action<,>);
						}
						else if (paramTypes.Count > 0) {
							action = typeof(Action<>);
						}
						//Create the closed generic type by supplying the types of our method arguments
						//E.g. the type would now be something like Action<X,Y,Z>, or the like.
						finalGenericType = action.MakeGenericType(paramTypes.ToArray());
					}

					//turn the whole thing into a new delegate of our newly created type Action<X,Y,Z> pointing to the implObject instance,
					//and the name of the method that we know will be there because it implements the interface
					//final two bool args are just if we are not case sensitive and if we throw an exception on failure
					var result = Delegate.CreateDelegate(finalGenericType, obj, method.Name, false, true);

					//add our delegate to an array that will be supplied as argument when invoking through reflection
					object[] parametersArray = new object[] {result};

					//Build and invoke a reflected version of EventCache.SubscribeEvent<>();
					//With as many arguments as would be there had we called it normally
					subscriptionMethod = subscriptionMethod.MakeGenericMethod(totalArguments);
					subscriptionMethod.Invoke(Events, parametersArray);
				}
			}
		}
	}

	//Does the object implement a given interface
	public static bool IsImplementationOf(Type checkMe, Type interfaceType)
	{
	    if (interfaceType.IsGenericTypeDefinition)
	        return checkMe.GetInterfaces().Select(i =>
	        {
	            if (i.IsGenericType)
	                return i.GetGenericTypeDefinition();

	            return i;
	        }).Any(i => i == interfaceType);

	    return interfaceType.IsAssignableFrom(checkMe);
	}
}
