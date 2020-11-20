/*
    ------------------------------------------------------------------------------------
    LICENSE:
    ------------------------------------------------------------------------------------
    This file is part of EVE#: The EVE Online Server Emulator
    Copyright 2012 - Glint Development Group
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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Common.Database;
using Common.Logging;
using Common.Services;
using Node.Data;
using Node.Database;
using Node.Inventory.Items;
using PythonTypes.Types.Exceptions;
using PythonTypes.Types.Primitives;

namespace Node.Services.Characters
{
    public class character : Service
    {
        enum NameValidationResults
        {
            Valid = 1,
            TooShort = -1,
            TooLong = -2,
            IllegalCharacters = -5,
            MoreThanOneSpace = -6,
            Taken = -101,
            Banned = -102
        };

        private readonly CharacterDB mDB = null;
        private readonly Dictionary<int, Bloodline> mBloodlineCache = null;
        private readonly Dictionary<int, Ancestry> mAncestriesCache = null;
        private readonly Configuration.Character mConfiguration = null;
        private readonly Channel Log = null;

        public character(DatabaseConnection db, Configuration.Character configuration, ServiceManager manager) : base(manager)
        {
            this.Log = manager.Container.Logger.CreateLogChannel("character");
            this.mConfiguration = configuration;
            this.mDB = new CharacterDB(db, manager.Container.ItemFactory);
            this.mBloodlineCache = this.mDB.GetBloodlineInformation();
            this.mAncestriesCache = this.mDB.GetAncestryInformation(this.mBloodlineCache);
        }

        public PyDataType GetCharactersToSelect(PyDictionary namedPayload, Client client)
        {
            return this.mDB.GetCharacterList(client.AccountID);
        }

        public PyDataType LogStartOfCharacterCreation(PyDictionary namedPayload, Client client)
        {
            return null;
        }

        public PyDataType GetCharCreationInfo(PyDictionary namedPayload, Client client)
        {
            return this.ServiceManager.CacheStorage.GetHints(CacheStorage.CreateCharacterCacheTable);
        }

        public PyDataType GetAppearanceInfo(PyDictionary namedPayload, Client client)
        {
            return this.ServiceManager.CacheStorage.GetHints(CacheStorage.CharacterAppearanceCacheTable);
        }

        public PyDataType GetCharNewExtraCreationInfo(PyDictionary namedPayload, Client client)
        {
            return new PyDictionary();
        }

        public PyInteger ValidateNameEx(PyString name, PyDictionary namedPayload, Client client)
        {
            string characterName = name;
            
            if (characterName.Length < 3)
                return new PyInteger((int) NameValidationResults.TooShort);

            // character name length is maximum 24 characters based on the error messages used for the user
            if (characterName.Length > 24)
                return new PyInteger((int) NameValidationResults.TooLong);

            // ensure only alphanumeric characters and/or spaces are used
            if (Regex.IsMatch(characterName, "^[a-zA-Z0-9 ]*$") == false)
                return new PyInteger((int) NameValidationResults.IllegalCharacters);

            // no more than one space allowed
            if (characterName.IndexOf(' ') != characterName.LastIndexOf(' '))
                return new PyInteger((int) NameValidationResults.MoreThanOneSpace);

            // ensure there is no character registered with this name already
            if (this.mDB.IsCharacterNameTaken(characterName) == true)
                return new PyInteger((int) NameValidationResults.Taken);

            // TODO: IMPLEMENT BANLIST OF WORDS
            return new PyInteger((int) NameValidationResults.Valid);
        }

        public PyDataType CreateCharacter2(
            PyString characterName, PyInteger bloodlineID, PyInteger genderID, PyInteger ancestryID,
            PyDictionary appearance, PyDictionary namedPayload, Client client)
        {
            int validationError = this.ValidateNameEx(characterName, null, client);

            // ensure the name is valid
            switch (validationError)
            {
                case (int) NameValidationResults.TooLong:
                    throw new UserError("CharNameInvalidMaxLength");
                case (int) NameValidationResults.Taken:
                    throw new UserError("CharNameInvalidTaken");
                case (int) NameValidationResults.IllegalCharacters:
                    throw new UserError("CharNameInvalidSomeChar");
                case (int) NameValidationResults.TooShort:
                    throw new UserError("CharNameInvalidMinLength");
                case (int) NameValidationResults.MoreThanOneSpace:
                    throw new UserError("CharNameInvalidMaxSpaces");
                case (int) NameValidationResults.Banned:
                    throw new UserError("CharNameInvalidBannedWord");
                case (int) NameValidationResults.Valid:
                    break;
                default:
                    // unknown actual error, return generic error
                    throw new UserError("CharNameInvalid");
            }
            
            // get the System item's id
            int systemItemID = this.ServiceManager.Container.Constants["locationSystem"];

            // load the item into memory
            ItemEntity owner = this.ServiceManager.Container.ItemFactory.ItemManager.LoadItem(systemItemID);
            
            // load bloodline and ancestry info for the requested character
            Bloodline bloodline = this.mBloodlineCache[bloodlineID];
            Ancestry ancestry = this.mAncestriesCache[ancestryID];
            long currentTime = DateTime.UtcNow.ToFileTimeUtc();
            
            // TODO: DETERMINE SCHOOLID, CARREERID AND CAREERSPECIALITYID PROPERLY

            if (ancestry.Bloodline != bloodline)
            {
                Log.Error($"The ancestry {ancestryID} doesn't belong to the given bloodline {bloodlineID}");
                
                throw new UserError("BannedBloodline", 
                    new PyDictionary ()
                    {
                        {"name", ancestry.Name},
                        {"bloodlineName", bloodline.Name}
                    }
                );
            }

            /*
            this.mDB.CreateCharacter(
                bloodline.ItemType, client.AccountID, this.mConfiguration.Balance, 0.0, bloodline.CorporationID,
                0, 0, 0, 0, 0, currentTime, currentTime,
                currentTime, ancestryID, 11, 0, 11, genderID,
            */
            
            // TODO: FINISH THIS METHOD
            return null;
        }

        public PyDataType Ping(PyDictionary namedPayload, Client client)
        {
            return client.AccountID;
        }
    }
}