using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
//https://m2mqtt.wordpress.com/using-mqttclient/
namespace MQTT_Test
{
    class Program
    {
        static string channel = "BaltazarBerg";
        static void Main(string[] args)
        {
            MqttClient client = new MqttClient(IPAddress.Parse("18.184.104.180"));

            // register to message received
            //choose Method- when receiving a message 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;


            //random clientID generator
            var clientId = Guid.NewGuid().ToString();
            Console.WriteLine(clientId);
            // possible to set more parameters like cleansession 
            client.Connect(clientId);

            // subscribe to topic - get channel and qualtity of serice = 2 
            client.Subscribe(
                new string[] { channel },
                new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            //send message to channel
            client.Publish(channel, Encoding.UTF8.GetBytes("Oh Baltazar!"));

        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received, get message as bitarray e -> msg string
            string msg = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            Console.WriteLine("message = " + msg);
        }
    }
}
