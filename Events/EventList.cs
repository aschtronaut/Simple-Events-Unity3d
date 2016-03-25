using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public interface IEvent {}
public interface IEventInvoker {}


public interface IOnExampleEvent: IEvent
{
    void OnExampleEvent(string test);
}
