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

            Constraints.SetAllInputLocalAreas(EnhancedDistrictServicesUIPanel.InputMode.INCOMING, building, Constraints.InputAllLocalAreas(EnhancedDistrictServicesUIPanel.InputMode.INCOMING, BuildingTemplate));
            Constraints.SetAllInputLocalAreas(EnhancedDistrictServicesUIPanel.InputMode.INCOMING2, building, Constraints.InputAllLocalAreas(EnhancedDistrictServicesUIPanel.InputMode.INCOMING2, BuildingTemplate));
            Constraints.SetAllOutputLocalAreas(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING, building, Constraints.OutputAllLocalAreas(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING, BuildingTemplate));
            Constraints.SetAllOutputLocalAreas(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING2, building, Constraints.OutputAllLocalAreas(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING2, BuildingTemplate));

            Constraints.SetAllInputOutsideConnections(EnhancedDistrictServicesUIPanel.InputMode.INCOMING, building, Constraints.InputOutsideConnections(EnhancedDistrictServicesUIPanel.InputMode.INCOMING, BuildingTemplate));
            Constraints.SetAllInputOutsideConnections(EnhancedDistrictServicesUIPanel.InputMode.INCOMING2, building, Constraints.InputOutsideConnections(EnhancedDistrictServicesUIPanel.InputMode.INCOMING2, BuildingTemplate));
            Constraints.SetAllOutputOutsideConnections(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING, building, Constraints.OutputOutsideConnections(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING, BuildingTemplate));
            Constraints.SetAllOutputOutsideConnections(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING2, building, Constraints.OutputOutsideConnections(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING2, BuildingTemplate));

            var inputDistrictsServed = Constraints.InputDistrictParkServiced(EnhancedDistrictServicesUIPanel.InputMode.INCOMING, BuildingTemplate);
            if (inputDistrictsServed != null)
            {
                foreach (var districtPark in inputDistrictsServed)
                {
                    Constraints.AddInputDistrictParkServiced(EnhancedDistrictServicesUIPanel.InputMode.INCOMING, building, districtPark);
                }
            }

            var inputDistrictsServed2 = Constraints.InputDistrictParkServiced(EnhancedDistrictServicesUIPanel.InputMode.INCOMING2, BuildingTemplate);
            if (inputDistrictsServed2 != null)
            {
                foreach (var districtPark in inputDistrictsServed2)
                {
                    Constraints.AddInputDistrictParkServiced(EnhancedDistrictServicesUIPanel.InputMode.INCOMING2, building, districtPark);
                }
            }

            var outputDistrictsServed = Constraints.OutputDistrictParkServiced(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING, BuildingTemplate);
            if (outputDistrictsServed != null)
            {
                foreach (var districtPark in outputDistrictsServed)
                {
                    Constraints.AddOutputDistrictParkServiced(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING, building, districtPark);
                }
            }

            var outputDistrictsServed2 = Constraints.OutputDistrictParkServiced(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING2, BuildingTemplate);
            if (outputDistrictsServed2 != null)
            {
                foreach (var districtPark in outputDistrictsServed2)
                {
                    Constraints.AddOutputDistrictParkServiced(EnhancedDistrictServicesUIPanel.InputMode.OUTGOING2, building, districtPark);
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
