using System;
using System.IO;
using Common.Logging;
using Common.Services;
using Node.Database;
using Node.Exceptions;
using Node.Inventory;
using Node.Inventory.Items.Types;
using Node.Network;
using PythonTypes;
using PythonTypes.Types.Database;
using PythonTypes.Types.Exceptions;
using PythonTypes.Types.Primitives;

namespace Node.Services.Chat
{
    public class LSC : Service
    {
        private MessagesDB MessagesDB { get; }
        private ChatDB DB { get; }
        private CharacterDB CharacterDB { get; }
        private Channel Log;
        private ItemManager ItemManager { get; }
        private NodeContainer NodeContainer { get; }
        
        public LSC(ChatDB db, MessagesDB messagesDB, CharacterDB characterDB, ItemManager itemManager, NodeContainer nodeContainer, Logger logger)
        {
            this.DB = db;
            this.MessagesDB = messagesDB;
            this.CharacterDB = characterDB;
            this.ItemManager = itemManager;
            this.NodeContainer = nodeContainer;
            this.Log = logger.CreateLogChannel("LSC");
        }

        private void ParseTupleChannelIdentifier(PyTuple tuple, out int channelID, out string channelType, out int? entityID)
        {
            if (tuple.Count != 1 || tuple[0] is PyTuple == false)
                throw new InvalidDataException("LSC received a wrongly formatted channel identifier");

            PyTuple channelInfo = tuple[0] as PyTuple;

            if (channelInfo.Count != 2 || channelInfo[0] is PyString == false || channelInfo[1] is PyInteger == false)
                throw new InvalidDataException("LSC received a wrongly formatted channel identifier");
            
            channelType = channelInfo[0] as PyString;
            entityID = channelInfo[1] as PyInteger;

            channelID = this.DB.GetChannelIDFromRelatedEntity((int) entityID, false);
            
            if (channelID < 0)
                throw new InvalidDataException("LSC received a wrongly formatted channel identifier (negative entityID)");
        }
        
        private void ParseChannelIdentifier(PyDataType channel, out int channelID, out string channelType, out int? entityID)
        {
            switch (channel)
            {
                case PyInteger integer:
                    channelID = integer;
                    // positive channel ids are entity ids, negatives are custom user channels
                    entityID = null;
                    if (channelID > ChatDB.MIN_CHANNEL_ENTITY_ID && channelID < ChatDB.MAX_CHANNEL_ENTITY_ID)
                        entityID = channelID;
                    // get the full channel identifier
                    channelType = ChatDB.CHANNEL_TYPE_NORMAL;
                    break;
                case PyTuple tuple:
                    this.ParseTupleChannelIdentifier(tuple, out channelID, out channelType, out entityID);
                    break;
                default:
                    throw new InvalidDataException("LSC received a wrongly formatted channel identifier");
            }
            
            // ensure the channelID is the correct one and not an entityID
            if (entityID != null)
                channelID = this.DB.GetChannelIDFromRelatedEntity((int) entityID, channelID == entityID);
                
            if (channelID == 0)
                throw new InvalidDataException("LSC could not determine chatID for the requested chats");
        }

        private void ParseChannelIdentifier(PyDataType channel, out int channelID, out string channelType)
        {
            this.ParseChannelIdentifier(channel, out channelID, out channelType, out _);
        }

        public PyDataType GetChannels(CallInformation call)
        {
            return this.DB.GetChannelsForCharacter(call.Client.EnsureCharacterIsSelected(), call.Client.CorporationID);
        }

        public PyDataType GetChannels(PyInteger reload, CallInformation call)
        {
            return this.GetChannels(call);
        }

        public PyDataType GetMembers(PyDataType channel, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            int channelID;
            string channelType;

            try
            {
                this.ParseChannelIdentifier(channel, out channelID, out channelType);
            }
            catch (InvalidDataException)
            {
                Log.Error("Error parsing channel identifier for GetMembers");
                return null;
            }
            
            return this.DB.GetChannelMembers(channelID, callerCharacterID);
        }

