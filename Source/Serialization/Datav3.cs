﻿using System;
using System.Collections.Generic;

namespace EnhancedDistrictServices.Serialization
{
    [Serializable]
    public class Datav3
    {
        public bool[] InputBuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public bool[] InputBuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] InputBuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        public bool[] OutputBuildingToAllLocalAreas = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public bool[] OutputBuildingToOutsideConnections = new bool[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] OutputBuildingToDistrictServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];

        public int[] BuildingToInternalSupplyBuffer = new int[BuildingManager.MAX_BUILDING_COUNT];
        public List<int>[] BuildingToBuildingServiced = new List<int>[BuildingManager.MAX_BUILDING_COUNT];
        public int GlobalOutsideConnectionIntensity = 15;

        private static readonly string m_id = "EnhancedDistrictServices_v3";

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public static bool TryLoadData(EnhancedDistrictServicesSerializableData loader, out Datav3 data)
        {
            if (loader.TryLoadData(m_id, null, out Datav3 target))
            {
                data = target;
                return true;
            }
            else
            {
                data = null;
                return false;
            }
        }
    }
}