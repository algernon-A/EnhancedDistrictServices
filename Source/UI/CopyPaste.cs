using System.Collections.Generic;

namespace EnhancedDistrictServices
{
    public static class CopyPaste
    {
        public static ushort BuildingTemplate = 0;

        public static bool CopyPolicyTo(ushort building)
        {
            Constraints.ReleaseBuilding(building);

            Constraints.SetInternalSupplyReserve(building, Constraints.InternalSupplyBuffer(BuildingTemplate));

            Constraints.SetAllInputLocalAreas(InputType.INCOMING, building, Constraints.InputAllLocalAreas(InputType.INCOMING, BuildingTemplate));
            Constraints.SetAllInputLocalAreas(InputType.INCOMING2, building, Constraints.InputAllLocalAreas(InputType.INCOMING2, BuildingTemplate));
            Constraints.SetAllOutputLocalAreas(InputType.OUTGOING, building, Constraints.OutputAllLocalAreas(InputType.OUTGOING, BuildingTemplate));
            Constraints.SetAllOutputLocalAreas(InputType.OUTGOING2, building, Constraints.OutputAllLocalAreas(InputType.OUTGOING2, BuildingTemplate));

            Constraints.SetAllInputOutsideConnections(InputType.INCOMING, building, Constraints.InputOutsideConnections(InputType.INCOMING, BuildingTemplate));
            Constraints.SetAllInputOutsideConnections(InputType.INCOMING2, building, Constraints.InputOutsideConnections(InputType.INCOMING2, BuildingTemplate));
            Constraints.SetAllOutputOutsideConnections(InputType.OUTGOING, building, Constraints.OutputOutsideConnections(InputType.OUTGOING, BuildingTemplate));
            Constraints.SetAllOutputOutsideConnections(InputType.OUTGOING2, building, Constraints.OutputOutsideConnections(InputType.OUTGOING2, BuildingTemplate));

            var inputDistrictsServed = Constraints.InputDistrictParkServiced(InputType.INCOMING, BuildingTemplate);
            if (inputDistrictsServed != null)
            {
                foreach (var districtPark in inputDistrictsServed)
                {
                    Constraints.AddInputDistrictParkServiced(InputType.INCOMING, building, districtPark);
                }
            }

            var inputDistrictsServed2 = Constraints.InputDistrictParkServiced(InputType.INCOMING2, BuildingTemplate);
            if (inputDistrictsServed2 != null)
            {
                foreach (var districtPark in inputDistrictsServed2)
                {
                    Constraints.AddInputDistrictParkServiced(InputType.INCOMING2, building, districtPark);
                }
            }

            var outputDistrictsServed = Constraints.OutputDistrictParkServiced(InputType.OUTGOING, BuildingTemplate);
            if (outputDistrictsServed != null)
            {
                foreach (var districtPark in outputDistrictsServed)
                {
                    Constraints.AddOutputDistrictParkServiced(InputType.OUTGOING, building, districtPark);
                }
            }

            var outputDistrictsServed2 = Constraints.OutputDistrictParkServiced(InputType.OUTGOING2, BuildingTemplate);
            if (outputDistrictsServed2 != null)
            {
                foreach (var districtPark in outputDistrictsServed2)
                {
                    Constraints.AddOutputDistrictParkServiced(InputType.OUTGOING2, building, districtPark);
                }
            }

            bool copySucceeded = true;

            var supplySources = Constraints.SupplySources(BuildingTemplate);
            for (int index = 0; index < supplySources?.Count; index++)
            {
                if (TransferManagerInfo.IsValidSupplyChainLink((ushort)supplySources[index], building))
                {
                    Constraints.AddSupplyChainConnection((ushort)supplySources[index], building);
                }
                else
                {
                    copySucceeded = false;
                }
            }

            var supplyDestinations = Constraints.SupplyDestinations(BuildingTemplate);
            for (int index = 0; index < supplyDestinations?.Count; index++)
            {
                if (TransferManagerInfo.IsValidSupplyChainLink(building, (ushort)supplyDestinations[index]))
                {
                    Constraints.AddSupplyChainConnection(building, (ushort)supplyDestinations[index]);
                }
                else
                {
                    copySucceeded = false;
                }
            }

            VehicleManagerMod.ReleaseBuilding(building);
            VehicleManagerMod.SetBuildingUseDefaultVehicles(building, VehicleManagerMod.BuildingUseDefaultVehicles[BuildingTemplate]);
            if (VehicleManagerMod.BuildingToVehicles[BuildingTemplate] != null && VehicleManagerMod.BuildingToVehicles[BuildingTemplate].Count > 0)
            {
                foreach (var prefabIndex in VehicleManagerMod.BuildingToVehicles[BuildingTemplate])
                {
                    VehicleManagerMod.AddCustomVehicle(building, prefabIndex);
                }                    
            }

            return copySucceeded;
        }
    }
}