        private static PyDataType GenerateLSCNotification(string type, PyDataType channel, PyTuple args, Client client)
        {
            PyTuple who = new PyTuple(6)
            {
                [0] = client.AllianceID,
                [1] = client.CorporationID,
                [2] = client.EnsureCharacterIsSelected(),
                [3] = client.Role,
                [4] = client.CorporationRole,
                [5] = client.WarFactionID
            };

            // this could also be a list having senderID, senderName and senderType in that order
            return new PyTuple(5)
            {
                [0] = channel,
                [1] = 1,
                [2] = type,
                [3] = who,
                [4] = args
            };
        }

        private PyTuple GetChannelInformation(string channelType, int channelID, int? entityID, int callerCharacterID, PyDataType channelIDExtended, CallInformation call)
        {
            Row info;

            if (channelID < ChatDB.MIN_CHANNEL_ENTITY_ID || entityID == null || channelID >= ChatDB.MAX_CHANNEL_ENTITY_ID)
                info = this.DB.GetChannelInfo(channelID, callerCharacterID);
            else
                info = this.DB.GetChannelInfoByRelatedEntity((int) entityID, callerCharacterID, channelID == entityID);

            // check if the channel must include the list of members
            PyInteger actualChannelID = info.Line[0] as PyInteger;
            PyString displayName = info.Line[2] as PyString;
            string typeValue = this.DB.ChannelNameToChannelType(displayName);
            Rowset mods = null;
            Rowset chars = null;
            
            if (typeValue != ChatDB.CHANNEL_TYPE_REGIONID && typeValue != ChatDB.CHANNEL_TYPE_CONSTELLATIONID && typeValue != ChatDB.CHANNEL_TYPE_SOLARSYSTEMID2)
            {
                mods = this.DB.GetChannelMods(actualChannelID);
                chars = this.DB.GetChannelMembers(actualChannelID, callerCharacterID);
                
                // the extra field is at the end
                int extraIndex = chars.Header.Count - 1;
        
                // ensure they all have the owner information
                foreach (PyList row in chars.Rows)
                {
                    // fill it with information
                    row[extraIndex] = this.DB.GetExtraInfo(row[0] as PyInteger);
                }    
            }
            else
            {
                // build empty rowsets for channels that should not reveal anyone unless they talk
                mods = new Rowset(new PyDataType[]
                    {"accessor", "mode", "untilWhen", "originalMode", "admin", "reason"});
                chars = new Rowset(new PyDataType[]
                    {"charID", "corpID", "allianceID", "warFactionID", "role", "extra"});
            }

            return new PyTuple(3)
            {
                [0] = channelIDExtended,
                [1] = 1,
                [2] = new PyTuple(3)
                {
                    [0] = info,
                    [1] = mods,
                    [2] = chars
                }
            };
        }
        
