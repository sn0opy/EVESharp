using Common.Logging;
using Common.Services;
using Node.Database;
using Node.Inventory;
using Node.Network;
using PythonTypes.Types.Exceptions;
using PythonTypes.Types.Primitives;

namespace Node.Services.Config
{
    public class config : Service
    {
        private ConfigDB DB { get; }
        private Channel Log { get; }
        private ItemManager ItemManager { get; }
        
        public config(ConfigDB db, ItemManager itemManager, Logger log)
        {
            this.DB = db;
            this.ItemManager = itemManager;
            this.Log = log.CreateLogChannel("Map");
        }

        public PyDataType GetMultiOwnersEx(PyList ids, CallInformation call)
        {
            // return item data from the entity table and call it a day
            return this.DB.GetMultiOwnersEx(ids);
        }

        public PyDataType GetMultiGraphicsEx(PyList ids, CallInformation call)
        {
            return this.DB.GetMultiGraphicsEx(ids);
        }

        public PyDataType GetMultiLocationsEx(PyList ids, CallInformation call)
        {
            return this.DB.GetMultiLocationsEx(ids);
        }

        public PyDataType GetMultiAllianceShortNamesEx(PyList ids, CallInformation call)
        {
            return this.DB.GetMultiAllianceShortNamesEx(ids);
        }

        public PyDataType GetMap(PyInteger solarSystemID, CallInformation call)
        {
            return this.DB.GetMap(solarSystemID);
        }

        // THESE PARAMETERS AREN'T REALLY USED ANYMORE, THIS FUNCTION IS USUALLY CALLED WITH LOCATIONID, 1
        public PyDataType GetMapObjects(PyInteger locationID, PyInteger ignored1, CallInformation call)
        {
            return this.DB.GetMapObjects(locationID);
        }

        // THESE PARAMETERS AREN'T REALLY USED ANYMORE THIS FUNCTION IS USUALLY CALLED WITH LOCATIONID, 0, 0, 0, 1, 0
        public PyDataType GetMapObjects(PyInteger locationID, PyInteger wantRegions, PyInteger wantConstellations,
            PyInteger wantSystems, PyInteger wantItems, PyInteger unknown)
        {
            return this.DB.GetMapObjects(locationID);
        }

        public PyDataType GetMapOffices(PyInteger solarSystemID, CallInformation call)
        {
            return this.DB.GetMapOffices(solarSystemID);
        }

        public PyDataType GetCelestialStatistic(PyInteger celestialID, CallInformation call)
        {
            if (ItemManager.IsCelestialID(celestialID) == false)
                throw new CustomError($"Unexpected celestialID {celestialID}");
            
            // TODO: CHECK FOR STATIC DATA TO FETCH IT OFF MEMORY INSTEAD OF DATABASE?
            return this.DB.GetCelestialStatistic(celestialID);
        }

        public PyDataType GetMultiInvTypesEx(PyList typeIDs, CallInformation call)
        {
            return this.DB.GetMultiInvTypesEx(typeIDs);
        }

        public PyDataType GetStationSolarSystemsByOwner(PyInteger ownerID, CallInformation call)
        {
            return this.DB.GetStationSolarSystemsByOwner(ownerID);
        }

        public PyDataType GetMapConnections(PyInteger itemID, PyDataType isRegion, PyDataType isConstellation,
            PyDataType isSolarSystem, PyDataType isCelestial, PyInteger unknown2 = null, CallInformation call = null)
        {
            bool isRegionBool = false;
            bool isConstellationBool = false;
            bool isSolarSystemBool = false;
            bool isCelestialBool = false;

            if (isRegion is PyBool regionBool)
                isRegionBool = regionBool;
            if (isRegion is PyInteger regionInt)
                isRegionBool = regionInt.Value == 1;
            if (isConstellation is PyBool constellationBool)
                isConstellationBool = constellationBool;
            if (isConstellation is PyInteger constellationInt)
                isConstellationBool = constellationInt.Value == 1;
            if (isSolarSystem is PyBool solarSystemBool)
                isSolarSystemBool = solarSystemBool;
            if (isSolarSystem is PyInteger solarSystemInt)
                isSolarSystemBool = solarSystemInt.Value == 1;
            if (isCelestial is PyBool celestialBool)
                isCelestialBool = celestialBool;
            if (isCelestial is PyInteger celestialInt)
                isCelestialBool = celestialInt.Value == 1;

            if (isRegionBool == true)
            {
                return this.DB.GetMapRegionConnection(itemID);
            }
            if (isConstellationBool == true)
            {
                return this.DB.GetMapConstellationConnection(itemID);
            }
            if (isSolarSystemBool == true)
            {
                return this.DB.GetMapSolarSystemConnection(itemID);
            }
            if (isCelestialBool == true)
            {
                Log.Error("GetMapConnections called with celestial id. Not implemented yet!");
            }
            
            return null;
        }
    }
}