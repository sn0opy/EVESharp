﻿/*
    ------------------------------------------------------------------------------------
    LICENSE:
    ------------------------------------------------------------------------------------
    This file is part of EVE#: The EVE Online Server Emulator
    Copyright 2021 - EVE# Team
    ------------------------------------------------------------------------------------
    This program is free software; you can redistribute it and/or modify it under
    the terms of the GNU Lesser General Public License as published by the Free Software
    Foundation; either version 2 of the License, or (at your option) any later
    version.

    This program is distributed in the hope that it will be useful, but WITHOUT
    ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
    FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License along with
    this program; if not, write to the Free Software Foundation, Inc., 59 Temple
    Place - Suite 330, Boston, MA 02111-1307, USA, or go to
    http://www.gnu.org/copyleft/lesser.txt.
    ------------------------------------------------------------------------------------
    Creator: Almamu
*/

using System;
using System.Collections.Generic;
using Common.Database;
using MySql.Data.MySqlClient;
using Node.Inventory;
using Node.Inventory.Items;
using Node.Inventory.Items.Attributes;
using Node.Inventory.Items.Types;
using Node.Inventory.SystemEntities;
using PythonTypes.Types.Database;
using PythonTypes.Types.Primitives;

namespace Node.Database
{
    public class ItemDB : DatabaseAccessor
    {
        private ItemFactory ItemFactory { get; }
        private ClientManager ClientManager { get; }
        private TimerManager TimerManager { get; }
        private AttributeManager AttributeManager => this.ItemFactory.AttributeManager;
        private GroupManager GroupManager => this.ItemFactory.GroupManager;
        private CategoryManager CategoryManager => this.ItemFactory.CategoryManager;
        private TypeManager TypeManager => this.ItemFactory.TypeManager;
        private StationManager StationManager => this.ItemFactory.StationManager;

        public Dictionary<int, ItemCategory> LoadItemCategories()
        {
            MySqlConnection connection = null;

            MySqlDataReader reader = Database.Query(ref connection,
                "SELECT categoryID, categoryName, description, graphicID, published FROM invCategories"
            );

            using (connection)
            using (reader)
            {
                Dictionary<int, ItemCategory> itemCategories = new Dictionary<int, ItemCategory>();

                while (reader.Read())
                {
                    ItemCategory itemCategory = new ItemCategory(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetInt32OrDefault(3),
                        reader.GetBoolean(4)
                    );

                    itemCategories[itemCategory.ID] = itemCategory;
                }

                return itemCategories;
            }
        }

        public Dictionary<int, ItemType> LoadItemTypes()
        {
            MySqlConnection connection = null;

            MySqlDataReader reader = Database.Query(ref connection,
                "SELECT typeID, groupID, typeName, description, graphicID, radius, mass, volume, capacity, portionSize, raceID, basePrice, published, marketGroupID, chanceOfDuplicating FROM invTypes"
            );

            using (connection)
            using (reader)
            {
                Dictionary<int, ItemType> itemTypes = new Dictionary<int, ItemType>();

                while (reader.Read())
                {
                    int typeID = reader.GetInt32(0);
                    
                    Dictionary<int, ItemAttribute> defaultAttributes = null;

                    if (this.AttributeManager.DefaultAttributes.ContainsKey(typeID) == true)
                        defaultAttributes = this.AttributeManager.DefaultAttributes[typeID];
                    else
                        defaultAttributes = new Dictionary<int, ItemAttribute>();
                    
                    ItemType type = new ItemType(
                        typeID,
                        this.GroupManager[reader.GetInt32(1)],
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetInt32OrDefault(4),
                        reader.GetDouble(5),
                        reader.GetDouble(6),
                        reader.GetDouble(7),
                        reader.GetDouble(8),
                        reader.GetInt32(9),
                        reader.GetInt32OrDefault(10),
                        reader.GetDouble(11),
                        reader.GetBoolean(12),
                        reader.GetInt32OrDefault(13),
                        reader.GetDouble(14),
                        defaultAttributes
                    );

                    itemTypes[type.ID] = type;
                }

                return itemTypes;
            }
        }

        public Dictionary<int, ItemGroup> LoadItemGroups()
        {
            MySqlConnection connection = null;

            MySqlDataReader reader = Database.Query(ref connection,
                "SELECT groupID, categoryID, groupName, description, graphicID, useBasePrice, allowManufacture, allowRecycler, anchored, anchorable, fittableNonSingleton, published FROM invGroups"
            );

            using (connection)
            using (reader)
            {
                Dictionary<int, ItemGroup> itemGroups = new Dictionary<int, ItemGroup>();

                while (reader.Read() == true)
                {
                    ItemGroup group = new ItemGroup(
                        reader.GetInt32(0),
                        this.CategoryManager[reader.GetInt32(1)],
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetInt32OrDefault(4),
                        reader.GetBoolean(5),
                        reader.GetBoolean(6),
                        reader.GetBoolean(7),
                        reader.GetBoolean(8),
                        reader.GetBoolean(9),
                        reader.GetBoolean(10),
                        reader.GetBoolean(11)
                    );

                    itemGroups[group.ID] = group;
                }

                return itemGroups;
            }
        }