        public PyDataType JoinChannels(PyList channels, PyInteger role, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            PyList result = new PyList();

            foreach (PyDataType channel in channels)
            {
                int channelID;
                string channelType;
                int? entityID;
                PyDataType channelIDExtended = null;

                try
                {
                    this.ParseChannelIdentifier(channel, out channelID, out channelType, out entityID);
                }
                catch (InvalidDataException)
                {
                    throw new LSCCannotJoin("The specified channel cannot be found: " + PrettyPrinter.FromDataType(channel));
                }

                if (channelType == ChatDB.CHANNEL_TYPE_NORMAL)
                    channelIDExtended = channelID;
                else
                {
                    channelIDExtended = new PyTuple(1)
                    {
                        [0] = new PyTuple(2)
                        {
                            [0] = channelType,
                            [1] = entityID
                        }
                    };
                }
                
                // send notifications only on channels that should be receiving notifications
                // we don't want people in local to know about players unless they talk there
                if (channelType != ChatDB.CHANNEL_TYPE_REGIONID && channelType != ChatDB.CHANNEL_TYPE_CONSTELLATIONID && channelType != ChatDB.CHANNEL_TYPE_SOLARSYSTEMID2)
                {
                    PyDataType notification =
                        GenerateLSCNotification("JoinChannel", channelIDExtended, new PyTuple(0), call.Client);

                    if (channelType == ChatDB.CHANNEL_TYPE_NORMAL)
                    {
                        if (channelID < ChatDB.MIN_CHANNEL_ENTITY_ID)
                        {
                            // get users in the channel that are online now
                            PyList characters = this.DB.GetOnlineCharsOnChannel(channelID);

                            // notify them all
                            call.Client.ClusterConnection.SendNotification("OnLSC", "charid", characters, notification);  
                        }
                    }
                    else
                    {
                        // notify all players on the channel
                        call.Client.ClusterConnection.SendNotification("OnLSC", channelType, new PyDataType [] { entityID }, notification);                            
                    }
                }

                try
                {
                    result.Add(
                        this.GetChannelInformation(
                            channelType, channelID, entityID, callerCharacterID, channelIDExtended, call
                        )
                    );
                }
                catch (Exception e)
                {
                    // most of the time this indicates a destroyed channel
                    // so build a destroy notification and let the client know this channel
                    // can be removed from it's lists
                    if (channelType == ChatDB.CHANNEL_TYPE_NORMAL && channelID != entityID)
                    {
                        // notify everyone in the channel only when it should
                        PyDataType notification =
                            GenerateLSCNotification("DestroyChannel", channelID, new PyTuple(0), call.Client);

                        // notify all characters in the channel
                        call.Client.ClusterConnection.SendNotification("OnLSC", "charid", callerCharacterID, call.Client, notification);
                    }
                    
                    Log.Error($"LSC could not get channel information. Error: {e.Message}");
                }
            }
            
            return result;
        }

        public PyDataType GetMyMessages(CallInformation call)
        {
            return this.MessagesDB.GetMailHeaders(call.Client.EnsureCharacterIsSelected());
        }
        
        public PyDataType GetRookieHelpChannel(CallInformation call)
        {
            return ChatDB.CHANNEL_ROOKIECHANNELID;
        }

        public PyDataType SendMessage(PyDataType channel, PyString message, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            int channelID;
            int? entityID;
            string channelType;

            try
            {
                this.ParseChannelIdentifier(channel, out channelID, out channelType, out entityID);
            }
            catch (InvalidDataException)
            {
                throw new LSCCannotSendMessage("Cannot get channel information");
            }

            // ensure the player is allowed to chat in there
            if (channelType == ChatDB.CHANNEL_TYPE_NORMAL && this.DB.IsPlayerAllowedToChat(channelID, callerCharacterID) == false)
                throw new LSCCannotSendMessage("Insufficient permissions");
            if (channelType != ChatDB.CHANNEL_TYPE_NORMAL && this.DB.IsPlayerAllowedToChatOnRelatedEntity((int) entityID, callerCharacterID) == false)
                throw new LSCCannotSendMessage("Insufficient permissions");

            PyTuple notificationBody = new PyTuple(1) {[0] = message};
            
            if (channelType == ChatDB.CHANNEL_TYPE_NORMAL)
            {
                PyDataType notification =
                    GenerateLSCNotification("SendMessage", channelID, notificationBody, call.Client);

                call.Client.ClusterConnection.SendNotification(
                    "OnLSC",
                    "charid",
                    this.DB.GetOnlineCharsOnChannel(channelID),
                    notification
                );
            }
            else
            {
                PyTuple identifier = new PyTuple(1)
                {
                    [0] = new PyTuple(2)
                    {
                        [0] = channelType,
                        [1] = entityID
                    }
                };
                
                PyDataType notification =
                    GenerateLSCNotification("SendMessage", identifier, notificationBody, call.Client);

                call.Client.ClusterConnection.SendNotification(
                    "OnLSC",
                    channelType,
                    new PyDataType [] { entityID },
                    notification
                );

            }

            return null;
        }

