using System;
using FastFitParser.Core;

namespace FitAnalysis
{
    public static class ParserExtensionMethods
    {
        public static bool IsStopTimerEvent(this DataRecord record)
        {
            if (record.GlobalMessageNumber == GlobalMessageDefs.Event)
            {
                byte eventField, eventTypeField;
                if (record.TryGetField(EventDef.Event, out eventField))
                {
                    if ((Event)eventField == Event.Timer)
                    {
                        if (record.TryGetField(EventDef.EventType, out eventTypeField))
                        {
                            EventType eventType = (EventType)eventTypeField;
                            if (eventType == EventType.Stop || eventType == EventType.StopAll)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}