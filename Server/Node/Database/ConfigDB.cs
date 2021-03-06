using System;
using System.Collections.Generic;
using Common.Database;
using MySql.Data.MySqlClient;
using PythonTypes.Types.Database;
using PythonTypes.Types.Primitives;

namespace Node.Database
{
    public class ConfigDB : DatabaseAccessor
    {
        private NodeContainer NodeContainer { get; }
        
        public ConfigDB(NodeContainer nodeContainer, DatabaseConnection db) : base(db)
        {
            this.NodeContainer = nodeContainer;
        }

        public PyDataType GetMultiOwnersEx(PyList ids)
        {
            string query = "SELECT itemID as ownerID, itemName as ownerName, typeID FROM eveNames WHERE itemID IN (";
            Dictionary<string, object> parameters = new Dictionary<string,object>();

            foreach (PyDataType id in ids)
                parameters["@itemID" + parameters.Count.ToString("X")] = (int) (id as PyInteger);

            // prepare the correct list of arguments
            query += String.Join(',', parameters.Keys) + ")";
            
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection, query, parameters);
            
            using (connection)
            using (reader)
            {
                return TupleSet.FromMySqlDataReader(reader);
            }
        }

        public PyDataType GetMultiGraphicsEx(PyList ids)
        {
            string query = "SELECT graphicID, url3D, urlWeb, icon, urlSound, explosionID FROM eveGraphics WHERE graphicID IN (";
            Dictionary<string, object> parameters = new Dictionary<string,object>();

            foreach (PyDataType id in ids)
                parameters["@graphicID" + parameters.Count.ToString("X")] = (int) (id as PyInteger);

            // prepare the correct list of arguments
            query += String.Join(',', parameters.Keys) + ")";
            
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection, query, parameters);
            