        public PyDataType LeaveChannels(PyList channels, PyDataType boolUnsubscribe, PyInteger role, CallInformation call)
        {
            foreach (PyDataType channelInfo in channels)
            {
                if (channelInfo is PyTuple == false)
                {
                    Log.Error("LSC received a channel identifier in LeaveChannels that doesn't resemble anything we know");
                    continue;
                }

                PyTuple tuple = channelInfo as PyTuple;

                if (tuple.Count != 2)
                {
                    Log.Error(
                        "LSC received a tuple for channel in LeaveChannels that doesn't resemble anything we know");
                    return null;
                }

                PyDataType channelID = tuple[0];
                PyBool announce = tuple[1] as PyBool;

                this.LeaveChannel(channelID, announce, call);
            }
            
            return null;
        }
        
        public PyDataType LeaveChannel(PyDataType channel, PyInteger announce, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            int channelID;
            string channelType;

            try
            {
                this.ParseChannelIdentifier(channel, out channelID, out channelType);
            }
            catch (InvalidDataException ex)
            {
                Log.Error("Error parsing channel identifier for LeaveChannel");
                return null;
            }

            // make sure the character is actually in the channel
            if (this.DB.IsCharacterMemberOfChannel(channelID, callerCharacterID) == false)
                return null;
            
            if (channelType != ChatDB.CHANNEL_TYPE_CORPID && channelType != ChatDB.CHANNEL_TYPE_SOLARSYSTEMID2 && announce == 1)
            {
                // notify everyone in the channel only when it should
                PyDataType notification =
                    GenerateLSCNotification("LeaveChannel", channel, new PyTuple(0), call.Client);
                
                if (channelType != ChatDB.CHANNEL_TYPE_NORMAL)
                    call.Client.ClusterConnection.SendNotification("OnLSC", channelType, new PyDataType [] { channel }, notification);
                else
                {
                    // get users in the channel that are online now
                    PyList characters = this.DB.GetOnlineCharsOnChannel(channelID);

                    call.Client.ClusterConnection.SendNotification("OnLSC", "charid", characters, notification);
                }
            }

            return null;
        }

        public PyDataType CreateChannel(PyString name, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            if (name.Length > 60)
                throw new ChatCustomChannelNameTooLong(60);

            bool mailingList = false;

            if (call.NamedPayload.ContainsKey("mailingList") == true)
                mailingList = call.NamedPayload["mailingList"] as PyBool;
            
            // create the channel in the database
            int channelID = (int) this.DB.CreateChannel(callerCharacterID, null, "Private Channel\\" + name, mailingList);
            
            // join the character to this channel
            this.DB.JoinChannel(channelID, callerCharacterID, ChatDB.CHATROLE_CREATOR);
            
            Rowset mods = this.DB.GetChannelMods(channelID);
            Rowset chars = this.DB.GetChannelMembers(channelID, callerCharacterID);
                        
            // the extra field is at the end
            int extraIndex = chars.Header.Count - 1;
                
            // ensure they all have the owner information
            foreach (PyList row in chars.Rows)
            {
                // fill it with information
                row[extraIndex] = this.DB.GetExtraInfo(row[0] as PyInteger);
            }
            
            // retrieve back the information about the characters as there is ONE character in here
            // return the normal channel information
            return new PyTuple(3)
            {
                [0] = this.DB.GetChannelInfo(channelID, callerCharacterID),
                [1] = mods,
                [2] = chars
            };
        }

        public PyDataType DestroyChannel(PyInteger channelID, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            // ensure the character has enough permissions
            if (this.DB.IsCharacterAdminOfChannel(channelID, callerCharacterID) == false)
                throw new LSCCannotDestroy("Insufficient permissions");

            // get users in the channel that are online now
            PyList characters = this.DB.GetOnlineCharsOnChannel(channelID);
            
            // remove channel off the database
            this.DB.DestroyChannel(channelID);
            
            // notify everyone in the channel only when it should
            PyDataType notification =
                GenerateLSCNotification("DestroyChannel", channelID, new PyTuple(0), call.Client);

            // notify all characters in the channel
            call.Client.ClusterConnection.SendNotification("OnLSC", "charid", characters, notification);

            return null;
        }

