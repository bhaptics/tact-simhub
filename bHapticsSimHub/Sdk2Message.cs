using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace bHapticsSimHub
{
    public static class MessageType
    {
        public const string SdkPingAll = "SdkPingAll";
        public const string SdkPlay = "SdkPlay";
        public const string SdkPlayDotMode = "SdkPlayDotMode";
        public const string SdkStopAll = "SdkStopAll";
        public const string SdkRequestAuth = "SdkRequestAuth";

        public static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
    }

    public class Sdk2Message
    {
        public string Type { get; set; } // MessageType 참조
        public string Message { get; set; }


        public static Sdk2Message SdkAuthenticationMessage(AuthenticationMessage message)
        {
            return new Sdk2Message()
            {
                Message = JsonConvert.SerializeObject(message, MessageType.SerializerSettings),
                Type = MessageType.SdkRequestAuth
            };
        }

        public static Sdk2Message SdkPlay(PlayMessage message)
        {
            return new Sdk2Message()
            {
                Message = JsonConvert.SerializeObject(message, MessageType.SerializerSettings),
                Type = MessageType.SdkPlay
            };
        }

        public static Sdk2Message SdkPlayDot(PlayDotMessage message)
        {
            return new Sdk2Message()
            {
                Message = JsonConvert.SerializeObject(message, MessageType.SerializerSettings),
                Type = MessageType.SdkPlayDotMode,
            };
        }
        public static readonly Sdk2Message SdkPingAll = new Sdk2Message()
        {
            Type = MessageType.SdkPingAll,
        };

        public static readonly Sdk2Message StopAll = new Sdk2Message()
        {
            Type = MessageType.SdkStopAll,
        };
    }

    public class AuthenticationMessage
    {
        public string SDKApiKey { get; set; }
        public string Cipher { get; set; }
        public string ApplicationId { get; set; }
        public string NonceHashValue { get; set; }
        public string ApplicationIdHashValue { get; set; }
        public int Version { get; set; } = 1;
    }

    public class PlayMessage
    {
        public string EventName { get; set; }
        public int RequestId { get; set; }
        public int Position { get; set; } = 0;
        public float Intensity { get; set; } = 1f;
        public float Duration { get; set; } = 1f;
        public float OffsetAngleX { get; set; } = 0f;
        public float OffsetY { get; set; } = 0f;
    }
    public class PlayDotMessage
    {
        public int RequestId { get; set; }
        public int Position { get; set; } = 0;
        public int[] MotorValues { get; set; } = new int[40];
        public int DurationMillis { get; set; } = 1000;
        public int DeviceIndex { get; set; } = -1;
    }
}
