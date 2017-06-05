using System;
using System.Collections.Generic;
using OpenPop.Mime;
using OpenPop.Pop3;

namespace ResumeRepairApplication.Common
{
    /// <summary>
    /// 
    /// </summary>
    public class EmailFactory
    {
        /// <summary>
        /// Get the unread messages from service which from specify address.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="useSsl"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="address"></param>
        /// <param name="seenUids"></param>
        /// <returns></returns>
        public static Dictionary<string, Message> FetchUnseenMessages(string hostname, int port, bool useSsl, string username, string password, string address, List<string> seenUids)
        {
            var messages = new Dictionary<string, Message>();

            try
            {
                using (var client = new Pop3Client())
                {
                    client.Connect(hostname, port, useSsl);

                    client.Authenticate(username, password);

                    var count = client.GetMessageCount();

                    for (var i = 1; i <= count; i++)
                    {
                        var header = client.GetMessageHeaders(i);

                        var uid = client.GetMessageUid(i);

                        if (seenUids.Contains(uid))
                        {
                            continue;
                        }

                        if (header.From.Address == address)
                        {
                            messages.Add(uid, client.GetMessage(i));
                        }
                    }
                }

                return messages;
            }
            catch (Exception)
            {
                return messages;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="useSsl"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static List<Message> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {
            // The client disconnects from the server when being disposed

            using (var client = new Pop3Client())
            {
                // Connect to the server

                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server

                client.Authenticate(username, password);

                //client.Authenticate(username, password, AuthenticationMethod.UsernameAndPassword);

                // Get the number of messages in the inbox

                var messageCount = client.GetMessageCount();

                // We want to download all messages

                var allMessages = new List<Message>(messageCount);

                // Messages are numbered in the interval: [1, messageCount]

                // Ergo: message numbers are 1-based.

                // Most servers give the latest message the highest number

                for (var i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                }

                // Now return the fetched messages

                return allMessages;
            }
        }

        /// <summary>
        /// Example showing:
        ///  - how to use UID's (unique ID's) of messages from the POP3 server
        ///  - how to download messages not seen before
        ///    (notice that the POP3 protocol cannot see if a message has been read on the server
        ///     before. Therefore the client need to maintain this state for itself)
        /// </summary>
        /// <param name="hostname">Hostname of the server. For example: pop3.live.com</param>
        /// <param name="port">Host port to connect to. Normally: 110 for plain POP3, 995 for SSL POP3</param>
        /// <param name="useSsl">Whether or not to use SSL to connect to server</param>
        /// <param name="username">Username of the user on the server</param>
        /// <param name="password">Password of the user on the server</param>
        /// <param name="seenUids">
        /// List of UID's of all messages seen before.
        /// New message UID's will be added to the list.
        /// Consider using a HashSet if you are using >= 3.5 .NET
        /// </param>
        /// <returns>A List of new Messages on the server</returns>
        public static List<EmailMessage> FetchUnseenMessages(string hostname, int port, bool useSsl, string username, string password, List<string> seenUids)
        {
            // The client disconnects from the server when being disposed

            using (var client = new Pop3Client())
            {
                // Connect to the server

                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server

                client.Authenticate(username, password);

                // Fetch all the current uids seen

                var uids = client.GetMessageUids();

                // Create a list we can return with all new messages

                var newMessages = new List<EmailMessage>();

                // All the new messages not seen by the POP3 client

                for (var i = 0; i < uids.Count; i++)
                {
                    var currentUidOnServer = uids[i];

                    if (!seenUids.Contains(currentUidOnServer))
                    {
                        // We have not seen this message before.

                        // Download it and add this new uid to seen uids

                        // the uids list is in messageNumber order - meaning that the first

                        // uid in the list has messageNumber of 1, and the second has 

                        // messageNumber 2. Therefore we can fetch the message using

                        // i + 1 since messageNumber should be in range [1, messageCount]

                        var unseenMessage = client.GetMessage(i + 1);

                        // Add the message to the new messages

                        newMessages.Add(new EmailMessage { message = unseenMessage, messageId = currentUidOnServer });

                        // Add the uid to the seen uids, as it has now been seen

                        seenUids.Add(currentUidOnServer);
                    }
                }

                // Return our new found messages

                return newMessages;
            }
        }

        /// <summary>
        /// Example showing:
        ///  - how to delete fetch an emails headers only
        ///  - how to delete a message from the server
        /// </summary>
        /// <param name="client">A connected and authenticated Pop3Client from which to delete a message</param>
        /// <param name="messageId">A message ID of a message on the POP3 server. Is located in <see cref="MessageHeader.MessageId"/></param>
        /// <returns><see langword="true"/> if message was deleted, <see langword="false"/> otherwise</returns>
        public static bool DeleteMessageByMessageId(string hostname, int port, bool useSsl, string username, string password, string messageId)
        {
            using (var client = new Pop3Client())
            {
                // Connect to the server

                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server

                client.Authenticate(username, password);

                // Get the number of messages on the POP3 server
                int messageCount = client.GetMessageCount();

                // Run trough each of these messages and download the headers
                for (int messageItem = messageCount; messageItem > 0; messageItem--)
                {
                    // If the Message ID of the current message is the same as the parameter given, delete that message
                    if (client.GetMessageHeaders(messageItem).MessageId == messageId)
                    {
                        // Delete
                        client.DeleteMessage(messageItem);
                        return true;
                    }
                }
            }

            // We did not find any message with the given messageId, report this back
            return false;
        }

    }

    public class EmailMessage
    {
        public string messageId { get; set; }
        public Message message { get; set; }
    }
}