using UnityEngine;
using System.Collections;

public abstract class BaseObject : MonoBehaviour {
	private readonly static EventCache<IEvent> _eventCache = new EventCache<IEvent>();

	//If you implement IEventInvoker you can access the Events property
	//With this you can run the InvokeEvent<Interface>() methods
	protected EventCache<IEvent> Events{
		get{
			if(this is IEventInvoker){
				if(_eventCache != null){
					return _eventCache;
				}
				else {
					throw new System.InvalidOperationException("An EventCache is null");
				}
			}
			else {
				throw new System.InvalidOperationException(this.gameObject + " does not implement IEventInvoker and can therefore not invoke events");
			}
		}
	}

	//If you inherit from this class, you cannot implement the Awake() method in you class and must use the LateAwake override instead.
	//If you have an Awake method, this will just silently not run, and events will not get linked up for that class
	private void Awake(){
		EarlyAwake();
		LinkEvents();
		LateAwake();
	}

	protected virtual void LateAwake () {}
	protected virtual void EarlyAwake () {}

	private void LinkEvents(){
		if(_eventCache != null){
			EventLinker.LinkEvents<IEvent>(this, _eventCache);
        }
	}

	private void OnDestroy(){
		if(_eventCache != null){
			EventLinker.UnLinkEvents<IEvent>(this, _eventCache);
        }
	}
}
