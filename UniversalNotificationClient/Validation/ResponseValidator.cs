using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UniversalNotificationClient.DataExchange;

namespace UniversalNotificationClient.Validation
{
    internal static class ResponseValidator
    {
        public static WebSocketOption ValidateOption(byte[] array)
        {
            WebSocketOption newOption = 
                Regex.IsMatch(Encoding.UTF8.GetString(array), "^HTTP", RegexOptions.IgnoreCase) 
                ? WebSocketOption.Handshake 
                : array[0].GetOption();

            //if (newOption != WebSocketOption.ConnectionClose && newOption != option)
            //{
            //    throw new ArgumentException($"Requested option: {option} is Not valid to: {newOption}");
            //}

            return newOption;
        }

        public static bool ValidateResponse(NotificationResponse response, NotificationRequest request)
        {
            if (response.Option == WebSocketOption.Handshake)
            {
                string responseSwk = Regex.Match(response.Payload, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();

                string swk = Regex.Match(request.Payload, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string requestSwk = Convert.ToBase64String(swkaSha1);

                return requestSwk == responseSwk;
            }

            return true;
        }
    }
}
