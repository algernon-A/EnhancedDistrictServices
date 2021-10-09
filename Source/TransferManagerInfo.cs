using ColossalFramework;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Misc helper methods for classifying buildings and offers.
    /// </summary>
    public static class TransferManagerInfo
    {
        /// <summary>
        /// Returns the building id associated with the offer, if specified.
        /// If a citizen is associated with the offer, returns the citizen's home building id.
        /// If a service vehicle is associated with the offer, returns that vehicle's service building.
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public static ushort GetHomeBuilding(ref TransferManager.TransferOffer offer)
        {
            if (offer.Vehicle != 0)
            {
                return VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].m_sourceBuilding;
            }

            if (offer.Citizen != 0)
            {
                return CitizenManager.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
            }

            if (offer.Building != 0)
            {
                return offer.Building;
            }

            return 0;
        }

        /// <summary>
        /// Returns the name of the building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetBuildingName(int building)
        {
            return Singleton<BuildingManager>.instance.GetBuildingName((ushort)building, InstanceID.Empty);
        }

        /// <summary>
        /// Returns the input type of the building, used to determine which GUI elements are shown in the main panel.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static List<InputType> GetBuildingInputTypes(int building)
        {
            var inputTypes = new List<InputType>();
            if (building == 0)
            {
                inputTypes.Add(InputType.NONE);
                return inputTypes;
            }

            // The only building type for which we will not show an outgoing tab is coal and heating power plants.

            var info = BuildingManager.instance.m_buildings.m_buffer[building].Info;
            if (TransferManagerInfo.IsDistrictServicesBuilding(building))
            {
                if (IsTwoOutputBuilding(building))
                {
                    inputTypes.Add(InputType.OUTGOING);
                    inputTypes.Add(InputType.OUTGOING2);
                }
                else if ((info?.GetService() == ItemClass.Service.Electricity && info?.GetAI() is PowerPlantAI) ||
                    (info?.GetService() == ItemClass.Service.Water && info?.GetAI() is HeatingPlantAI) ||
                    (info?.GetService() == ItemClass.Service.Monument && info?.gameObject?.name == "ChirpX Launch Control Center"))
                {
                }
                else if (!Settings.enableIndustriesControl && info?.GetService() == ItemClass.Service.PlayerIndustry)
                {
                }
                else
                {
                    inputTypes.Add(InputType.OUTGOING);
                }
            }

            if (Settings.enableIndustriesControl && TransferManagerInfo.IsSupplyChainBuilding(building))
            {
                inputTypes.Add(InputType.SUPPLY_CHAIN);
                if (IsTwoInputBuilding(building))
                {
                    inputTypes.Add(InputType.INCOMING);
                    inputTypes.Add(InputType.INCOMING2);
                }
                else if (!(info?.GetAI() is ExtractingFacilityAI || info?.GetAI() is FishFarmAI || info?.GetAI() is FishingHarborAI || info?.GetAI() is LandfillSiteAI landfillSiteAI && landfillSiteAI.m_info.name.Contains("Recycling Center")))
                {
                    inputTypes.Add(InputType.INCOMING);
                }
            }

            if (TransferManagerInfo.IsCustomVehiclesBuilding(building))
            {
                inputTypes.Add(InputType.VEHICLES);
            }

            if(inputTypes.Count == 0)
            {
                inputTypes.Add(InputType.NONE);
            }

            return inputTypes;
        }

        public static string GetBuildingInputTypeText(int building)
        {
            var inputTypes = GetBuildingInputTypes(building);

            var txtItems = new List<string>();
            if (!inputTypes.Contains(InputType.SUPPLY_CHAIN))
            {
                txtItems.Add($"Services");
            }
            else
            {
                txtItems.Add($"Supply Chain");

                if (inputTypes.Contains(InputType.INCOMING))
                {
                    txtItems.Add($"Incoming");
                }

                if (inputTypes.Contains(InputType.INCOMING2))
                {
                    txtItems.Add($"Incoming2");
                }

                if (inputTypes.Contains(InputType.OUTGOING))
                {
                    txtItems.Add($"Outgoing");
                }

                if (inputTypes.Contains(InputType.OUTGOING2))
                {
                    txtItems.Add($"Outgoing2");
                }
            }

            return $"Building Type: {string.Join(", ", txtItems.ToArray())}";
        }

        /// <summary>
        /// Returns the district of the offer's home building or segment.
        /// Should return 0 if the offer does not originate from a district.
        /// </summary>
        /// <returns></returns>
        public static DistrictPark GetDistrictPark(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            if (offer.NetSegment != 0)
            {
                var position = NetManager.instance.m_segments.m_buffer[offer.NetSegment].m_middlePosition;
                return DistrictPark.FromPosition(position);
            }
            else if ((material == TransferManager.TransferReason.Sick || material == TransferManager.TransferReason.Taxi) && offer.Citizen != 0)
            {
                return DistrictPark.FromPosition(offer.Position);
            }
            else
            {
                return GetDistrictPark(GetHomeBuilding(ref offer));
            }
        }

        /// <summary>
        /// Returns the district and/or park of the building.
        /// Should return 0 if the building is not in a district and/or park.
        /// </summary>
        /// <returns></returns>
        public static DistrictPark GetDistrictPark(int building)
        {
            if (building != 0)
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
                return DistrictPark.FromPosition(position);
            }
            else
            {
                return new DistrictPark();
            }
        }

        public static string GetCustomVehiclesText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            if (!VehicleManagerMod.BuildingUseDefaultVehicles[building] && VehicleManagerMod.BuildingToVehicles[building] != null)
            {
                var txtItems = new List<string>();
                txtItems.Add($"<<Custom Vehicles>>");

                foreach (var prefabIndex in VehicleManagerMod.BuildingToVehicles[building])
                {
                    var name = ColossalFramework.Globalization.Locale.Get("VEHICLE_TITLE", PrefabCollection<VehicleInfo>.PrefabName((uint)prefabIndex));
                    txtItems.Add(name);
                }

                return string.Join("\n", txtItems.ToArray());
            }
            else 
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the home district and/or park of the specified building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetDistrictParkText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var districtPark = GetDistrictPark(building);
            if (!districtPark.IsEmpty)
            {
                return $"Home district: {districtPark.Name}";
            }
            else
            {
                return $"Home district: (Not in a district)";
            }
        }

        public static int GetCargoVehicleCount(ushort building, TransferManager.TransferReason material)
        {
            VehicleManager instance = Singleton<VehicleManager>.instance;
            ushort vehicleID = BuildingManager.instance.m_buildings.m_buffer[building].m_ownVehicles;
            int count = 0;
            while (vehicleID != (ushort)0)
            {
                var vehiclePrefabAI = instance.m_vehicles.m_buffer[(int)vehicleID].Info.GetAI();
                var vehicleMaterial = (TransferManager.TransferReason)instance.m_vehicles.m_buffer[(int)vehicleID].m_transferType;

                if (vehiclePrefabAI.GetType() == typeof(CargoPlaneAI) || vehiclePrefabAI.GetType() == typeof(CargoShipAI) || vehiclePrefabAI.GetType() == typeof(CargoTrainAI) || vehiclePrefabAI.GetType().Name.Equals("CargoFerryHarborAI"))
                {
                    count += 5;
                }
                else if (vehicleMaterial == material)
                {
                    if ((instance.m_vehicles.m_buffer[(int)vehicleID].m_flags & (Vehicle.Flags.Importing | Vehicle.Flags.Exporting)) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
                    {
                        // Logger.LogVerbose($"TransferManagerInfo::GetCargeVehicleCount: {building}, {vehicleID}, {material}, {instance.m_vehicles.m_buffer[(int)vehicleID].m_leadingVehicle}, {instance.m_vehicles.m_buffer[(int)vehicleID].m_trailingVehicle}, {count}");
                        ++count;
                    }
                }

                vehicleID = instance.m_vehicles.m_buffer[(int)vehicleID].m_nextOwnVehicle;
                if (count > 16384)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns a descriptive text indicating the districts that are served by the specified building.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetOutputDistrictsServedText(InputType inputType, ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Districts served>>");

            if (Constraints.OutputAllLocalAreas(inputType, building))
            {
                txtItems.Add($"All local areas");
            }
            else if (Constraints.OutputDistrictParkServiced(inputType, building) == null || Constraints.OutputDistrictParkServiced(inputType, building).Count == 0)
            {
                return $"<<No districts served!>>";
            }
            else
            {
                var districtParkNames = Constraints.OutputDistrictParkServiced(inputType, building)
                    .Select(dp => dp.Name)
                    .OrderBy(s => s);

                foreach (var districtParkName in districtParkNames)
                {
                    txtItems.Add(districtParkName);
                }
            }

            return string.Join("\n", txtItems.ToArray());
        }

        /// <summary>
        /// Returns a descriptive text about the type of service provided by the building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetServicesText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[building].Info;
            var service = buildingInfo.GetService();
            var subService = buildingInfo.GetSubService();
            if (buildingInfo.GetAI() is OutsideConnectionAI)
            {
                if (buildingInfo.GetService() == ItemClass.Service.Road)
                {
                    return $"Service: OutsideConnection (Road)";
                }
                else
                {
                    return $"Service: OutsideConnection ({subService})";
                }
            }
            else if (service == ItemClass.Service.PlayerIndustry)
            {
                var resource = GetSupplyBuildingOutputMaterial(building);
                if (resource != TransferManager.TransferReason.None)
                {
                    return $"Service: {service} ({resource})";
                }
                else
                {
                    return $"Service: {service}";
                }
            }
            else
            {
                return $"Service: {service}";
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the supply chain destination buildings that the given building
        /// will ship to.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplyBuildingDestinationsText(InputType inputType, ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Supply Chain Shipments To>>");

            if (Constraints.OutputAllLocalAreas(inputType, building))
            {
                txtItems.Add($"All local areas");
            }

            if (Constraints.OutputOutsideConnections(inputType, building))
            {
                txtItems.Add($"All outside connections");
            }

            if (Constraints.OutputAllLocalAreas(inputType, building))
            {
                return string.Join("\n", txtItems.ToArray());
            }

            // Next, list output building names
            var supply = Constraints.SupplyDestinations(building);
            if (supply?.Count > 0)
            {
                var buildingNames = supply
                    .Select(b => $"{GetBuildingName(b)} ({b})")
                    .OrderBy(s => s);

                foreach (var buildingName in buildingNames)
                {
                    txtItems.Add(buildingName);
                }
            }

            var districts = Constraints.OutputDistrictParkServiced(inputType, building);
            if (districts?.Count > 0)
            {
                // Then add district names
                var districtParkNames = districts
                    .Select(dp => dp.Name)
                    .OrderBy(s => s);

                foreach (var districtParkName in districtParkNames)
                {
                    txtItems.Add($"{districtParkName} (DISTRICT)");
                }
            }

            if (txtItems.Count == 1)
            {
                return $"<<WARNING: No supply chain shipments output!>>";
            }
            else
            {
                return string.Join("\n", txtItems.ToArray());
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the supply chain source buildings that the given building
        /// will receive shipments from.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplyBuildingSourcesText(InputType inputType, ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Supply Chain Shipments From>>");

            if (Constraints.InputAllLocalAreas(inputType, building))
            {
                txtItems.Add($"All local areas");
            }

            if (Constraints.InputOutsideConnections(inputType, building))
            {
                txtItems.Add($"All outside connections");
            }

            if (Constraints.InputAllLocalAreas(inputType, building))
            {
                return string.Join("\n", txtItems.ToArray());
            }

            // Next, list input building names
            var supply = Constraints.SupplySources(building);
            if (supply?.Count > 0)
            {
                var buildingNames = supply
                    .Select(b => $"{GetBuildingName(b)} ({b})")
                    .OrderBy(s => s);

                foreach (var buildingName in buildingNames)
                {
                    txtItems.Add(buildingName);
                }
            }

            // Then add district names
            var districts = Constraints.InputDistrictParkServiced(inputType, building);
            if (districts?.Count > 0)
            {
                var districtParkNames = districts
                    .Select(dp => dp.Name)
                    .OrderBy(s => s);

                foreach (var districtParkName in districtParkNames)
                {
                    txtItems.Add($"{districtParkName} (DISTRICT)");
                }
            }

            if (txtItems.Count == 1)
            {
                return $"<<WARNING: No supply chain shipments accepted!>>";
            }
            else
            {
                return string.Join("\n", txtItems.ToArray());
            }
        }

        /// <summary>
        /// Returns a descriptive text describing problems with this supply chain building ...
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplyBuildingProblemsText(ushort building)
        {
            if (building == 0 || !TransferManagerInfo.IsSupplyChainBuilding(building))
            {
                return string.Empty;
            }

            bool FindSourceBuilding(TransferManager.TransferReason material)
            {
                if (material == TransferManager.TransferReason.None)
                {
                    return true;
                }

                // Assume for now that the outside connection can supply the building with the materials it needs.
                if (Constraints.InputOutsideConnections(InputType.INCOMING, building) || Constraints.InputOutsideConnections(InputType.INCOMING2, building))
                {
                    return true;
                }

                for (ushort buildingIn = 1; buildingIn < BuildingManager.MAX_BUILDING_COUNT; buildingIn++)
                {
                    if (!TransferManagerInfo.IsSupplyChainBuilding(building))
                    {
                        continue;
                    }

                    if (GetSupplyBuildingOutputMaterial(buildingIn) == material)
                    {
                        // Check if a supply link exists ...
                        if (Constraints.SupplyDestinations(buildingIn)?.Count > 0 && Constraints.SupplyDestinations(buildingIn).Contains(building))
                        {
                            return true;
                        }

                        var requestDistrictPark = TransferManagerInfo.GetDistrictPark(building);
                        var responseDistrictPark = TransferManagerInfo.GetDistrictPark(buildingIn);

                        if (!Constraints.InputAllLocalAreas(InputType.INCOMING, building) && !Constraints.InputAllLocalAreas(InputType.INCOMING2, building))
                        {
                            var requestDistrictParksServed = Constraints.InputDistrictParkServiced(InputType.INCOMING, building);
                            var requestDistrictParksServed2 = Constraints.InputDistrictParkServiced(InputType.INCOMING2, building);
                            if (!responseDistrictPark.IsServedBy(requestDistrictParksServed) && !responseDistrictPark.IsServedBy(requestDistrictParksServed2))
                            {
                                continue;
                            }
                        }

                        if (Constraints.OutputAllLocalAreas(InputType.OUTGOING, buildingIn) || Constraints.OutputAllLocalAreas(InputType.OUTGOING2, buildingIn))
                        {
                            return true;
                        }
                        else
                        {
                            // The call to TransferManagerInfo.GetDistrict applies to offers that are come from buildings, service 
                            // vehicles, citizens, AND segments.  The latter needs to be considered for road maintenance.
                            var responseDistrictParksServed = Constraints.OutputDistrictParkServiced(InputType.OUTGOING, buildingIn);
                            var responseDistrictParksServed2 = Constraints.OutputDistrictParkServiced(InputType.OUTGOING2, buildingIn);
                            if (requestDistrictPark.IsServedBy(responseDistrictParksServed) || requestDistrictPark.IsServedBy(responseDistrictParksServed2))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            // Detect if we do have an other building that can supply materials to this building.
            List<TransferManager.TransferReason> notFound = new List<TransferManager.TransferReason>();
            switch (BuildingManager.instance.m_buildings.m_buffer[building].Info?.GetAI())
            {
                case ProcessingFacilityAI processingFacilityAI:
                    if (!FindSourceBuilding(processingFacilityAI.m_inputResource1))
                    {
                        notFound.Add(processingFacilityAI.m_inputResource1);
                    }

                    if (!FindSourceBuilding(processingFacilityAI.m_inputResource2))
                    {
                        notFound.Add(processingFacilityAI.m_inputResource2);
                    }

                    if (!FindSourceBuilding(processingFacilityAI.m_inputResource3))
                    {
                        notFound.Add(processingFacilityAI.m_inputResource3);
                    }

                    if (!FindSourceBuilding(processingFacilityAI.m_inputResource4))
                    {
                        notFound.Add(processingFacilityAI.m_inputResource4);
                    }

                    break;

                default:
                    break;
            }

            if (notFound.Count > 0)
            {
                return string.Join(",", notFound.Select(x => x.ToString()).ToArray());
            }
            else
            {
                return string.Empty;
            }
        }

        public static int GetSupplyBuildingAmount(int buildingId)
        {
            switch (BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.GetAI())
            {
                case ExtractingFacilityAI extractingFacilityAI:
                    int outputBufferSize1 = extractingFacilityAI.GetOutputBufferSize((ushort)buildingId, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId]);
                    return (int)(((double)Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_customBuffer1) * 100 / (outputBufferSize1));

                case ProcessingFacilityAI processingFacilityAI:
                    int outputBufferSize2 = processingFacilityAI.GetOutputBufferSize((ushort)buildingId, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId]);
                    return (int)(((double)Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_customBuffer1) * 100 / (outputBufferSize2));

                case WarehouseAI warehouseAI:
                    return (int)(((double)Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_customBuffer1) * 100 * 100 / warehouseAI.m_storageCapacity);
            }

            return 0;
        }

        public static TransferManager.TransferReason GetSupplyBuildingOutputMaterial(ushort buildingId)
        {
            var ai = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.GetAI();
            switch (ai)
            {
                case ExtractingFacilityAI extractingFacilityAI:
                    return extractingFacilityAI.m_outputResource;

                case FishFarmAI fishFarmAI:
                    return fishFarmAI.m_outputResource;

                case FishingHarborAI fishingHarborAI:
                    return fishingHarborAI.m_outputResource;

                case ProcessingFacilityAI processingFacilityAI:
                    return processingFacilityAI.m_outputResource;

                case WarehouseAI warehouseAI:
                    return warehouseAI.GetTransferReason(buildingId, ref BuildingManager.instance.m_buildings.m_buffer[buildingId]);

                default:
                    return TransferManager.TransferReason.None;
            }
        }

        /// <summary>
        /// Returns true if the building's vehicles are customizable.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsCustomVehiclesBuilding(int building)
        {
            if (building == 0)
            {
                return false;
            }

            var instance = Singleton<BuildingManager>.instance;

            if ((instance.m_buildings.m_buffer[building].m_flags & Building.Flags.Created) != Building.Flags.None)
            {
                var info = instance.m_buildings.m_buffer[building].Info;
                switch (info?.GetService())
                {
                    case ItemClass.Service.Beautification:
                        return info.GetAI() is MaintenanceDepotAI;

                    case ItemClass.Service.Disaster:
                    case ItemClass.Service.FireDepartment:
                    case ItemClass.Service.Garbage:
                    case ItemClass.Service.HealthCare:
                    case ItemClass.Service.PoliceDepartment:
                        return !(
                            info.GetAI() is ChildcareAI ||
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is EldercareAI ||
                            info.GetAI() is SaunaAI);

                    case ItemClass.Service.PlayerIndustry:
                        return !(
                            info.GetAI() is AuxiliaryBuildingAI ||
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is MainIndustryBuildingAI);

                    case ItemClass.Service.PublicTransport:
                        return (
                            (Settings.enableSelectOutsideConnection && info.GetAI() is OutsideConnectionAI) ||
                            (info.GetAI() is CargoStationAI) ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportCableCar ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportPlane ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportPost ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportTaxi);

                    case ItemClass.Service.Road:
                        return (
                            info.GetAI() is MaintenanceDepotAI ||
                            info.GetAI() is OutsideConnectionAI ||
                            info.GetAI() is SnowDumpAI);

                    case ItemClass.Service.Water:
                        return (
                            info.GetAI() is WaterFacilityAI waterFacilityAI &&
                            waterFacilityAI.m_pumpingVehicles > 0);

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the building's service is a supported district-only service.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsDistrictServicesBuilding(int building)
        {
            if (building == 0)
            {
                return false;
            }

            var instance = Singleton<BuildingManager>.instance;

            if ((instance.m_buildings.m_buffer[building].m_flags & Building.Flags.Created) != Building.Flags.None)
            {
                var info = instance.m_buildings.m_buffer[building].Info;
                switch (info?.GetService())
                {
                    case ItemClass.Service.Beautification:
                        return info.GetAI() is MaintenanceDepotAI;

                    case ItemClass.Service.Disaster:
                    case ItemClass.Service.Education:
                    case ItemClass.Service.FireDepartment:
                    case ItemClass.Service.Garbage:
                    case ItemClass.Service.HealthCare:
                    case ItemClass.Service.PoliceDepartment:
                        return !(
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is LibraryAI ||
                            info.GetAI() is SaunaAI);

                    case ItemClass.Service.Electricity:
                        return (
                            info.GetAI() is PowerPlantAI);

                    case ItemClass.Service.Fishing:
                        return (
                            info.GetAI() is FishFarmAI ||
                            info.GetAI() is FishingHarborAI ||
                            info.GetAI() is ProcessingFacilityAI ||
                            info.GetAI() is MarketAI);

                    case ItemClass.Service.Monument:
                        return (info?.gameObject?.name == "ChirpX Launch Control Center");

                    case ItemClass.Service.PlayerEducation:
                        return !(
                            info.GetSubService() == ItemClass.SubService.PlayerEducationLiberalArts ||
                            info.GetSubService() == ItemClass.SubService.PlayerEducationTradeSchool ||
                            info.GetSubService() == ItemClass.SubService.PlayerEducationUniversity);

                    case ItemClass.Service.PlayerIndustry:
                        return !(
                            info.GetAI() is AuxiliaryBuildingAI ||
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is MainIndustryBuildingAI);

                    case ItemClass.Service.PublicTransport:
                        return (
                            (Settings.enableSelectOutsideConnection && info.GetAI() is OutsideConnectionAI) ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportPost ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportTaxi);

                    case ItemClass.Service.Road:
                        return (
                            info.GetAI() is MaintenanceDepotAI ||
                            info.GetAI() is OutsideConnectionAI ||
                            info.GetAI() is SnowDumpAI);

                    case ItemClass.Service.Water:
                        return (
                            (info.GetAI() is WaterFacilityAI waterFacilityAI && waterFacilityAI.m_pumpingVehicles > 0) ||
                            (info.GetAI() is HeatingPlantAI));

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the building corresponds to an outside connection.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsOutsideBuilding(int building)
        {
            return building != 0 && Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].Info.m_buildingAI is OutsideConnectionAI;
        }

        public static bool IsOutsideRoadConnection(int building)
        {
            if (building != 0 && 
                Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].Info.m_buildingAI is OutsideConnectionAI outsideConnectionAI &&
                outsideConnectionAI.m_transportInfo?.m_netService == ItemClass.Service.Road)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the building's service is a supported supply chain service.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsSupplyChainBuilding(int building)
        {
            if (building == 0)
            {
                return false;
            }

            var instance = Singleton<BuildingManager>.instance;

            if ((instance.m_buildings.m_buffer[building].m_flags & Building.Flags.Created) != Building.Flags.None)
            {
                var my_building = instance.m_buildings.m_buffer[building];
                var info = instance.m_buildings.m_buffer[building].Info;
                switch (info?.GetService())
                {
                    case ItemClass.Service.Electricity:
                        return (
                            info.GetAI() is PowerPlantAI);

                    case ItemClass.Service.Fishing:
                        return (
                            info.GetAI() is FishFarmAI ||
                            info.GetAI() is FishingHarborAI ||
                            info.GetAI() is ProcessingFacilityAI ||
                            info.GetAI() is MarketAI);

                    case ItemClass.Service.Monument:
                        return (info?.gameObject?.name == "ChirpX Launch Control Center");

                    // recycling center
                    case ItemClass.Service.Garbage:
                        return (info.GetAI() is LandfillSiteAI landfillSiteAI && landfillSiteAI.m_info.name.Contains("Recycling Center"));
                    
                    // support for prison helicopter mod
                    case ItemClass.Service.PoliceDepartment:
                        return (info.GetAI().GetType().Name.Equals("NewPoliceStationAI") && (my_building.m_flags & Building.Flags.Downgrading) == 0);

                    case ItemClass.Service.PlayerIndustry:
                        return !(
                            info.GetAI() is AuxiliaryBuildingAI ||
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is MainIndustryBuildingAI ||
                            info.GetAI() is ProcessingFacilityAI);
                    
                    // post office support
                    case ItemClass.Service.PublicTransport:
                        return (
                            (Settings.enableSelectOutsideConnection && info.GetAI() is OutsideConnectionAI) ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportPost);

                    case ItemClass.Service.Road:
                        return (
                            info.GetAI() is OutsideConnectionAI);

                    case ItemClass.Service.Water:
                        return (
                            info.GetAI() is HeatingPlantAI);

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if there is a valid supply chain link between the source and the destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static bool IsValidSupplyChainLink(ushort source, ushort destination)
        {
            var sourceMaterial = GetSupplyBuildingOutputMaterial(source);
            if (sourceMaterial == TransferManager.TransferReason.None)
            {
                return false;
            }

            var my_building = BuildingManager.instance.m_buildings.m_buffer[destination];
            var info = BuildingManager.instance.m_buildings.m_buffer[destination].Info;
            if (info?.GetService() == ItemClass.Service.Electricity && info?.GetAI() is PowerPlantAI)
            {
                return 
                    sourceMaterial == TransferManager.TransferReason.Coal ||
                    sourceMaterial == TransferManager.TransferReason.Petrol;
            }

            if (info?.GetService() == ItemClass.Service.Monument && info?.gameObject?.name == "ChirpX Launch Control Center")
            {
                return
                    sourceMaterial == TransferManager.TransferReason.Coal ||
                    sourceMaterial == TransferManager.TransferReason.Petrol;
            }

            if ((info?.GetService() == ItemClass.Service.PlayerIndustry || info?.GetService() == ItemClass.Service.Fishing) && info?.GetAI() is ProcessingFacilityAI processingFacilityAI)
            {
                return
                    processingFacilityAI.m_inputResource1 == sourceMaterial ||
                    processingFacilityAI.m_inputResource2 == sourceMaterial ||
                    processingFacilityAI.m_inputResource3 == sourceMaterial ||
                    processingFacilityAI.m_inputResource4 == sourceMaterial;
            }

            if(info?.GetService() == ItemClass.Service.PoliceDepartment && (info.GetAI().GetType().Name.Equals("NewPoliceStationAI") && (my_building.m_flags & Building.Flags.Downgrading) == 0))
            {
                return sourceMaterial == TransferManager.TransferReason.CriminalMove;
            }

            if(info?.GetService() == ItemClass.Service.Garbage && info.GetAI() is LandfillSiteAI landfillSiteAI && landfillSiteAI.m_info.name.Contains("Recycling Center"))
            {
                return  
                    sourceMaterial == TransferManager.TransferReason.Coal ||
                    sourceMaterial == TransferManager.TransferReason.Lumber ||
                    sourceMaterial == TransferManager.TransferReason.Petrol;
            }

            if (info?.GetService() == ItemClass.Service.PlayerIndustry && info?.GetAI() is WarehouseAI)
            {
                return GetSupplyBuildingOutputMaterial(destination) == sourceMaterial;
            }

            if (info?.GetService() == ItemClass.Service.PublicTransport && info?.GetSubService() == ItemClass.SubService.PublicTransportPost && info?.GetAI() is PostOfficeAI)
            {
                return 
                    sourceMaterial == TransferManager.TransferReason.SortedMail ||
                    sourceMaterial == TransferManager.TransferReason.UnsortedMail ||
                    sourceMaterial == TransferManager.TransferReason.IncomingMail ||
                    sourceMaterial == TransferManager.TransferReason.OutgoingMail;
            }

            if (info?.GetService() == ItemClass.Service.Water && info?.GetAI() is HeatingPlantAI)
            {
                return sourceMaterial == TransferManager.TransferReason.Petrol;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the offer concerns a city service that should be restricted within a district.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool IsDistrictOffer(TransferManager.TransferReason material)
        {
            return
                material == TransferManager.TransferReason.Garbage ||
                material == TransferManager.TransferReason.Crime || // police
                material == TransferManager.TransferReason.CriminalMove || // prison vans - if PH is enabled prison helicopters from police helicopter depot
                material == (TransferManager.TransferReason)126 || // prison vans from prison (PH mod)
                material == (TransferManager.TransferReason)125 || // prison vans from police station (PH mod)

                material == TransferManager.TransferReason.Sick ||
                material == TransferManager.TransferReason.Dead ||
                material == TransferManager.TransferReason.Fire ||
                material == TransferManager.TransferReason.Mail ||
                material == TransferManager.TransferReason.GarbageTransfer ||

                material == TransferManager.TransferReason.ParkMaintenance ||
                material == TransferManager.TransferReason.RoadMaintenance ||
                material == TransferManager.TransferReason.Snow ||

                material == TransferManager.TransferReason.ForestFire ||
                material == TransferManager.TransferReason.Collapsed ||
                material == TransferManager.TransferReason.Collapsed2 ||
                material == TransferManager.TransferReason.Fire2 ||
                material == TransferManager.TransferReason.Sick2 ||
                material == TransferManager.TransferReason.FloodWater ||
                material == TransferManager.TransferReason.EvacuateA ||
                material == TransferManager.TransferReason.EvacuateB ||
                material == TransferManager.TransferReason.EvacuateC ||
                material == TransferManager.TransferReason.EvacuateD ||
                material == TransferManager.TransferReason.EvacuateVipA ||
                material == TransferManager.TransferReason.EvacuateVipB ||
                material == TransferManager.TransferReason.EvacuateVipC ||
                material == TransferManager.TransferReason.EvacuateVipD ||

                material == TransferManager.TransferReason.ChildCare ||
                material == TransferManager.TransferReason.ElderCare ||
                material == TransferManager.TransferReason.Student1 ||
                material == TransferManager.TransferReason.Student2 ||
                material == TransferManager.TransferReason.Taxi;
        }

        /// <summary>
        /// Returns true if the offer concerns a supported supply chain material.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool IsSupplyChainOffer(TransferManager.TransferReason material)
        {
            return
                material == TransferManager.TransferReason.Coal ||
                material == TransferManager.TransferReason.Food ||
                material == TransferManager.TransferReason.Petrol ||
                material == TransferManager.TransferReason.Lumber ||

                material == TransferManager.TransferReason.Logs ||
                material == TransferManager.TransferReason.Paper ||
                material == TransferManager.TransferReason.PlanedTimber ||

                material == TransferManager.TransferReason.Grain ||
                material == TransferManager.TransferReason.Flours ||
                material == TransferManager.TransferReason.AnimalProducts ||

                material == TransferManager.TransferReason.Oil ||
                material == TransferManager.TransferReason.Petroleum ||
                material == TransferManager.TransferReason.Plastics ||

                material == TransferManager.TransferReason.Ore ||
                material == TransferManager.TransferReason.Glass ||
                material == TransferManager.TransferReason.Metals ||

                material == TransferManager.TransferReason.Fish ||
                material == TransferManager.TransferReason.Goods ||
                material == TransferManager.TransferReason.LuxuryProducts ||
                material == TransferManager.TransferReason.CriminalMove ||
                material == (TransferManager.TransferReason)125 || // prison helicopter mod
                material == (TransferManager.TransferReason)126 || // guest prison vans from prison
                
                material == TransferManager.TransferReason.SortedMail ||
                material == TransferManager.TransferReason.UnsortedMail ||
                material == TransferManager.TransferReason.IncomingMail ||
                material == TransferManager.TransferReason.OutgoingMail;
        }

        /// <summary>
        /// Returns true if the offer was given from an outside connection.
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public static bool IsOutsideOffer(ref TransferManager.TransferOffer offer)
        {
            return IsOutsideBuilding(GetHomeBuilding(ref offer));
        }

        /// <summary>
        /// Returns true if the building has two inputs.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsTwoInputBuilding(int building)
        {
            var my_building = BuildingManager.instance.m_buildings.m_buffer[building];
            var info = my_building.Info;
            return false;
        }

        /// <summary>
        /// Returns true if the building has two outputs.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsTwoOutputBuilding(int building)
        {
            var my_building = BuildingManager.instance.m_buildings.m_buffer[building];
            var info = BuildingManager.instance.m_buildings.m_buffer[building].Info;
            if (info?.GetService() == ItemClass.Service.PublicTransport && info?.GetSubService() == ItemClass.SubService.PublicTransportPost && info?.GetAI() is PostOfficeAI || 
                info?.GetService() == ItemClass.Service.PoliceDepartment && (
                info.GetAI().GetType().Name.Equals("NewPoliceStationAI") && info.m_class.m_level < ItemClass.Level.Level4 || info.GetAI() is HelicopterDepotAI) && (my_building.m_flags & Building.Flags.Downgrading) == 0 ||
                info?.GetAI() is LandfillSiteAI landfillSiteAI && landfillSiteAI.m_info.name.Contains("Recycling Center"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
