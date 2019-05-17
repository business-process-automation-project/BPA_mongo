using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


namespace BPA_Version1
{
    class Program
    {
        public static MongoClient client = new MongoClient("mongodb://localhost:27017");
        public static IMongoDatabase  database = client.GetDatabase("BPA");
        public static IMongoCollection<BsonDocument> Qcollection = database.GetCollection<BsonDocument>("Question");

        static void Main(string[] args)
        {
            MqttClient client = new MqttClient(IPAddress.Parse("18.184.104.180"));
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            var clientId = Guid.NewGuid().ToString();
            client.Connect(clientId);
            client.Subscribe(
                new string[] { "BaltazarBerg"},
                new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
           

            client.Publish("BaltazarBerg", Encoding.UTF8.GetBytes("Oh Baltazar!"));

            Program p = new Program();
            string path = "C:\\Users\\D064979\\Desktop\\json\\json1.json"; 
            p.SaveQuestion(path);
            p.DeleteOneQuestion("true", 2);
            p.ShowDB();
            p.DeleteCollection("Question"); 
                   
        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received, get message as bitarray e -> msg string
            string msg = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            Console.WriteLine("message = " + msg);
        }

        public void DeleteOneQuestion( string attribute, int value)
        {
            Qcollection.DeleteOne(new BsonDocument(attribute, value)); 
        }

        public void DeleteCollection(string name)
        {
            database.DropCollection(name); 
        }

        public void ShowDB() {
            var dbList = client.ListDatabases().ToList();
            Console.WriteLine("The list of databases are :");
            foreach (var item in dbList)
            {
                Console.WriteLine(item);
            }
            Console.ReadKey();
        }

        static void DeleteDB(string name)
        {
            client.DropDatabase(name); 
        }

        public void SaveQuestion ( string file)
        {
            
            string text = System.IO.File.ReadAllText(@file);
            var document = BsonSerializer.Deserialize<BsonDocument>(text);
            Qcollection.InsertOne(document);
        }


        public void oldCode()
        {
            
            //mehrere Strings einlesen Test
            string myjson = System.IO.File.ReadAllText(@"C:\Users\D064979\Desktop\json\json2.json");
            var doc = new BsonDocument {
                    { "values", BsonSerializer.Deserialize<BsonArray>(myjson) }
                };
            //collection.InsertOne(doc);
                                 

            //Test: Document finden bei dem richtige Frage = 2 ist
            //var results = collection.CountDocuments(new BsonDocument("true", 2));
            //Console.WriteLine("{0} ist die Anzahl", results);


        }
      
    }
    
}