        public PyDataType AccessControl(PyInteger channelID, PyInteger characterID, PyInteger accessLevel, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            if (this.DB.IsCharacterOperatorOrAdminOfChannel(channelID, callerCharacterID) == false)
                throw new LSCCannotAccessControl("Insufficient permissions");

            this.DB.UpdatePermissionsForCharacterOnChannel(channelID, characterID, accessLevel);

            PyTuple args = new PyTuple(6)
            {
                [0] = characterID,
                [1] = accessLevel,
                [2] = new PyNone(),
                [3] = accessLevel,
                [4] = "",
                [5] = accessLevel == ChatDB.CHATROLE_CREATOR
            };
            
            PyDataType notification = GenerateLSCNotification("AccessControl", channelID, args, call.Client);
            
            // get users in the channel that are online now
            PyList characters = this.DB.GetOnlineCharsOnChannel(channelID);

            call.Client.ClusterConnection.SendNotification("OnLSC", "charid", characters, notification);
            
            // TODO: CHECK IF THIS IS A CHARACTER'S ADDRESS BOOK AND CHECK FOR OTHER CHARACTER'S ADDRESSBOOK STATUS
            // TODO: TO ENSURE THEY DON'T HAVE US BLOCKED
            
            return null;
        }

        public PyDataType ForgetChannel(PyInteger channelID, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            // announce leaving, important in private channels
            PyDataType notification =
                GenerateLSCNotification("LeaveChannel", channelID, new PyTuple(0), call.Client);
            
            // get users in the channel that are online now
            PyList characters = this.DB.GetOnlineCharsOnChannel(channelID);

            call.Client.ClusterConnection.SendNotification("OnLSC", "charid", characters, notification);

            this.DB.LeaveChannel(channelID, callerCharacterID);
            
            return null;
        }

        public void InviteAnswerCallback(RemoteCall callInfo, PyDataType result)
        {
            InviteExtraInfo call = callInfo.ExtraInfo as InviteExtraInfo;

            if (result is PyString answer)
            {
                // this user's character might not be in the service
                // so fetch the name from the database
                call.OriginalCall.Client.SendException(
                    call.OriginalCall,
                    new UserError(
                        answer,
                        new PyDictionary
                        {
                            ["channel"] = this.DB.GetChannelName(call.ChannelID),
                            ["char"] = this.CharacterDB.GetCharacterName(call.ToCharacterID)
                        }
                    )
                );
            } 
            
            // return an empty response to the original calling client, this should get mechanism going for the JoinChannel notification
            call.OriginalCall.Client.SendCallResponse(call.OriginalCall, null);
            
            // character has accepted, notify all users of the channel
            string channelType = this.DB.GetChannelType(call.ChannelID);
            
            PyDataType notification =
                GenerateLSCNotification("JoinChannel", call.ChannelID, new PyTuple(0), callInfo.Client);

            // you should only be able to invite to global channels as of now
            // TODO: CORP CHANNELS SHOULD BE SUPPORTED TOO
            if (channelType == ChatDB.CHANNEL_TYPE_NORMAL)
            {
                // get users in the channel that are online now
                PyList characters = this.DB.GetOnlineCharsOnChannel(call.ChannelID);

                // notify them all
                call.OriginalCall.Client.ClusterConnection.SendNotification("OnLSC", "charid", characters, notification);  
            }
        }

        public void InviteTimeoutCallback(RemoteCall callInfo)
        {
            // if the call timed out the character is not connected
            InviteExtraInfo call = callInfo.ExtraInfo as InviteExtraInfo;

            call.OriginalCall.Client.SendException(
                call.OriginalCall,
                new ChtCharNotReachable(this.CharacterDB.GetCharacterName(call.ToCharacterID))
            );
        }