            using (connection)
            using (reader)
            {
                return TupleSet.FromMySqlDataReader(reader);
            }
        }
        
        public PyDataType GetMultiLocationsEx(PyList ids)
        {
            string query = "SELECT itemID as locationID, itemName as locationName, x, y, z FROM invItems LEFT JOIN eveNames USING(itemID) LEFT JOIN invPositions USING (itemID) WHERE itemID IN (";
            Dictionary<string, object> parameters = new Dictionary<string,object>();

            foreach (PyDataType id in ids)
                parameters["@itemID" + parameters.Count.ToString("X")] = (int) (id as PyInteger);

            // prepare the correct list of arguments
            query += String.Join(',', parameters.Keys) + ")";
            
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection, query, parameters);
            
            using (connection)
            using (reader)
            {
                return TupleSet.FromMySqlDataReader(reader);
            }
        }
        
        public PyDataType GetMultiAllianceShortNamesEx(PyList ids)
        {
            string query = "SELECT allianceID, shortName FROM alliance_shortnames WHERE allianceID IN (";
            Dictionary<string, object> parameters = new Dictionary<string,object>();

            foreach (PyDataType id in ids)
                parameters["@itemID" + parameters.Count.ToString("X")] = (int) (id as PyInteger);

            // prepare the correct list of arguments
            query += String.Join(',', parameters.Keys) + ")";
            
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection, query, parameters);
            
            using (connection)
            using (reader)
            {
                return TupleSet.FromMySqlDataReader(reader);
            }
        }

        public Rowset GetMap(int solarSystemID)
        {
            Rowset result = Database.PrepareRowsetQuery(
                "SELECT IF(groupID = 10, (SELECT GROUP_CONCAT(celestialID SEPARATOR ',') FROM mapJumps WHERE stargateID = itemID), NULL) AS destinations, itemID, itemName, typeID, mapDenormalize.x, mapDenormalize.y, mapDenormalize.z, xMin, yMin, zMin, xMax, yMax, zMax, orbitID, luminosity, mapDenormalize.solarSystemID AS locationID FROM mapDenormalize LEFT JOIN mapSolarSystems ON mapSolarSystems.solarSystemID = mapDenormalize.itemID WHERE mapDenormalize.solarSystemID = @solarSystemID",
                new Dictionary<string, object>()
                {
                    {"@solarSystemID", solarSystemID}
                }
            );
            
            // iterate all the results and create the list based on the concatenated column
            foreach (PyDataType lineData in result.Rows)
            {
                // get destinations string
                PyList line = lineData as PyList;
                PyDataType firstItem = line[0];

                // ignore null entries
                if (firstItem is PyString == false)
                    continue;
                
                PyString destinations = firstItem as PyString;

                // get the list of ids
                string[] list = destinations.Value.Split(',');

                PyList destinationsList = new PyList(list.Length);

                int i = 0;
                
                // put the ids in the new destination list
                foreach (string id in list)
                    destinationsList[i++] = int.Parse(id);

                // finally store it in the line
                line[0] = destinationsList;
            }

            // return the result!
            return result;
        }

        public CRowset GetMapObjects(int itemID)
        {
            if (itemID == this.NodeContainer.Constants["locationUniverse"])
            {
                return Database.PrepareCRowsetQuery(
                    $"SELECT groupID, typeID, itemID, itemName, {itemID} as locationID, orbitID, 0 AS connection, x, y, z FROM mapDenormalize WHERE typeID = 3"
                );
            }
            else
            {
                return Database.PrepareCRowsetQuery(
                    "SELECT groupID, typeID, itemID, itemName, solarSystemID AS locationID, orbitID, 0 AS connection, x, y, z FROM mapDenormalize WHERE solarSystemID = @solarSystemID",
                    new Dictionary<string, object>()
                    {
                        {"@solarSystemID", itemID}
                    }
                );    
            }
        }

        public Rowset GetMapOffices(int solarSystemID)
        {
            return Database.PrepareRowsetQuery(
                "SELECT crpOffices.corporationID, crpOffices.stationID FROM crpOffices, staStations WHERE crpOffices.stationID = staStations.stationID AND staStations.solarSystemID = @solarSystemID",
                new Dictionary<string, object>()
                {
                    {"@solarSystemID", solarSystemID}
                }
            );
        }

        public CRowset GetCelestialStatistic(int celestialID)
        {
            return Database.PrepareCRowsetQuery(
                "SELECT temperature, spectralClass, luminosity, age, life, orbitRadius, eccentricity, massDust, massGas, fragmented, density, surfaceGravity, escapeVelocity, orbitPeriod, rotationRate, locked, pressure, radius, mass FROM mapCelestialStatistics WHERE celestialID = @celestialID",
                new Dictionary<string, object>()
                {
                    {"@celestialID", celestialID}
                }
            );
        }

        public PyDataType GetMultiInvTypesEx(PyList ids)
        {
            string query = "SELECT typeID, groupID, typeName, description, graphicID, radius, mass, volume, capacity, portionSize, raceID, basePrice, published, marketGroupID, chanceOfDuplicating, dataID FROM invTypes WHERE typeID IN (";
            Dictionary<string, object> parameters = new Dictionary<string,object>();

            foreach (PyDataType id in ids)
                parameters["@typeID" + parameters.Count.ToString("X")] = (int) (id as PyInteger);

            // prepare the correct list of arguments
            query += String.Join(',', parameters.Keys) + ")";
            
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection, query, parameters);
            
            using (connection)
            using (reader)
            {
                return RowList.FromMySqlDataReader(reader);
            }
        }

        public PyDataType GetStationSolarSystemsByOwner(int ownerID)
        {
            return Database.PrepareRowsetQuery(
                "SELECT corporationID, solarSystemID FROM staStations WHERE corporationID = @corporationID",
                new Dictionary<string, object>()
                {
                    {"@corporationID", ownerID}
                }
            );
        }

        public PyList GetMapRegionConnection(int universeID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT origin.regionID AS fromRegionID, origin.constellationID AS fromConstellationID, origin.solarSystemID AS fromSolarSystemID, stargateID, celestialID, destination.solarSystemID AS toSolarSystemID, destination.constellationID AS toConstellationID, destination.regionID AS toRegionID FROM mapJumps LEFT JOIN mapDenormalize origin ON origin.itemID = mapJumps.stargateID LEFT JOIN mapDenormalize destination ON destination.itemID = mapJumps.celestialID",
                new Dictionary<string, object>()
                {
                    {"@locationID", universeID}
                }
            );
            
            using(connection)
            using (reader)
            {
                PyList result = new PyList();

                while (reader.Read() == true)
                {
                    result.Add(new PyTuple(9)
                        {
                            [0] = "",
                            [1] = reader.GetInt32(0),
                            [2] = reader.GetInt32(1),
                            [3] = reader.GetInt32(2),
                            [4] = reader.GetInt32(3),
                            [5] = reader.GetInt32(4),
                            [6] = reader.GetInt32(5),
                            [7] = reader.GetInt32(6),
                            [8] = reader.GetInt32(7),
                        }
                    );
                }

                return result;
            }
        }

        public PyList GetMapConstellationConnection(int regionID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT fromRegionID, fromConstellationID, toConstellationID, toRegionID FROM mapConstellationJumps WHERE fromRegionID = @locationID",
                new Dictionary<string, object>()
                {
                    {"@locationID", regionID}
                }
            );
            
            using(connection)
            using (reader)
            {
                PyList result = new PyList();

                while (reader.Read() == true)
                {
                    result.Add(new PyTuple(9)
                        {
                            [0] = "",
                            [1] = reader.GetInt32(0),
                            [2] = reader.GetInt32(1),
                            [3] = 0,
                            [4] = 0,
                            [5] = 0,
                            [6] = 0,
                            [7] = reader.GetInt32(2),
                            [8] = reader.GetInt32(3),
                        }
                    );
                }

                return result;
            }
        }

        public PyList GetMapSolarSystemConnection(int constellationID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT fromRegionID, fromConstellationID, fromSolarSystemID, toSolarSystemID, toConstellationID, toRegionID FROM mapSolarSystemJumps WHERE fromConstellationID = @locationID",
                new Dictionary<string, object>()
                {
                    {"@locationID", constellationID}
                }
            );
            
            using(connection)
            using (reader)
            {
                PyList result = new PyList();

                while (reader.Read() == true)
                {
                    result.Add(new PyTuple(9)
                        {
                            [0] = "",
                            [1] = reader.GetInt32(0),
                            [2] = reader.GetInt32(1),
                            [3] = reader.GetInt32(2),
                            [4] = 0,
                            [5] = 0,
                            [6] = reader.GetInt32(3),
                            [7] = reader.GetInt32(4),
                            [8] = reader.GetInt32(5),
                        }
                    );
                }

                return result;
            }
        }
    }
}