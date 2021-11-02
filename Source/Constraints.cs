using ColossalFramework;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Contains all the district and supply chain constraint data that used by the TransferManager (patch) to determine
    /// how best to match incoming and outgoing offers.
    /// </summary>
    public static class Constraints
    {
        
        /// <summary>
        /// Map of building id to bool indicating whether all local areas are serviced by the building.
        /// </summary>
        private static readonly bool[] m_inputBuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to bool indicating whether all local areas are serviced by the building.
        /// </summary>
        private static readonly bool[] m_inputBuildingToAllLocalAreas2 = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to bool indicating whether outside connections are serviced by the building.
        /// </summary>
        private static readonly bool[] m_inputBuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to bool indicating whether outside connections are serviced by the building.
        /// </summary>
        private static readonly bool[] m_inputBuildingToOutsideConnections2 = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to list of districts or parks served by the building.
        /// </summary>
        private static readonly List<DistrictPark>[] m_inputBuildingToDistrictParkServiced = new List<DistrictPark>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to list of districts or parks served by the building.
        /// </summary>
        private static readonly List<DistrictPark>[] m_inputBuildingToDistrictParkServiced2 = new List<DistrictPark>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to bool indicating whether all local areas are serviced by the building.
        /// </summary>
        private static readonly bool[] m_outputBuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];

         /// <summary>
        /// Map of building id to bool indicating whether all local areas are serviced by the building.
        /// </summary>
        private static readonly bool[] m_outputBuildingToAllLocalAreas2 = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to bool indicating whether outside connections are serviced by the building.
        /// </summary>
        private static readonly bool[] m_outputBuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to bool indicating whether outside connections are serviced by the building.
        /// </summary>
        private static readonly bool[] m_outputBuildingToOutsideConnections2 = new bool[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to list of districts or parks served by the building.
        /// </summary>
        private static readonly List<DistrictPark>[] m_outputBuildingToDistrictParkServiced = new List<DistrictPark>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to list of districts or parks served by the building.
        /// </summary>
        private static readonly List<DistrictPark>[] m_outputBuildingToDistrictParkServiced2 = new List<DistrictPark>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to the specified supply goods buffer, below which all good sent from this building will
        /// be to specified districts or supply out buildings only.  Default value is 100, meaning all goods are sent
        /// to destinations that satisfy district and supply out restrictions.
        /// </summary>
        private static readonly int[] m_buildingToInternalSupplyBuffer = new int[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Map of building id to the list of allowed destination building ids.  For supply chains only.
        /// If specified, this overrides all other constraints.
        /// </summary>
        private static readonly List<int>[] m_supplyDestinations = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        /// <summary>
        /// Betweeen 0 and 1000 inclusive, this controls how much incoming/outgoing traffic comes into the game.
        /// </summary>
        private static int m_globalOutsideConnectionIntensity = 600;

        /// <summary>
        /// Betweeen 0 and 100 inclusive, this controls the max percentage of traffic that can be outside-to-outside traffic.
        /// </summary>
        private static int m_globalOutsideToOutsideMaxPerc = 50;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Constraints()
        {
            Clear();
        }

        /// <summary>
        /// Reset all data structures.
        /// </summary>
        public static void Clear()
        {
            for (ushort buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                ReleaseBuilding(buildingId);
            }
        }

        /// <summary>
        /// Load data from given object.
        /// </summary>
        /// <param name="data"></param>
        public static void LoadData(Serialization.Datav4 data)
        {
            Logger.Log($"Constraints::LoadData: version {data.Id}");
            Clear();

            var buildings = Utils.GetSupportedServiceBuildings();
            foreach (var building in buildings)
            {
                var name = TransferManagerInfo.GetBuildingName(building);
                var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[building].Info;
                var service = buildingInfo.GetService();
                var subService = buildingInfo.GetSubService();
                var ai = buildingInfo.GetAI();

                Logger.Log($"Constraints::LoadData: buildingName={name}, buildingId={building}, service={service}, subService={subService}, ai={ai}");

                var restrictions1a = data.InputBuildingToAllLocalAreas[building];
                SetAllInputLocalAreas(InputType.INCOMING, building, restrictions1a);

                var restrictions1b = data.InputBuildingToAllLocalAreas2[building];
                SetAllInputLocalAreas(InputType.INCOMING2, building, restrictions1b);

                var restrictions2a = data.InputBuildingToOutsideConnections[building];
                SetAllInputOutsideConnections(InputType.OUTGOING, building, restrictions2a);

                var restrictions2b = data.InputBuildingToOutsideConnections2[building];
                SetAllInputOutsideConnections(InputType.OUTGOING2, building, restrictions2b);

                var restrictions3a = data.InputBuildingToDistrictServiced[building];
                if (restrictions3a != null)
                {
                    foreach (var districtPark in restrictions3a)
                    {
                        AddInputDistrictParkServiced(InputType.INCOMING, building, DistrictPark.FromSerializedInt(districtPark));
                    }
                }

                var restrictions3b = data.InputBuildingToDistrictServiced2[building];
                if (restrictions3b != null)
                {
                    foreach (var districtPark in restrictions3b)
                    {
                        AddInputDistrictParkServiced(InputType.INCOMING2, building, DistrictPark.FromSerializedInt(districtPark));
                    }
                }

                var restrictions4a = data.OutputBuildingToAllLocalAreas[building];
                SetAllOutputLocalAreas(InputType.OUTGOING, building, restrictions4a);

                var restrictions4b = data.OutputBuildingToAllLocalAreas2[building];
                SetAllOutputLocalAreas(InputType.OUTGOING2, building, restrictions4b);

                var restrictions5a = data.OutputBuildingToOutsideConnections[building];
                SetAllOutputOutsideConnections(InputType.OUTGOING, building, restrictions5a);

                var restrictions5b = data.OutputBuildingToOutsideConnections2[building];
                SetAllOutputOutsideConnections(InputType.OUTGOING2, building, restrictions5b);

                var restrictions6a = data.OutputBuildingToDistrictServiced[building];
                if (restrictions6a != null)
                {
                    foreach (var districtPark in restrictions6a)
                    {
                        AddOutputDistrictParkServiced(InputType.OUTGOING, building, DistrictPark.FromSerializedInt(districtPark));
                    }
                }

                var restrictions6b = data.OutputBuildingToDistrictServiced2[building];
                if (restrictions6b != null)
                {
                    foreach (var districtPark in restrictions6b)
                    {
                        AddOutputDistrictParkServiced(InputType.OUTGOING2, building, DistrictPark.FromSerializedInt(districtPark));
                    }
                }
                
                if (data.BuildingToInternalSupplyBuffer != null)
                {
                    var restrictions7 = data.BuildingToInternalSupplyBuffer[building];
                    SetInternalSupplyReserve(building, restrictions7);
                }

                if (data.BuildingToBuildingServiced != null)
                {
                    var restrictions8 = data.BuildingToBuildingServiced[building];
                    if (restrictions8 != null)
                    {
                        foreach (var destination in restrictions8)
                        {
                            AddSupplyChainConnection(building, (ushort)destination);
                        }
                    }
                }

                m_globalOutsideConnectionIntensity = data.GlobalOutsideConnectionIntensity;
                m_globalOutsideToOutsideMaxPerc = data.GlobalOutsideToOutsideMaxPerc;

                Logger.Log("");
            }
        }

        /// <summary>
        /// Saves a copy of the data in this object, for serialization.
        /// </summary>
        /// <returns></returns>
        public static Serialization.Datav4 SaveData()
        {
            return new Serialization.Datav4
            {
                InputBuildingToAllLocalAreas = m_inputBuildingToAllLocalAreas.ToArray(),
                InputBuildingToOutsideConnections = m_inputBuildingToOutsideConnections.ToArray(),
                InputBuildingToDistrictServiced = m_inputBuildingToDistrictParkServiced
                    .Select(list => list?.Select(districtPark => districtPark.ToSerializedInt()).ToList())
                    .ToArray(),

                InputBuildingToAllLocalAreas2 = m_inputBuildingToAllLocalAreas2.ToArray(),
                InputBuildingToOutsideConnections2 = m_inputBuildingToOutsideConnections2.ToArray(),
                InputBuildingToDistrictServiced2 = m_inputBuildingToDistrictParkServiced2
                    .Select(list => list?.Select(districtPark => districtPark.ToSerializedInt()).ToList())
                    .ToArray(),

                OutputBuildingToAllLocalAreas = m_outputBuildingToAllLocalAreas.ToArray(),
                OutputBuildingToOutsideConnections = m_outputBuildingToOutsideConnections.ToArray(),
                OutputBuildingToDistrictServiced = m_outputBuildingToDistrictParkServiced
                    .Select(list => list?.Select(districtPark => districtPark.ToSerializedInt()).ToList())
                    .ToArray(),

                OutputBuildingToAllLocalAreas2 = m_outputBuildingToAllLocalAreas2.ToArray(),
                OutputBuildingToOutsideConnections2 = m_outputBuildingToOutsideConnections2.ToArray(),
                OutputBuildingToDistrictServiced2 = m_outputBuildingToDistrictParkServiced2
                    .Select(list => list?.Select(districtPark => districtPark.ToSerializedInt()).ToList())
                    .ToArray(),

                BuildingToInternalSupplyBuffer = m_buildingToInternalSupplyBuffer.ToArray(),
                BuildingToBuildingServiced = m_supplyDestinations.ToArray(),
                GlobalOutsideConnectionIntensity = m_globalOutsideConnectionIntensity,
                GlobalOutsideToOutsideMaxPerc = m_globalOutsideToOutsideMaxPerc
            };
        }

        /// <summary>
        /// Called when a building is first created.  If situated in a district or park, then automatically restricts that
        /// building to serve its home district only.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void CreateBuilding(ushort buildingId)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            var service = buildingInfo.GetService();
            var subService = buildingInfo.GetSubService();
            var ai = buildingInfo.GetAI();

            // Do not pack the homeDistrict and homePark into a single DistrictPark struct.  Otherwise, it will make 
            // removing districts/parks a lot harder!!
            var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            var homeDistrict = DistrictManager.instance.GetDistrict(position);
            var homePark = DistrictManager.instance.GetPark(position);

            Logger.Log($"Constraints::CreateBuilding: buildingId={buildingId}, homeDistrict={homeDistrict}, homePark={homePark}, service={service}, subService={subService}, ai={ai}");

            // Set default input settings.
            SetAllInputLocalAreas(InputType.INCOMING, buildingId, true);
            m_inputBuildingToDistrictParkServiced[buildingId] = null;
            if(TransferManagerInfo.IsTwoInputBuilding(buildingId))
            {
                SetAllInputLocalAreas(InputType.INCOMING2, buildingId, true);
                SetAllInputOutsideConnections(InputType.INCOMING2, buildingId, true);
                m_inputBuildingToDistrictParkServiced2[buildingId] = null;
            }
            else
            {
                SetAllInputOutsideConnections(InputType.INCOMING, buildingId, true);
            }


            // Serve all areas if the building doesn't belong to any district or park.
            SetAllOutputLocalAreas(InputType.OUTGOING, buildingId, homeDistrict == 0 && homePark == 0);
            m_outputBuildingToDistrictParkServiced[buildingId] = null;
            if(TransferManagerInfo.IsTwoOutputBuilding(buildingId))
            {
                SetAllOutputLocalAreas(InputType.OUTGOING2, buildingId, homeDistrict == 0 && homePark == 0);
                SetAllOutputOutsideConnections(InputType.OUTGOING2, buildingId, homeDistrict == 0 && homePark == 0);
                m_outputBuildingToDistrictParkServiced2[buildingId] = null;
            }
            else
            {
                SetAllOutputOutsideConnections(InputType.OUTGOING, buildingId, homeDistrict == 0 && homePark == 0);
            }
            
            if (homeDistrict != 0)
            {
                AddOutputDistrictParkServiced(InputType.OUTGOING, buildingId, DistrictPark.FromDistrict(homeDistrict));
                if(TransferManagerInfo.IsTwoOutputBuilding(buildingId))
                {
                    AddOutputDistrictParkServiced(InputType.OUTGOING2, buildingId, DistrictPark.FromDistrict(homeDistrict));
                }
                
            }

            if (homePark != 0)
            {
                AddOutputDistrictParkServiced(InputType.OUTGOING, buildingId, DistrictPark.FromPark(homePark));
                if(TransferManagerInfo.IsTwoOutputBuilding(buildingId))
                {
                    AddOutputDistrictParkServiced(InputType.OUTGOING2, buildingId, DistrictPark.FromPark(homePark));
                }
            }
        }

        /// <summary>
        /// Called when a building is destroyed.
        /// </summary>
        /// <param name="buildingId"></param>
        public static void ReleaseBuilding(ushort buildingId)
        {
            m_inputBuildingToAllLocalAreas[buildingId] = true;
            m_inputBuildingToAllLocalAreas2[buildingId] = true;
            m_inputBuildingToOutsideConnections[buildingId] = true;
            m_inputBuildingToOutsideConnections2[buildingId] = true;
            m_inputBuildingToDistrictParkServiced[buildingId] = null;
            m_inputBuildingToDistrictParkServiced2[buildingId] = null;

            m_outputBuildingToAllLocalAreas[buildingId] = true;
            m_outputBuildingToAllLocalAreas2[buildingId] = true;
            m_outputBuildingToOutsideConnections[buildingId] = true;
            m_outputBuildingToOutsideConnections2[buildingId] = true;
            m_outputBuildingToDistrictParkServiced[buildingId] = null;
            m_outputBuildingToDistrictParkServiced2[buildingId] = null;

            m_buildingToInternalSupplyBuffer[buildingId] = 100;

            RemoveAllSupplyChainConnectionsToDestination(buildingId);
            RemoveAllSupplyChainConnectionsFromSource(buildingId);
        }

        /// <summary>
        /// Called when a district or park is removed.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="districtPark"></param>
        public static void ReleaseDistrictPark(DistrictPark districtPark)
        {
            Logger.Log($"Constraints::ReleaseDistrictPark: {districtPark.Name}");

            for (int buildingId = 0; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId++)
            {
                RemoveInputDistrictParkServiced(InputType.INCOMING, buildingId, districtPark);
                RemoveInputDistrictParkServiced(InputType.INCOMING2, buildingId, districtPark);
                RemoveOutputDistrictParkServiced(InputType.OUTGOING, buildingId, districtPark);
                RemoveOutputDistrictParkServiced(InputType.OUTGOING2, buildingId, districtPark);
            }
        }

        #region Accessors

        /// <summary>
        /// Returns true if all local areas are serviced by the building.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool InputAllLocalAreas(InputType inputType, ushort buildingId)
        {
            if(inputType == InputType.INCOMING2)
            {
                return m_inputBuildingToAllLocalAreas2[buildingId];
            }
            else
            {
                return m_inputBuildingToAllLocalAreas[buildingId];
            }
           
        }

        /// <summary>
        /// Returns true if outside connections are allowed by the building.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool InputOutsideConnections(InputType inputType, ushort buildingId)
        {
            if(inputType == InputType.INCOMING2)
            {
                return m_inputBuildingToOutsideConnections2[buildingId];
            }
            else
            {
                return m_inputBuildingToOutsideConnections[buildingId];
            }
            
        }

        /// <summary>
        /// Returns the list of districts or parks served by the building.
        /// TODO: Replace with IReadOnlyList.  Can't do it with older version of .NET.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static List<DistrictPark> InputDistrictParkServiced(InputType inputType, ushort buildingId)
        {
            if(inputType == InputType.INCOMING2)
            {
                return m_inputBuildingToDistrictParkServiced2[buildingId];
            }
            else
            {
                return m_inputBuildingToDistrictParkServiced[buildingId];
            }
            
        }

        /// <summary>
        /// Returns true if all local areas are serviced by the building.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool OutputAllLocalAreas(InputType inputType, ushort buildingId)
        {
            if(inputType == InputType.OUTGOING2)
            {
                return m_outputBuildingToAllLocalAreas2[buildingId];
            }
            else
            {
                return m_outputBuildingToAllLocalAreas[buildingId];
            }
            
        }

        /// <summary>
        /// Returns true if outside connections are allowed by the building.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool OutputOutsideConnections(InputType inputType, ushort buildingId)
        {
            if(inputType == InputType.OUTGOING2)
            {
                return m_outputBuildingToOutsideConnections2[buildingId];
            }
            else
            {
                return m_outputBuildingToOutsideConnections[buildingId];
            }
            
        }

        /// <summary>
        /// Returns the list of districts or parks served by the building.
        /// TODO: Replace with IReadOnlyList.  Can't do it with older version of .NET.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static List<DistrictPark> OutputDistrictParkServiced(InputType inputType, ushort buildingId)
        {
            if(inputType == InputType.OUTGOING2)
            {
                return m_outputBuildingToDistrictParkServiced2[buildingId];
            } 
            else
            {
                return m_outputBuildingToDistrictParkServiced[buildingId];
            }
            
        }

        /// <summary>
        /// Returns the internal supply buffer on the building.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static int InternalSupplyBuffer(ushort buildingId)
        {
            return m_buildingToInternalSupplyBuffer[buildingId];
        }

        /// <summary>
        /// Returns the list of allowed source building ids.  For supply chains only.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static List<int> SupplySources(ushort buildingId)
        {
            var supplySources = new List<int>();
            for (int b = 0; b < m_supplyDestinations.Length; b++)
            {
                if (m_supplyDestinations[b]?.Count > 0 && m_supplyDestinations[b].Contains(buildingId))
                {
                    supplySources.Add(b);
                }
            }

            return supplySources;
        }

        /// <summary>
        /// Returns the list of allowed destination building ids.  For supply chains only.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static List<int> SupplyDestinations(ushort buildingId)
        {
            return m_supplyDestinations[buildingId];
        }

        /// <summary>
        /// Between 0 and 1000 inclusive, controls the intensity of traffic gonig to outside connections.
        /// </summary>
        /// <returns></returns>
        public static int GlobalOutsideConnectionIntensity()
        {
            return m_globalOutsideConnectionIntensity;
        }


        /// <summary>
        /// Between 0 and 100 inclusive, controls the max percentage of traffic that can be outside-to-outside traffic.
        /// </summary>
        /// <returns></returns>
        public static int GlobalOutsideToOutsidePerc()
        {
            return m_globalOutsideToOutsideMaxPerc;
        }

        #endregion

        #region Local Areas and Outside Connections methods

        public static void SetAllInputLocalAreas(InputType inputType, int buildingId, bool status)
        {
            var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
            Logger.LogVerbose($"Constraints::SetAllInputLocalAreas: {buildingName} ({buildingId}) => {status} ...");

            if(inputType == InputType.INCOMING && TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                SetArrayStatus(m_inputBuildingToAllLocalAreas, buildingId, status);
            }
            else if(inputType == InputType.INCOMING2 && TransferManagerInfo.IsSupplyChainBuilding(buildingId))
            {
                SetArrayStatus(m_inputBuildingToAllLocalAreas2, buildingId, status);
            }
        }

        public static void SetAllInputOutsideConnections(InputType inputType, int buildingId, bool status)
        {
            var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
            Logger.LogVerbose($"Constraints::SetAllInputOutsideConnections: {buildingName} ({buildingId}) => {status} ...");

            if(inputType == InputType.INCOMING && TransferManagerInfo.IsSupplyChainBuilding(buildingId))
            {
                SetArrayStatus(m_inputBuildingToOutsideConnections, buildingId, status);
            }
            else if(inputType == InputType.INCOMING2 && TransferManagerInfo.IsSupplyChainBuilding(buildingId))
            {
                SetArrayStatus(m_inputBuildingToOutsideConnections2, buildingId, status);
            }
        }

        public static void SetAllOutputLocalAreas(InputType inputType, int buildingId, bool status)
        {
            var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
            Logger.LogVerbose($"Constraints::SetAllOutputLocalAreas: {buildingName} ({buildingId}) => {status} ...");

            if(inputType == InputType.OUTGOING && TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                SetArrayStatus(m_outputBuildingToAllLocalAreas, buildingId, status);
            }
            else if(inputType == InputType.OUTGOING2 && TransferManagerInfo.IsSupplyChainBuilding(buildingId))
            {
                SetArrayStatus(m_outputBuildingToAllLocalAreas2, buildingId, status);
            }
        }

        public static void SetAllOutputOutsideConnections(InputType inputType, int buildingId, bool status)
        {
            var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
            Logger.LogVerbose($"Constraints::SetAllOutputOutsideConnections: {buildingName} ({buildingId}) => {status} ...");

            if(inputType == InputType.OUTGOING && TransferManagerInfo.IsSupplyChainBuilding(buildingId))
            {
                SetArrayStatus(m_outputBuildingToOutsideConnections, buildingId, status);
            }
            else if(inputType == InputType.OUTGOING2 && TransferManagerInfo.IsSupplyChainBuilding(buildingId))
            {
                SetArrayStatus(m_outputBuildingToOutsideConnections2, buildingId, status);
            }
        }

        private static void SetArrayStatus(bool[] array, int buildingId, bool status)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                return;
            }

            array[buildingId] = status;
        }

        /// <summary>
        /// Sets the internal supply reserve.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <param name="amount">Must bet between 0 and 100.</param>
        public static void SetInternalSupplyReserve(int buildingId, int amount)
        {
            var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
            Logger.LogVerbose($"Constraints::SetInternalSupplyReserve: {buildingName} ({buildingId}) => {amount} ...");

            m_buildingToInternalSupplyBuffer[buildingId] = COMath.Clamp(amount, 0, 100);
        }

        /// <summary>
        /// Set the global outside connection intensity.
        /// </summary>
        /// <param name="amount"></param>
        public static void SetGlobalOutsideConnectionIntensity(int amount)
        {
            Logger.LogVerbose($"Constraints::SetGlobalOutsideConnectionIntensity: {amount} ...");
            m_globalOutsideConnectionIntensity = COMath.Clamp(amount, 0, 1000);
        }

        /// <summary>
        /// Set the max percentage of traffic that can be outside-to-outside traffic.
        /// </summary>
        /// <param name="amount"></param>
        public static void SetGlobalOutsideToOutsideMaxPerc(int amount)
        {
            Logger.LogVerbose($"Constraints::SetGlobalOutsideToOutsideMaxPerc: {amount} ...");
            m_globalOutsideToOutsideMaxPerc = COMath.Clamp(amount, 0, 100);
        }

        #endregion

        #region District Services methods

        /// <summary>
        /// Allow the specified district or park to be serviced by the specified building
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <param name="districtPark"></param>
        public static void AddInputDistrictParkServiced(InputType inputType, int buildingId, DistrictPark districtPark)
        {
            if (inputType == InputType.INCOMING && TransferManagerInfo.IsDistrictServicesBuilding(buildingId) && AddDistrictParkServiced(m_inputBuildingToDistrictParkServiced, buildingId, districtPark))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"Constraints::AddInputDistrictParkServiced: {districtPark.Name} => {buildingName} ({buildingId}) ...");
            }
            else if (inputType == InputType.INCOMING2 && TransferManagerInfo.IsSupplyChainBuilding(buildingId) && AddDistrictParkServiced(m_inputBuildingToDistrictParkServiced2, buildingId, districtPark))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"Constraints::AddInputDistrictParkServiced: {districtPark.Name} => {buildingName} ({buildingId}) ...");
            }
        }

        /// <summary>
        /// Allow the specified district or park to be serviced by the specified building
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <param name="districtPark"></param>
        public static void AddOutputDistrictParkServiced(InputType inputType, int buildingId, DistrictPark districtPark)
        {
            if (inputType == InputType.OUTGOING && TransferManagerInfo.IsDistrictServicesBuilding(buildingId) && AddDistrictParkServiced(m_outputBuildingToDistrictParkServiced, buildingId, districtPark))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"Constraints::AddOutputDistrictParkServiced: {buildingName} ({buildingId}) => {districtPark.Name} ...");
            }
            else if (inputType == InputType.OUTGOING2 && TransferManagerInfo.IsSupplyChainBuilding(buildingId) && AddDistrictParkServiced(m_outputBuildingToDistrictParkServiced2, buildingId, districtPark))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.Log($"Constraints::AddOutputDistrictParkServiced: {buildingName} ({buildingId}) => {districtPark.Name} ...");
            }
        }

        private static bool AddDistrictParkServiced(List<DistrictPark>[] array, int buildingId, DistrictPark districtPark)
        {
            if (!TransferManagerInfo.IsDistrictServicesBuilding(buildingId))
            {
                var buildingName = TransferManagerInfo.GetBuildingName(buildingId);
                Logger.LogWarning($"Constraints::AddDistrictParkServiced: Ignoring {districtPark.Name} restriction because {buildingName} ({buildingId}) is not a district services building.");
                return false;
            }

            if (!districtPark.Exists)
            {
                Logger.LogWarning($"Constraints::AddDistrictParkServiced: Ignoring {districtPark.Name} restriction because this district/park does not exist.");
                return false;
            }

            if (array[buildingId] == null)
            {
                array[buildingId] = new List<DistrictPark>();
            }

            if (!array[buildingId].Contains(districtPark))
            {
                array[buildingId].Add(districtPark);
            }

            return true;
        }

        /// <summary>
        /// Disallow the specified district or park from being serviced by the specified building
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <param name="districtPark"></param>
        public static void RemoveInputDistrictParkServiced(InputType inputType, int buildingId, DistrictPark districtPark)
        {
            if(inputType == InputType.INCOMING)
            {
                RemoveDistrictParkServiced(m_inputBuildingToDistrictParkServiced, buildingId, districtPark);
            }
            else if(inputType == InputType.INCOMING2)
            {
                RemoveDistrictParkServiced(m_inputBuildingToDistrictParkServiced2, buildingId, districtPark);
            }
        }

        /// <summary>
        /// Disallow the specified district or park from being serviced by the specified building
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="buildingId"></param>
        /// <param name="districtPark"></param>
        public static void RemoveOutputDistrictParkServiced(InputType inputType, int buildingId, DistrictPark districtPark)
        {
            if(inputType == InputType.OUTGOING)
            {
                RemoveDistrictParkServiced(m_outputBuildingToDistrictParkServiced, buildingId, districtPark);
            }
            else if(inputType == InputType.OUTGOING2)
            {
                RemoveDistrictParkServiced(m_outputBuildingToDistrictParkServiced2, buildingId, districtPark);
            }
        }

        private static void RemoveDistrictParkServiced(List<DistrictPark>[] array, int buildingId, DistrictPark districtPark)
        {
            if (array[buildingId] == null)
            {
                return;
            }

            for (int i = 0; i < array[buildingId].Count;)
            {
                if (array[buildingId][i].IsServedBy(districtPark))
                {
                    array[buildingId].RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            if (array[buildingId].Count == 0)
            {
                array[buildingId] = null;
            }
        }

        #endregion

        #region Supply Chain methods

        /// <summary>
        /// Add a supply chain link between the source and destination buildings.
        /// The supply chain link overrides all local area, all outside connections, and all district constraints.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void AddSupplyChainConnection(ushort source, ushort destination)
        {
            if (!TransferManagerInfo.IsSupplyChainBuilding(source) || !TransferManagerInfo.IsSupplyChainBuilding(destination))
            {
                return;
            }

            if (!TransferManagerInfo.IsValidSupplyChainLink(source, destination))
            {
                Logger.Log($"Constraints::AddSupplyChainConnection: Could not add invalid supply chain link: source={source}, destination={destination}");
                return;
            }

            bool added = false;

            if (m_supplyDestinations[source] == null)
            {
                m_supplyDestinations[source] = new List<int>();
            }

            if (!m_supplyDestinations[source].Contains(destination))
            {
                added = true;
                m_supplyDestinations[source].Add(destination);
            }

            if (added)
            {
                var sourceBuildingName = TransferManagerInfo.GetBuildingName(source);
                var destinationBuildingName = TransferManagerInfo.GetBuildingName(destination);
                Logger.Log($"Constraints::AddSupplyChainConnection: {sourceBuildingName} ({source}) => {destinationBuildingName} ({destination}) ...");
            }
        }

        /// <summary>
        /// Remove the supply chain link between the source and destination buildings, if it exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private static void RemoveSupplyChainConnection(int source, int destination)
        {
            m_supplyDestinations[source]?.Remove(destination);
            if (m_supplyDestinations[source]?.Count == 0)
            {
                m_supplyDestinations[source] = null;
            }
        }

        /// <summary>
        /// Remove all supply chain links that are sourced from the given building id.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool RemoveAllSupplyChainConnectionsFromSource(int buildingId)
        {
            if (m_supplyDestinations[buildingId] != null)
            {
                while (m_supplyDestinations[buildingId]?.Count > 0)
                {
                    RemoveSupplyChainConnection(buildingId, m_supplyDestinations[buildingId][0]);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove all supply chain links where the destination is the given building id.
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        public static bool RemoveAllSupplyChainConnectionsToDestination(int buildingId)
        {
            bool removed = false;

            // First remove this building from any lists that might refer to this building ...
            for (int b = 0; b < m_supplyDestinations.Length; b++)
            {
                if (m_supplyDestinations[b] != null && m_supplyDestinations[b].Contains(buildingId))
                {
                    RemoveSupplyChainConnection(b, buildingId);
                    removed = true;
                }
            }

            return removed;
        }

        #endregion
    }
}