        public Dictionary<int, AttributeInfo> LoadAttributesInformation()
        {
            MySqlConnection connection = null;

            // sort the attributes by maxAttributeID so the simple attributes are loaded first
            // and then the complex ones that are related to other attributes
            MySqlDataReader reader = Database.Query(ref connection,
                "SELECT attributeID, attributeName, attributeCategory, description, maxAttributeID, attributeIdx, graphicID, chargeRechargeTimeID, defaultValue, published, displayName, unitID, stackable, highIsGood, categoryID FROM dgmAttributeTypes ORDER BY maxAttributeID ASC"
            );

            using (connection)
            using (reader)
            {
                Dictionary<int, AttributeInfo> attributes = new Dictionary<int, AttributeInfo>();

                while (reader.Read())
                {
                    AttributeInfo info = new AttributeInfo(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetInt32(2),
                        reader.GetString(3),
                        reader.IsDBNull(4) ? null : attributes[reader.GetInt32(4)],
                        reader.GetInt32OrDefault(5),
                        reader.GetInt32OrDefault(6),
                        reader.GetInt32OrDefault(7),
                        reader.GetDouble(8),
                        reader.GetInt32(9),
                        reader.GetString(10),
                        reader.GetInt32OrDefault(11),
                        reader.GetInt32(12),
                        reader.GetInt32(13),
                        reader.GetInt32OrDefault(14)
                    );

                    attributes[info.ID] = info;
                }

                return attributes;
            }
        }

        public Dictionary<int, Dictionary<int, ItemAttribute>> LoadDefaultAttributes()
        {
            MySqlConnection connection = null;
            
            MySqlDataReader reader = Database.Query(ref connection, 
                "SELECT typeID, attributeID, valueInt, valueFloat FROM dgmTypeAttributes"
            );
            
            using(connection)
            using (reader)
            {
                Dictionary<int, Dictionary<int, ItemAttribute>> attributes = new Dictionary<int, Dictionary<int, ItemAttribute>>();

                while (reader.Read() == true)
                {
                    int typeID = reader.GetInt32(0);
                    
                    if(attributes.ContainsKey(typeID) == false)
                        attributes[typeID] = new Dictionary<int, ItemAttribute>();
                    
                    ItemAttribute attribute = null;
                    
                    if (reader.IsDBNull(2) == false)
                    {
                        attribute = new ItemAttribute(
                            this.AttributeManager[reader.GetInt32(1)],
                            reader.GetInt32(2)
                        );
                    }
                    else
                    {
                        attribute = new ItemAttribute(
                            this.AttributeManager[reader.GetInt32(1)],
                            reader.GetDouble(3)
                        );
                    }

                    attributes[typeID][attribute.Info.ID] = attribute;
                }

                return attributes;
            }
        }

        public List<ItemEntity> LoadStaticItems()
        {
            // TODO: THIS NEEDS SOME CHANGE, WE'RE LOADING HALF A MILLION ITEMS INTO MEMORY
            MySqlConnection connection = null;
            MySqlCommand command = Database.PrepareQuery(ref connection,
                $"SELECT itemID, eveNames.itemName, invItems.typeID, ownerID, locationID, flag, contraband, singleton, quantity, x, y, z, custominfo FROM invItems LEFT JOIN eveNames USING(itemID) LEFT JOIN invPositions USING (itemID) WHERE itemID < {ItemManager.USERGENERATED_ID_MIN} AND (groupID = {(int) ItemGroups.Station} OR groupID = {(int) ItemGroups.Faction} OR groupID = {(int) ItemGroups.SolarSystem} OR groupID = {(int) ItemGroups.Corporation} OR groupID = {(int) ItemGroups.System})"
            );
            
            using (connection)
            using (command)
            {
                MySqlDataReader reader = command.ExecuteReader();
                List<ItemEntity> itemList = new List<ItemEntity>();
                
                using (reader)
                {
                    while (reader.Read () == true)
                        itemList.Add(this.BuildItemFromReader(reader));
                }

                return itemList;
            }
        }

        private Item BuildItemFromReader(MySqlDataReader reader)
        {
            ItemType itemType = this.TypeManager[reader.GetInt32(2)];
            Item newItem = new Item(
                reader.GetStringOrNull(1), // itemName
                reader.GetInt32(0), // itemID
                itemType, // typeID
                reader.GetInt32(3), // ownerID
                reader.GetInt32(4), // locationID
                (ItemFlags) reader.GetInt32(5), // flag
                reader.GetBoolean(6), // contraband
                reader.GetBoolean(7), // singleton
                reader.GetInt32(8), // quantity
                reader.GetDoubleOrNull(9), // x
                reader.GetDoubleOrNull(10), // y
                reader.GetDoubleOrNull(11), // z
                reader.GetStringOrDefault(12), // customInfo
                new AttributeList(
                    this.ItemFactory,
                    itemType,
                    this.LoadAttributesForItem(reader.GetInt32(0))
                ), 
                this.ItemFactory
            );

            return newItem;
        }
        
