using CustomWebSocketSharp;
using Newtonsoft.Json;
using System;
using System.Timers;
using WebSocket = CustomWebSocketSharp.WebSocket;

namespace bHapticsSimHub
{
    public class BhapticsWebsocket
    {
        private WebSocket _websocket;
        private Timer _timer;
        private string _appId;
        private string _apiKey;
        private string _version = "1.0.2";
        private string _url = "wss://127.0.0.1:15882/v3/feedback";
        private bool _websocketConnected = false;

        public BhapticsWebsocket(string appId, string apiKey, bool retry = true)
        {
            _appId = appId;
            _apiKey = apiKey;

            Initialize(retry);
        }

        private void TimerOnElapsed(object o, ElapsedEventArgs args)
        {
            if (!_websocketConnected)
            {
                SimHub.Logging.Current.Warn("[bHaptics] RetryConnect()");
                _websocket.Connect();
            }
            else
            {
                _timer.Stop();
            }
        }

        private void Initialize(bool tryReconnect)
        {
            if (_websocket != null)
            {
                SimHub.Logging.Current.Info("[bHaptics] Initialized");
                return;
            }

            if (tryReconnect)
            {
                _timer = new Timer(3 * 1000); // 3 sec
                _timer.Elapsed += TimerOnElapsed;
                _timer.Start();
            }

            _websocket = new WebSocket($"{_url}?workspace_id={_appId}&api_key={_apiKey}&version={_version}");
            _websocket.SslConfiguration.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
            {
                return true;
            };
            _websocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            _websocket.OnMessage += Websocket_OnMessage;
            _websocket.OnOpen += Websocket_OnOpen;
            _websocket.OnError += Websocket_OnError;
            _websocket.OnClose += Websocket_OnClose;

            _websocket.Connect();
        }

        private void Websocket_OnClose(object sender, CloseEventArgs e)
        {
            SimHub.Logging.Current.Info($"[bHaptics] ws onClose");
            _websocketConnected = false;

            if (_timer == null)
            {
                _timer = new Timer(3 * 1000); // 3 sec
                _timer.Elapsed += TimerOnElapsed;
                _timer.Start();
            }
            else
            {
                _timer.Start();
            }
        }

        private void Websocket_OnError(object sender, ErrorEventArgs e)
        {
            SimHub.Logging.Current.Info($"[bHaptics] ws onError {e.Message}");
        }

        private void Websocket_OnOpen(object sender, System.EventArgs e)
        {
            SimHub.Logging.Current.Info($"[bHaptics] ws onOpen");
            _websocketConnected = true;
            
            Send(Sdk2Message.SdkAuthenticationMessage(new AuthenticationMessage()
            {
                ApplicationId = _appId,
                ApplicationIdHashValue = _appId,
                SDKApiKey = _apiKey,
                NonceHashValue = _appId,
                Cipher = _appId
            }));
        }

        private void Websocket_OnMessage(object sender, MessageEventArgs e)
        {
            SimHub.Logging.Current.Info($"[bHaptics] ws onMessage");
        }

        public int Play(int[] motors, int duration = 80, int pos = 0)
        {
            var requestId = new Random(Guid.NewGuid().GetHashCode()).Next(int.MaxValue);

            var msg = Sdk2Message.SdkPlayDot(new PlayDotMessage()
            {
                DurationMillis = duration,
                Position = pos,
                RequestId = requestId,
                MotorValues = motors
            });

            Send(msg);

            return requestId;
        }

        public int PlaySleeves(int[] motors, bool isLeft = true, int duration = 80)
        {
            var requestId = new Random(Guid.NewGuid().GetHashCode()).Next(int.MaxValue);

            var pos = (isLeft) ? 1 : 2;

            var msg = Sdk2Message.SdkPlayDot(new PlayDotMessage()
            {
                DurationMillis = duration,
                Position = pos,
                RequestId = requestId,
                MotorValues = motors
            });

            Send(msg);

            return requestId;
        }

        public int PlayEvent(string eventName, float intensity){
            var requestId = new Random(Guid.NewGuid().GetHashCode()).Next(int.MaxValue);

            var msg = Sdk2Message.SdkPlay(new PlayMessage()
            {
                EventName = eventName,
                RequestId = requestId,
                Intensity = intensity
            });

            Send(msg);

            return requestId;
        }

        public void PingAll()
        {
            Send(Sdk2Message.SdkPingAll);
        }

        public void Stop()
        {
            Send(Sdk2Message.StopAll);
        }

        private void Send(Sdk2Message message)
        {
            try
            {
                if (!_websocketConnected)
                {
                    SimHub.Logging.Current.Info($"[bHaptics] ws not connected");
                    return;
                }

                var msg = JsonConvert.SerializeObject(message);

                //SimHub.Logging.Current.Info($"[bHaptics] send");
                _websocket.SendAsync(msg, b =>
                {
                    //SimHub.Logging.Current.Info($"[bHaptics] SendAsync Completed.");
                });
            }
            catch (Exception e)
            {
                Console.Write($"{e.Message} {e}\n");
            }
        }
    }
}
