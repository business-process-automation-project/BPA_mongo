﻿using System;
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
        //MongoDB Globale Settings
        public static MongoClient client = new MongoClient("mongodb://localhost:27017");
        public static IMongoDatabase  database = client.GetDatabase("BPA");
        public static IMongoCollection<BsonDocument> Qcollection = database.GetCollection<BsonDocument>("Question");
        public static IMongoCollection<BsonDocument> Pcollection = database.GetCollection<BsonDocument>("Player");
        
        //MQTT Globale Settings mit Brocker und Topics
        public static MqttClient mqtt = new MqttClient(IPAddress.Parse("141.56.180.120"));
        public static String[] topics = {"BPA", "TEST","REGISTER"};

        public static Program p = new Program();
        
        static void Main(string[] args)
        {
            p.SetMQTT(); //MQTT Connection aufbauen
            p.MongoInit(); //MongoDB mit Fragen initialisieren
        }

        //MQTT Client verbinden und Topics subscriben
        public bool SetMQTT(){
            mqtt.MqttMsgPublishReceived += MQTTHandler;        
            mqtt.Connect("Spielmanager","","",false,ushort.MaxValue);

            if(mqtt.IsConnected)
            {
                Console.WriteLine("MQTT connected.");
                byte[] qos = new byte[topics.Length];
                for(int i=0; i < topics.Length;i++)
                {
                    qos[i] = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE;
                }

                mqtt.Subscribe(topics, qos);
                return true;
            } else 
            {
                Console.WriteLine("MQTT NOT connected.");
                return false;
            }
        }

        //MQTT Senden von Nachrichten
        public void MQTTPublish(string channel, string msg)
        {
            mqtt.Publish(channel, Encoding.UTF8.GetBytes(msg));
        }

        //MQTT Verarbeiten von eingegangen Nachrichten auf verschiedenen Topics
        static void MQTTHandler(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            switch (e.Topic)
            {
                case "BPA":
                    Console.WriteLine("I'm a message from topic BPA\t{0}",msg);
                    break;
                case "TEST":
                    Console.WriteLine("I'm a message from topic TEST\t{0}",msg);
                    break;
                case "Luisa":
                    if (msg == "Fragen")
                    {
                        p.MQTTPublish("Luisa",p.SelectQuestion("true", "2"));
                    }
                    break;
                default:
                    Console.WriteLine("Topic {0} is not defined. \tMessage = {1}",e.Topic, msg);
                    break;
            }
        }

        //MongoDB Initiales laden der Fragen in die MongoDB
        public bool MongoInit()
        {
            try
            {
                p.DeleteCollection("Question");
                System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo(@"C:\json");
                foreach (System.IO.FileInfo f in ParentDirectory.GetFiles())
                {
                    p.SaveQuestion(f.FullName.ToString());
                }
                
                long count = Qcollection.Count(new BsonDocument());

                if(count>0)
                {
                    Console.WriteLine("Added {0} questions to the database.",count);
                    return true;
                } else
                {
                    Console.WriteLine("No questions added to the database.",count);
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("MongoDB initialization failed. \nException: {0}",e.Message);
                return false;
            }
        }
 
        //MongoDB Löschen einer Frage
        public void DeleteOneQuestion( string attribute, int value)
        {
            Qcollection.DeleteOne(new BsonDocument(attribute, value)); 
        }

        //MongoDB Löschen einer Collection
        public void DeleteCollection(string name)
        {
            database.DropCollection(name); 
        }

        //MongoDB Löschen einer Datenbank
        static void DeleteDB(string name)
        {
            client.DropDatabase(name); 
        }

        //MongoDB Ausgabe aller Datenbanken auf der MongoDB
        public void ShowDB() {
            var dbList = client.ListDatabases().ToList();
            Console.WriteLine("The list of databases are :");
            foreach (var item in dbList)
            {
                Console.WriteLine(item);
            }
            Console.ReadKey();
        }

        //MongoDB Speichern einer Frage
        public void SaveQuestion (string file)
        {
            string text = System.IO.File.ReadAllText(@file);
            var document = BsonSerializer.Deserialize<BsonDocument>(text);
            Qcollection.InsertOne(document);
        }
        
        //MongoDB Speichern mehrerer Fragen
        public void SaveMultipleQuestions(string name)
        {
            string myjson = System.IO.File.ReadAllText(@name);
            var doc = new BsonDocument {
            { "values", BsonSerializer.Deserialize<BsonArray>(myjson) }
            };
            Qcollection.InsertOne(doc);
        }

        //MongoDB Select einer Frage
        public string SelectQuestion (string att, string numb)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(att, numb);
            var result = Qcollection.Find(filter).ToList();
            foreach (var doc in result)
            {
                Console.WriteLine(doc.ToJson());
            }
            return result.ToString(); 
        }      
    }    
}