        public Item LoadItem(int itemID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT itemID, eveNames.itemName, invItems.typeID, ownerID, locationID, flag, contraband, singleton, quantity, x, y, z, customInfo, nodeID FROM invItems LEFT JOIN eveNames USING (itemID) LEFT JOIN invPositions USING (itemID) WHERE itemID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", itemID}
                }
            );

            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                Item newItem = this.BuildItemFromReader(reader);
                
                // the non-user generated items cannot be owned by any node
                if (itemID < ItemManager.USERGENERATED_ID_MIN)
                    return newItem;

                if (reader.IsDBNull(13) == false && reader.GetInt32(13) != 0)
                    throw new Exception("Trying to load an item that is loaded on another node!");
                    
                // Update the database information
                Database.PrepareQuery(
                    "UPDATE invItems SET nodeID = @nodeID WHERE itemID = @itemID",
                    new Dictionary<string, object>()
                    {
                        {"@nodeID", Program.NodeID},
                        {"@itemID", itemID}
                    }
                );

                return newItem;
            }
        }

        public List<int> GetInventoryItems(int inventoryID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT itemID FROM invItems WHERE locationID = @inventoryID",
                new Dictionary<string, object>()
                {
                    {"@inventoryID", inventoryID}
                }
            );

            using (connection)
            using (reader)
            {
                List<int> itemList = new List<int>();

                while (reader.Read()) itemList.Add(reader.GetInt32(0));

                return itemList;
            }
        }

        /// <summary>
        /// WARNING: ONLY USE WHEN THE ITEM IS NOT STATIC DATA AND DOES NOT BELONG TO OUR NODE!
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public PyInteger GetItemTypeID(int itemID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT typeID FROM invItems WHERE itemID = @itemID", new Dictionary<string, object>()
                {
                    {"@itemID", itemID}
                }
            );
            
            using(connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                return reader.GetInt32(0);
            }
        }

        /// <summary>
        /// WARNING: ONLY USE WHEN THE ITEM IS NOT STATIC DATA AND DOES NOT BELONG TO OUR NODE!
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public PyInteger GetItemLocationID(int itemID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT locationID FROM invItems WHERE itemID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", itemID}
                }
            );
            
            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                return reader.GetInt32(0);
            }
        }
        
        public int GetCategoryID(int typeID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT invCategories.categoryID FROM invCategories LEFT JOIN invTypes ON invTypes.typeID = @typeID LEFT JOIN invGroups ON invGroups.groupID = invTypes.groupID WHERE invCategories.categoryID = invGroups.groupID",
                new Dictionary<string, object>()
                {
                    {"@typeID",typeID}
                }
            );

            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return 0;

                return reader.GetInt32(0);
            }
        }

        public Blueprint LoadBlueprint(ItemEntity item)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT copy, materialLevel, productivityLevel, licensedProductionRunsRemaining FROM invBlueprints WHERE blueprintID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", item.ID}
                }
            );

            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                return new Blueprint(item, reader.GetBoolean(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3));
            }
        }

        public Corporation LoadCorporation(ItemEntity item)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT description, tickerName, url, taxRate," +
                " minimumJoinStanding, corporationType, hasPlayerPersonnelManager, sendCharTerminationMessage," +
                " creatorID, ceoID, stationID, raceID, allianceID, shares, memberCount, memberLimit," +
                " allowedMemberRaceIDs, graphicID, shape1, shape2, shape3, color1, color2, color3, typeface," +
                " division1, division2, division3, division4, division5, division6, division7, walletDivision1," +
                " walletDivision2, walletDivision3, walletDivision4, walletDivision5, walletDivision6," + 
                " walletDivision7, balance, deleted" +
                " FROM corporation WHERE corporationID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", item.ID}
                }
            );
            
            using(connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                return new Corporation(item,
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDouble(3),
                    reader.GetDouble(4),
                    reader.GetInt32(5),
                    reader.GetBoolean(6),
                    reader.GetBoolean(7),
                    reader.GetInt32(8),
                    reader.GetInt32(9),
                    reader.GetInt32(10),
                    reader.GetInt32(11),
                    reader.GetInt32(12),
                    reader.GetInt64(13),
                    reader.GetInt32(14),
                    reader.GetInt32(15),
                    reader.GetInt32(16),
                    reader.GetInt32(17),
                    reader.GetInt32OrNull(18),
                    reader.GetInt32OrNull(19),
                    reader.GetInt32OrNull(20),
                    reader.GetInt32OrNull(21),
                    reader.GetInt32OrNull(22),
                    reader.GetInt32OrNull(23),
                    reader.GetStringOrNull(24),
                    reader.GetStringOrNull(25),
                    reader.GetStringOrNull(26),
                    reader.GetStringOrNull(27),
                    reader.GetStringOrNull(28),
                    reader.GetStringOrNull(29),
                    reader.GetStringOrNull(30),
                    reader.GetStringOrNull(31),
                    reader.GetStringOrNull(32),
                    reader.GetStringOrNull(33),
                    reader.GetStringOrNull(34),
                    reader.GetStringOrNull(35),
                    reader.GetStringOrNull(36),
                    reader.GetStringOrNull(37),
                    reader.GetStringOrNull(38),
                    reader.GetDouble(39),
                    reader.GetBoolean(40)
                );
            }
        }
        
        public Character LoadCharacter(ItemEntity item)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT characterID, accountID, activeCloneID, title, description, bounty, balance, securityRating," +
                " petitionMessage, logonMinutes, corporationID, corpRole, rolesAtAll, rolesAtBase, rolesAtHQ," +
                " rolesAtOther, corporationDateTime, startDateTime, createDateTime, ancestryID, careerID, schoolID," +
                " careerSpecialityID, gender, accessoryID, beardID, costumeID, decoID, eyebrowsID, eyesID, hairID," +
                " lipstickID, makeupID, skinID, backgroundID, lightID, headRotation1, headRotation2, headRotation3," +
                " eyeRotation1, eyeRotation2, eyeRotation3, camPos1, camPos2, camPos3, morph1e, morph1n, morph1s," +
                " morph1w, morph2e, morph2n, morph2s, morph2w, morph3e, morph3n, morph3s, morph3w, morph4e, morph4n," +
                " morph4s, morph4w, stationID, solarSystemID, constellationID, regionID, online, freeRespecs, nextRespecTime," +
                " timeLastJump, titleMask " +
                "FROM chrInformation WHERE characterID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", item.ID}
                }
            );
            
            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;
                
                return new Character(
                    this.ClientManager,
                    this.TimerManager,
                    item,
                    reader.GetInt32(0),
                    reader.GetInt32OrDefault(1),
                    reader.GetInt32OrNull(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetDouble(5),
                    reader.GetDouble(6),
                    reader.GetDouble(7),
                    reader.GetString(8),
                    reader.GetInt32(9),
                    reader.GetInt32(10),
                    reader.GetInt32(11),
                    reader.GetInt32(12),
                    reader.GetInt32(13),
                    reader.GetInt32(14),
                    reader.GetInt32(15),
                    reader.GetInt64(16),
                    reader.GetInt64(17),
                    reader.GetInt64(18),
                    reader.GetInt32(19),
                    reader.GetInt32(20),
                    reader.GetInt32(21),
                    reader.GetInt32(22),
                    reader.GetInt32(23),
                    reader.GetInt32OrNull(24),
                    reader.GetInt32OrNull(25),
                    reader.GetInt32(26),
                    reader.GetInt32OrNull(27),
                    reader.GetInt32(28),
                    reader.GetInt32(29),
                    reader.GetInt32(30),
                    reader.GetInt32OrNull(31),
                    reader.GetInt32OrNull(32),
                    reader.GetInt32(33),
                    reader.GetInt32(34),
                    reader.GetInt32(35),
                    reader.GetDouble(36),
                    reader.GetDouble(37),
                    reader.GetDouble(38),
                    reader.GetDouble(39),
                    reader.GetDouble(40),
                    reader.GetDouble(41),
                    reader.GetDouble(42),
                    reader.GetDouble(43),
                    reader.GetDouble(44),
                    reader.GetDoubleOrNull(45),
                    reader.GetDoubleOrNull(46),
                    reader.GetDoubleOrNull(47),
                    reader.GetDoubleOrNull(48),
                    reader.GetDoubleOrNull(49),
                    reader.GetDoubleOrNull(50),
                    reader.GetDoubleOrNull(51),
                    reader.GetDoubleOrNull(52),
                    reader.GetDoubleOrNull(53),
                    reader.GetDoubleOrNull(54),
                    reader.GetDoubleOrNull(55),
                    reader.GetDoubleOrNull(56),
                    reader.GetDoubleOrNull(57),
                    reader.GetDoubleOrNull(58),
                    reader.GetDoubleOrNull(59),
                    reader.GetDoubleOrNull(60),
                    reader.GetInt32(61),
                    reader.GetInt32(62),
                    reader.GetInt32(63),
                    reader.GetInt32(64),
                    reader.GetInt32(65),
                    reader.GetInt32(66),
                    reader.GetInt64(67),
                    reader.GetInt64(68),
                    reader.GetInt32(69)
                );
            }
        }

        public Faction LoadFaction(ItemEntity item)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT description, raceIDs, solarSystemID, corporationID, sizeFactor, stationCount," +
                " stationSystemCount, militiaCorporationID" +
                " FROM chrFactions",
                new Dictionary<string, object>()
                {
                    {"@itemID", item.ID}
                }
            );
            
            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;
                
                return new Faction(item,
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    reader.GetDouble(4),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.GetInt32(7)
                );
            }
        }

        public Skill LoadSkill(ItemEntity item)
        {
            return new Skill(item);
        }

        public Ship LoadShip(ItemEntity item)
        {
            return new Ship(item);
        }

        public Implant LoadImplant(ItemEntity item)
        {
            return new Implant(item);
        }

        public Station LoadStation(ItemEntity item)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT operationID, security, dockingCostPerVolume, maxShipVolumeDockable, officeRentalCost, constellationID, regionID, reprocessingEfficiency, reprocessingStationsTake, reprocessingHangarFlag FROM staStations WHERE stationID = @stationID",
                new Dictionary<string, object>
                {
                    {"@stationID", item.ID}
                }
            );
            
            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                return new Station(
                    this.StationManager.StationTypes[item.Type.ID],
                    this.StationManager.Operations[reader.GetInt32(0)],
                    reader.GetInt32(1),
                    reader.GetDouble(2),
                    reader.GetDouble(3),
                    reader.GetInt32(4),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.GetDouble(7),
                    reader.GetDouble(8),
                    reader.GetInt32(9),
                    item
                );
            }
        }

        public Clone LoadClone(ItemEntity item)
        {
            return new Clone(item);
        }

        public ulong CreateItem(string itemName, ItemType type, ItemEntity owner, ItemEntity location, ItemFlags flag,
            bool contraband, bool singleton, int quantity, double? x, double? y, double? z, string customInfo)
        {
            ulong newItemID = Database.PrepareQueryLID(
                "INSERT INTO invItems(itemID, typeID, ownerID, locationID, flag, contraband, singleton, quantity, customInfo)VALUES(NULL, @typeID, @ownerID, @locationID, @flag, @contraband, @singleton, @quantity, @customInfo)",
                new Dictionary<string, object>()
                {
                    {"@typeID", type.ID},
                    {"@ownerID", owner?.ID},
                    {"@locationID", location?.ID},
                    {"@flag", flag},
                    {"@contraband", contraband},
                    {"@singleton", singleton},
                    {"@quantity", quantity},
                    {"@x", x},
                    {"@y", y},
                    {"@z", z},
                    {"@customInfo", customInfo}
                }
            );

            if (itemName != null)
            {
                // create the item name entry if needed
                Database.PrepareQuery(
                    "INSERT INTO eveNames(itemID, itemName, categoryID, groupID, typeID)VALUES(@itemID, @itemName, @categoryID, @groupID, @typeID)",
                    new Dictionary<string, object>()
                    {
                        {"@itemID", newItemID},
                        {"@itemName", itemName},
                        {"@categoryID", type.Group.Category.ID},
                        {"@groupID", type.Group.ID},
                        {"@typeID", type.ID}
                    }
                );
            }

            if (x != null && y != null && z != null)
            {
                // create the entry for item position
                Database.PrepareQuery(
                    "INSERT INTO invPositions(itemID, x, y, z)VALUES(@itemID, @x, @y, @z)",
                    new Dictionary<string, object>()
                    {
                        {"@itemID", newItemID},
                        {"@x", x},
                        {"@y", y},
                        {"@z", z}
                    }
                );
            }

            return newItemID;
        }

        public ulong CreateItem(string itemName, int typeID, int owner, int? location, ItemFlags flag, bool contraband,
            bool singleton, int quantity, double? x, double? y, double? z, string customInfo)
        {
            ulong newItemID = Database.PrepareQueryLID(
                "INSERT INTO invItems(itemID, typeID, ownerID, locationID, flag, contraband, singleton, quantity, customInfo)VALUES(NULL, @typeID, @ownerID, @locationID, @flag, @contraband, @singleton, @quantity, @customInfo)",
                new Dictionary<string, object>()
                {
                    {"@typeID", typeID},
                    {"@ownerID", owner},
                    {"@locationID", location},
                    {"@flag", flag},
                    {"@contraband", contraband},
                    {"@singleton", singleton},
                    {"@quantity", quantity},
                    {"@customInfo", customInfo}
                }
            );

            if (itemName != null)
            {
                ItemType type = this.TypeManager[typeID];
                
                // create the item name entry if needed
                Database.PrepareQuery(
                    "INSERT INTO eveNames(itemID, itemName, categoryID, groupID, typeID)VALUES(@itemID, @itemName, @categoryID, @groupID, @typeID)",
                    new Dictionary<string, object>()
                    {
                        {"@itemID", newItemID},
                        {"@itemName", itemName},
                        {"@categoryID", type.Group.Category.ID},
                        {"@groupID", type.Group.ID},
                        {"@typeID", type.ID}
                    }
                );
            }

            if (x != null && y != null && z != null)
            {
                // create the entry for item position
                Database.PrepareQuery(
                    "INSERT INTO invPositions(itemID, x, y, z)VALUES(@itemID, @x, @y, @z)",
                    new Dictionary<string, object>()
                    {
                        {"@itemID", newItemID},
                        {"@x", x},
                        {"@y", y},
                        {"@z", z}
                    }
                );
            }

            return newItemID;
        }

        public ulong CreateShip(ItemType shipType, ItemEntity location, Character owner)
        {
            return this.CreateItem(
                $"{owner.Name}'s {shipType.Name}", shipType, owner, location, ItemFlags.Hangar,
                false, true, 1, null, null, null, null
            );
        }

        public Dictionary<int, ItemEntity> LoadItemsLocatedAt(int locationID, ItemFlags ignoreFlag)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT itemID FROM invItems WHERE locationID = @locationID AND flag != @flag",
                new Dictionary<string, object>()
                {
                    {"@locationID", locationID},
                    {"@flag", ignoreFlag}
                }
            );

            using (connection)
            using (reader)
            {
                Dictionary<int, ItemEntity> items = new Dictionary<int, ItemEntity>();

                while (reader.Read())
                {
                    items[reader.GetInt32(0)] = this.ItemFactory.ItemManager.LoadItem(reader.GetInt32(0));
                }

                return items;
            }
        }

        public Dictionary<int, ItemEntity> LoadItemsLocatedAtByOwner(int locationID, int ownerID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT itemID FROM invItems WHERE locationID = @locationID AND ownerID = @ownerID",
                new Dictionary<string, object>()
                {
                    {"@locationID", locationID},
                    {"@ownerID", ownerID}
                }
            );

            using (connection)
            using (reader)
            {
                Dictionary<int, ItemEntity> items = new Dictionary<int, ItemEntity>();

                while (reader.Read())
                {
                    items[reader.GetInt32(0)] = this.ItemFactory.ItemManager.LoadItem(reader.GetInt32(0));
                }

                return items;
            }
        }
        
        public SolarSystem LoadSolarSystem(ItemEntity item)
        {   
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT regionID, constellationID, x, y, z, xMin, yMin, zMin, xMax, yMax, zMax, luminosity, border, fringe, corridor, hub, international, regional, constellation, security, factionID, radius, sunTypeID, securityClass FROM mapSolarSystems WHERE solarSystemID = @solarSystemID",
                new Dictionary<string, object>()
                {
                    {"@solarSystemID", item.ID}
                }
            );

            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                return new SolarSystem(item,
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetDouble(2),
                    reader.GetDouble(3),
                    reader.GetDouble(4),
                    reader.GetDouble(5),
                    reader.GetDouble(6),
                    reader.GetDouble(7),
                    reader.GetDouble(8),
                    reader.GetDouble(9),
                    reader.GetDouble(10),
                    reader.GetDouble(11),
                    reader.GetBoolean(12),
                    reader.GetBoolean(13),
                    reader.GetBoolean(14),
                    reader.GetBoolean(15),
                    reader.GetBoolean(16),
                    reader.GetBoolean(17),
                    reader.GetBoolean(18),
                    reader.GetDouble(19),
                    reader.GetInt32OrNull(20),
                    reader.GetDouble(21),
                    reader.GetInt32(22),
                    reader.GetStringOrNull(23)
                );
            }
        }

        public Constellation LoadConstellation(ItemEntity item)
        {   
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT regionID, x, y, z, xMin, yMin, zMin, xMax, yMax, zMax, factionID, radius FROM mapConstellations WHERE constellationID = @constellationID",
                new Dictionary<string, object>()
                {
                    {"@constellationID", item.ID}
                }
            );

            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                return new Constellation(item,
                    reader.GetInt32(0),
                    reader.GetDouble(1),
                    reader.GetDouble(2),
                    reader.GetDouble(3),
                    reader.GetDouble(4),
                    reader.GetDouble(5),
                    reader.GetDouble(6),
                    reader.GetDouble(7),
                    reader.GetDouble(8),
                    reader.GetDouble(9),
                    reader.GetInt32OrNull(10),
                    reader.GetDouble(11)
                );
            }
        }
        
        public Region LoadRegion(ItemEntity item)
        {   
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT x, y, z, xMin, yMin, zMin, xMax, yMax, zMax, factionID, radius FROM mapRegions WHERE regionID = @regionID",
                new Dictionary<string, object>()
                {
                    {"@regionID", item.ID}
                }
            );

            using (connection)
            using (reader)
            {
                if (reader.Read() == false)
                    return null;

                return new Region(item,
                    reader.GetDouble(0),
                    reader.GetDouble(1),
                    reader.GetDouble(2),
                    reader.GetDouble(3),
                    reader.GetDouble(4),
                    reader.GetDouble(5),
                    reader.GetDouble(6),
                    reader.GetDouble(7),
                    reader.GetDouble(8),
                    reader.GetInt32OrNull(9),
                    reader.GetDouble(10)
                );
            }
        }

        public void UnloadItem(int itemID)
        {
            // non-user generated items are not owned by anyone
            if (itemID < ItemManager.USERGENERATED_ID_MIN)
                return;
            
            Database.PrepareQuery("UPDATE invItems SET nodeID = 0 WHERE itemID = @itemID", new Dictionary<string, object>()
                {
                    {"@itemID", itemID}
                }
            );
        }

        public Dictionary<int, ItemAttribute> LoadAttributesForItem(int itemID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.PrepareQuery(ref connection,
                "SELECT attributeID, valueInt, valueFloat FROM invItemsAttributes WHERE itemID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", itemID}
                }
            );

            using (connection)
            using (reader)
            {
                Dictionary<int, ItemAttribute> result = new Dictionary<int, ItemAttribute>();

                while (reader.Read())
                {
                    ItemAttribute attribute = null;

                    if (reader.IsDBNull(1) == true)
                    {
                        attribute = new ItemAttribute(
                            this.AttributeManager[reader.GetInt32(0)],
                            reader.GetDouble(2)
                        );
                    }
                    else
                    {
                        attribute = new ItemAttribute(
                            this.AttributeManager[reader.GetInt32(0)],
                            reader.GetInt64(1)
                        );
                    }

                    result[attribute.Info.ID] = attribute;
                }

                return result;
            }
        }

        /// <summary>
        /// Saves an entity to the database
        /// </summary>
        /// <param name="item"></param>
        public void PersistEntity(ItemEntity item)
        {
            Database.PrepareQuery(
                "UPDATE invItems SET typeID = @typeID, ownerID = @ownerID, locationID = @locationID, flag = @flag, contraband = @contraband, singleton = @singleton, quantity = @quantity, customInfo = @customInfo WHERE itemID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemName", item.Name},
                    {"@typeID", item.Type.ID},
                    {"@ownerID", item.OwnerID},
                    {"@locationID", item.LocationID},
                    {"@flag", item.Flag},
                    {"@contraband", item.Contraband},
                    {"@singleton", item.Singleton},
                    {"@quantity", item.Quantity},
                    {"@customInfo", item.CustomInfo},
                    {"@itemID", item.ID}
                }
            );

            // ensure naming information is up to date
            if (item.HasName)
            {
                // save item name if exists
                Database.PrepareQuery(
                    "REPLACE INTO eveNames (itemID, itemName, categoryID, groupID, typeID)VALUES(@itemID, @itemName, @categoryID, @groupID, @typeID)",
                    new Dictionary<string, object>()
                    {
                        {"@itemID", item.ID},
                        {"@itemName", item.Name},
                        {"@categoryID", item.Type.Group.Category.ID},
                        {"@groupID", item.Type.Group.ID},
                        {"@typeID", item.Type.ID}
                    }
                );
            }
            else if (item.HadName)
            {
                Database.PrepareQuery(
                    "DELETE FROM eveNames WHERE itemID = @itemID",
                    new Dictionary<string, object>()
                    {
                        {"@itemID", item.ID}
                    }
                );
            }

            if (item.HasPosition)
            {
                Database.PrepareQuery(
                    "REPLACE INTO invPositions (itemID, x, y, z)VALUES(@itemID, @x, @y, @z)",
                    new Dictionary<string, object>()
                    {
                        {"@itemID", item.ID},
                        {"@x", item.X},
                        {"@y", item.Y},
                        {"@z", item.Z}
                    }
                );
            }
            else if (item.HadPosition)
            {
                Database.PrepareQuery(
                    "DELETE FROM invPositions WHERE itemID = @itemID",
                    new Dictionary<string, object>()
                    {
                        {"@itemID", item.ID}
                    }
                );
            }
        }

        public void PersistAttributeList(ItemEntity item, AttributeList list)
        {
            MySqlConnection connection = null;
            MySqlCommand command = Database.PrepareQuery(
                ref connection,
                "REPLACE INTO invItemsAttributes(itemID, attributeID, valueInt, valueFloat) VALUE (@itemID, @attributeID, @valueInt, @valueFloat)"
            );

            using (connection)
            using (command)
            {
                foreach (KeyValuePair<int, ItemAttribute> pair in list)
                {
                    // only update dirty records
                    if (pair.Value.Dirty == false && pair.Value.New == false)
                        continue;

                    command.Parameters.Clear();
                    
                    command.Parameters.AddWithValue("@itemID", item.ID);
                    command.Parameters.AddWithValue("@attributeID", pair.Value.Info.ID);

                    if (pair.Value.ValueType == ItemAttribute.ItemAttributeValueType.Integer)
                        command.Parameters.AddWithValue("@valueInt", pair.Value.Integer);
                    else
                        command.Parameters.AddWithValue("@valueInt", null);

                    if (pair.Value.ValueType == ItemAttribute.ItemAttributeValueType.Double)
                        command.Parameters.AddWithValue("@valueFloat", pair.Value.Float);
                    else
                        command.Parameters.AddWithValue("@valueFloat", null);

                    command.ExecuteNonQuery();

                    pair.Value.New = false;
                    pair.Value.Dirty = false;
                }
            }
        }

        public void PersistBlueprint(int itemID, bool copy, int materialLevel, int productivityLevel, int licensedProductionRunsRemaining)
        {
            Database.PrepareQuery(
                "UPDATE invBlueprints SET copy = @copy, materialLevel = @materialLevel, productivityLevel = @productivityLevel, licensedProductionRunsRemaining = @licensedProductionRunsRemaining WHERE blueprintID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", itemID},
                    {"@copy", copy},
                    {"@materialLevel", materialLevel},
                    {"@productivityLevel", productivityLevel},
                    {"@licensedProductionRunsRemaining", licensedProductionRunsRemaining}
                }
            );
        }

        public PyDataType ListStations(int ownerID)
        {
            return Database.PrepareCRowsetQuery(
                "SELECT stationID, COUNT(itemID) AS itemCount, COUNT(invBlueprints.blueprintID) AS blueprintCount " +
                "FROM staStations " +
                "LEFT JOIN invItems ON locationID = stationID " +
                "LEFT JOIN invBlueprints ON invBlueprints.blueprintID = itemID " +
                "WHERE ownerID=@characterID AND flag=@hangarFlag " +
                "GROUP BY stationID",
                new Dictionary<string, object>()
                {
                    {"@characterID", ownerID},
                    {"@hangarFlag", ItemFlags.Hangar}
                }
            );
        }

        public PyDataType ListStationItems(int stationID, int ownerID)
        {
            return Database.PrepareCRowsetQuery(
                "SELECT itemID, invItems.typeID, locationID, ownerID, flag, contraband, singleton, quantity,"+
                " invTypes.groupID, invGroups.categoryID " +
                "FROM invItems " +
                "LEFT JOIN invTypes ON invItems.typeID = invTypes.typeID " +
                "LEFT JOIN invGroups ON invTypes.groupID = invGroups.groupID " +
                "WHERE ownerID=@ownerID AND locationID=@stationID AND flag=@hangarFlag",
                new Dictionary<string, object>()
                {
                    {"@ownerID", ownerID},
                    {"@stationID", stationID},
                    {"@hangarFlag", ItemFlags.Hangar}
                }
            );
        }

        public Rowset GetClonesForCharacter(int characterID, int activeCloneID)
        {
            // TODO: CACHE THIS IN A INTERMEDIATE TABLE TO MAKE THINGS EASIER TO QUERY
            return Database.PrepareRowsetQuery(
                "SELECT itemID AS jumpCloneID, typeID, locationID FROM invItems WHERE flag = @cloneFlag AND ownerID = @characterID AND itemID != @activeCloneID AND locationID IN(SELECT stationID FROM staStations)",
                new Dictionary<string, object>()
                {
                    {"@cloneFlag", ItemFlags.Clone},
                    {"@activeCloneID", activeCloneID},
                    {"@characterID", characterID}
                }
            );
        }

        public PyDataType GetClonesInShipForCharacter(int characterID)
        {
            return Database.PrepareRowsetQuery(
                "SELECT itemID AS jumpCloneID, typeID, locationID FROM invItems WHERE flag = @cloneFlag AND ownerID = @characterID AND locationID NOT IN(SELECT stationID FROM staStations)",
                new Dictionary<string, object>()
                {
                    {"@cloneFlag", ItemFlags.Clone},
                    {"@characterID", characterID}
                }
            );
        }
        
        public PyDataType GetImplantsForCharacterClones(int characterID)
        {
            return Database.PrepareRowsetQuery(
                "SELECT invItems.itemID, invItems.typeID, invItems.locationID as jumpCloneID FROM invItems LEFT JOIN invItems second ON invItems.locationID = second.itemID  WHERE invItems.flag = @implantFlag AND second.flag = @cloneFlag AND second.ownerID = @characterID",
                new Dictionary<string, object>()
                {
                    {"@characterID", characterID},
                    {"@implantFlag", ItemFlags.Implant},
                    {"@cloneFlag", ItemFlags.Clone}
                }
            );
        }

        public void DestroyItem(ItemEntity item)
        {
            Database.PrepareQuery(
                "DELETE FROM invItems WHERE itemID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", item.ID}
                }
            );
            Database.PrepareQuery(
                "DELETE FROM eveNames WHERE itemID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", item.ID}
                }
            );
            Database.PrepareQuery("DELETE FROM invItemsAttributes WHERE itemID = @itemID",
                new Dictionary<string, object>()
                {
                    {"@itemID", item.ID}
                }
            );
        }
        
        public ItemDB(DatabaseConnection db, ItemFactory factory, ClientManager clientManager, TimerManager timerManager) : base(db)
        {
            this.ItemFactory = factory;
            this.ClientManager = clientManager;
            this.TimerManager = timerManager;
        }
    }
}