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
        public static MqttClient mqttclient = new MqttClient(IPAddress.Parse("141.56.180.120"));
        public static String channel = new string("BPA");

        static void Main(string[] args)
        {

            mqttclient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            var clientId = Guid.NewGuid().ToString();
            mqttclient.Connect(clientId);
            mqttclient.Subscribe(
                new string[] {channel},
                new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                      

            Program p = new Program();
            //zum Testen 
            p.DeleteCollection("Question");
        
            //Initiales Laden der Fragen aus dem Ordner
            System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo(@"C:\json");
            foreach (System.IO.FileInfo f in ParentDirectory.GetFiles())
            {
                p.SaveQuestion(f.FullName.ToString());
            }


           
                                 
        }

        public void MQTTPublish(string msg)
        {
            mqttclient.Publish(channel, Encoding.UTF8.GetBytes(msg));
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
        
        public void SaveMultipleQuestions(string name)
        {
            string myjson = System.IO.File.ReadAllText(@name);
            var doc = new BsonDocument {
            { "values", BsonSerializer.Deserialize<BsonArray>(myjson) }
            };
            Qcollection.InsertOne(doc);
        }


        public void SelectQuestion (string att, string numb)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(att, numb);
            var result = Qcollection.Find(filter).ToList();
            foreach (var doc in result)
            {
                Console.WriteLine(doc.ToJson());
            }
        }


        public void oldCode()
        {
            
            //mehrere Strings einlesen Test
            //
                                 

            //Test: Document finden bei dem richtige Frage = 2 ist
            //var results = collection.CountDocuments(new BsonDocument("true", 2));
            //Console.WriteLine("{0} ist die Anzahl", results);


        }
      
    }
    
}
