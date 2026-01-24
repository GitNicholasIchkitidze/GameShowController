// Heplers/RedisTtlProvider.cs
using GameController.FBService.Models;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace GameController.FBService.Heplers
{
	public interface IPayloadHelper
    {
        string ExtractVoteName(string payload);
        string? ExtractMessageId(JsonObject payload, string? messageType = null);
        string? ExtractMessageType(JsonObject payload);
        string? ExtractMessageSenderOrRecipientId(JsonObject payload, string type = "sender");
        string? ExtractMessagePostbackPayLoad(JsonObject payload);
        string? ExtractMessageText(JsonObject payload);
    }

	public class PayloadHelper : IPayloadHelper
    {
		

		public PayloadHelper()
		{
			
		}




        /// <summary>
        /// Extracts vote name from payload using your existing pattern.
        /// </summary>
        public string ExtractVoteName(string payload)
        {
            // legacy: sometimes payload contains "_" and ":" patterns
            var voteName = payload.Split('_').Last();
            voteName = payload.Split(':').First();
            return voteName;
        }


        public string? ExtractMessageId(JsonObject payload, string? messageType = null)
        {
            try
            {
                var entry = payload["entry"]?.AsArray()?.FirstOrDefault()?.AsObject();
                var messaging = entry?["messaging"]?.AsArray()?.FirstOrDefault()?.AsObject();
                if (messageType != null && messageType == "postback")
                {
                    var postback = messaging?["postback"]?.AsObject();
                    return postback?["mid"]?.GetValue<string>();
                }
                else
                {
                    var message = messaging?["message"]?.AsObject();
                    return message?["mid"]?.GetValue<string>();
                }
            }
            catch
            {
                return null;
            }
        }
        public string? ExtractMessageType(JsonObject payload)
        {
            // Navigate to the core messaging event object
            var messagingEvent = payload?["entry"]?.AsArray().FirstOrDefault()?
                                        .AsObject()?["messaging"]?.AsArray().FirstOrDefault()?
                                        .AsObject();

            if (messagingEvent == null)
            {
                return null;
            }

            // Check for the presence of known event type keys
            if (messagingEvent.ContainsKey("postback"))
            {
                return "postback";
            }

            if (messagingEvent.ContainsKey("message"))
            {
                return "message";
            }

            if (messagingEvent.ContainsKey("reaction"))
            {
                return "reaction";
            }

            if (messagingEvent.ContainsKey("read"))
            {
                return "read";
            }

            // Return null or "unknown" if no known type is found
            return null;
        }

        public string? ExtractMessageSenderOrRecipientId(JsonObject payload, string type = "sender")
        {
            try
            {
                var entry = payload["entry"]?.AsArray()?.FirstOrDefault()?.AsObject();
                var messaging = entry?["messaging"]?.AsArray()?.FirstOrDefault()?.AsObject();
                var sender = messaging?[type]?.AsObject();
                return sender?["id"]?.GetValue<string>();
            }
            catch
            {
                return null;
            }
        }

        public string? ExtractMessagePostbackPayLoad(JsonObject payload)
        {
            try

            {
                var entry = payload["entry"]?.AsArray()?.FirstOrDefault()?.AsObject();
                var messaging = entry?["messaging"]?.AsArray()?.FirstOrDefault()?.AsObject();
                var postback = messaging?["postback"]?.AsObject();
                return postback?["payload"]?.GetValue<string>();

            }
            catch
            {
                return null;
            }
        }

        public string? ExtractMessageText(JsonObject payload)
        {
            try

            {
                var entry = payload["entry"]?.AsArray()?.FirstOrDefault()?.AsObject();
                var messaging = entry?["messaging"]?.AsArray()?.FirstOrDefault()?.AsObject();
                var text = messaging?["message"]?.AsObject();
                return text?["text"]?.GetValue<string>();

            }
            catch
            {
                return null;
            }
        }


    }
}
