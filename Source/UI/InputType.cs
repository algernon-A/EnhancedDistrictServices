using System;

namespace EnhancedDistrictServices
{
    [Flags]
    public enum InputType
    {
        NONE = 0,
        INCOMING = 1,
        OUTGOING = 2,
        SUPPLY_CHAIN = 4,
        VEHICLES = 8,
        INCOMING2 = 5,
        OUTGOING2 = 6
    }
}