        public PyDataType Invite(PyInteger characterID, PyInteger channelID, PyString channelTitle, PyBool addAllowed, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();

            try
            {
                if (characterID == callerCharacterID)
                    throw new ChtCannotInviteSelf();
                
                if (ItemManager.IsNPC(characterID) == true)
                {
                    // NPCs should be loaded, so the character instance can be used willy-nilly
                    Character npcChar = this.ItemManager.GetItem(characterID) as Character;

                    throw new ChtNPC(npcChar.Name);
                }
                
                // ensure our character has admin perms first
                if (this.DB.IsCharacterOperatorOrAdminOfChannel(channelID, callerCharacterID) == false)
                    throw new ChtWrongRole(this.DB.GetChannelName(channelID), "Operator");

                // ensure the character is not there already
                if (this.DB.IsCharacterMemberOfChannel(channelID, characterID) == true)
                    throw new ChtAlreadyInChannel(this.CharacterDB.GetCharacterName(characterID));
                
                Character character = this.ItemManager.GetItem(callerCharacterID) as Character;

                PyTuple args = new PyTuple(4)
                {
                    [0] = callerCharacterID,
                    [1] = character.Name,
                    [2] = character.Gender,
                    [3] = channelID
                };

                InviteExtraInfo info = new InviteExtraInfo
                {
                    OriginalCall = call,
                    Arguments = args,
                    ChannelID = channelID,
                    ToCharacterID = characterID,
                    FromCharacterID = callerCharacterID
                };
                
                // no timeout for this call
                call.Client.ClusterConnection.SendServiceCall(
                    characterID,
                    "LSC", "ChatInvite", args, new PyDictionary(),
                    InviteAnswerCallback, InviteTimeoutCallback,
                    info, ProvisionalResponse.DEFAULT_TIMEOUT - 5
                );
                
                // subscribe the user to the chat
                this.DB.JoinChannel(channelID, characterID);
            }
            catch (ArgumentOutOfRangeException)
            {
                Log.Warning("Trying to invite a non-online character, aborting...");
            }

            // return SOMETHING to the client with the provisional data
            // the real answer will come later on
            throw new ProvisionalResponse(new PyString("OnDummy"), new PyTuple(0));
        }

        public PyDataType Page(PyList destinationMailboxes, PyString subject, PyString message, CallInformation call)
        {
            int callerCharacterID = call.Client.EnsureCharacterIsSelected();
            
            foreach (PyDataType destinationID in destinationMailboxes)
            {
                // only mail to integer channel id's are supported
                if (destinationID is PyInteger == false)
                    continue;
                
                ulong messageID = this.MessagesDB.StoreMail(destinationID as PyInteger, callerCharacterID, subject, message, out string mailboxType);
                
                // send notification to the destination
                PyTuple notification = new PyTuple(5)
                {
                    [0] = destinationMailboxes,
                    [1] = messageID,
                    [2] = callerCharacterID,
                    [3] = subject,
                    [4] = DateTime.UtcNow.ToFileTimeUtc()
                };
                
                // *multicastID are a special broadcast type that allows to notify different users based on things like charid or corpid
                // under the same notification, making things easier for us
                // sadly supporting that is more painful that actually spamming the cluster controller with single corpid or charid type broadcast
                // but supporting multicastIDs would be perfect
                
                // the list of id's on a *multicastID would be a PyTuple with the type and the id in it, instead of just a list of integers
                call.Client.ClusterConnection.SendNotification("OnMessage", mailboxType, destinationID as PyInteger, notification);
            }
            
            return null;
        }

        public PyDataType GetMessageDetails(PyInteger channelID, PyInteger messageID, CallInformation call)
        {
            // ensure the player is allowed to read messages off this mail list
            if (this.DB.IsPlayerAllowedToRead(channelID, call.Client.EnsureCharacterIsSelected()) == false)
                return null;
            
            return this.MessagesDB.GetMessageDetails(channelID, messageID);
        }

        public PyDataType MarkMessagesRead(PyList messageIDs, CallInformation call)
        {
            // TODO: CHECK FOR PERMISSIONS ON lscChannelPermissions
            foreach (PyDataType messageID in messageIDs)
            {
                if (messageID is PyInteger == false)
                    continue;
                
                this.MessagesDB.MarkMessagesRead(call.Client.EnsureCharacterIsSelected(), messageID as PyInteger);
            }
            
            return null;
        }
        
        class InviteExtraInfo
        {
            public CallInformation OriginalCall { get; set; }
            public int FromCharacterID { get; set; }
            public int ToCharacterID { get; set; }
            public int ChannelID { get; set; }
            public PyTuple Arguments { get; set; }
        }
    }
}