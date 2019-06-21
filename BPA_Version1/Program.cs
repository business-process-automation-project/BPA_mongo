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
        //MongoDB Globale Settings
        public static MongoClient client = new MongoClient("mongodb://localhost:27017");
        public static IMongoDatabase database = client.GetDatabase("BPA");
        public static IMongoCollection<BsonDocument> Qcollection = database.GetCollection<BsonDocument>("Question");
        public static IMongoCollection<BsonDocument> Pcollection = database.GetCollection<BsonDocument>("Player");

        //MQTT Globale Settings mit Brocker und Topics
        public static MqttClient mqtt = new MqttClient(IPAddress.Parse("34.230.40.176"));
        public static String[] topics = { "GetPlayer", "GetScoreboard", "RequestQuestions", "GetWinner" , "ClearSession"};

        public string usedQuestions = "";

        public static Program p = new Program();

        static void Main(string[] args)
        {
            p.SetMQTT(); //MQTT Connection aufbauen
            p.LoadQuestionsToMongo();
            p.LoadPlayerToMongo();

        }

        //MQTT Client verbinden und Topics subscriben
        public bool SetMQTT()
        {
            mqtt.MqttMsgPublishReceived += MQTTHandler;
            mqtt.Connect("Spielmanager_Lennert", "", "", false, ushort.MaxValue);

            if (mqtt.IsConnected)
            {
                Console.WriteLine("MQTT connected.");
                byte[] qos = new byte[topics.Length];
                for (int i = 0; i < topics.Length; i++)
                {
                    qos[i] = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE;
                }

                mqtt.Subscribe(topics, qos);
                return true;
            }
            else
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
                case "Lennert":
                    if (msg == "Fragen")
                    {
                        Qcollection.Find(new BsonDocument()).ForEachAsync(X => Console.WriteLine(X));

                        var filter = Builders<BsonDocument>.Filter.Empty;//Gt( .Eq("Fragen", "Was ist 1+1");
                        var result = Qcollection.Find(filter).ToList();
                        foreach (var doc in result)
                        {
                            var j = doc.ToJson();
                            Console.WriteLine(j);
                            p.MQTTPublish("Lennert", j);
                        }
                    }
                    break;
                case "RequestQuestions":
                    {
                        string result = p.SelectNRandomQuestions(Convert.ToInt32(msg));
                        Console.WriteLine(result);
                        p.MQTTPublish("ResponseQuestions", result);
                        break;
                    }
                    break;
                case "GetPlayer":
                    {
                        try 
                        {
                            p.SaveDocument(Pcollection, msg);
                        }
                        catch (Exception ex)
                        {
                            p.MQTTPublish("Info","Spieler konnte nicht hinzugefügt werden.");
                        }
                        break;
                    }
                    break;
                case "GetScoreboard":
                    {
                        p.SendScoreboard();
                        break;
                    }
                    break;
                case "GetWinner":
                    {
                        p.AddPoints(msg);
                        break;
                    }
                case "ClearSession":
                    {
                        p.LoadQuestionsToMongo();
                        p.DeleteCollection("Player");
                        Console.WriteLine("Session cleaned");
                        break;
                    }
                case "AddQuestion":
                    { 
                        try 
                        {
                            p.SaveDocument(Qcollection, msg);
                            Console.WriteLine("Question added");
                        }
                        catch (Exception ex)
                        {
                            p.MQTTPublish("Info","Frage konnte nicht hinzugefügt werden.");
                        }
                        break;
                    }
                default:
                    Console.WriteLine("Topic {0} is not defined. \tMessage = {1}", e.Topic, msg);
                    break;
            }
        }

        //MongoDB Lade alle Fragen
        public bool LoadQuestionsToMongo()
        {
            try
            {
                p.DeleteCollection("Question");
                System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo(@"C:\json");
                foreach (System.IO.FileInfo f in ParentDirectory.GetFiles())
                {
                    p.SaveDocumentFromFile(Qcollection, f.FullName.ToString());
                }

                long count = Qcollection.Count(new BsonDocument());

                if (count > 0)
                {
                    Console.WriteLine("Added {0} questions to the database.", count);
                }
                else
                {
                    Console.WriteLine("No questions added to the database.", count);
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("MongoDB questions initialization failed. \nException: {0}", e.Message);
                return false;
            }
        }

        //MongoDB Lade alle Spieler
        public bool LoadPlayerToMongo()
        {
            try
            {
                p.DeleteCollection("Player");
                System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo(@"C:\player");
                foreach (System.IO.FileInfo f in ParentDirectory.GetFiles())
                {

                    p.SaveDocumentFromFile(Pcollection, f.FullName.ToString());
                }

                long count = Pcollection.Count(new BsonDocument());

                if (count > 0)
                {
                    Console.WriteLine("Added {0} player to the database.", count);
                }
                else
                {
                    Console.WriteLine("No player added to the database.", count);
                }
                return true;

            }
            catch (Exception e)
            {
                Console.WriteLine("MongoDB player initialization failed. \nException: {0}", e.Message);
                return false;
            }
        }

        //MongoDB Löschen einer Frage
        public void DeleteOneQuestion(string attribute, string value)
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
        public void ShowDB()
        {
            var dbList = client.ListDatabases().ToList();
            Console.WriteLine("The list of databases are :");
            foreach (var item in dbList)
            {
                Console.WriteLine(item);
            }
            Console.ReadKey();
        }

        //MongoDB Speichern eines Dokuments von einer Datei
        public void SaveDocumentFromFile(IMongoCollection<BsonDocument> col, string file)
        {
            string text = System.IO.File.ReadAllText(@file);
            var document = BsonSerializer.Deserialize<BsonDocument>(text);
            col.InsertOne(document);
        }

        //MongoDB Speichern eines Dokuments per MQTT
        public void SaveDocument(IMongoCollection<BsonDocument> col, string msg)
        {
            var document = BsonSerializer.Deserialize<BsonDocument>(msg);
            col.InsertOne(document);
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
        public string SelectQuestion(string att, string numb)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(att, numb);
            var result = Qcollection.Find(filter).ToList();
            var x = "";
            foreach (var doc in result)
            {
                Console.WriteLine(doc.ToJson());
                x = doc.ToJson();
            }
            return x;
        }


        public string SelectNRandomQuestions(int n)
        {
            //Hole alle Fragen aus der Datenbank
            var filter = Builders<BsonDocument>.Filter.Empty;
            var result = Qcollection.Find(filter).ToList();

            //Wie viele Fragen gibt es in der Datenbank
            int count = Convert.ToInt32(Qcollection.Count(new BsonDocument()));
            if (count >= 1)
            {
                //Erzeugung von n Zufallszahlen, die sich nicht doppeln
                Random rnd = new Random();
                int[] randomQuestions = new int[n];
                int dummy;

                randomQuestions[0] = rnd.Next(count);
                for (int x = 1; x < n;)
                {
                    dummy = rnd.Next(count);
                    if (!randomQuestions.Contains(dummy))
                    {
                        randomQuestions[x] = dummy;
                        x++;
                    }
                }
                //Zufallszahlen sortieren
                Array.Sort(randomQuestions);

                //String zusammenbauen
                string resultString = "[";
                //string resultString1 = "[";
                int counter = 0;

                //Alle Dokumente durchlaufen und wenn das x-te Dokument in den Zufallszahlen enthalten ist wird es zum String hinzugefügt
                foreach (var doc in result)
                {
                    if (randomQuestions.Contains(counter))
                    {
                        //Aufbau ohne ObjectID von der MongoDB
                        resultString += "{ \"Question\" : " + doc[1].ToJson() + ", \"Answer\" : ";
                        resultString += doc[2].ToJson() + "},";                 

                        var f = Builders<BsonDocument>.Filter.Eq("_id", doc[0]);
                        var r = Qcollection.DeleteOne(f);
                    }
                    counter++;
                }
                //Löschen des letztens Kommas aus der foreach-Schleife
                resultString = resultString.Substring(0, resultString.Length - 1);
                resultString += "]";

                return resultString;
            }
            else
            {
                Console.WriteLine("No more questions at database. Clean session via message at topic 'ClearSession'");
                return "[]";
            }
        }

        public void SendScoreboard()
        {
            //Hole alle Spieler sortiert nach Punktzahl aus der Datenbank
            var list = Pcollection.Find(new BsonDocument()).Sort(Builders<BsonDocument>.Sort.Descending("score")).ToList();

            string resultString = "[";
            try { 
            foreach (var doc in list)
            {
                //Doc 0 = mongoID 1 = badgeId 2=name 3=age 4=score

                resultString += "{";
                resultString += "\"badgeId\":\"" + doc[1] + "\",";
                resultString += "\"name\":\"" + doc[2] + "\",";
                resultString += "\"age\":" + doc[3] + ",";
                resultString += "\"score\":" + doc[4] + ",";
                resultString += "\"avatar\":" + doc[5] + "},";
            }

            //Löschen des letztens Kommas aus der foreach-Schleife
            resultString = resultString.Substring(0, resultString.Length - 1);
            resultString += "]";

            //für die Übergabe der Rangliste 
            p.MQTTPublish("SetScoreboard", resultString);
            }
            catch (Exception e ) { }
        }

        public void AddPoints(string msg)
        {
            string id;
            int score, avatar ;
            Random rnd = new Random();

            var list = Pcollection.Find(new BsonDocument()).Sort(Builders<BsonDocument>.Sort.Descending("score")).ToList();
            foreach (var doc in list)
            {
                score = 0;
                id = doc[1].AsString;
                //Punkte
                if (msg.Contains(doc[1].AsString))
                {
                    
                    
                     
                    try { score = doc[4].AsInt32 + 1; }
                    catch (Exception e ) { score = 1; }; 
                                   


                }

                //Avatar setzen
                try { avatar = doc[5].AsInt32;}
                catch (Exception e ) 
                {
                    avatar = rnd.Next(44);
                }; 

                var filter = Builders<BsonDocument>.Filter.Eq("badgeId", id);
                var updateScore = Builders<BsonDocument>.Update.Set("score", score);
                var updateAvatar = Builders<BsonDocument>.Update.Set("avatar", avatar);
                Pcollection.UpdateOne(filter, updateScore);
                Pcollection.UpdateOne(filter, updateAvatar);
            }
            p.SendScoreboard(); 
        }
    }
}

