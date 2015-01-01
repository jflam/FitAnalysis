using System;
using FastFitParser.Core;

namespace FitAnalysis
{
    public static class ParserExtensionMethods
    {
        public static bool IsStopTimerEvent(this DataRecord record)
        {
            if (record.GlobalMessageNumber == GlobalMessageNumber.Event)
            {
                byte eventField, eventTypeField;
                if (record.TryGetField((byte)EventFieldNumber.Event, out eventField))
                {
                    if ((Event)eventField == Event.Timer)
                    {
                        if (record.TryGetField((byte)EventFieldNumber.EventType, out eventTypeField))
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